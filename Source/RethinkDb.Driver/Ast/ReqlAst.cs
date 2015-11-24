using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Dynamic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.Serialization;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using RethinkDb.Driver.Model;
using RethinkDb.Driver.Net;
using RethinkDb.Driver.Proto;
using RethinkDb.Driver.Utils;

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
                var collect = Args.Select(a => { return a.Build(); });
                list.Add(new JArray(collect));
            }
            else
            {
                list.Add(new JArray());
            }

            if( OptArgs.Count > 0 )
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

        public async virtual Task<dynamic> runAsync<T>(Connection conn, object globalOpts = null)
        {
            return await conn.runAsync<T>(this, globalOpts);
        }

        public async virtual Task<dynamic> runAsync(Connection conn, object globalOpts = null)
        {
            return await runAsync<dynamic>(conn, globalOpts);
        }

        public virtual dynamic run<T>(Connection conn, object globalOpts = null)
        {
            return conn.runAsync<T>(this, globalOpts).RunSync();
        }

        public virtual dynamic run(Connection conn, object globalOpts = null)
        {
            return run<dynamic>(conn, globalOpts);
        }

        /// <summary>
        /// Use this method if you're expecting a cursor from your query. This
        /// method offers a slight edge in performance without the need for the
        /// dynamic language runtime like the run() method uses.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="conn">connection</param>
        /// <param name="globalOpts">global anonymous type optional arguments</param>
        /// <returns>A Cursor</returns>
        public async virtual Task<Cursor<T>> runCursorAsync<T>(Connection conn, object globalOpts = null)
        {
            return await conn.runCursorAsync<T>(this, globalOpts);
        }
        /// <summary>
        /// Use this method if you're expecting a cursor from your query. This
        /// method offers a slight edge in performance without the need for the
        /// dynamic language runtime like the run() method uses.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="conn">connection</param>
        /// <param name="globalOpts">global anonymous type optional arguments</param>
        /// <returns>A Cursor</returns>
        public virtual Cursor<T> runCursor<T>(Connection conn, object globalOpts = null)
        {
            return conn.runCursorAsync<T>(this, globalOpts).RunSync();
        }

        /// <summary>
        /// Helper shortcut for DML type of queries that returns # of inserts, deletes, errors.
        /// </summary>
        public async virtual Task<Result> runResultAsync(Connection conn, object globalOpts = null)
        {
            return await conn.runAsync<Result>(this, globalOpts) as Result;
        }

        /// <summary>
        /// Helper shortcut for DML type of queries that returns # of inserts, deletes, errors.
        /// </summary>
        public virtual Result runResult(Connection conn, object globalOpts = null)
        {
            return conn.runAsync<Result>(this, globalOpts).RunSync() as Result;
        }

        /// <summary>
        /// Helper shortcut for change feeds, use if your query is expecting an infinite changes() stream.
        /// </summary>
        /// <typeparam name="T">The document type of new/old value</typeparam>
        public async virtual Task<Cursor<Change<T>>> runChangesAsync<T>(Connection conn, object globalOpts = null)
        {
            return await conn.runCursorAsync<Change<T>>(this, globalOpts);
        }

        /// <summary>
        /// Helper shortcut for change feeds, use if your query is expecting an infinite changes() stream.
        /// </summary>
        /// <typeparam name="T">The document type of new/old value</typeparam>
        public virtual Cursor<Change<T>> runChanges<T>(Connection conn, object globalOpts = null)
        {
            return conn.runCursorAsync<Change<T>>(this, globalOpts).RunSync();
        }

        public void runNoReply(Connection conn, object globalOpts = null)
        {
            conn.runNoReply(this, globalOpts);
        }

        /// <summary>
        /// Helper shortcut for grouping queries.
        /// </summary>
        /// <typeparam name="TKey">The key type of how items are grouped</typeparam>
        /// <typeparam name="TItem">The type of items</typeparam>
        public async virtual Task<IEnumerable<GroupedResult<TKey, TItem>>> runGroupingAsync<TKey, TItem>(Connection conn, object globalOpts = null)
        {
            return await runAsync<GroupedResult<TKey, TItem>>(conn, globalOpts);
        }
        /// <summary>
        /// Helper shortcut for grouping queries.
        /// </summary>
        /// <typeparam name="TKey">The key type of how items are grouped</typeparam>
        /// <typeparam name="TItem">The type of items</typeparam>
        public virtual IEnumerable<GroupedResult<TKey, TItem>> runGrouping<TKey, TItem>(Connection conn, object globalOpts = null)
        {
            return runAsync<GroupedResult<TKey, TItem>>(conn, globalOpts).RunSync();
        }

    }
}