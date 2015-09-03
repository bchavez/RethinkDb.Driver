using System;
using System.Collections.Generic;
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
                "true", "try", "typeof", "uint", "ulong", "unchecked", "unsafe", "ushort",
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

        private readonly Dictionary<string, JObject> allTerms;

        public CSharpTermInfoMutator(Dictionary<string, JObject> allTerms)
        {
            this.allTerms = allTerms;
        }

        public void EnsureLanguageSafeTerms()
        {
            DeleteIfBlackListed();
            MutateMethodName();
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

                var proposedName = info["methodname"].ToString();

                if( Keywords.Any(k => string.Equals(k, proposedName, StringComparison.OrdinalIgnoreCase)) )
                {
                    proposedName += "_";
                }
                else if( ObjectMethods.Any(m => string.Equals(m, proposedName, StringComparison.OrdinalIgnoreCase)) )
                {
                    proposedName += "_";
                }
                info["methodname"] = proposedName;
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