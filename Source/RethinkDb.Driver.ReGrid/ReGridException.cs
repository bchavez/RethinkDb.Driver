using System;

namespace RethinkDb.Driver.ReGrid
{
    public class ReGridException : Exception
    {
        public ReGridException()
        {
        }

        public ReGridException(string message) : base(message)
        {
        }

        public ReGridException(string message, Exception innerException) : base(message, innerException)
        {
        }
    }
}
