using System.Collections.Generic;
using System.Text;
using Newtonsoft.Json;

namespace RethinkDb.Driver.Tests
{
    public class YamlTestContext
    {
        public string TestFile { get; set; }
        public string LineNum { get; set; }
        public string ExpectedOriginal { get; set; }
        public string ExpectedJava { get; set; }
        public string Original { get; set; }
        public string Java { get; set; }
        public List<RunOpt> RunOpts { get; set; }
        public List<string> OtherLines = new List<string>();

        public class RunOpt
        {
            public string Key { get; set; }
            public string Val { get; set; }
        }

        public override string ToString()
        {
            var sb = new StringBuilder();
            sb.AppendLine();
            sb.AppendLine($"Test:");
            sb.AppendLine($"\t{TestFile}, LineNum: {LineNum}");
            sb.AppendLine("Expected ReQL:");
            sb.AppendLine($"\t{ExpectedOriginal}");
            sb.AppendLine("Expected C#/Java:");
            sb.AppendLine($"\t{ExpectedJava}");
            sb.AppendLine("Run ReQL:");
            sb.AppendLine($"\t{Original}");
            sb.AppendLine("Run C#/Java:");
            sb.AppendLine($"\t{Java}");
            sb.AppendLine($"RunOpts: {JsonConvert.SerializeObject(RunOpts)}");
            sb.AppendLine("Log In Context:");
            foreach( var otherLine in OtherLines )
            {
                sb.AppendLine("\t" + otherLine);
            }
            sb.AppendLine();
            sb.AppendLine("PROBLEM:");
            return sb.ToString();
        }
    }
}