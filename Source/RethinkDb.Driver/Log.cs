using System;
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
        /// Returns true if trace log level is enabled.
        /// </summary>
        public static bool IsTraceEnabled
        {
            get
            {
#if STANDARD
                return Instance?.IsEnabled(LogLevel.Trace) ?? false;
#else
                return Instance.IsTraceEnabled;
#endif
            }
        }

        /// <summary>
        /// Returns true if debug log level is enabled.
        /// </summary>
        public static bool IsDebugEnabled
        {
            get
            {
#if STANDARD
                return Instance?.IsEnabled(LogLevel.Debug) ?? false;
#else
                return Instance.IsDebugEnabled;
#endif
            }
        }


        /// <summary>
        /// Trace message
        /// </summary>
        public static void Trace(string msg)
        {
            if( IsTraceEnabled )
            {
#if STANDARD
                Instance?.LogDebug(Filter(msg));
#else
                Instance.Trace(Filter(msg));
#endif
            }
        }

        /// <summary>
        /// Debug message
        /// </summary>
        public static void Debug(string msg)
        {

            if( IsDebugEnabled )
            {
#if STANDARD
                Instance?.LogDebug(Filter(msg));
#else
                Instance.Debug(Filter(msg));
#endif
            }
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

        internal static string Filter(string msg)
        {
            const string BinaryStart = @"{""$reql_type$"":""BINARY"",""data"":""";
            const string BinaryEnd = @"""}";

            if ( TruncateBinaryTypes )
            {
                int bookmark = 0;

                StringBuilder sb = null;
                while ( bookmark < msg.Length )
                {
                    var match = msg.IndexOf(BinaryStart, bookmark, StringComparison.Ordinal);
                    if( match == -1 && sb == null)
                    {
                        return msg;
                    }
                    if( match != -1 && sb == null )
                    {
                        sb = new StringBuilder();
                    }
                    if( match == -1 && sb != null )
                    {
                        sb.Append(msg.Substring(bookmark));
                        return sb.ToString();
                    }
                    var end = msg.IndexOf(BinaryEnd, match, StringComparison.Ordinal);


                    sb.Append(msg.Substring(bookmark, (match + BinaryStart.Length) - bookmark ));
                    sb.Append("BASE64_STRING_TRUNCATED_BY_LOG");
                    sb.Append(msg.Substring(end, BinaryEnd.Length));
                    bookmark = end + BinaryEnd.Length;
                }
            }

            return msg;
        }
    }
}