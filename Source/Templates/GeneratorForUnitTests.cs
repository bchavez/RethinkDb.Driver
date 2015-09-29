using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Text;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using NUnit.Framework;
using Templates.CodeGen.Util;
using Templates.UnitTests;
using Templates.Utils;
using YamlDotNet.Serialization;
using Z.ExtensionMethods;

namespace Templates
{
    public class YamlTest
    {
        public string ModuleName { get; set; }
        public string[] TableVarNames { get; set; }
        public DefTest[] DefsAndTests { get; set; }

        [YamlMember(Alias = "render_something")]
        public bool RenderSomething { get; set; }

        public class DefTest
        {
            public string TestType { get; set; }
            public string TestFile { get; set; }
            public int TestNum { get; set; }
            public string Original { get; set; }
            public string Java { get; set; }
            public string ExpectedType { get; set; }
            public string ExpectedJava { get; set; }
            public string Obtained { get; set; }
            public List<RunOpt> RunOpts { get; set; }
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
            }
        }
    }


    [TestFixture]
	public class GeneratorForUnitTests
	{
		private const string YamlImportDir = "../../UnitTests";
		private const string JsonOutputDir = "../../UnitTests";


	    [Test]
        [Explicit]
	    public void CleanUpYamlTests()
	    {
            var ser = new Serializer();
	        var dser = new Deserializer();

            var files = GetAllYamlFiles();
	        foreach( var file in files )
	        {
	            var lines = File.ReadAllLines(file);
	            var sw = new StringWriter(new StringBuilder());
	            for( int i = 0; i < lines.Length; i++ )
	            {
	                var line = lines[i];
                    if( line.StartsWith("//"))
                        continue;

	                sw.WriteLine(line);
	            }

	            var sr = new StringReader(sw.ToString());
	            var yamltests = dser.Deserialize<YamlTest>(sr);
	            yamltests.Decode();

	            var sfile = new StringWriter(new StringBuilder());

	            ser.Serialize(sfile, yamltests);

	            File.WriteAllText(file, sfile.ToString());
	        }

	    }

		[Test]
		[Explicit]
		public void ImportYamlTestsAndConvertToJson()
		{
			var files = GetAllYamlFiles();
			var json = new Serializer(SerializationOptions.JsonCompatible);

			foreach( var file in files )
			{
			    Console.WriteLine("reading: " + file);

			    var sr = new StringReader(File.ReadAllText(file));

                var d = new Deserializer();
				//var yobj = d.Deserialize(sr);
			    var yobj = d.Deserialize<YamlTest>(sr);

			    yobj.Decode();

			    OutputToJson(yobj, json, file);
			}
		}

        private void OutputToJson(YamlTest yobj, Serializer js, string file)
        {
            var sb = new StringBuilder();
            var sw = new StringWriter(sb);
            js.Serialize(sw, yobj);

            var relPath = file;

            var jsonPath = Path.Combine(JsonOutputDir, relPath);
            var jsonFullPath = Path.GetFullPath(jsonPath);
            jsonFullPath = Path.ChangeExtension(jsonFullPath, ".json");

            var dirPath = Path.GetDirectoryName(jsonFullPath);
            if (!Directory.Exists(dirPath))
                Directory.CreateDirectory(dirPath);


            var json = JObject.Parse(sw.ToString()).ToString(Formatting.Indented);
            File.WriteAllText(jsonFullPath, json);
        }


		private string[] GetAllYamlFiles()
		{
			var dir = Path.GetFullPath(YamlImportDir);

			var allTests = Directory.GetFiles(dir, "*.yaml", SearchOption.AllDirectories);

			return allTests.ToArray();
		}
	}
}