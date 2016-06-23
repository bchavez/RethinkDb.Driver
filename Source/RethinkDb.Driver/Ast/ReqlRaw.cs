using System;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using RethinkDb.Driver.Model;
using RethinkDb.Driver.Proto;

namespace RethinkDb.Driver.Ast
{
    /// <summary>
    /// Used to inject raw protocol strings when the AST is dumped on the wire.
    /// </summary>
    public class ReqlRawExpr : ReqlExpr
    {
        private string RawProtocol { get; }

        /// <summary>
        /// Create an AST mid-flight with the raw protocol string.
        /// </summary>
        /// <param name="rawProtocol">Raw protocol string from ReqlAst.Build()</param>
        internal ReqlRawExpr(string rawProtocol) : base(new TermType(), null, null)
        {
            this.RawProtocol = rawProtocol;
        }

        /// <summary>
        /// Return the JToken representation of the raw protocol
        /// </summary>
        protected internal override object Build()
        {
            return JToken.Parse(this.RawProtocol);
        }
    }

    /// <summary>
    /// Used to inject raw protocol strings when the AST is dumped on the wire.
    /// </summary>
    public class ReqlRaw : ReqlAst
    {
        private string RawProtocol { get; }

        /// <summary>
        /// Create an AST mid-flight with the raw protocol string.
        /// </summary>
        /// <param name="rawProtocol">Raw protocol string from ReqlAst.Build()</param>
        internal ReqlRaw(string rawProtocol) : base(new TermType(), null, null)
        {
            this.RawProtocol = rawProtocol;
        }

        /// <summary>
        /// Return the JToken representation of the raw protocol
        /// </summary>
        protected internal override object Build()
        {
            return JToken.Parse(this.RawProtocol);
        }

        private const string RawTokenType = "$reql_reqlraw$";

        /// <summary>
        /// Convert an AST item into a raw protocol string representation.
        /// Useful for serialization.
        /// </summary>
        /// <param name="astItem">Can be anything, ReqlFunction or otherwise</param>
        public static string ToRawString(object astItem)
        {
            var list = new List<Guid>();
            var token = Util.ToReqlAst(astItem, o => HookFuncTypes(o, list)).Build() as JToken;
            var query = token.ToString(Formatting.None);
            //pack
            return $"{string.Join("|", list)}{RawTokenType}{query}";
        }

        internal static string HydrateProtocolString(string reqlRawString)
        {
            var tokenLocation = reqlRawString.IndexOf(RawTokenType, StringComparison.Ordinal);
            var guidStrings = reqlRawString.Substring(0, tokenLocation);
            var query = reqlRawString.Substring(tokenLocation + RawTokenType.Length);

            //unpack
            var replaceTokens = guidStrings.Split('|')
                .Select(g => new { GuidString = $@"""{g}""", VarId = Func.NextVarId() });

            foreach (var token in replaceTokens)
            {
                query = query.Replace(token.GuidString, token.VarId.ToString());
            }
            return query;
        }

		/// <summary>
		/// Convert a raw protocol string into an AST term that will
		/// be injected when the AST is serialized.
		/// </summary>
		/// <param name="reqlRawString">The raw protocol string to inject</param>
		/// <returns>A raw AST term</returns>
		public static ReqlRaw FromRawString(string reqlRawString)
        {
            var query = HydrateProtocolString(reqlRawString);
            return new ReqlRaw(query);
        }


        private static ReqlAst HookFuncTypes(object val, List<Guid> context)
        {
            var del = val as Delegate;
            if( del != null )
            {
                return Func.Serialize(del, context);
            }
            return null;
        }
    }
}