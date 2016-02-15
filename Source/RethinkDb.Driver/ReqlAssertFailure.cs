#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member

using System;

namespace RethinkDb.Driver
{
    public class ReqlAssertFailure : ReqlError
    {
        public ReqlAssertFailure(Exception e) : base(e)
        {
        }

        public ReqlAssertFailure(string message) : base(message)
        {
        }

        public ReqlAssertFailure(string message, Exception innerException) : base(message, innerException)
        {
        }
    }
}