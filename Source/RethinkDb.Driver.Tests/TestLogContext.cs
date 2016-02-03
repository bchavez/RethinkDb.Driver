using System.Collections.Generic;
using System.Runtime.Remoting.Messaging;
using System.Text;

namespace RethinkDb.Driver.Tests
{
    public class TestLogContext
    {
        public List<string> OtherLines = new List<string>();

        public override string ToString()
        {
            var sb = new StringBuilder();
            sb.AppendLine();
            sb.AppendLine("Protocol In Context:");
            foreach (var otherLine in OtherLines)
            {
                sb.AppendLine("\t" + otherLine);
            }
            sb.AppendLine();
            sb.AppendLine("PROBLEM:");
            return sb.ToString();
        }

        public static void LogInContext(string message)
        {
            Context?.OtherLines.Add(message);
        }

        public static void ResetContext()
        {
            Context = new TestLogContext();
        }

        public static TestLogContext Context { get; set; }
    }
}