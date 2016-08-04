using RethinkDb.Driver.Ast;

namespace RethinkDb.Driver.Net.Newtonsoft
{
    /// <summary>
    /// Newtonsoft cursor provider
    /// </summary>
    public class NewtonsoftCursorProvider : ICursorProvider
    {
        /// <summary>
        /// Called when <see cref="Connection"/> needs to build a cursor out of the response.
        /// </summary>
        public Cursor<T> MakeCursor<T>(Query query, Response firstResponse, Connection conn)
        {
            var fmt = FormatOptions.FromOptArgs(query.GlobalOptions);
            var buffer = new NewtonsoftCursorBuffer<T>(firstResponse, fmt);
            var cursor = new Cursor<T>(conn, query, firstResponse, buffer);
            return cursor;
        }
    }
}