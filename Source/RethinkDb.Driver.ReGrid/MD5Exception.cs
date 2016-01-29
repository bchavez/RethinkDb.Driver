using System;

namespace RethinkDb.Driver.ReGrid
{
    public class MD5Exception : ReGridException
    {
        private static string FormatMessage(Guid id)
        {
            return $"ReGrid MD5 check failed: file id {id}.";
        }

        public MD5Exception(Guid id) : base(FormatMessage(id))
        {
        }
    }
}
