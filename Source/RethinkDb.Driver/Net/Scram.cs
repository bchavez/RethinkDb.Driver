using System;

namespace RethinkDb.Driver.Net
{
    internal class ScramAttributes
    {
        // Getters
        public string AuthIdentity { get; private set; } // a
        public string Username { get; private set; } // n
        public string Nonce { get; private set; } // r

        public string HeaderAndChannelBinding { get; private set; } // c
        public byte[] Salt { get; private set; } // s
        public int IterationCount { get; private set; } // i
        public string ClientProof { get; private set; } // p
        public byte[] ServerSignature { get; private set; } // v
        public string Error { get; private set; } // e

        public string OriginalString = null;

        public static ScramAttributes create()
        {
            return new ScramAttributes();
        }

        static ScramAttributes from(ScramAttributes other)
        {
            var obj = new ScramAttributes
                {
                    AuthIdentity = other.AuthIdentity,
                    Username = other.Username,
                    Nonce = other.Nonce,
                    HeaderAndChannelBinding = other.HeaderAndChannelBinding,
                    Salt = other.Salt,
                    IterationCount = other.IterationCount,
                    ClientProof = other.ClientProof,
                    ServerSignature = other.ServerSignature,
                    Error = other.Error
                };
            return obj;
        }

        static ScramAttributes from(string input)
        {
            var sa = new ScramAttributes
                {
                    OriginalString = input
                };

            foreach( string section in input.Split(','))
            {
                string[] keyVal = section.Split(new[] {'='}, 2);
                sa.setAttribute(keyVal[0], keyVal[1]);
            }
            return sa;
        }

        private void setAttribute(string key, string val)
        {
            switch( key )
            {
                case "a":
                    AuthIdentity = val;
                    break;
                case "n":
                    Username = val;
                    break;
                case "r":
                    Nonce = val;
                    break;
                case "m":
                    throw new ReqlAuthError("m field disallowed");
                case "c":
                    HeaderAndChannelBinding = val;
                    break;
                case "s":
                    Salt = Convert.FromBase64String(val);
                    break;
                case "i":
                    IterationCount = int.Parse(val);
                    break;
                case "p":
                    ClientProof = val;
                    break;
                case "v":
                    ServerSignature = Convert.FromBase64String(val);
                    break;
                case "e":
                    Error = val;
                    break;
                // Supposed to ignore unexpected fields
            }
        }

        public string toString()
        {
            if( !string.IsNullOrWhiteSpace(OriginalString) )
            {
                return OriginalString;
            }
            string output = "";
            if(!string.IsNullOrWhiteSpace(Username) )
            {
                output += ",n=" + Username;
            }
            if(!string.IsNullOrWhiteSpace(Nonce) )
            {
                output += ",r=" + Nonce;
            }
            if(!string.IsNullOrWhiteSpace(HeaderAndChannelBinding) )
            {
                output += ",c=" + HeaderAndChannelBinding;
            }
            if(!string.IsNullOrWhiteSpace(ClientProof) )
            {
                output += ",p=" + ClientProof;
            }
            if( output.StartsWith(",") )
            {
                return output.Substring(1);
            }
            else
            {
                return output;
            }
        }

        // Setters with coercion
        ScramAttributes username(string username)
        {
            ScramAttributes next = ScramAttributes.from(this);
            next.Username = username.Replace("=", "=3D").Replace(",", "=2C");
            return next;
        }

        ScramAttributes nonce(string nonce)
        {
            ScramAttributes next = ScramAttributes.from(this);
            next.Nonce = nonce;
            return next;
        }

        ScramAttributes headerAndChannelBinding(string hacb)
        {
            ScramAttributes next = ScramAttributes.from(this);
            next.HeaderAndChannelBinding = hacb;
            return next;
        }

        ScramAttributes clientProof(byte[] clientProof)
        {
            ScramAttributes next = ScramAttributes.from(this);
            next.ClientProof = Convert.ToBase64String(clientProof);
            return next;
        }


    }
}