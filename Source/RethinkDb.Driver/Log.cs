#if DNX
using Microsoft.Extensions.Logging;
#else
using Common.Logging;
#endif

using System.Text;


namespace RethinkDb.Driver
{
    /// <summary>
    /// Logger class for the driver.
    /// </summary>
    public static class Log
    {
        /// <summary>
        /// Truncates BASE64 responses to make logs easier to read. Default true.
        /// </summary>
        public static bool TruncateBinaryTypes = true;

#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member

#if DNX
        public static ILogger Instance = null;
#else
        public static ILog Instance = LogManager.GetLogger("RethinkDb.Driver");
#endif

#pragma warning restore CS1591 // Missing XML comment for publicly visible type or member

        private static string Filter(string msg, int startAfter = 0)
        {

            if( TruncateBinaryTypes )
            {
                var start = msg.IndexOf(@"{""$reql_type$"":""BINARY"",""data"":""", startAfter);
                if( start == -1 )
                    return msg;

                start += 32;

                var end = msg.IndexOf(@"""}", start);

                var sb = new StringBuilder();
                sb.Append(msg.Substring(0, start));
                sb.Append("BASE64_STRING_TRUNCATED_BY_LOG");
                sb.Append(msg.Substring(end));
                return Filter(sb.ToString(), start);
            }


            return msg;
        }

        /// <summary>
        /// Trace message
        /// </summary>
        public static void Trace(string msg)
        {
#if DNX
            Instance?.LogDebug(msg);
#else
            Instance.Trace(Filter(msg));
#endif
        }

        /// <summary>
        /// Debug message
        /// </summary>
        public static void Debug(string msg)
        {
#if DNX
            Instance?.LogDebug(msg);
#else
            Instance.Debug(Filter(msg));
#endif
        }

#if DNX
        public static void EnableRethinkDbLogging(this ILoggerFactory loggerFactory)
        {
            Instance = loggerFactory.CreateLogger("RethinkDb.Driver");
        }
#endif
    }
}