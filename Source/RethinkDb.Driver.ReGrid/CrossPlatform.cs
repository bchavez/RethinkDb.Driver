using System;
using System.Net.Sockets;
using System.Reflection;
using System.Security.Cryptography;
#if DNX
using System.Reflection;
#endif

namespace RethinkDb.Driver.ReGrid
{

    public class Hasher : IDisposable
    {
#if NETCORE50
        private IncrementalHash hasher;
#else
        private SHA256 hasher;
#endif
        public Hasher()
        {
#if NETCORE50
            hasher = IncrementalHash.CreateHash(HashAlgorithmName.SHA256);
#else
            hasher = SHA256.Create();
#endif
        }

        public void AppendData(byte[] data)
        {
#if NETCORE50
            hasher.AppendData(data);
#else
            hasher.TransformBlock(data, 0, data.Length, null, 0);
#endif
        }

        public string GetHashAndReset()
        {
#if NETCORE50
            return Util.GetHexString(hasher.GetHashAndReset());
#else
            hasher.TransformFinalBlock(new byte[0], 0, 0);
            return Util.GetHexString(hasher.Hash);
#endif
        }

        public void Dispose()
        {
            hasher.Dispose();
        }
    }


    internal static class ExtensionsForDNX
    {


 
    }
}