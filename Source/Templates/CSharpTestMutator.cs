using System.Collections.Specialized;

namespace Templates
{
    public class CSharpTestMutator
    {
        private readonly YamlTest yamlTest;

        public static NameValueCollection TypeRenames = new NameValueCollection
            {
                {"Boolean", "bool"},
                {"Integer", "int"},
                {"Double", "double"}
            };

        public static NameValueCollection JavaLineReplacements = new NameValueCollection
            {
                {"Math.pow(", "Math.Pow("},
                {"->", "=>"}
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

                test.Java = FixUpJava(test.Java);
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
            return javaLine;
        }
    }
}