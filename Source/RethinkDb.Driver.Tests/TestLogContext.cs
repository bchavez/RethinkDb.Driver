using System.Collections.Generic;
using System.Runtime.Remoting.Messaging;
using System.Text;
using NLog;
using NLog.Targets;

namespace RethinkDb.Driver.Tests
{
    public static class TestLogContext
    {
        private static MemoryTarget memoryTarget;

        public static string LogText()
        {
            var sb = new StringBuilder();
            sb.AppendLine();
            sb.AppendLine("Protocol In Context:");
            foreach (var otherLine in memoryTarget.Logs)
            {
                sb.AppendLine("\t" + otherLine);
            }
            sb.AppendLine();
            sb.AppendLine("PROBLEM:");
            return sb.ToString();
        }

        public static void ResetContext()
        {
            memoryTarget = LogManager.Configuration.FindTargetByName<MemoryTarget>("memory");
            memoryTarget.Logs.Clear();
        }
    }
}