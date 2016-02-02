using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using Newtonsoft.Json.Linq;
using NUnit.Framework;
using Templates.CodeGen;
using Templates.CodeGen.Specialized;
using Templates.CodeGen.Util;
using Templates.Metadata;

namespace Templates
{
    [TestFixture]
    public class GeneratorForAst
    {
        public string ProjectFolder = "RethinkDb.Driver";
        public string GenerateRootDir = @"./Generated";
        public string ProtoDir = @"./Generated/Proto";
        public string AstClasses = @"./Generated/Ast";
        public string ModelDir = @"./Generated/Model";

        public void SetPaths(string driverFolder)
        {
            MetaDb.Initialize(Path.Combine(driverFolder, @"..\Templates\Metadata"));

            this.GenerateRootDir = Path.GetFullPath(Path.Combine(driverFolder, GenerateRootDir));
            this.ProtoDir = Path.GetFullPath(Path.Combine(driverFolder, ProtoDir));
            this.GenerateRootDir = Path.GetFullPath(Path.Combine(driverFolder, GenerateRootDir));
            this.AstClasses = Path.GetFullPath(Path.Combine(driverFolder, AstClasses));
            this.ModelDir = Path.GetFullPath(Path.Combine(driverFolder, ModelDir));
            this.ProjectFolder = Path.GetFullPath(driverFolder);
        }

        [TestFixtureSetUp]
        public void BeforeRunningTestSession()
        {
            //remount the working directory before we begin.
            var driverFolder = Path.Combine(Directory.GetCurrentDirectory(), @"..\..\..", ProjectFolder);
            SetPaths(driverFolder);

            EnsurePathsExist();
        }

        private void Clean()
        {
            if (!File.Exists(Path.Combine(ProjectFolder,"RethinkDb.Driver.csproj")))
            {
                throw new FileNotFoundException("RethinkDb.Driver.csproj not found. Being safe and existing before deleting directories.");
            }

            if ( Directory.Exists(GenerateRootDir) )
            {
                Directory.Delete(GenerateRootDir, true);
            }
        }

        public void EnsurePathsExist()
        {
            if (!File.Exists(Path.Combine(ProjectFolder, "RethinkDb.Driver.csproj")))
            {
                throw new FileNotFoundException("RethinkDb.Driver.csproj not found. Being safe and existing before deleting directories.");
            }

            if( !Directory.Exists(GenerateRootDir) )
                Directory.CreateDirectory(GenerateRootDir);
            if( !Directory.Exists(ProtoDir) )
                Directory.CreateDirectory(ProtoDir);
            if( !Directory.Exists(AstClasses) )
                Directory.CreateDirectory(AstClasses);
            if( !Directory.Exists(ModelDir) )
                Directory.CreateDirectory(ModelDir);
        }

        [Test]
        [Explicit]
        public void Generate_All()
        {
            Clean();
            EnsurePathsExist();
            Render_Proto_Enums();
            Render_Ast_SubClasses();
            Render_TopLevel();
            Render_Funtion_Interfaces();
            Render_Exceptions();
            Render_OptArg_Enums();
        }

        [Test]
        public void Render_Funtion_Interfaces()
        {
            //Determines the maximum reql lambda arity that shows up in any signature
            var maxArity = GetMaxArity() + 1;

            foreach( var n in Enumerable.Range(0, maxArity) )
            {
                var tmpl = new ReqlFunctionTemplate()
                    {
                        Arity = n
                    };

                File.WriteAllText(Path.Combine(ModelDir, $"ReqlFunction{n}.cs"), tmpl.TransformText());
            }
        }

        public static int GetMaxArity()
        {
            var maxArity = MetaDb.JavaTermInfo.SelectTokens("..signatures")
                .SelectMany(t => t.ToObject<List<Signature>>())
                .SelectMany(s => s.Args)
                .Select(s => s.Type)
                .Where(s => s.StartsWith("ReqlFunction"))
                .Max(s => int.Parse(s.Substring(12)));
            return maxArity;
        }

        [Test]
        public void Render_TopLevel()
        {
            var allTerms = MetaDb.JavaTermInfo.ToObject<Dictionary<string, JObject>>();
            var mutator = new CSharpTermInfoMutator(allTerms);
            mutator.EnsureLanguageSafeTerms();

            var tmpl = new TopLevelTemplate()
                {
                    AllTerms = allTerms
                };

            File.WriteAllText(Path.Combine(ModelDir, "TopLevel.cs"), tmpl.TransformText());
        }

