using System;
using System.Collections.Generic;
using System.Text;
using YamlDotNet.Serialization;
using Z.ExtensionMethods;

namespace Templates
{
    public class YamlTest
    {
        public string ModuleName { get; set; }
        public string[] TableVarNames { get; set; } = {};
        public DefTest[] DefsAndTests { get; set; } = {};

        [YamlMember(Alias = "render_something")]
        public bool RenderSomething { get; set; }

        public class DefTest
        {
            public string TestType { get; set; }
            public string TestFile { get; set; }
            public int TestNum { get; set; }
            public string Original { get; set; }
            public string Java { get; set; }
            public string ExpectedOriginal { get; set; }
            public string ExpectedType { get; set; }
            public string ExpectedJava { get; set; }
            public string Obtained { get; set; }
            public List<RunOpt> RunOpts { get; set; } = new List<RunOpt> {};
            public bool RenderSomething { get; set; }
        }

        public class RunOpt
        {
            public string Key { get; set; }
            public string Val { get; set; }
        }

        public void Decode()
        {
            //runs decoding on all encoded b64 encoded values
            //to avoid dealing with yaml ', :, and " edge cases
            // yaml reparses : after it's been used once
            //and 

            if( DefsAndTests == null ) return;
            
            foreach( var test in DefsAndTests )
            {
                var value = test.Original;
                if( value.IsNotNullOrWhiteSpace() )
                {
                    test.Original =
                        Encoding.UTF8.GetString(Convert.FromBase64String(value.Substring(1).Trim('\'')));
                }

                value = test.ExpectedJava;
                if( value.IsNotNullOrWhiteSpace() )
                {
                    test.ExpectedJava =
                        Encoding.UTF8.GetString(Convert.FromBase64String(value.Substring(1).Trim('\'')));
                }

                value = test.ExpectedOriginal;
                if (value.IsNotNullOrWhiteSpace())
                {
                    test.ExpectedOriginal =
                        Encoding.UTF8.GetString(Convert.FromBase64String(value.Substring(1).Trim('\'')));
                }
            }
        }
    }
}