using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Dynamic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.Serialization;
using Newtonsoft.Json;
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

            if( OptArgs.Count > 0)
            {
                list.Add(buildOptarg(this.OptArgs));
            }
            return list;
        }


        public static JObject buildOptarg(OptArgs opts)
        {
            var dict = opts.ToDictionary(kvp => kvp.Key, kvp => kvp.Value.Build());
            return JObject.FromObject(dict);
        }

        public virtual dynamic run<T>(Connection conn, object globalOpts = null)
        {
            return conn.run<T>(this, globalOpts);
        }

        public virtual dynamic run(Connection conn, object globalOpts = null)
        {
            return run<dynamic>(conn, globalOpts);
        }

        public virtual Cursor<T> runCursor<T>(Connection conn, object globalOpts = null)
        {
            return conn.runCursor<T>(this, globalOpts);
        }

        public void runNoReply(Connection conn, object globalOpts = null)
        {
            conn.runNoReply(this, globalOpts);
        }

    }


}
