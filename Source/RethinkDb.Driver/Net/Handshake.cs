using System.IO;
using System.Linq;
using System.Text;
using Newtonsoft.Json.Linq;

namespace RethinkDb.Driver.Net
{
    internal class Handshake
    {
        const Proto.Version Version = Proto.Version.V1_0;
        const long SubProtocolVersion = 0L;
        const Proto.Protocol Protocol = Proto.Protocol.JSON;

        private const string ClientKey = "Client Key";
        private const string ServerKey = "Server Key";

        private string username;
        private string password;

        private IProtocolState state;

        private interface IProtocolState
        {
            IProtocolState NextState(string response);
            byte[] ToSend();
            bool IsFinished { get; }
        }

        public Handshake(string username, string password)
        {
            this.username = username;
            this.password = password;
        }

        class InitialState : IProtocolState
        {
            private readonly string nonce;
            private readonly string username;
            private readonly byte[] password;

            internal InitialState(string username, string password)
            {
                this.username = username;
                this.password = Encoding.UTF8.GetBytes(password);
                this.nonce = Crypto.MakeNonce();
            }

            public IProtocolState NextState(string response)
            {
                if( response != null )
                {
                    throw new ReqlDriverError("Unexpected response");
                }
                // We could use a json serializer, but it's fairly straightforward
                var clientFirstMessageBare = new ScramAttributes()
                    .SetUsername(username)
                    .SetNonce(nonce);

                byte[] jsonBytes = Encoding.UTF8.GetBytes(
                    "{" +
                    "\"protocol_version\":" + SubProtocolVersion + "," +
                    "\"authentication_method\":\"SCRAM-SHA-256\"," +
                    "\"authentication\":" + "\"n,," + clientFirstMessageBare + "\"" +
                    "}"
                    );

                byte[] msg;
                using (var ms = new MemoryStream())
                using (var bw = new BinaryWriter(ms))
                {
                    bw.Write((int)Version);
                    bw.Write(jsonBytes);
                    bw.Write('\0');
                    bw.Flush();
                    msg = ms.ToArray();
                }

                return new WaitingForProtocolRange(
                    nonce, password, clientFirstMessageBare, msg);
            }


            public byte[] ToSend()
            {
                return null;
            }


            public bool IsFinished => false;
        }

        class WaitingForProtocolRange : IProtocolState
        {
            private string nonce;
            private byte[] message;
            private ScramAttributes clientFirstMessageBare;
            private byte[] password;

            internal WaitingForProtocolRange(
                string nonce,
                byte[] password,
                ScramAttributes clientFirstMessageBare,
                byte[] message)
            {
                this.nonce = nonce;
                this.password = password;
                this.clientFirstMessageBare = clientFirstMessageBare;
                this.message = message;
            }


            public IProtocolState NextState(string response)
            {
                var json = JObject.Parse(response);
                ThrowIfFailure(json);
                long minVersion = json["min_protocol_version"].Value<long>();
                long maxVersion = json["max_protocol_version"].Value<long>();
                if( SubProtocolVersion < minVersion || SubProtocolVersion > maxVersion )
                {
                    throw new ReqlDriverError(
                        "Unsupported protocol version " + SubProtocolVersion +
                        ", expected between " + minVersion + " and " + maxVersion);
                }
                return new WaitingForAuthResponse(nonce, password, clientFirstMessageBare);
            }


            public byte[] ToSend()
            {
                return message;
            }


            public bool IsFinished => false;
        }

        private class WaitingForAuthResponse : IProtocolState
        {
            private string nonce;
            private byte[] password;
            private ScramAttributes clientFirstMessageBare;

            public WaitingForAuthResponse(string nonce, byte[] password, ScramAttributes clientFirstMessageBare)
            {
                this.nonce = nonce;
                this.password = password;
                this.clientFirstMessageBare = clientFirstMessageBare;
            }


