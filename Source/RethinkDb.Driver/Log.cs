using System;
using Common.Logging;

namespace RethinkDb.Driver
{
    public static class Log
    {
        public static ILog Instance = LogManager.GetLogger("RethinkDb.Driver");
    }
}