using System;

namespace RethinkDb.Driver.ReGrid
{
    public class ChunkException : ReGridException
    {
        private static string FormatMessage(Guid id, long n, string reason)
        {
            return $"ReGrid chunk {n} of file id {id} is {reason}.";
        }

        public ChunkException(Guid id, long n, string reason)
            : base(FormatMessage(id, n, reason))
        {
        }
    }
}
