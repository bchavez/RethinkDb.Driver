using System;

namespace RethinkDb.Driver.ReGrid
{
    public class InvalidPathException : ReGridException
    {
        public InvalidPathException()
        {
        }

        public InvalidPathException(string message) : base(message)
        {
        }

        public InvalidPathException(string message, Exception innerException) : base(message, innerException)
        {
        }
    }
}