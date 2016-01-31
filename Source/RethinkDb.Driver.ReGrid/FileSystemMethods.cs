using System;
using System.Collections.Generic;
using System.IO;
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
        

        public static Cursor<FileInfo> GetAllFileRevisions(this Bucket bucket, string filename)
        {
            return GetAllRevisionsAsync(bucket, filename).WaitSync();
        }

        public static async Task<Cursor<FileInfo>> GetAllRevisionsAsync(this Bucket bucket, string filename)
        {
            filename = filename.SafePath();

            var index = new { index = bucket.fileIndexPath };

            var cursor = await bucket.fileTable
                .between(r.array(Status.Completed, filename, r.minval()), r.array(Status.Completed, filename, r.maxval()))[index]
                .runCursorAsync<FileInfo>(bucket.conn)
                .ConfigureAwait(false);

            return cursor;
        }




        public static FileInfo GetFileInfo(this Bucket bucket, Guid fileId)
        {
            return GetFileInfoAsync(bucket, fileId).WaitSync();
        }

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

        public static FileInfo GetFileInfoByName(this Bucket bucket, string filename, int revision = -1)
        {
            return GetFileInfoByNameAsync(bucket, filename, revision).WaitSync();
        }

        public static async Task<FileInfo> GetFileInfoByNameAsync(this Bucket bucket, string filename, int revision = -1)
        {
            filename = filename.SafePath();

            var index = new { index = bucket.fileIndexPath };

            var between = bucket.fileTable
                .between(r.array(Status.Completed, filename, r.minval()), r.array(Status.Completed, filename, r.maxval()))[index];

            var sort = revision >= 0 ? r.asc(bucket.fileIndexPath) : r.desc(bucket.fileIndexPath) as ReqlExpr;

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






        public static void Delete(this Bucket bucket, Guid fileId, bool softDelete = true, object deleteOpts = null)
        {
            DeleteAsync(bucket, fileId, softDelete, deleteOpts).WaitSync();
        }

        public static async Task DeleteAsync(this Bucket bucket, Guid fileId, bool softDelete = true, object deleteOpts = null)
        {
            var result = await bucket.fileTable.get(fileId)
                .update(
                    r.hashMap(FileInfo.StatusJsonName, Status.Deleted)
                        .with(FileInfo.DeletedDateJsonName, DateTimeOffset.UtcNow))[deleteOpts]
                .runResultAsync(bucket.conn)
                .ConfigureAwait(false);

            result.AssertReplaced(1);

            if (!softDelete)
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




    }
}