        [Test]
        public void Render_Proto_Enums()
        {
            RenderEnum("Version", MetaDb.Protocol["VersionDummy"]["Version"].ToObject<Dictionary<string, object>>());
            RenderEnum("Protocol", MetaDb.Protocol["VersionDummy"]["Protocol"].ToObject<Dictionary<string, object>>());
            RenderEnum("QueryType", MetaDb.Protocol["Query"]["QueryType"].ToObject<Dictionary<string, object>>());
            RenderEnum("FrameType", MetaDb.Protocol["Frame"]["FrameType"].ToObject<Dictionary<string, object>>());
            RenderEnum("ResponseType", MetaDb.Protocol["Response"]["ResponseType"].ToObject<Dictionary<string, object>>());
            RenderEnum("ResponseNote", MetaDb.Protocol["Response"]["ResponseNote"].ToObject<Dictionary<string, object>>());
            RenderEnum("ErrorType", MetaDb.Protocol["Response"]["ErrorType"].ToObject<Dictionary<string, object>>());
            RenderEnum("DatumType", MetaDb.Protocol["Datum"]["DatumType"].ToObject<Dictionary<string, object>>());
            RenderEnum("TermType", MetaDb.Protocol["Term"]["TermType"].ToObject<Dictionary<string, object>>());
        }

        [Test]
        public void Render_Ast_SubClasses()
        {
            var allTerms = MetaDb.JavaTermInfo.ToObject<Dictionary<string, JObject>>();
            var mutator = new CSharpTermInfoMutator(allTerms);
            mutator.EnsureLanguageSafeTerms();

            RenderAstSubclass(null, "ReqlExpr", "ReqlAst", allTerms, null);

            foreach( var kvp in allTerms )
            {
                var termName = kvp.Key;
                var termMeta = kvp.Value;

                if( !kvp.Value["deprecated"]?.ToObject<bool?>() ?? true )
                {
                    var className = termMeta["classname"].ToString();
                    var superclass = termMeta["superclass"].ToString();

                    RenderAstSubclass(termName, className, superclass, allTerms, termMeta);
                }
                else
                {
                    Console.WriteLine("Deprcated:" + kvp.Key);
                }
            }
        }


        [Test]
        public void Render_Exceptions()
        {
            var errorHearchy = MetaDb.Global["exception_hierarchy"] as JObject;
            RenderExceptions(errorHearchy);
        }

        [Test]
        public void Render_OptArg_Enums()
        {
            var optArgs = MetaDb.Global["optarg_enums"].ToObject<Dictionary<string, string[]>>();

            foreach( var kvp in optArgs )
            {
                var enumName = kvp.Key.Substring(2).ToLower().ClassName();
                var values = kvp.Value;
                RenderEnumString(enumName, values);
            }
        }


        private void RenderExceptions(JObject error, string superClass = "Exception")
        {
            foreach( var p in error.Properties() )
            {
                RenderErrorClass(p.Name, superClass);
                RenderExceptions(p.Value as JObject, p.Name);
            }
        }

        public void RenderErrorClass(string className, string superClass)
        {
            var tmpl = new ExceptionTemplate()
                {
                    ClassName = className,
                    SuperClass = superClass
                };

            File.WriteAllText(Path.Combine(GenerateRootDir, $"{className.ClassName()}.cs"), tmpl.TransformText());
        }

        public void RenderAstSubclass(string termType, string className, string superClass, Dictionary<string, JObject> allTerms, JObject termMeta = null)
        {
            className = className ?? termType.ToLower();

            var tmpl = GetSpeicalizedTemplate<AstSubclassTemplate>(className) ?? new AstSubclassTemplate();

            tmpl.TermName = termType;
            tmpl.ClassName = className;
            tmpl.Superclass = superClass;
            tmpl.TermMeta = termMeta;
            tmpl.AllTerms = allTerms;

            File.WriteAllText(Path.Combine(AstClasses, $"{className.ClassName()}.cs"), tmpl.TransformText());
        }

        public T GetSpeicalizedTemplate<T>(string className)
        {
            var type = typeof(MakeObj);
            var path = type.FullName.Replace(type.Name, "");
            var lookingFor = $"{path}{className.ClassName()}";

            var specialType = Assembly.GetExecutingAssembly().GetType(lookingFor, false, true);
            if( specialType != null )
            {
                Console.WriteLine("Using Speical Template: " + lookingFor);
                return (T)Activator.CreateInstance(specialType);
            }

            return default(T);
        }


        public void RenderEnum(string enumName, Dictionary<string, object> enums)
        {
            var tmpl = GetSpeicalizedTemplate<EnumTemplate>(enumName) ?? new EnumTemplate();

            tmpl.EnumName = enumName;
            tmpl.Enums = enums;

            File.WriteAllText(Path.Combine(ProtoDir, $"{enumName.ClassName()}.cs"), tmpl.TransformText());
        }

        public void RenderEnumString(string enumName, string[] enums)
        {
            var tmpl = GetSpeicalizedTemplate<EnumStringTemplate>(enumName) ?? new EnumStringTemplate();

            tmpl.EnumName = enumName;
            tmpl.Enums = enums;

            File.WriteAllText(Path.Combine(ModelDir, $"{enumName.ClassName()}.cs"), tmpl.TransformText());
        }
    }
}