#if DNX
using Microsoft.Framework.Logging;
#else
using Common.Logging;
#endif

namespace RethinkDb.Driver
{
    public static class Log
    {
#if DNX
        public static ILogger Instance = null;
#else
        public static ILog Instance = LogManager.GetLogger("RethinkDb.Driver");
#endif


        public static void Trace(string msg)
        {
#if DNX
            Instance?.LogDebug(msg);
#else
            Instance.Trace(msg);
#endif
        }

        public static void Debug(string msg)
        {
#if DNX
            Instance?.LogDebug(msg);
#else
            Instance.Debug(msg);
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