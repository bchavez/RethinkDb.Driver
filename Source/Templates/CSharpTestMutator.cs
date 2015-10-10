using System.Collections.Specialized;
using System.Linq;
using System.Text;
using Z.ExtensionMethods;

namespace Templates
{
    public class CSharpTestMutator
    {
        private readonly YamlTest yamlTest;

        public static NameValueCollection TypeRenames = new NameValueCollection
            {
                {"Boolean", "bool"},
                {"Integer", "int"},
                {"Double", "double"},
                {"Long", "long"},
                {"List ", "IList "},
                {"(List)", "(IList)"},
                {" Map ", " MapObject "},
                {"(Map)", "(MapObject)"},
                {"(OffsetDateTime)", "(DateTimeOffset)" }
            };

        public static NameValueCollection JavaLineReplacements = new NameValueCollection
            {
                {"Math.pow(", "Math.Pow("},
                {"->", "=>"},
                {"(ReqlFunction1)", ""},
                {"<< 53L", "<< 53"},
                {"<< 53 - 1L", "<< 53 - 1"},
                {"LongStream.range(", "EnumerableLRange("},
                {".boxed().map(", ".Select("},
                {".collect(Collectors.toList())", ".ToList()"},
                {"sys.floatInfo.max", "double.MaxValue"},
                {"sys.floatInfo.min", "double.MinValue"},
                {"r.object(", "r.object_("},
                {"Stream.concat(", "Enumerable.Concat("},
                {".stream()", ".OfType<object>().ToList()"},
            };

        public CSharpTestMutator(YamlTest yamlTest)
        {
            this.yamlTest = yamlTest;
        }

        public void MutateTests()
        {
            foreach( var test in yamlTest.DefsAndTests )
            {
                if( TypeRenames[test.ExpectedType] != null )
                    test.ExpectedType = TypeRenames[test.ExpectedType];

                if ( test.TestType == "JavaDef" && !test.Java.StartsWith("ReqlFunction") )
                {
                    test.Java = "var " + test.Java.GetAfter(" ");
                }

                test.Java = FixUpJava(test.Java);

                if( test.ExpectedJava.IsNotNullOrWhiteSpace() )
                {
                    test.ExpectedJava = FixUpJava(test.ExpectedJava);
                }
            }
        }

        private string FixUpJava(string javaLine)
        {
            foreach( var lineReplacement in JavaLineReplacements.AllKeys )
            {
                if( javaLine.Contains(lineReplacement) )
                {
                    javaLine = javaLine.Replace(lineReplacement, JavaLineReplacements[lineReplacement]);
                }
            }

            foreach (var typeRename in TypeRenames.AllKeys)
            {
                if (javaLine.Contains(typeRename))
                {
                    javaLine = javaLine.Replace(typeRename, TypeRenames[typeRename]);
                }
            }

            if( javaLine.Contains("byte[]{") )
            {//upvert java's signed bytes to real bytes.
                javaLine = ConvertJavaSignedBytes(javaLine);
            }


            return ScanLiteral(javaLine);
            //return javaLine;
        }

        private string ConvertJavaSignedBytes(string javaLine)
        {
            do
            {
                var byteStr = javaLine.GetBetween("byte[]{", "}");

                if( byteStr.IsNullOrWhiteSpace() )
                {
                    javaLine = javaLine.Replace("byte[]{}", "byte[] {}");
                    continue;
                }

                var unsignedBytes = byteStr
                    .Split(",")
                    .Select(s => s.ExtractInt32())
                    .Select(i => i < 0 ? 256 + i : i)
                    .ToArray();
                
                //... keep converting until we got dat space.
                javaLine = javaLine.Replace($"byte[]{{{byteStr}}}", $"byte[] {{ {string.Join(", ", unsignedBytes)} }}");
            } while( javaLine.Contains("byte[]{") );

            return javaLine;
        }


        public string ScanLiteral(string input)
        {
            var literal = new StringBuilder(input.Length + 2);
            foreach (var c in input)
            {
                switch (c)
                {
                    case '\0': literal.Append(@"\0"); break;
                    case '\a': literal.Append(@"\a"); break;
                    case '\b': literal.Append(@"\b"); break;
                    case '\f': literal.Append(@"\f"); break;
                    case '\n': literal.Append(@"\n"); break;
                    case '\r': literal.Append(@"\r"); break;
                    case '\t': literal.Append(@"\t"); break;
                    case '\v': literal.Append(@"\v"); break;
                    default:
                        // ASCII printable character
                        if (c >= 0x20 && c <= 0x7e)
                        {
                            literal.Append(c);
                            // As UTF16 escaped character
                        }
                        else
                        {
                            literal.Append(@"\u");
                            literal.Append(((int)c).ToString("x4"));
                        }
                        break;
                }
            }
            return literal.ToString();
        }
    }
}