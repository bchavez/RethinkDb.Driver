using System;

namespace RethinkDb.Driver.ReGrid
{
    /// <summary>
    /// Utility helper methods for ReGrid.
    /// </summary>
    public static class Util
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
