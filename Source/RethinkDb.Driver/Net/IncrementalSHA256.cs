using System;
using System.Security.Cryptography;
using RethinkDb.Driver.Utils;

namespace RethinkDb.Driver.Net
{
    /// <summary>
    /// Cross platform SHA 256 Hasher
    /// </summary>
    public class IncrementalSHA256 : IDisposable
    {
#if DOTNET5_4 || DNXCORE50
        private IncrementalHash hasher;
#else
        private SHA256 hasher;
#endif

        /// <summary>
        /// Constructor
        /// </summary>
        public IncrementalSHA256()
        {
#if DOTNET5_4 || DNXCORE50
            hasher = IncrementalHash.CreateHash(HashAlgorithmName.SHA256);
#else
            hasher = SHA256.Create();
#endif
        }

        /// <summary>
        /// Updates the hash value
        /// </summary>
        public void AppendData(byte[] data)
        {
#if DOTNET5_4 || DNXCORE50
            hasher.AppendData(data);
#else
            hasher.TransformBlock(data, 0, data.Length, null, 0);
#endif
        }

        /// <summary>
        /// Gets the final hash calculation in hex string.
        /// </summary>
        public string GetHashStringAndReset()
        {
            return StringHelper.GetHexString(GetHashAndReset());
        }

        /// <summary>
        /// Gets the final hash calculation as byte array.
        /// </summary>
        public byte[] GetHashAndReset()
        {
#if DOTNET5_4 || DNXCORE50
            return hasher.GetHashAndReset();
#else
            hasher.TransformFinalBlock(new byte[0], 0, 0);
            return hasher.Hash;
#endif
        }

        /// <summary>
        /// Disposes the hasher.
        /// </summary>
        public void Dispose()
        {
            hasher.Dispose();
        }
    }
}