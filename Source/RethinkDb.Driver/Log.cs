#if DOTNET
using Microsoft.Extensions.Logging;
#else
using Common.Logging;
#endif

namespace RethinkDb.Driver
{
    public static class Log
    {
#if DOTNET
        public static ILogger Instance = new LoggerFactory().CreateLogger("RethinkDb.Driver");
#else
        public static ILog Instance = LogManager.GetLogger("RethinkDb.Driver");
#endif


        
        public static void Trace(string msg)
        {
#if DOTNET
            Instance.LogDebug(msg);
#else
            Instance.Trace(msg);
#endif
        }

        public static void Debug(string msg)
        {
#if DOTNET
            Instance.LogDebug(msg);
#else
            Instance.Debug(msg);
#endif
        }
    }
}

