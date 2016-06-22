#pragma warning disable 1591 // Missing XML comment for publicly visible type or member

using System;
using RethinkDb.Driver.Net.JsonConverters;

namespace RethinkDb.Driver.Ast
{
    public partial class TopLevel
    {
        /// <summary>
        /// Type-safe helper method for R.Iso8601
        /// </summary>
        public Iso8601 Iso8601(DateTime? datetime)
        {
            var str = datetime?.ToString("o");
            return Ast.Iso8601.FromString(str);
        }
        /// <summary>
        /// Type-safe helper method for R.Iso8601
        /// </summary>
        public Iso8601 Iso8601(DateTimeOffset? datetime)
        {
            var str = datetime?.ToString("o");
            return Ast.Iso8601.FromString(str);
        }

        /// <summary>
        /// Type-safe helper for R.EpochTime
        /// </summary>
        public EpochTime EpochTime(DateTime? datetime)
        {
            var ticks = datetime?.ToUniversalTime().Ticks;
            var epoch = ReqlDateTimeConverter.ToUnixTime(ticks.Value);
            return EpochTime(epoch);
        }
        /// <summary>
        /// Type-safe helper for R.EpochTime
        /// </summary>
        public EpochTime EpochTime(DateTimeOffset? datetime)
        {
            var ticks = datetime?.UtcTicks;
            var epoch = ReqlDateTimeConverter.ToUnixTime(ticks.Value);
            return EpochTime(epoch);
        }

        public ReqlRawExpr FromRawString(string reqlRawString)
        {
            var rawProtocol = ReqlRaw.HidrateProtocolString(reqlRawString);
            return new ReqlRawExpr(rawProtocol);
        }
    }
}