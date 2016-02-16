#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member

using System;

namespace RethinkDb.Driver.ReGrid
{
    public class FileNotFoundException : ReGridException
    {
        private static string FormatMessage(Guid id)
        {
            return $"ReGrid file not found: file id {id}.";
        }

        private static string FormatMessage(string filename, int revision)
        {
            return $"ReGrid file not found: revision {revision} of filename \"{filename}\".";
        }

        public FileNotFoundException(Guid id)
            : base(FormatMessage(id))
        {
        }

        public FileNotFoundException(string filename, int revision)
            : base(FormatMessage(filename, revision))
        {
        }
    }
}