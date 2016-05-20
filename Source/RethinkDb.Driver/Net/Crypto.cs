using System;
using System.Security.Cryptography;
using System.Text;

namespace RethinkDb.Driver.Net
{
    internal class Crypto
    {
        private static readonly SecureRandom secureRandom = new SecureRandom();

        private const int NonceBytes = 18;

        public const int Pbkdf2Iterations = 4096;


        public static byte[] Sha256(byte[] clientKey)
        {
            using( var sha = new IncrementalSHA256() )
            {
                sha.AppendData(clientKey);
                return sha.GetHashAndReset();
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
            /*
            // Algorithm Credits to https://github.com/vexocide
            //
            // Implements PBKDF2WithHmacSHA256 in Java. Beautifully Amazing.
            using (var mac = new HMACSHA256(password))
            {
                mac.TransformBlock(salt, 0, salt.Length, salt, 0);
                byte[] i = { 0, 0, 0, 1 };
                mac.TransformFinalBlock(i, 0, i.Length);
                byte[] t = mac.Hash;
                mac.Initialize();

                byte[] u = t;
                for (uint c = 2; c <= iterations; c++)
                {
                    t = mac.ComputeHash(t);
                    for (int j = 0; j < mac.HashSize / 8; j++)
                    {
                        u[j] ^= t[j];
                    }
                }

                return u;
            }
*/
#if STANDARD
            using( var macSalt = IncrementalHash.CreateHMAC(HashAlgorithmName.SHA256, password) )
#endif
            using ( var mac = new HMACSHA256(password) )
            {
#if STANDARD
                macSalt.AppendData(salt);
#else
                mac.TransformBlock(salt, 0, salt.Length, salt, 0);
#endif
                byte[] i = {0, 0, 0, 1};
#if STANDARD
                macSalt.AppendData(i);
#else
                mac.TransformFinalBlock(i, 0, i.Length);
#endif
#if STANDARD
                byte[] t = macSalt.GetHashAndReset();
#else
                byte[] t = mac.Hash;
                mac.Initialize();
#endif

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
            byte[] result = new byte[a.Length];
            for( var i = 0; i < result.Length; i++ )
                result[i] = (byte)(a[i] ^ b[i]);

            return result;
        }
    }
}