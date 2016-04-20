using RethinkDb.Driver.Proto;

namespace RethinkDb.Driver.Net
{
    public class Handshake
    {
        static Version VERSION = Version.V1_0;
        static long SUB_PROTOCOL_VERSION = 0L;
        static Protocol PROTOCOL = Protocol.JSON;

        private static string CLIENT_KEY = "Client Key";
        private static string SERVER_KEY = "Server Key";

        private string username;
        private string password;
        private ProtocolState state;

        private interface ProtocolState
        {
            ProtocolState nextState(string response);
            Optional<ByteBuffer> toSend();
            bool isFinished();
        }

        private class InitialState : ProtocolState
        {
            private string nonce;
            private string username;
            private byte[] password;

            InitialState(string username, string password)
            {
                this.username = username;
                this.password = toUTF8(password);
                this.nonce = makeNonce();
            }


            public ProtocolState nextState(string response)
            {
                if( response != null )
                {
                    throw new ReqlDriverError("Unexpected response");
                }
                // We could use a json serializer, but it's fairly straightforward
                ScramAttributes clientFirstMessageBare = ScramAttributes.create()
                    .username(username)
                    .nonce(nonce);
                byte[] jsonBytes = toUTF8(
                    "{" +
                    "\"protocol_version\":" + SUB_PROTOCOL_VERSION + "," +
                    "\"authentication_method\":\"SCRAM-SHA-256\"," +
                    "\"authentication\":" + "\"n,," + clientFirstMessageBare + "\"" +
                    "}"
                    );
                ByteBuffer msg = Util.leByteBuffer(
                    Integer.BYTES + // size of VERSION
                    jsonBytes.length + // json auth payload
                    1 // terminating null byte
                    ).putInt(VERSION.value)
                    .put(jsonBytes)
                    .put(new byte[1]);
                return new WaitingForProtocolRange(
                    nonce, password, clientFirstMessageBare, msg);
            }


            public Optional<ByteBuffer> toSend()
            {
                return Optional.empty();
            }


            public bool isFinished()
            {
                return false;
            }
        }

        private class WaitingForProtocolRange : ProtocolState
        {
            private string nonce;
            private ByteBuffer message;
            private ScramAttributes clientFirstMessageBare;
            private byte[] password;

            WaitingForProtocolRange(
                string nonce,
                byte[] password,
                ScramAttributes clientFirstMessageBare,
                ByteBuffer message)
            {
                this.nonce = nonce;
                this.password = password;
                this.clientFirstMessageBare = clientFirstMessageBare;
                this.message = message;
            }


            public ProtocolState nextState(string response)
            {
                JSONObject json = toJSON(response);
                throwIfFailure(json);
                long minVersion = (long)json.get("min_protocol_version");
                long maxVersion = (long)json.get("max_protocol_version");
                if( SUB_PROTOCOL_VERSION < minVersion || SUB_PROTOCOL_VERSION > maxVersion )
                {
                    throw new ReqlDriverError(
                        "Unsupported protocol version " + SUB_PROTOCOL_VERSION +
                        ", expected between " + minVersion + " and " + maxVersion);
                }
                return new WaitingForAuthResponse(nonce, password, clientFirstMessageBare);
            }


            public Optional<ByteBuffer> toSend()
            {
                return Optional.of(message);
            }


            public bool isFinished()
            {
                return false;
            }
        }

        private class WaitingForAuthResponse : ProtocolState
        {
            private string nonce;
            private byte[] password;
            private ScramAttributes clientFirstMessageBare;

            WaitingForAuthResponse(
                string nonce, byte[] password, ScramAttributes clientFirstMessageBare)
            {
                this.nonce = nonce;
                this.password = password;
                this.clientFirstMessageBare = clientFirstMessageBare;
            }


