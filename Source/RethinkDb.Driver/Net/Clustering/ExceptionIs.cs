using System;
using System.IO;

namespace RethinkDb.Driver.Net.Clustering
{
    internal static class ExceptionIs
    {
        public static bool NetworkError(Exception e)
        {
            return e is IOException || e is ObjectDisposedException;
        }
    }
}