            public IProtocolState NextState(string response)
            {
                var json = JObject.Parse(response);
                ThrowIfFailure(json);
                string serverFirstMessage = json["authentication"].Value<string>();
                ScramAttributes serverAuth = ScramAttributes.From(serverFirstMessage);
                if( !serverAuth.Nonce.StartsWith(nonce) )
                {
                    throw new ReqlAuthError("Invalid nonce from server");
                }
                ScramAttributes clientFinalMessageWithoutProof = new ScramAttributes()
                    .SetHeaderAndChannelBinding("biws")
                    .SetNonce(serverAuth.Nonce);

                // SaltedPassword := Hi(Normalize(password), salt, i)
                byte[] saltedPassword = Crypto.Pbkdf2(
                    password, serverAuth.Salt, serverAuth.IterationCount);

                // ClientKey := HMAC(SaltedPassword, "Client Key")
                byte[] clientKey = Crypto.Hmac(saltedPassword, ClientKey);

                // StoredKey := H(ClientKey)
                byte[] storedKey = Crypto.Sha256(clientKey);

                // AuthMessage := client-first-message-bare + "," +
                //                server-first-message + "," +
                //                client-final-message-without-proof
                string authMessage =
                    clientFirstMessageBare + "," +
                    serverFirstMessage + "," +
                    clientFinalMessageWithoutProof;

                // ClientSignature := HMAC(StoredKey, AuthMessage)
                byte[] clientSignature = Crypto.Hmac(storedKey, authMessage);

                // ClientProof := ClientKey XOR ClientSignature
                byte[] clientProof = Crypto.Xor(clientKey, clientSignature);

                // ServerKey := HMAC(SaltedPassword, "Server Key")
                byte[] serverKey = Crypto.Hmac(saltedPassword, ServerKey);

                // ServerSignature := HMAC(ServerKey, AuthMessage)
                byte[] serverSignature = Crypto.Hmac(serverKey, authMessage);

                ScramAttributes auth = clientFinalMessageWithoutProof
                    .SetClientProof(clientProof);
                byte[] authJson = Encoding.UTF8.GetBytes("{\"authentication\":\"" + auth + "\"}");

                byte[] message;
                using (var ms = new MemoryStream())
                using (var bw = new BinaryWriter(ms))
                {
                    bw.Write(authJson);
                    bw.Write('\0');
                    bw.Flush();
                    message = ms.ToArray();
                }

                return new WaitingForAuthSuccess(serverSignature, message);
            }


            public byte[] ToSend()
            {
                return null;
            }


            public bool IsFinished => false;
        }

        private class WaitingForAuthSuccess : IProtocolState
        {
            private byte[] serverSignature;
            private byte[] message;

            public WaitingForAuthSuccess(byte[] serverSignature, byte[] message)
            {
                this.serverSignature = serverSignature;
                this.message = message;
            }


            public IProtocolState NextState(string response)
            {
                var json = JObject.Parse(response);
                ThrowIfFailure(json);
                ScramAttributes auth = ScramAttributes
                    .From(json["authentication"].Value<string>());


                if( !auth.ServerSignature.SequenceEqual(serverSignature) )
                {
                    throw new ReqlAuthError("Invalid server signature");
                }
                return new HandshakeSuccess();
            }


            public byte[] ToSend()
            {
                return message;
            }


            public bool IsFinished => false;
        }

        private class HandshakeSuccess : IProtocolState
        {
            public IProtocolState NextState(string response)
            {
                return this;
            }


            public byte[] ToSend()
            {
                return null;
            }


            public bool IsFinished => true;
        }

        static void ThrowIfFailure(JObject json)
        {
            if( !json["success"].Value<bool>() )
            {
                long errorCode = json["error_code"].Value<long>();
                if( errorCode >= 10 && errorCode <= 20 )
                {
                    throw new ReqlAuthError(json["error"].Value<string>());
                }
                else
                {
                    throw new ReqlDriverError(json["error"].Value<string>());
                }
            }
        }

        public void Reset()
        {
            this.state = new InitialState(this.username, this.password);
        }

        public byte[] NextMessage(string response)
        {
            this.state = this.state.NextState(response);
            return this.state.ToSend();
        }

        public bool IsFinished => this.state.IsFinished;
    }
}