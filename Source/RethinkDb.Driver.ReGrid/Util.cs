using System;

namespace RethinkDb.Driver.ReGrid
{
    public static class Util
    {
        public static string GetHexString(byte[] bytes)
        {
            return BitConverter.ToString(bytes).Replace("-", "").ToLowerInvariant();
        }
    }
}
