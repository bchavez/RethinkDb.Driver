using System.Text;
#if STANDARD
using Microsoft.Extensions.Logging;
#else
using Common.Logging;
#endif

namespace RethinkDb.Driver
{
    /// <summary>
    /// Logger class for the driver.
    /// </summary>
    public static class Log
    {
#if STANDARD
        /// <summary>
        /// RethinkDB Logger
        /// </summary>
        public static ILogger Instance = null;
#else
        /// <summary>
        /// RethinkDB Logger
        /// </summary>
        public static ILog Instance = LogManager.GetLogger("RethinkDb.Driver");
#endif

        /// <summary>
        /// Trace message
        /// </summary>
        public static void Trace(string msg)
        {
#if STANDARD
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
#if STANDARD
            Instance?.LogDebug(msg);
#else
            Instance.Debug(Filter(msg));
#endif
        }

#if STANDARD
        /// <summary>
        /// Enables RehtinkDB Driver Logging
        /// </summary>
        public static void EnableRethinkDbLogging(this ILoggerFactory loggerFactory)
        {
            Instance = loggerFactory.CreateLogger("RethinkDb.Driver");
        }
#endif

        /// <summary>
        /// Truncates BASE64 responses to make logs easier to read. Default true.
        /// </summary>
        public static bool TruncateBinaryTypes = true;

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
    }
}