using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using RethinkDb.Driver.Ast;
using RethinkDb.Driver.Model;
using RethinkDb.Driver.Net;
using RethinkDb.Driver.Utils;

namespace RethinkDb.Driver.ReGrid
{
    /// <summary>
    /// File system methods for ReGrid.
    /// </summary>
    public static class FileSystemMethods
    {
        private static readonly RethinkDB R = RethinkDB.R;


        /// <summary>
        /// Gets all 'completed' file revisions for a given file. Deleted and incomplete files are excluded.
        /// </summary>
        public static Cursor<FileInfo> GetAllRevisions(this Bucket bucket, string filename)
        {
            return GetAllRevisionsAsync(bucket, filename).WaitSync();
        }

        /// <summary>
        /// Gets all 'completed' file revisions for a given file. Deleted and incomplete files are excluded.
        /// </summary>
        public static async Task<Cursor<FileInfo>> GetAllRevisionsAsync(this Bucket bucket, string filename, CancellationToken cancelToken = default)
        {
            filename = filename.SafePath();

            var index = new {index = bucket.fileIndex};

            var cursor = await bucket.fileTable
                .Between(R.Array(Status.Completed, filename, R.Minval()), R.Array(Status.Completed, filename, R.Maxval()))[index]
                .RunCursorAsync<FileInfo>(bucket.conn, cancelToken)
                .ConfigureAwait(false);

            return cursor;
        }


        /// <summary>
        /// Gets the FileInfo for a given fileId
        /// </summary>
        public static FileInfo GetFileInfo(this Bucket bucket, Guid fileId)
        {
            return GetFileInfoAsync(bucket, fileId).WaitSync();
        }

        /// <summary>
        /// Gets the FileInfo for a given fileId
        /// </summary>
        public static async Task<FileInfo> GetFileInfoAsync(this Bucket bucket, Guid fileId, CancellationToken cancelToken = default)
        {
            var fileInfo = await bucket.fileTable
                .Get(fileId).RunAtomAsync<FileInfo>(bucket.conn, cancelToken)
                .ConfigureAwait(false);

            if( fileInfo == null )
            {
                throw new FileNotFoundException(fileId);
            }

            return fileInfo;
        }

        /// <summary>
        /// Gets a FileInfo for a given filename and revision. Considers only 'completed' uploads.
        /// </summary>
        /// <param name="revision">-1: The most recent revision. -2: The second most recent revision. -3: The third most recent revision. 0: The original stored file. 1: The first revision. 2: The second revision. etc...</param>
        /// <param name="bucket"><see cref="Bucket"/></param>
        /// <param name="filename">The filename</param>
        public static FileInfo GetFileInfoByName(this Bucket bucket, string filename, int revision = -1)
        {
            return GetFileInfoByNameAsync(bucket, filename, revision).WaitSync();
        }

        /// <summary>
        /// Gets a FileInfo for a given filename and revision. Considers only 'completed' uploads.
        /// </summary>
        /// <param name="revision">-1: The most recent revision. -2: The second most recent revision. -3: The third most recent revision. 0: The original stored file. 1: The first revision. 2: The second revision. etc...</param>
        /// <param name="bucket"><see cref="Bucket"/></param>
        /// <param name="cancelToken"><see cref="CancellationToken"/></param>
        /// <param name="filename">The filename</param>
        public static async Task<FileInfo> GetFileInfoByNameAsync(this Bucket bucket, string filename, int revision = -1,
            CancellationToken cancelToken = default)
        {
            filename = filename.SafePath();

            var index = new {index = bucket.fileIndex};

            var between = bucket.fileTable
                .Between(R.Array(Status.Completed, filename, R.Minval()), R.Array(Status.Completed, filename, R.Maxval()))[index];

            var sort = revision >= 0 ? R.Asc(bucket.fileIndex) : R.Desc(bucket.fileIndex) as ReqlExpr;

            revision = revision >= 0 ? revision : (revision * -1) - 1;

            var selection = await between.OrderBy()[new {index = sort}]
                .Skip(revision).Limit(1) // so the driver doesn't throw an error when a file isn't found.
                .RunResultAsync<List<FileInfo>>(bucket.conn, cancelToken)
                .ConfigureAwait(false);

            var fileinfo = selection.FirstOrDefault();
            if( fileinfo == null )
            {
                throw new FileNotFoundException(filename, revision);
            }

            return fileinfo;
        }


        private static char[] PrefixChar = {'/'};


        /// <summary>
        /// List files with a given path.
        /// </summary>
        /// <param name="path">The path starting with /</param>
        /// <param name="bucket"><see cref="Bucket"/></param>
        public static Cursor<FileInfo> ListFilesByPrefix(this Bucket bucket, string path)
        {
            return ListFilesByPrefixAsync(bucket, path).WaitSync();
        }

