using System;
using System.Security.Cryptography;

namespace RethinkDb.Driver.Net
{
    internal class SecureRandom : IDisposable
    {
#if DNX
        private RandomNumberGenerator random = RandomNumberGenerator.Create();
#else
        private RNGCryptoServiceProvider random = new RNGCryptoServiceProvider();
#endif

        public void GetBytes(byte[] data)
        {
            random.GetBytes(data);
        }

        public void Dispose()
        {
            random.Dispose();
        }
    }
}