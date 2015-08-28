using System;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
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
	    public QueryType Type { get; }
	    public long Token { get; }
	    public ReqlAst Term { get; }
	    public GlobalOptions GlobalOptions { get; }

	    public Query(QueryType type, long token, ReqlAst term, GlobalOptions globalOptions)
		{
			this.Type = type;
			this.Token = token;
			this.Term = term;
			this.GlobalOptions = globalOptions;
		}

		public Query(QueryType type, long token) : this(type, token, null, new GlobalOptions())
		{
		}

		public static Query Stop(long token)
		{
			return new Query(QueryType.STOP, token, null, new GlobalOptions());
		}

		public static Query Continue(long token)
		{
			return new Query(QueryType.CONTINUE, token, null, new GlobalOptions());
		}

		public static Query Start(long token, ReqlAst term, GlobalOptions globalOptions)
		{
			return new Query(QueryType.START, token, term, globalOptions);
		}

		public static Query NoReplyWait(long token)
		{
			return new Query(QueryType.NOREPLY_WAIT, token, null, new GlobalOptions());
		}

		public virtual string Serialize()
		{
			var queryArr = new JArray();

            queryArr.Add(Type);

            if( Term != null )
		    {
		        queryArr.Add(Term.Build());
		    }
		    queryArr.Add(GlobalOptions.ToOptArgs());

			string queryJson = queryArr.ToString(Formatting.None);

			Console.WriteLine($"Sending: Token: {Token}, JSON: {queryJson}"); //RSI

			return queryJson;
		}
	}

}