        /// <summary>
        /// List files with a given path.
        /// </summary>
        /// <param name="path">The path starting with /</param>
        /// <param name="bucket"><see cref="Bucket"/></param>
        /// <param name="cancelToken"><see cref="CancellationToken"/></param>
        public static async Task<Cursor<FileInfo>> ListFilesByPrefixAsync(this Bucket bucket, string path, CancellationToken cancelToken = default)
        {
            path = path.SafePath();

            var parts = path.Split(PrefixChar, StringSplitOptions.RemoveEmptyEntries);


            var index = new {index = bucket.fileIndexPrefix};

            var between = bucket.fileTable
                .Between(R.Array(parts, R.Minval()), R.Array(parts, R.Maxval()))[index];

            var sort = R.Desc(bucket.fileIndexPrefix);

            var selection = await between.OrderBy()[new {index = sort}]
                .RunCursorAsync<FileInfo>(bucket.conn, cancelToken)
                .ConfigureAwait(false);

            return selection;
        }


        /// <summary>
        /// Deletes a file in the bucket.
        /// </summary>
        /// <param name="mode">Soft deletes are atomic. Hard are atomic on the file but not a file's chunks.</param>
        /// <param name="deleteOpts">Delete durability options. See ReQL API.</param>
        /// <param name="bucket"><see cref="Bucket"/></param>
        /// <param name="fileId"><see cref="FileInfo.Id"/></param>
        public static void DeleteRevision(this Bucket bucket, Guid fileId, DeleteMode mode = DeleteMode.Soft, object deleteOpts = null)
        {
            DeleteRevisionAsync(bucket, fileId, mode, deleteOpts).WaitSync();
        }

        /// <summary>
        /// Deletes a file in the bucket.
        /// </summary>
        /// <param name="mode">Soft deletes are atomic. Hard are atomic on the file but not a file's chunks.</param>
        /// <param name="deleteOpts">Delete durability options. See ReQL API.</param>
        /// <param name="bucket"><see cref="Bucket"/></param>
        /// <param name="cancelToken"><see cref="CancellationToken"/></param>
        /// <param name="fileId"><see cref="FileInfo.Id"/></param>
        public static async Task DeleteRevisionAsync(this Bucket bucket, Guid fileId, DeleteMode mode = DeleteMode.Soft, object deleteOpts = null,
            CancellationToken cancelToken = default)
        {
            var result = await bucket.fileTable.Get(fileId)
                .Update(
                    R.HashMap(FileInfo.StatusJsonName, Status.Deleted)
                        .With(FileInfo.DeletedDateJsonName, DateTimeOffset.UtcNow)
                )[deleteOpts]
                .RunWriteAsync(bucket.conn, cancelToken)
                .ConfigureAwait(false);

            result.AssertReplaced(1);

            if( mode == DeleteMode.Hard )
            {
                //delete the chunks....
                await bucket.chunkTable.Between(
                    R.Array(fileId, R.Minval()),
                    R.Array(fileId, R.Maxval()))[new {index = bucket.chunkIndexName}]
                    .Delete()[deleteOpts]
                    .RunWriteAsync(bucket.conn, cancelToken)
                    .ConfigureAwait(false);

                //then delete the file.
                await bucket.fileTable.Get(fileId).Delete()[deleteOpts]
                    .RunWriteAsync(bucket.conn, cancelToken)
                    .ConfigureAwait(false);
            }
        }

        /// <summary>
        /// Deletes a file and it's associated revisions. Iteratively deletes revisions for a file one by one.
        /// </summary>
        /// <param name="mode">Soft deletes are atomic. Hard are atomic on the file but not a file's chunks.</param>
        /// <param name="deleteOpts">Delete durability options. See ReQL API.</param>
        /// <param name="bucket"><see cref="Bucket"/></param>
        /// <param name="filename">The filename</param>
        public static void DeleteAllRevisions(this Bucket bucket, string filename, DeleteMode mode = DeleteMode.Soft, object deleteOpts = null)
        {
            DeleteAllRevisionsAsync(bucket, filename, mode, deleteOpts).WaitSync();
        }

        /// <summary>
        /// Deletes a file and it's associated revisions. Iteratively deletes revisions for a file one by one.
        /// </summary>
        /// <param name="mode">Soft deletes are atomic. Hard are atomic on the file but not a file's chunks.</param>
        /// <param name="deleteOpts">Delete durability options. See ReQL API.</param>
        /// <param name="bucket"><see cref="Bucket"/></param>
        /// <param name="filename">The filename</param>
        public static async Task DeleteAllRevisionsAsync(this Bucket bucket, string filename, DeleteMode mode = DeleteMode.Soft, object deleteOpts = null)
        {
            filename = filename.SafePath();

            var result = await GetAllRevisionsAsync(bucket, filename)
                .ConfigureAwait(false);

            foreach( var file in result )
            {
                await DeleteRevisionAsync(bucket, file.Id, mode, deleteOpts)
                    .ConfigureAwait(false);
            }
        }
    }
}