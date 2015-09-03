using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json.Linq;
using RethinkDb.Driver.Model;
using RethinkDb.Driver.Net;
using RethinkDb.Driver.Proto;

namespace RethinkDb.Driver.Ast
{

	/// <summary>
	/// Base class for all reql queries.
	/// </summary>
	public class ReqlAst
	{
	    protected internal TermType TermType { get; }
	    protected internal Arguments Args { get; }
	    protected internal OptArgs OptArgs { get; }

	    protected internal ReqlAst(TermType termType, Arguments args, OptArgs optargs)
        {
            this.TermType = termType;
            this.Args = args ?? new Arguments();
            this.OptArgs = optargs ?? new OptArgs();
        }

        protected internal ReqlAst(TermType termType, Arguments args) : this(termType, args, null)
        {
        }

        protected internal virtual object Build()
        {
            // Create a JSON object from the Ast
            JArray list = new JArray();
            list.Add(TermType);
            if( Args.Count > 0 )
            {
                var collect = Args.Select(a =>
                    {
                        return a.Build();
                    });
                list.Add(new JArray(collect));
            }
            else
            {
                list.Add(new JArray());
            }

            if( OptArgs.Count > 0 )
            {
                list.Add(BuildOptarg(OptArgs));
            }
            return list;
        }

	    public static Dictionary<string, object> BuildOptarg(OptArgs opts)
	    {
	        return opts.ToDictionary(kvp => kvp.Key, kvp => kvp.Value.Build());
	    }

        public virtual T run<T>(Connection conn, OptArgs g)
        {
            return (T)conn.run<T>(this, g);
        }

        public virtual T run<T>(Connection conn)
        {
            return (T)conn.run<T>(this, new OptArgs());
        }

	    public void runNoReply(Connection conn)
	    {
	        conn.runNoReply(this, new OptArgs());
	    }
        public void runNoReply(Connection conn, OptArgs globalOpts)
        {
            conn.runNoReply(this, globalOpts);
        }
        
    }

}