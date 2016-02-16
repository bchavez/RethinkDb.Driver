using System;
using System.Threading;
using System.Threading.Tasks;
using RethinkDb.Driver.Net;
using RethinkDb.Driver.Utils;

namespace RethinkDb.Driver.ReGrid
{
    /// <summary>
    /// Low level functions for chunks and files.
    /// </summary>
    public static class GridUtility
    {
        private static readonly RethinkDB R = RethinkDB.R;

        /// <summary>
        /// Reclaims space from incomplete files.
        /// </summary>
        public static void CleanUp(Bucket bucket)
        {
        }

        /// <summary>
        /// Enumerate the file system entries of a given particular status.
        /// </summary>
        public static Cursor<FileInfo> EnumerateFileEntries(Bucket bucket, string filename, Status status)
        {
            return EnumerateFileEntriesAsync(bucket, filename, status).WaitSync();
        }

        /// <summary>
        /// Enumerate the file system entries of a given particular status.
        /// </summary>
        public static async Task<Cursor<FileInfo>> EnumerateFileEntriesAsync(Bucket bucket, string filename, Status status,
            CancellationToken cancelToken = default(CancellationToken))
        {
            filename = filename.SafePath();

            var index = new {index = bucket.fileIndex};

            var cursor = await bucket.fileTable
                .Between(R.Array(status, filename, R.Minval()), R.Array(status, filename, R.Maxval()))[index]
                .RunCursorAsync<FileInfo>(bucket.conn, cancelToken)
                .ConfigureAwait(false);

            return cursor;
        }

        /// <summary>
        /// Enumerate all possible file system entries for a given filename.
        /// </summary>
        public static Cursor<FileInfo> EnumerateFileEntries(Bucket bucket, string filename)
        {
            return EnumerateFileEntriesAsync(bucket, filename).WaitSync();
        }

        /// <summary>
        /// Enumerate all possible file system entries for a given filename
        /// </summary>
        public static async Task<Cursor<FileInfo>> EnumerateFileEntriesAsync(Bucket bucket, string filename, CancellationToken cancelToken = default(CancellationToken))
        {
            filename = filename.SafePath();

            var index = new {index = bucket.fileIndex};

            var cursor = await bucket.fileTable
                .Between(R.Array(R.Minval(), filename, R.Minval()), R.Array(R.Maxval(), filename, R.Maxval()))[index]
                .RunCursorAsync<FileInfo>(bucket.conn, cancelToken)
                .ConfigureAwait(false);

            return cursor;
        }

        /// <summary>
        /// Gets the enumeration of chunks for file id
        /// </summary>
        public static Cursor<Chunk> EnumerateChunks(Bucket bucket, Guid fileId)
        {
            return EnumerateChunksAsync(bucket, fileId).WaitSync();
        }

        /// <summary>
        /// Gets the enumeration of chunks for file id
        /// </summary>
        public static async Task<Cursor<Chunk>> EnumerateChunksAsync(Bucket bucket, Guid fileId, CancellationToken cancelToken = default(CancellationToken))
        {
            var index = new {index = bucket.chunkIndexName};
            return await bucket.chunkTable.Between(R.Array(fileId, R.Minval()), R.Array(fileId, R.Maxval()))[index]
                .OrderBy("n")[index]
                .RunCursorAsync<Chunk>(bucket.conn, cancelToken)
                .ConfigureAwait(false);
        }

        /// <summary>
        /// Get a chunk in a bucket for file id.
        /// </summary>
        public static Chunk GetChunk(Bucket bucket, Guid fileId, long n)
        {
            return GetChunkAsync(bucket, fileId, n).WaitSync();
        }

        /// <summary>
        /// Get a chunk in a bucket for file id.
        /// </summary>
        public static async Task<Chunk> GetChunkAsync(Bucket bucket, Guid fileId, long n, CancellationToken cancelToken = default(CancellationToken))
        {
            var index = new {index = bucket.chunkIndexName};
            return await bucket.chunkTable.GetAll(R.Array(fileId, n))[index]
                .Nth(0)
                .RunResultAsync<Chunk>(bucket.conn, cancelToken)
                .ConfigureAwait(false);
        }
    }
}