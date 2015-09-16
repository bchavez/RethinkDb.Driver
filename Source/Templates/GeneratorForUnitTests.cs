using System;
using System.IO;
using System.Linq;
using System.Text;
using IronPython;
using IronPython.Compiler;
using IronPython.Compiler.Ast;
using IronPython.Hosting;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.Scripting;
using Microsoft.Scripting.Hosting;
using Microsoft.Scripting.Hosting.Providers;
using Microsoft.Scripting.Runtime;
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
	public class ReQLVisitor
	{
		
	}
	public class TestDef { }
	public class Def : TestDef
	{
		
	}
	public class Query : TestDef
	{
		public Expression QueryAst;
		public string RawLine;

	}
	public static class TestAndDefs
	{
		public static TestDef Inspect(Statement s, string rawLine)
		{
			if( s is ExpressionStatement )
			{
				var es = s as ExpressionStatement;
				
				return new Query()
					{
						QueryAst = es.Expression,
						RawLine = rawLine
					};
			}
			return null;
		}

		public static void AstToCSharp(TestDef item)
		{
			if( item is Query )
			{
				
			}
			else if( item is Def )
			{
				
			}
		}
	}
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

		private ScriptEngine GetEngine()
		{
			return Python.CreateEngine();
		}

		private Parser GetParser(string pycode, ScriptEngine engine)
		{
			var src = engine.CreateScriptSourceFromString(pycode);

			var code = HostingHelpers.GetSourceUnit(src);
			var lc = HostingHelpers.GetLanguageContext(engine);

			var ctx = new CompilerContext(code, lc.GetCompilerOptions(), ErrorSink.Default);

			var pythonOpts = lc.Options as PythonOptions;
			
			var parser = Parser.CreateParser(ctx, pythonOpts);

			return parser;
		}


		[Test]
		public void ReadDefaultTest()
		{
			var tg = UnitTestDb.LoadAll().First();
			var pyengine = GetEngine();

			foreach( var test in tg.Tests.Where( t => t["py"] != null) )
			{
				var py = test["py"].ToString();
				Console.WriteLine("-" + py);

				var parser = GetParser(py, pyengine);
				var ast = parser.ParseSingleStatement();


				var testDef = TestAndDefs.Inspect(ast.Body, py);
				//var walker = new SharpWalker();
				//ast.Walk(walker);

				//Console.WriteLine("+"+walker.gen);
				break;
			}
		}

		private const string YamlImportDir = "../../../../../rethinkdb/test/rql_test/src";
		private const string JsonOutputDir = "../../UnitTests";

		private readonly string[] TestExclusions = {
				// python only tests
				"regression/1133",
				"regression/767",
				"regression/1005",
				// double run
				"changefeeds/squash",
				// arity checked at compile time
				"arity",
			};

		[Test]
		[Explicit]
		public void ImportYamlTestsAndConvertToJson()
		{
			var files = GetAllYamlTests();
			var js = new Serializer(SerializationOptions.JsonCompatible);

			foreach( var file in files )
			{
				var d = new Deserializer();
				var yobj = d.Deserialize(new StringReader(File.ReadAllText(file)));

				var sb = new StringBuilder();
				var sw = new StringWriter(sb);
				js.Serialize(sw, yobj);

				var relPath = RelativePath(file);

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

		private string RelativePath(string yamlFile)
		{
			return yamlFile.GetAfter(@"rql_test\src\");
		}

		private string[] GetAllYamlTests()
		{
			var dir = Path.GetFullPath(YamlImportDir);

			var exclude = TestExclusions
				.Select(e => Path.Combine(dir, e))
				.Select(e => Path.GetFullPath(e));

			var allTests = Directory.GetFiles(dir, "*.yaml", SearchOption.AllDirectories)
				.Where(s => !exclude.Any(s.Contains));

			return allTests.ToArray();
		}
	}
}