using System;
using RethinkDb.Driver.Utils;

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

        public static ScramAttributes From(ScramAttributes other)
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

        public static ScramAttributes From(string input)
        {
            var sa = new ScramAttributes
                {
                    OriginalString = input
                };

            foreach( string section in input.Split(','))
            {
                string[] keyVal = section.Split(new[] {'='}, 2);
                sa.SetAttribute(keyVal[0], keyVal[1]);
            }
            return sa;
        }

        private void SetAttribute(string key, string val)
        {
            switch( key )
            {
                case "a":
                    this.AuthIdentity = val;
                    break;
                case "n":
                    this.Username = val;
                    break;
                case "r":
                    this.Nonce = val;
                    break;
                case "m":
                    throw new ReqlAuthError("m field disallowed");
                case "c":
                    this.HeaderAndChannelBinding = val;
                    break;
                case "s":
                    this.Salt = Convert.FromBase64String(val);
                    break;
                case "i":
                    this.IterationCount = int.Parse(val);
                    break;
                case "p":
                    this.ClientProof = val;
                    break;
                case "v":
                    this.ServerSignature = Convert.FromBase64String(val);
                    break;
                case "e":
                    this.Error = val;
                    break;
                // Supposed to ignore unexpected fields
            }
        }

        public override string ToString()
        {
            if( this.OriginalString.IsNotNullOrEmpty() )
            {
                return OriginalString;
            }
            string output = "";
            if( this.Username.IsNotNullOrEmpty() )
            {
                output += ",n=" + this.Username;
            }
            if( this.Nonce.IsNotNullOrEmpty() )
            {
                output += ",r=" + this.Nonce;
            }
            if( this.HeaderAndChannelBinding.IsNotNullOrEmpty() )
            {
                output += ",c=" + this.HeaderAndChannelBinding;
            }
            if( this.ClientProof.IsNotNullOrEmpty() )
            {
                output += ",p=" + this.ClientProof;
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
        public ScramAttributes SetUsername(string username)
        {
            ScramAttributes next = ScramAttributes.From(this);
            next.Username = username.Replace("=", "=3D").Replace(",", "=2C");
            return next;
        }

        public ScramAttributes SetNonce(string nonce)
        {
            ScramAttributes next = ScramAttributes.From(this);
            next.Nonce = nonce;
            return next;
        }

        public ScramAttributes SetHeaderAndChannelBinding(string hacb)
        {
            ScramAttributes next = ScramAttributes.From(this);
            next.HeaderAndChannelBinding = hacb;
            return next;
        }

        public ScramAttributes SetClientProof(byte[] clientProof)
        {
            ScramAttributes next = ScramAttributes.From(this);
            next.ClientProof = Convert.ToBase64String(clientProof);
            return next;
        }
    }
}