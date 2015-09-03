using System;
using Common.Logging;
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
	    private static ILog log = Log.Instance;
	    public QueryType Type { get; }
	    public long Token { get; }
	    public ReqlAst Term { get; }
	    public OptArgs GlobalOptions { get; }

	    public Query(QueryType type, long token, ReqlAst term, OptArgs globalOptions)
		{
			this.Type = type;
			this.Token = token;
			this.Term = term;
			this.GlobalOptions = globalOptions;
		}

		public Query(QueryType type, long token) : this(type, token, null, new OptArgs())
		{
		}

		public static Query Stop(long token)
		{
			return new Query(QueryType.STOP, token, null, new OptArgs());
		}

		public static Query Continue(long token)
		{
			return new Query(QueryType.CONTINUE, token, null, new OptArgs());
		}

		public static Query Start(long token, ReqlAst term, OptArgs globalOptions)
		{
			return new Query(QueryType.START, token, term, globalOptions);
		}

		public static Query NoReplyWait(long token)
		{
			return new Query(QueryType.NOREPLY_WAIT, token, null, new OptArgs());
		}

		public virtual string Serialize()
		{
			var queryArr = new JArray();

            queryArr.Add(Type);

            if( Term != null )
		    {
		        queryArr.Add(Term.Build());
		    }
		    if( GlobalOptions != null )
		    {
		        queryArr.Add(ReqlAst.BuildOptarg(GlobalOptions));
		    }

            string queryJson = queryArr.ToString(Formatting.None);

		    log.Debug($"Sending: Token: {Token}, JSON: {queryJson}");//RSI

            return queryJson;
		}
	}

}