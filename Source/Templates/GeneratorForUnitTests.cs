using System;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Text;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using NUnit.Framework;
using Templates.CodeGen;
using Templates.CodeGen.Util;
using Templates.Utils;
using YamlDotNet.Serialization;
using Z.ExtensionMethods;

namespace Templates
{
    [TestFixture]
	public class GeneratorForUnitTests
	{

        public string ProjectFolder = "RethinkDb.Driver.Tests";
        public string OutputDir = "./Generated";

        private const string YamlImportDir = "../Templates/UnitTests";

        [TestFixtureSetUp]
        public void BeforeRunningTestSession()
        {
            //remount the working directory before we begin.
            var rootProjectPath = Path.Combine(Directory.GetCurrentDirectory(), @"..\..\..", ProjectFolder);
            Directory.SetCurrentDirectory(rootProjectPath);
            EnsurePathsExist();
        }


        private void Clean()
        {
            if (Directory.Exists(OutputDir))
            {
                Directory.Delete(OutputDir, true);
            }
        }


        public void EnsurePathsExist()
        {
            if (!Directory.Exists(OutputDir))
                Directory.CreateDirectory(OutputDir);
        }

        [Test]
        public void Generate_All()
        {
            Clean();
            EnsurePathsExist();

            var files = GetAllYamlFiles();

            var deser = new Deserializer();

            foreach( var file in files )
            {
                //if( !file.Contains("random", StringComparison.OrdinalIgnoreCase) )
                //    continue;//just deal with random for now.
                Console.WriteLine("READING: " + file);
                var sr = new StringReader(File.ReadAllText(file));
                var yamlTest = deser.Deserialize<YamlTest>(sr);

                var mutator = new CSharpTestMutator(yamlTest);
                mutator.MutateTests();


                var outputFile =
                    Path.Combine(OutputDir,
                        Path.GetFileName(
                            Path.ChangeExtension(file, ".cs")));

                Console.WriteLine("OUTPUT: " + outputFile);

                var template = new TestTemplate() {YamlTest = yamlTest};

                File.WriteAllText(outputFile, template.TransformText());
            }
        }



        [Test]
        [Explicit]
	    public void CleanUpYamlTests()
	    {
            var ser = new Serializer();
	        var dser = new Deserializer();

            var files = GetAllYamlFiles();
	        foreach( var file in files )
	        {
	            Console.WriteLine("READING: " + file);
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

		private string[] GetAllYamlFiles()
		{
			var dir = Path.GetFullPath(YamlImportDir);

			var allTests = Directory.GetFiles(dir, "*.yaml", SearchOption.AllDirectories);

			return allTests.ToArray();
		}
	}
}