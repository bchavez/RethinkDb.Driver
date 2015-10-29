using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;
using Newtonsoft.Json.Linq;
using Templates.CodeGen.Util;

namespace Templates
{
    public class CSharpTermInfoMutator
    {
        public static string[] Keywords =
            {
                "abstract", "as", "base", "bool", "break", "byte", "case", "catch", "char",
                "checked", "class", "const", "continue", "decimal", "default", "delegate",
                "do", "double", "else", "enum", "event", "explicit", "extern", "false",
                "finally", "fixed", "float", "for", "forech", "goto", "if", "implicit",
                "in", "int", "interface", "internal", "is", "lock", "long", "namespace",
                "new", "null", "object", "operator", "out", "override", "params", "private",
                "protected", "public", "readonly", "ref", "return", "sbyte", "sealed", "short",
                "sizeof", "stackalloc", "static", "string", "struct", "switch", "this", "throw",
                "true", "try", "uint", "ulong", "unchecked", "unsafe", "ushort",
                "using", "virtual", "volatile", "void", "while",
            };

        public static string[] ObjectMethods =
            {
                "Equals", "ToString", "GetHashCode", "GetType"
            };

        public static string[] Blacklist =
            {
                "row"
            };

        public static NameValueCollection TokenMutate = new NameValueCollection()
            {
                {"TIME.signatures[0].args[0].type", "int"},
                {"TIME.signatures[0].args[1].type", "int"},
                {"TIME.signatures[0].args[2].type", "int"},
                {"TIME.signatures[0].args[3].type", "int"},
                {"TIME.signatures[1].args[0].type", "int"},
                {"TIME.signatures[1].args[1].type", "int"},
                {"TIME.signatures[1].args[2].type", "int"},
                {"TIME.signatures[1].args[3].type", "int"},
                {"TIME.signatures[1].args[4].type", "int"},
                {"TIME.signatures[1].args[5].type", "int"},
                {"TIME.signatures[1].args[6].type", "int"},
            };

        private readonly Dictionary<string, JObject> allTerms;

        public CSharpTermInfoMutator(Dictionary<string, JObject> allTerms)
        {
            this.allTerms = allTerms;
        }

        public void EnsureLanguageSafeTerms()
        {
            DeleteIfBlackListed();
            MutateMethodName();
            MutateParameterArguments();
            MutateImpelments();
            MutateSignatures();
        }

        private void MutateSignatures()
        {
            foreach( var termInfo in allTerms )
            {
                var term = termInfo.Key;
                var info = termInfo.Value;


            }
        }

        private void MutateImpelments()
        {
            foreach (var termInfo in allTerms)
            {
                var term = termInfo.Key;
                var info = termInfo.Value;


            }
        }

        private void MutateParameterArguments()
        {
            foreach( var termInfo in allTerms )
            {
                var term = termInfo.Key;
                var info = termInfo.Value;
            }
        }

        private void MutateClassName()
        {
            //nothing to do.... yet.
        }

        private void MutateMethodName()
        {
            foreach( var termInfo in allTerms )
            {
                var term = termInfo.Key;
                var info = termInfo.Value;

                var proposedNames = info["methodnames"].ToObject<string[]>();
                var lst = new List<string>();
                foreach( var proposedName in proposedNames )
                {
                    var finalName = proposedName;
                    if( Keywords.Any(k => string.Equals(k, proposedName, StringComparison.OrdinalIgnoreCase)) )
                    {
                        finalName += "_";
                    }
                    else if( ObjectMethods.Any(m => string.Equals(m, proposedName, StringComparison.OrdinalIgnoreCase)) )
                    {
                        finalName += "_";
                    }
                    lst.Add(finalName);
                }
                info["methodnames"] = JArray.FromObject(lst);
            }
        }

        private void DeleteIfBlackListed()
        {
            foreach( var termInfo in allTerms )
            {
                var term = termInfo.Key;
                var info = termInfo.Value;

                var termName = term.MethodName();

                if( Blacklist.Any(b => string.Equals(termName, b, StringComparison.OrdinalIgnoreCase)) )
                {
                    allTerms.Remove(term);
                }
            }
        }

    }
}