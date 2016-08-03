#pragma warning disable 1591

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

    /// <summary>
    /// DO NOT USE DIRECTLY
    /// </summary>
    public class Query
    {
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

        public Query(QueryType type, long token) : this(type, token, null, null)
        {
        }

        public static Query Stop(long token)
        {
            return new Query(QueryType.STOP, token, null, null);
        }

        public static Query Continue(long token)
        {
            return new Query(QueryType.CONTINUE, token, null, null);
        }

        public static Query Start(long token, ReqlAst term, OptArgs globalOptions)
        {
            return new Query(QueryType.START, token, term, globalOptions);
        }

        public static Query ServerInfo(long token)
        {
            return new Query(QueryType.SERVER_INFO, token, null, null);
        }

        public static Query NoReplyWait(long token)
        {
            return new Query(QueryType.NOREPLY_WAIT, token, null, null);
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

            return queryArr.ToString(Formatting.None);
        }
    }

    public interface IQuerySeralizer
    {
        string ToProtocolString(Query q);
        object BuildTerm(ReqlAst ast);
    }

    public class DefaultQuerySeralizer : IQuerySeralizer
    {
        public string ToProtocolString(Query q)
        {
            var queryArr = new JArray();

            queryArr.Add(q.Type);

            if (q.Term != null)
            {
                queryArr.Add(q.Term.Build());
            }
            if (q.GlobalOptions != null)
            {
                queryArr.Add(ReqlAst.BuildOptarg(q.GlobalOptions));
            }

            return queryArr.ToString(Formatting.None);
        }

        public object BuildTerm(ReqlAst ast)
        {
            throw new System.NotImplementedException();
        }
    }
}