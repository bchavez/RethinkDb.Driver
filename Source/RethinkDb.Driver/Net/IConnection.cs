using System;
using System.Threading;
using System.Threading.Tasks;
using RethinkDb.Driver.Ast;

namespace RethinkDb.Driver.Net
{
    /// <summary>
    /// Represents a marker interface for either a Connection or ConnectionPool.
    /// This should really only be used for IoC purpose. Do not call methods
    /// explicitly from this interface, instead the Run* methods at the end of a query.
    /// </summary>
    public interface IConnection : IDisposable
    {
        /// <summary>
        /// DO NOT CALL THIS METHOD EXPLICITLY. USE Run*() METHODS AT THE END OF YOUR QUERY.
        /// </summary>
        Task<dynamic> RunAsync<T>(ReqlAst term, object globalOpts, CancellationToken cancelToken);

        /// <summary>
        /// DO NOT CALL THIS METHOD EXPLICITLY. USE Run*() METHODS AT THE END OF YOUR QUERY.
        /// </summary>
        Task<Cursor<T>> RunCursorAsync<T>(ReqlAst term, object globalOpts, CancellationToken cancelToken);

        /// <summary>
        /// DO NOT CALL THIS METHOD EXPLICITLY. USE Run*() METHODS AT THE END OF YOUR QUERY.
        /// </summary>
        Task<T> RunAtomAsync<T>(ReqlAst term, object globalOpts, CancellationToken cancelToken);

        /// <summary>
        /// DO NOT CALL THIS METHOD EXPLICITLY. USE Run*() METHODS AT THE END OF YOUR QUERY.
        /// </summary>
        Task<T> RunResultAsync<T>(ReqlAst term, object globalOpts, CancellationToken cancelToken);

        /// <summary>
        /// DO NOT CALL THIS METHOD EXPLICITLY. USE Run*() METHODS AT THE END OF YOUR QUERY.
        /// </summary>
        void RunNoReply(ReqlAst term, object globalOpts);

        /// <summary>
        /// DO NOT CALL THIS METHOD EXPLICITLY. USE Run*() METHODS AT THE END OF YOUR QUERY.
        /// </summary>
        Task<Response> RunUnsafeAsync(ReqlAst term, object globalOpts, CancellationToken cancelToken);
    }
}