using System;
using System.Threading.Tasks;
using RethinkDb.Driver.Net;
using RethinkDb.Driver.Utils;

namespace RethinkDb.Driver.ReGrid
{
    public static class GridUtility
    {
        private static readonly RethinkDB R = RethinkDB.r;

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
        public static async Task<Cursor<FileInfo>> EnumerateFileEntriesAsync(Bucket bucket, string filename, Status status)
        {
            filename = filename.SafePath();

            var index = new { index = bucket.fileIndex };

            var cursor = await bucket.fileTable
                .Between(R.Array(status, filename, R.Minval()), R.Array(status, filename, R.Maxval()))[index]
                .RunCursorAsync<FileInfo>(bucket.conn)
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
        public static async Task<Cursor<FileInfo>> EnumerateFileEntriesAsync(Bucket bucket, string filename)
        {
            filename = filename.SafePath();

            var index = new { index = bucket.fileIndex };

            var cursor = await bucket.fileTable
                .Between(R.Array(R.Minval(), filename, R.Minval()), R.Array( R.Maxval(), filename, R.Maxval()))[index]
                .RunCursorAsync<FileInfo>(bucket.conn)
                .ConfigureAwait(false);

            return cursor;
        }

        public static Cursor<Chunk> EnumerateChunks(Bucket bucket, Guid fileId)
        {
            return EnumerateChunksAsync(bucket, fileId).WaitSync();
        }
        public static async Task<Cursor<Chunk>> EnumerateChunksAsync(Bucket bucket, Guid fileId)
        {
            var index = new { index = bucket.chunkIndexName };
            return await bucket.chunkTable.Between(R.Array(fileId, R.Minval()), R.Array(fileId, R.Maxval()))[index]
                .OrderBy("n")[index]
                .RunCursorAsync<Chunk>(bucket.conn)
                .ConfigureAwait(false);
        }

        public static Chunk GetChunk(Bucket bucket, Guid fileId, long n)
        {
            return GetChunkAsync(bucket, fileId, n).WaitSync();
        }
        public static async Task<Chunk> GetChunkAsync(Bucket bucket, Guid fileId, long n)
        {
            var index = new { index = bucket.chunkIndexName };
            return await bucket.chunkTable.GetAll(R.Array(fileId, n))[index]
                .Nth(0)
                .RunResultAsync<Chunk>(bucket.conn)
                .ConfigureAwait(false);
        }
    }
}