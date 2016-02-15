#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member

using System;

namespace RethinkDb.Driver.ReGrid
{
    public class SHA256Exception : ReGridException
    {
        private static string FormatMessage(Guid id)
        {
            return $"ReGrid SHA256 check failed: file id {id}.";
        }

        public SHA256Exception(Guid id) : base(FormatMessage(id))
        {
        }
    }
}