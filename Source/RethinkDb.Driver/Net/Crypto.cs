using System;
using System.Collections;
using System.Security.Cryptography;

using System.Globalization;
using System.IO;
using System.Text;


namespace RethinkDb.Driver.Net
{
    internal class Crypto
    {
        private static string DEFAULT_SSL_PROTOCOL = "TLSv1.2";

        private static readonly RNGCryptoServiceProvider secureRandom = new RNGCryptoServiceProvider();

        private const int NonceBytes = 18;

        public const int Pbkdf2Iterations = 4096;


        public static byte[] Sha256(byte[] clientKey)
        {
            using( var sha = SHA256.Create() )
            {
                return sha.ComputeHash(clientKey);
            }
        }

        public static byte[] Hmac(byte[] key, string str)
        {
            using( var mac = new HMACSHA256(key) )
            {
                return mac.ComputeHash(Encoding.UTF8.GetBytes(str));
            }
        }

        public static byte[] Pbkdf2(byte[] password, byte[] salt, int iterations = Pbkdf2Iterations)
        {
            // Algorithm Credits to https://github.com/vexocide
            //
            // Implements PBKDF2WithHmacSHA256 in Java. Beautifully Amazing.
            using ( var mac = new HMACSHA256(password) )
            {
                mac.TransformBlock(salt, 0, salt.Length, salt, 0);
                byte[] i = {0, 0, 0, 1};
                mac.TransformFinalBlock(i, 0, i.Length);
                byte[] t = mac.Hash;
                mac.Initialize();

                byte[] u = t;
                for( uint c = 2; c <= iterations; c++ )
                {
                    t = mac.ComputeHash(t);
                    for( int j = 0; j < mac.HashSize / 8; j++ )
                    {
                        u[j] ^= t[j];
                    }
                }

                return u;
            }
        }

        public static string MakeNonce()
        {
            byte[] rawNonce = new byte[NonceBytes];
            secureRandom.GetBytes(rawNonce);
            return Convert.ToBase64String(rawNonce);
        }

        public static byte[] Xor(byte[] a, byte[] b)
        {
            var ba = new BitArray(a);
            var bb = new BitArray(b);
            byte[] result = new byte[a.Length];
            ba.Xor(bb).CopyTo(result, 0);
            return result;
        }
    }
}