#pragma warning disable 1591 // Missing XML comment for publicly visible type or member

using System;
using Newtonsoft.Json.Linq;
using RethinkDb.Driver;

namespace RethinkDb.Driver.Model
{
    public class Backtrace
    {
        public JArray RawBacktrace { get; }

        private Backtrace(JArray rawBacktrace)
        {
            this.RawBacktrace = rawBacktrace;
        }

        public static Backtrace FromJsonArray(JArray rawBacktrace)
        {
            if( rawBacktrace == null || rawBacktrace.Count == 0 )
            {
                return null;
            }
            return new Backtrace(rawBacktrace);
        }
    }
}