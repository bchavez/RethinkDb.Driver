using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Dynamic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.Serialization;
using System.Threading;
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
                list.Add(BuildOptarg(this.OptArgs));
            }
            return list;
        }

        protected internal static JObject BuildOptarg(OptArgs opts)
        {
            var dict = opts.ToDictionary(kvp => kvp.Key, kvp => kvp.Value.Build());
            return JObject.FromObject(dict);
        }


        
        #region DYNAMIC LANGUAGE RUNTIME (DLR) RUNNERS

        /// <summary>
        /// Runs the query on the connection. If you know the response type
        /// of your query, T (SUCCESS_ATOM) or Cursor[T] (SUCCESS_SEQUENCE or SUCCESS_PARTIAL)
        /// it's recommended to use `.runAtom` or `.runCursor` helpers
        /// as they offer a slight edge in performance since both bypass the 
        /// dynamic language runtime execution engine.
        /// </summary>
        /// <returns>Returns T or Cursor[T]</returns>
        public virtual Task<dynamic> RunAsync<T>(IConnection conn, object runOpts = null, CancellationToken cancelToken = default(CancellationToken))
        {
            return conn.RunAsync<T>(this, runOpts, cancelToken);
        }

        /// <summary>
        /// Runs the query on the connection. If you know the response type
        /// of your query, T (SUCCESS_ATOM) or Cursor[T] (SUCCESS_SEQUENCE or SUCCESS_PARTIAL)
        /// it's recommended to use `.runAtom` or `.runCursor` helpers
        /// as they offer a slight edge in performance since both bypass the 
        /// dynamic language runtime execution engine.
        /// </summary>
        /// <returns>Returns T or Cursor[T]</returns>
        public virtual Task<dynamic> RunAsync(IConnection conn, object runOpts = null, CancellationToken cancelToken = default(CancellationToken))
        {
            return RunAsync<dynamic>(conn, runOpts, cancelToken);
        }

        /// <summary>
        /// Runs the query on the connection. If you know the response type
        /// of your query, T (SUCCESS_ATOM) or Cursor[T] (SUCCESS_SEQUENCE or SUCCESS_PARTIAL)
        /// it's recommended to use `.runAtom` or `.runCursor` helpers
        /// as they offer a slight edge in performance since both bypass the 
        /// dynamic language runtime execution engine.
        /// </summary>
        /// <returns>Returns T or Cursor[T]</returns>
        public virtual dynamic Run<T>(IConnection conn, object runOpts = null)
        {
            return RunAsync<T>(conn, runOpts).WaitSync();
        }

        /// <summary>
        /// Runs the query on the connection. If you know the response type
        /// of your query, T (SUCCESS_ATOM) or Cursor[T] (SUCCESS_SEQUENCE or SUCCESS_PARTIAL)
        /// it's recommended to use `.runAtom` or `.runCursor` helpers
        /// as they offer a slight edge in performance since both bypass the 
        /// dynamic language runtime execution engine.
        /// </summary>
        /// <returns>Returns T or Cursor[T]</returns>
        public virtual dynamic Run(IConnection conn, object runOpts = null)
        {
            return Run<dynamic>(conn, runOpts);
        }

        #endregion






        #region RESPONSE TYPED RUNNERS

        /// <summary>
        /// Executes a query with no expected response. Useful for fire-and-forget queries like insert, update.
        /// </summary>
        public void RunNoReply(IConnection conn, object runOpts = null)
        {
            conn.RunNoReply(this, runOpts);
        }

        /// <summary>
        /// Use this method if you're expecting a cursor (SUCCESS_SEQUENCE or SUCCESS_PARTIAL) response
        /// from your query. This method offers a slight edge in performance without the need for the
        /// dynamic language runtime like the run() method uses.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="conn">connection</param>
        /// <param name="runOpts">global anonymous type optional arguments</param>
        /// <returns>A Cursor</returns>
        public virtual Task<Cursor<T>> RunCursorAsync<T>(IConnection conn, object runOpts = null, CancellationToken cancelToken = default(CancellationToken))
        {
            return conn.RunCursorAsync<T>(this, runOpts, cancelToken);
        }

        /// <summary>
        /// Use this method if you're expecting a cursor (SUCCESS_SEQUENCE or SUCCESS_PARTIAL) response
        /// from your query. This method offers a slight edge in performance without the need for the
        /// dynamic language runtime like the run() method uses.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="conn">connection</param>
        /// <param name="runOpts">global anonymous type optional arguments</param>
        /// <returns>A Cursor</returns>
        public virtual Cursor<T> RunCursor<T>(IConnection conn, object runOpts = null)
        {
            return RunCursorAsync<T>(conn, runOpts).WaitSync();
        }

        /// <summary>
        /// Use this method if you're expecting SUCCESS_ATOM response from your query. This
        /// method offers a slight edge in performance without the need for the
        /// dynamic language runtime like the run() method uses.
        /// </summary>
        public virtual Task<T> RunAtomAsync<T>(IConnection conn, object runOpts = null, CancellationToken cancelToken = default(CancellationToken))
        {
            return conn.RunAtomAsync<T>(this, runOpts, cancelToken);
        }

        /// <summary>
        /// Use this method if you're expecting SUCCESS_ATOM response from your query. This
        /// method offers a slight edge in performance without the need for the
        /// dynamic language runtime like the run() method uses.
        /// </summary>
        public virtual T RunAtom<T>(IConnection conn, object runOpts = null)
        {
            return RunAtomAsync<T>(conn, runOpts).WaitSync();
        }

        /// <summary>
        /// Use this method if you're expecting SUCCESS_ATOM response from your query. This
        /// method offers a slight edge in performance without the need for the
        /// dynamic language runtime like the run() method uses.
        /// </summary>
        public virtual Task<T> RunResultAsync<T>(IConnection conn, object runOpts = null, CancellationToken cancelToken = default(CancellationToken))
        {
            return conn.RunResultAsync<T>(this, runOpts, cancelToken);
        }

        /// <summary>
        /// Use this method if you're expecting SUCCESS_ATOM response from your query. This
        /// method offers a slight edge in performance without the need for the
        /// dynamic language runtime like the run() method uses.
        /// </summary>
        public virtual T RunResult<T>(IConnection conn, object runOpts = null)
        {
            return RunResultAsync<T>(conn, runOpts).WaitSync();
        }

        #endregion


        #region EXTRA RUN HELPERS

        /// <summary>
        /// Helper shortcut for DML type of queries that returns # of inserts, deletes, errors.
        /// This method bypasses the dynamic language runtime for extra performance.
        /// </summary>
        public virtual Task<Result> RunResultAsync(IConnection conn, object runOpts = null, CancellationToken cancelToken = default(CancellationToken))
        {
            return conn.RunAtomAsync<Result>(this, runOpts, cancelToken);
        }

        /// <summary>
        /// Helper shortcut for DML type of queries that returns # of inserts, deletes, errors.
        /// This method bypasses the dynamic language runtime for extra performance.
        /// </summary>
        public virtual Result RunResult(IConnection conn, object runOpts = null)
        {
            return RunResultAsync(conn, runOpts).WaitSync();
        }

        /// <summary>
        /// Helper shortcut for change feeds, use if your query is expecting an infinite changes() stream.
        /// This method bypasses the dynamic language runtime for extra performance.
        /// </summary>
        /// <typeparam name="T">The document type of new/old value</typeparam>
        public virtual Task<Cursor<Change<T>>> RunChangesAsync<T>(IConnection conn, object runOpts = null, CancellationToken cancelToken = default(CancellationToken))
        {
            return conn.RunCursorAsync<Change<T>>(this, runOpts, cancelToken);
        }

        /// <summary>
        /// Helper shortcut for change feeds, use if your query is expecting an infinite changes() stream.
        /// This method bypasses the dynamic language runtime for extra performance.
        /// </summary>
        /// <typeparam name="T">The document type of new/old value</typeparam>
        public virtual Cursor<Change<T>> RunChanges<T>(IConnection conn, object runOpts = null)
        {
            return RunChangesAsync<T>(conn, runOpts).WaitSync();
        }

        #endregion


        /// <summary>
        /// Helper shortcut for grouping queries.
        /// This method bypasses the dynamic language runtime for extra performance.
        /// </summary>
        /// <typeparam name="TKey">The key type of how items are grouped</typeparam>
        /// <typeparam name="TItem">The type of items</typeparam>
        public async virtual Task<IEnumerable<GroupedResult<TKey, TItem>>> RunGroupingAsync<TKey, TItem>(IConnection conn, object runOpts = null)
        {
            var tsk = await RunAtomAsync<GroupedResultSet<TKey, TItem>>(conn, runOpts).ConfigureAwait(false);
            return tsk;
        }
        /// <summary>
        /// Helper shortcut for grouping queries.
        /// This method bypasses the dynamic language runtime for extra performance.
        /// </summary>
        /// <typeparam name="TKey">The key type of how items are grouped</typeparam>
        /// <typeparam name="TItem">The type of items</typeparam>
        public virtual IEnumerable<GroupedResult<TKey, TItem>> RunGrouping<TKey, TItem>(IConnection conn, object runOpts = null)
        {
            return RunAtomAsync<GroupedResultSet<TKey, TItem>>(conn, runOpts).WaitSync();
        }

    }
}