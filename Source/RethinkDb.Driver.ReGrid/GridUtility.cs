using System.Threading.Tasks;
using RethinkDb.Driver.Net;
using RethinkDb.Driver.Utils;

namespace RethinkDb.Driver.ReGrid
{
    public static class GridUtility
    {
        private static readonly RethinkDB r = RethinkDB.r;

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

            var index = new { index = bucket.fileIndexPath };

            var cursor = await bucket.fileTable
                .between(r.array(status, filename, r.minval()), r.array(status, filename, r.maxval()))[index]
                .runCursorAsync<FileInfo>(bucket.conn)
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

            var index = new { index = bucket.fileIndexPath };

            var cursor = await bucket.fileTable
                .between(r.array(r.minval(), filename, r.minval()), r.array( r.maxval(), filename, r.maxval()))[index]
                .runCursorAsync<FileInfo>(bucket.conn)
                .ConfigureAwait(false);

            return cursor;
        }

    }
}