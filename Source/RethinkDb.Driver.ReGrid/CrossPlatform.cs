using System;
using System.Security.Cryptography;
#if DNX
using System.Reflection;
#endif

namespace RethinkDb.Driver.ReGrid
{
    /// <summary>
    /// Cross platform SHA 256 Hasher
    /// </summary>
    public class Hasher : IDisposable
    {
#if NETCORE50
        private IncrementalHash hasher;
#else
        private SHA256 hasher;
#endif
        /// <summary>
        /// Constructor
        /// </summary>
        public Hasher()
        {
#if NETCORE50
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
#if NETCORE50
            hasher.AppendData(data);
#else
            hasher.TransformBlock(data, 0, data.Length, null, 0);
#endif
        }

        /// <summary>
        /// Gets the final hash calculation.
        /// </summary>
        public string GetHashAndReset()
        {
#if NETCORE50
            return Util.GetHexString(hasher.GetHashAndReset());
#else
            hasher.TransformFinalBlock(new byte[0], 0, 0);
            return Util.GetHexString(hasher.Hash);
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