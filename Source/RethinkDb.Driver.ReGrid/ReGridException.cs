#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member

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