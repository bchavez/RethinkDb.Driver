using System;
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
	[TestFixture]
	public class GeneratorForUnitTests
	{
		[TestFixtureSetUp]
		public void BeforeRunningTestSession()
		{

		}

		[TestFixtureTearDown]
		public void AfterRunningTestSession()
		{

		}


		[SetUp]
		public void BeforeEachTest()
		{

		}

		[TearDown]
		public void AfterEachTest()
		{

		}

		private const string YamlImportDir = "../../UnitTests";
		private const string JsonOutputDir = "../../UnitTests";


	    [Test]
	    public void clean_up_yaml_tests()
	    {
	        var files = GetAllYamlFiles();
	        foreach( var file in files )
	        {
	            var lines = File.ReadAllLines(file);
	            for( int i = 0; i < lines.Length; i++ )
	            {
	                var line = lines[i];
	                if( line.IndexOf(':') != line.LastIndexOf(':'))
	                {
	                    if( line.IndexOf('"') > 0 )
	                    {
                            //line has " and : so enclose it with ''
	                        lines[i] = $"{line.GetBefore(":")}: '{line.GetAfter(":").Trim().Replace("'",@"""")}'";
	                    }
                        else if( line.IndexOf('\'') > 0 )
                        {
                            // line has ' and : so enclsoe with "
                            lines[i] = $"{line.GetBefore(":")}: \"{line.GetAfter(":").Trim().Replace(@"""", "'")}\"";
                        }
	                }
	            }
	            File.WriteAllLines(file, lines.Skip(4));
	        }
	    }

		[Test]
		[Explicit]
		public void ImportYamlTestsAndConvertToJson()
		{
			var files = GetAllYamlFiles();
			var js = new Serializer(SerializationOptions.JsonCompatible);

			foreach( var file in files )
			{
			    Console.WriteLine("reading: " + file);

			    var sr = new StringReader(File.ReadAllText(file));

                var d = new Deserializer();
				var yobj = d.Deserialize(sr);

				var sb = new StringBuilder();
				var sw = new StringWriter(sb);
				js.Serialize(sw, yobj);

				var relPath = file;

				var jsonPath = Path.Combine(JsonOutputDir, relPath);
				var jsonFullPath = Path.GetFullPath(jsonPath);
				jsonFullPath = Path.ChangeExtension(jsonFullPath, ".json");

				var dirPath = Path.GetDirectoryName(jsonFullPath);
				if( !Directory.Exists(dirPath) )
					Directory.CreateDirectory(dirPath);


				var json = JObject.Parse(sw.ToString()).ToString(Formatting.Indented);
				File.WriteAllText(jsonFullPath, json);
			}
		}


		private string[] GetAllYamlFiles()
		{
			var dir = Path.GetFullPath(YamlImportDir);

			var allTests = Directory.GetFiles(dir, "*.yaml", SearchOption.AllDirectories);

			return allTests.ToArray();
		}
	}
}