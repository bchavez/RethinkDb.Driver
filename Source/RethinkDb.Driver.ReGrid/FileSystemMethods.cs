using System;
using System.IO;
using System.Threading.Tasks;
using RethinkDb.Driver.Model;
using RethinkDb.Driver.Net;
using RethinkDb.Driver.Utils;

namespace RethinkDb.Driver.ReGrid
{
    public static class FileSystemMethods
    {
        private static readonly RethinkDB r = RethinkDB.r;
        

        public static Cursor<FileInfo> FindAllRevisions(this Bucket bucket, string filename)
        {
            return FindAllRevisionsAsync(bucket, filename).WaitSync();
        }

        public static async Task<Cursor<FileInfo>> FindAllRevisionsAsync(this Bucket bucket, string filename)
        {
            filename = filename.SafePath();

            var index = new { index = bucket.fileIndexPath };

            var cursor = await bucket.fileTable
                .between(r.array(Status.Completed, filename, r.minval()), r.array(Status.Completed, filename, r.maxval()))[index]
                .runCursorAsync<FileInfo>(bucket.conn)
                .ConfigureAwait(false);

            return cursor;
        }




        public static Cursor<FileInfo> FindFileInfo(this Bucket bucket, string filename, int revision = -1)
        {
            filename = filename.SafePath();

            return null;
        }

        public static Cursor<FileInfo> FindFileInfo(this Bucket bucket, string filename )
        {
            return null;
        }

        public static async Task<Cursor<FileInfo>> FindFileAsync(this Bucket bucket, string filename)
        {
            filename = filename.SafePath();

            return null;
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