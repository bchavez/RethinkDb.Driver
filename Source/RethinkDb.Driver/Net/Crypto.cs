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
    



    [System.Runtime.InteropServices.ComVisible(true)]
    internal class PBKDF2WithHmacSHA256 : DeriveBytes
    {
        private byte[] m_buffer;
        private byte[] m_salt;
        private HMACSHA256 m_HMACSHA256;  // The pseudo-random generator function used in PBKDF2

        private uint m_iterations;
        private uint m_block;
        private int m_startIndex;
        private int m_endIndex;
        private static RNGCryptoServiceProvider _rng;
        private static RNGCryptoServiceProvider StaticRandomNumberGenerator
        {
            get
            {
                if (_rng == null)
                {
                    _rng = new RNGCryptoServiceProvider();
                }
                return _rng;
            }
        }

        private const int BlockSize = 20;

        public PBKDF2WithHmacSHA256(string password, int saltSize) : this(password, saltSize, 1000) { }

        public PBKDF2WithHmacSHA256(string password, int saltSize, int iterations)
        {
            if (saltSize < 0)
                throw new ArgumentOutOfRangeException("saltSize");

            byte[] salt = new byte[saltSize];
            StaticRandomNumberGenerator.GetBytes(salt);

            Salt = salt;
            IterationCount = iterations;
            m_HMACSHA256 = new HMACSHA256(new UTF8Encoding(false).GetBytes(password));
            Initialize();
        }

        public PBKDF2WithHmacSHA256(string password, byte[] salt) : this(password, salt, 1000) { }

        public PBKDF2WithHmacSHA256(string password, byte[] salt, int iterations) : this(new UTF8Encoding(false).GetBytes(password), salt, iterations) { }

        public PBKDF2WithHmacSHA256(byte[] password, byte[] salt, int iterations)
        {
            Salt = salt;
            IterationCount = iterations;
            m_HMACSHA256 = new HMACSHA256(password);
            Initialize();
        }

        public int IterationCount
        {
            get { return (int)m_iterations; }
            set
            {
                if (value <= 0)
                    throw new ArgumentOutOfRangeException("value");
                m_iterations = (uint)value;
                Initialize();
            }
        }

        public byte[] Salt
        {
            get { return (byte[])m_salt.Clone(); }
            set
            {
                if (value == null)
                    throw new ArgumentNullException("value");
                if (value.Length < 8)
                    throw new ArgumentException();
                m_salt = (byte[])value.Clone();
                Initialize();
            }
        }

        public override byte[] GetBytes(int cb)
        {
            if (cb <= 0)
                throw new ArgumentOutOfRangeException("cb");
            byte[] password = new byte[cb];

            int offset = 0;
            int size = m_endIndex - m_startIndex;
            if (size > 0)
            {
                if (cb >= size)
                {
                    Buffer.BlockCopy(m_buffer, m_startIndex, password, 0, size);
                    m_startIndex = m_endIndex = 0;
                    offset += size;
                }
                else
                {
                    Buffer.BlockCopy(m_buffer, m_startIndex, password, 0, cb);
                    m_startIndex += cb;
                    return password;
                }
            }

            while (offset < cb)
            {
                byte[] T_block = Func();
                int remainder = cb - offset;
                if (remainder > BlockSize)
                {
                    Buffer.BlockCopy(T_block, 0, password, offset, BlockSize);
                    offset += BlockSize;
                }
                else
                {
                    Buffer.BlockCopy(T_block, 0, password, offset, remainder);
                    offset += remainder;
                    Buffer.BlockCopy(T_block, remainder, m_buffer, m_startIndex, BlockSize - remainder);
                    m_endIndex += (BlockSize - remainder);
                    return password;
                }
            }
            return password;
        }

        public override void Reset()
        {
            Initialize();
        }

        private void Initialize()
        {
            if (m_buffer != null)
                Array.Clear(m_buffer, 0, m_buffer.Length);
            m_buffer = new byte[BlockSize];
            m_block = 1;
            m_startIndex = m_endIndex = 0;
        }
        internal static byte[] Int(uint i)
        {
            byte[] b = BitConverter.GetBytes(i);
            byte[] littleEndianBytes = { b[3], b[2], b[1], b[0] };
            return BitConverter.IsLittleEndian ? littleEndianBytes : b;
        }
        // This function is defined as follow : 
        // Func (S, i) = HMAC(S || i) | HMAC2(S || i) | ... | HMAC(iterations) (S || i)
        // where i is the block number. 
        private byte[] Func()
        {
            byte[] INT_block = Int(m_block);

            m_HMACSHA256.TransformBlock(m_salt, 0, m_salt.Length, m_salt, 0);
            m_HMACSHA256.TransformFinalBlock(INT_block, 0, INT_block.Length);
            byte[] temp = m_HMACSHA256.Hash;
            m_HMACSHA256.Initialize();

            byte[] ret = temp;
            for (int i = 2; i <= m_iterations; i++)
            {
                temp = m_HMACSHA256.ComputeHash(temp);
                for (int j = 0; j < BlockSize; j++)
                {
                    ret[j] ^= temp[j];
                }
            }

            // increment the block count.
            m_block++;
            return ret;
        }
    }
}