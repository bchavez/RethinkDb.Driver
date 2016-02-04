using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using RethinkDb.Driver.Ast;
using RethinkDb.Driver.Model;
using RethinkDb.Driver.Net;
using RethinkDb.Driver.Utils;

namespace RethinkDb.Driver.ReGrid
{
    public static class FileSystemMethods
    {
        private static readonly RethinkDB r = RethinkDB.r;
        

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
        public static async Task<Cursor<FileInfo>> GetAllRevisionsAsync(this Bucket bucket, string filename)
        {
            filename = filename.SafePath();

            var index = new { index = bucket.fileIndex };

            var cursor = await bucket.fileTable
                .between(r.array(Status.Completed, filename, r.minval()), r.array(Status.Completed, filename, r.maxval()))[index]
                .runCursorAsync<FileInfo>(bucket.conn)
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
        public static async Task<FileInfo> GetFileInfoAsync(this Bucket bucket, Guid fileId)
        {
            var fileInfo = await bucket.fileTable
                .get(fileId).runAtomAsync<FileInfo>(bucket.conn)
                .ConfigureAwait(false);

            if (fileInfo == null)
            {
                throw new FileNotFoundException(fileId);
            }

            return fileInfo;
        }

        /// <summary>
        /// Gets a FileInfo for a given filename and revision. Considers only 'completed' uploads.
        /// </summary>
        /// <param name="revision">-1: The most recent revision. -2: The second most recent revision. -3: The third most recent revision. 0: The original stored file. 1: The first revision. 2: The second revision. etc...</param>
        public static FileInfo GetFileInfoByName(this Bucket bucket, string filename, int revision = -1)
        {
            return GetFileInfoByNameAsync(bucket, filename, revision).WaitSync();
        }

        /// <summary>
        /// Gets a FileInfo for a given filename and revision. Considers only 'completed' uploads.
        /// </summary>
        /// <param name="revision">-1: The most recent revision. -2: The second most recent revision. -3: The third most recent revision. 0: The original stored file. 1: The first revision. 2: The second revision. etc...</param>
        public static async Task<FileInfo> GetFileInfoByNameAsync(this Bucket bucket, string filename, int revision = -1)
        {
            filename = filename.SafePath();

            var index = new { index = bucket.fileIndex };

            var between = bucket.fileTable
                .between(r.array(Status.Completed, filename, r.minval()), r.array(Status.Completed, filename, r.maxval()))[index];

            var sort = revision >= 0 ? r.asc(bucket.fileIndex) : r.desc(bucket.fileIndex) as ReqlExpr;

            revision = revision >= 0 ? revision : (revision * -1) - 1;

            var selection = await between.orderBy()[new { index = sort }]
                .skip(revision).limit(1) // so the driver doesn't throw an error when a file isn't found.
                .runResultAsync<List<FileInfo>>(bucket.conn)
                .ConfigureAwait(false);

            var fileinfo = selection.FirstOrDefault();
            if (fileinfo == null)
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
        public static Cursor<FileInfo> ListFilesByPrefix(this Bucket bucket, string path)
        {
            return ListFilesByPrefixAsync(bucket, path).WaitSync();
        }

        /// <summary>
        /// List files with a given path.
        /// </summary>
        /// <param name="path">The path starting with /</param>
        public static async Task<Cursor<FileInfo>> ListFilesByPrefixAsync(this Bucket bucket, string path)
        {
            path = path.SafePath();

            var parts = path.Split(PrefixChar, StringSplitOptions.RemoveEmptyEntries);
            

            var index = new {index = bucket.fileIndexPrefix};

            var between = bucket.fileTable
                .between(r.array(parts, r.minval()), r.array(parts, r.maxval()))[index];

            var sort = r.desc(bucket.fileIndexPrefix);

            var selection = await between.orderBy()[new { index = sort }]
                .runCursorAsync<FileInfo>(bucket.conn)
                .ConfigureAwait(false);

            return selection;
        }





        /// <summary>
        /// Deletes a file in the bucket.
        /// </summary>
        /// <param name="mode">Soft deletes are atomic. Hard are atomic on the file but not a file's chunks.</param>
        /// <param name="deleteOpts">Delete durability options. See ReQL API.</param>
        public static void DeleteRevision(this Bucket bucket, Guid fileId, DeleteMode mode = DeleteMode.Soft, object deleteOpts = null)
        {
            DeleteRevisionAsync(bucket, fileId, mode, deleteOpts).WaitSync();
        }

        /// <summary>
        /// Deletes a file in the bucket.
        /// </summary>
        /// <param name="mode">Soft deletes are atomic. Hard are atomic on the file but not a file's chunks.</param>
        /// <param name="deleteOpts">Delete durability options. See ReQL API.</param>
        public static async Task DeleteRevisionAsync(this Bucket bucket, Guid fileId, DeleteMode mode = DeleteMode.Soft, object deleteOpts = null)
        {
            var result = await bucket.fileTable.get(fileId)
                .update(
                    r.hashMap(FileInfo.StatusJsonName, Status.Deleted)
                        .with(FileInfo.DeletedDateJsonName, DateTimeOffset.UtcNow)
                )[deleteOpts]
                .runResultAsync(bucket.conn)
                .ConfigureAwait(false);

            result.AssertReplaced(1);

            if (mode == DeleteMode.Hard)
            {
                //delete the chunks....
                await bucket.chunkTable.between(
                    r.array(fileId, r.minval()),
                    r.array(fileId, r.maxval()))[new { index = bucket.chunkIndexName }]
                    .delete()[deleteOpts]
                    .runResultAsync(bucket.conn)
                    .ConfigureAwait(false);

                //then delete the file.
                await bucket.fileTable.get(fileId).delete()[deleteOpts]
                    .runResultAsync(bucket.conn)
                    .ConfigureAwait(false);
            }
        }

        /// <summary>
        /// Deletes a file and it's associated revisions. Iteratively deletes revisions for a file one by one.
        /// </summary>
        /// <param name="mode">Soft deletes are atomic. Hard are atomic on the file but not a file's chunks.</param>
        /// <param name="deleteOpts">Delete durability options. See ReQL API.</param>
        public static void DeleteAllRevisions(this Bucket bucket, string filename, DeleteMode mode = DeleteMode.Soft, object deleteOpts = null)
        {
            DeleteAllRevisionsAsync(bucket, filename, mode, deleteOpts).WaitSync();
        }

        /// <summary>
        /// Deletes a file and it's associated revisions. Iteratively deletes revisions for a file one by one.
        /// </summary>
        /// <param name="mode">Soft deletes are atomic. Hard are atomic on the file but not a file's chunks.</param>
        /// <param name="deleteOpts">Delete durability options. See ReQL API.</param>
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