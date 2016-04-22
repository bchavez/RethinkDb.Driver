using System;

namespace RethinkDb.Driver.Utils
{
    internal static class StringHelper
    {
        /// <summary>
        /// Gets the hex representation of a byte[], in lower case.
        /// </summary>
        public static string GetHexString(byte[] bytes)
        {
            return BitConverter.ToString(bytes).Replace("-", "").ToLowerInvariant();
        }
    }
}