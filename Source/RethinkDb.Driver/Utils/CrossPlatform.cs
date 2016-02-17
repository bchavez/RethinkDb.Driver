namespace RethinkDb.Driver.Utils
{
    using System.Net.Sockets;

    internal static class ExtensionsForTcpClient
    {
        public static void Shutdown(this TcpClient tcp)
        {
#if DOTNET5_4 || DNXCORE50
            tcp.Dispose();
#else
            tcp.Close();
#endif
        }
    }
}

namespace RethinkDb.Driver
{
#if DNX
    using Microsoft.Extensions.Logging;
#else
    using Common.Logging;
#endif

    public static partial class Log
    {
#if DNX
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