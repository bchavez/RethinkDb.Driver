using System;
using System.Collections.Generic;
using System.IO;
using System.Security.Cryptography;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;
using RethinkDb.Driver.Ast;
using RethinkDb.Driver.Model;
using RethinkDb.Driver.Net;

namespace RethinkDb.Driver.ReGrid
{
    public class CrazyBucket
    {
        private readonly IConnection conn;
        private readonly BucketConfig config;
        private Db db;
        private string databaseName;
        private string chunkTableName;
        private string chunkIndexName;

        private string fileTableName;
        private string fileIndexIncomplete;

        private object tableOpts;
        private string fileIndexPath;

        public CrazyBucket()
        {
        }

        private async Task<Guid> CrazyUploadTest(string fileName, Stream stream, JObject metadata, int chunkSize, object insertOpts = null, int workers = 1)
        {
            var file = new FileInfo()
                {
                    Status = Status.Incomplete,
                    FileName = fileName,
                    Length = stream.Length,
                    StartedDate = DateTimeOffset.UtcNow,
                    Metadata = metadata,
                    ChunkSize = chunkSize
                };

            var fileTable = this.db.table(this.fileTableName);
            var chunkTable = this.db.table(this.chunkTableName);

            var fileResult = fileTable.insert(file).runResult(this.conn);
            fileResult.AssertInserted(1);

            var md5 = MD5.Create();

            var fileId = fileResult.GeneratedKeys[0];

            file.Id = fileId;

            var buffer = new byte[chunkSize];

            var chunkNumber = 0;

            var pendingChunks = new List<Task<Result>>();

            while (true)
            {
                var bytesRead = await stream.ReadAsync(buffer, 0, chunkSize)
                    .ConfigureAwait(false);


                if (bytesRead <= 0)
                {
                    break;
                }

                //Update the MD5 block
                md5.TransformBlock(buffer, 0, bytesRead, null, 0);

                var chunk = new Chunk()
                    {
                        FilesId = fileId,
                        Data = buffer,
                        Num = chunkNumber++
                    };

                //task a worker for the chunk insert.
                var task = Task.Run(() => chunkTable.insert(chunk)[insertOpts].runResultAsync(this.conn));
                pendingChunks.Add(task);

                //if we're at the threshold, wait for at least one to finish.
                while (pendingChunks.Count >= workers)
                {
                    var doneTask = await Task.WhenAny(pendingChunks)
                        .ConfigureAwait(false);

                    //a worker completed, check if it failed.
                    if (doneTask.IsFaulted || doneTask.IsCanceled ||
                        doneTask.Exception != null ||
                        doneTask.Result.Inserted != 1)
                    {
                        //not good, we failed.
                        throw new UploadException("One of the chunks failed to transfer.", doneTask.Exception);
                    }
                    pendingChunks.Remove(doneTask);
                }
            }

            //be sure to await for all pending worker tasks to finish before
            //transforming the final block.
            await Task.WhenAll(pendingChunks)
                .ConfigureAwait(false);

            md5.TransformFinalBlock(new byte[0], 0, 0);

            file.MD5 = Util.GetHexString(md5.Hash);
            file.UploadDate = DateTimeOffset.UtcNow;
            file.Status = Status.Completed;

            await fileTable.replace(file).runAsync(this.conn)
                .ConfigureAwait(false);

            return fileId;
        }
    }
}