            public ProtocolState nextState(string response)
            {
                JSONObject json = toJSON(response);
                throwIfFailure(json);
                string serverFirstMessage = (string)json.get("authentication");
                ScramAttributes serverAuth = ScramAttributes.from(serverFirstMessage);
                if( !serverAuth.nonce().startsWith(nonce) )
                {
                    throw new ReqlAuthError("Invalid nonce from server");
                }
                ScramAttributes clientFinalMessageWithoutProof = ScramAttributes.create()
                    .headerAndChannelBinding("biws")
                    .nonce(serverAuth.nonce());

                // SaltedPassword := Hi(Normalize(password), salt, i)
                byte[] saltedPassword = pbkdf2(
                    password, serverAuth.salt(), serverAuth.iterationCount());

                // ClientKey := HMAC(SaltedPassword, "Client Key")
                byte[] clientKey = hmac(saltedPassword, CLIENT_KEY);

                // StoredKey := H(ClientKey)
                byte[] storedKey = sha256(clientKey);

                // AuthMessage := client-first-message-bare + "," +
                //                server-first-message + "," +
                //                client-final-message-without-proof
                string authMessage =
                    clientFirstMessageBare + "," +
                    serverFirstMessage + "," +
                    clientFinalMessageWithoutProof;

                // ClientSignature := HMAC(StoredKey, AuthMessage)
                byte[] clientSignature = hmac(storedKey, authMessage);

                // ClientProof := ClientKey XOR ClientSignature
                byte[] clientProof = xor(clientKey, clientSignature);

                // ServerKey := HMAC(SaltedPassword, "Server Key")
                byte[] serverKey = hmac(saltedPassword, SERVER_KEY);

                // ServerSignature := HMAC(ServerKey, AuthMessage)
                byte[] serverSignature = hmac(serverKey, authMessage);

                ScramAttributes auth = clientFinalMessageWithoutProof
                    .clientProof(clientProof);
                byte[] authJson = toUTF8("{\"authentication\":\"" + auth + "\"}");
                ByteBuffer message = Util.leByteBuffer(authJson.length + 1)
                    .put(authJson)
                    .put(new byte[1]);
                return new WaitingForAuthSuccess(serverSignature, message);
            }


            public Optional<ByteBuffer> toSend()
            {
                return Optional.empty();
            }


            public bool isFinished()
            {
                return false;
            }
        }

        private class WaitingForAuthSuccess : ProtocolState
        {
            private byte[] serverSignature;
            private ByteBuffer message;

            public WaitingForAuthSuccess(byte[] serverSignature, ByteBuffer message)
            {
                this.serverSignature = serverSignature;
                this.message = message;
            }


            public ProtocolState nextState(string response)
            {
                JSONObject json = toJSON(response);
                throwIfFailure(json);
                ScramAttributes auth = ScramAttributes
                    .from((string)json.get("authentication"));
                if( !MessageDigest.isEqual(auth.serverSignature(), serverSignature) )
                {
                    throw new ReqlAuthError("Invalid server signature");
                }
                return new HandshakeSuccess();
            }


            public Optional<ByteBuffer> toSend()
            {
                return Optional.of(message);
            }


            public bool isFinished()
            {
                return false;
            }
        }

        private class HandshakeSuccess : ProtocolState
        {


            public ProtocolState nextState(string response)
            {
                return this;
            }


            public Optional<ByteBuffer> toSend()
            {
                return Optional.empty();
            }


            public bool isFinished()
            {
                return true;
            }
        }

        private void throwIfFailure(JSONObject json)
        {
            if( !(bool)json.get("success") )
            {
                long errorCode = (long)json.get("error_code");
                if( errorCode >= 10 && errorCode <= 20 )
                {
                    throw new ReqlAuthError((string)json.get("error"));
                }
                else
                {
                    throw new ReqlDriverError((string)json.get("error"));
                }
            }
        }

        public Handshake(string username, string password)
        {
            this.username = username;
            this.password = password;
            this.state = new InitialState(username, password);
        }

        public void reset()
        {
            this.state = new InitialState(this.username, this.password);
        }

        public Optional<ByteBuffer> nextMessage(string response)
        {
            this.state = this.state.nextState(response);
            return this.state.toSend();
        }

        public bool isFinished()
        {
            return this.state.isFinished();
        }

    }
}