using System;
using Newtonsoft.Json.Linq;
using RethinkDb.Driver;
using RethinkDb.Driver.Model;
using RethinkDb.Driver.Proto;

namespace RethinkDb.Driver.Ast
{
	/* An instance for a query that has been sent to the server. Keeps
	 * track of its token, the args to .run() it was called with, and its
	 * query type.
	*/
	public class Query
	{
		public readonly QueryType type;
		public readonly long token;
		public readonly ReqlAst term;
		public readonly GlobalOptions globalOptions;

		public Query(QueryType type, long token, ReqlAst term, GlobalOptions globalOptions)
		{
			this.type = type;
			this.token = token;
			this.term = null;
			this.globalOptions = globalOptions;
		}

		public Query(QueryType type, long token) : this(type, token, null, new GlobalOptions())
		{
		}

		public static Query stop(long token)
		{
			return new Query(QueryType.STOP, token, null, new GlobalOptions());
		}

		public static Query continue_(long token)
		{
			return new Query(QueryType.CONTINUE, token, null, new GlobalOptions());
		}

		public static Query start(long token, ReqlAst term, GlobalOptions globalOptions)
		{
			return new Query(QueryType.START, token, term, globalOptions);
		}

		public static Query noreplyWait(long token)
		{
			return new Query(QueryType.NOREPLY_WAIT, token, null, new GlobalOptions());
		}

		public virtual string serialize()
		{
			var queryArr = new JArray();

            queryArr.Add(type);

            if( term != null )
		    {
		        queryArr.Add(term.build());
		    }
		    queryArr.Add(globalOptions.toOptArgs());

			string queryJson = queryArr.ToString();

			Console.WriteLine($"Sending: Token: {token}, JSON: {queryJson}"); //RSI

			return queryJson;
		}
	}

}