using System;
using System.Security.Cryptography;

namespace RethinkDb.Driver.Net
{
    internal class SecureRandom : IDisposable
    {
        private RandomNumberGenerator random = RandomNumberGenerator.Create();

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