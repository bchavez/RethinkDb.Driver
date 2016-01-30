using System;
using System.Collections.Generic;
using System.IO;
using System.Security.Cryptography;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;
using RethinkDb.Driver.Ast;
using RethinkDb.Driver.Model;
using RethinkDb.Driver.Net;
using RethinkDb.Driver.Utils;

namespace RethinkDb.Driver.ReGrid
{
    public partial class Bucket
    {
        //PUBLIC

        public async Task<Guid> UploadAsync(string fileName, byte[] bytes, UploadOptions options = null)
        {
            using (var ms = new MemoryStream(bytes))
            {
                return await UploadAsync(fileName, ms, options)
                    .ConfigureAwait(false);
            }
        }

        public async Task<Guid> UploadAsync(string fileName, Stream source, UploadOptions options = null)
        {
            options = options ?? new UploadOptions();

            using (var destination = OpenUploadStream(fileName, options))
            {
                var chunkSize = options.ChunkSizeBytes;
                var buffer = new byte[chunkSize];

                while (true)
                {
                    int bytesRead = 0;
                    Exception sourceException = null;
                    try
                    {
                        bytesRead = await source.ReadAsync(buffer, 0, buffer.Length)
                            .ConfigureAwait(false);
                    }
                    catch (Exception ex)
                    {
                        sourceException = ex;
                    }
                    if (sourceException != null)
                    {
                        try
                        {
                            await destination.AbortAsync()
                                .ConfigureAwait(false);
                        }
                        catch { }
                        throw sourceException;
                    }
                    if (bytesRead == 0)
                    {
                        break;
                    }
                    await destination.WriteAsync(buffer, 0, bytesRead)
                        .ConfigureAwait(false);
                }

                await destination.CloseAsync()
                    .ConfigureAwait(false);

                return destination.Id;
            }
        }

        public Guid Upload(string fileName, Stream stream, UploadOptions options = null)
        {
            return UploadAsync(fileName, stream, options).WaitSync();
        }

        public Guid Upload(string fileName, byte[] bytes, UploadOptions options = null)
        {
            return UploadAsync(fileName, bytes, options).WaitSync();
        }

        public UploadStream OpenUploadStream(
            string fileName,
            UploadOptions options = null)
        {
            options = options ?? new UploadOptions();
            return CreateUploadStreamAsync(fileName, options).WaitSync();
        }

        public async Task<UploadStream> OpenUploadStreamAsync(
            string fileName,
            UploadOptions options)
        {
            options = options ?? new UploadOptions();

            return await CreateUploadStreamAsync(fileName, options)
                .ConfigureAwait(false);
        }





        // PRIVATE
        private async Task<UploadStream> CreateUploadStreamAsync(string fileName, UploadOptions options)
        {
            Ensure.IsNotNull(options, nameof(options));

            var fileInfo = new FileInfo()
            {
                Status = Status.Incomplete,
                FileName = fileName,
                StartedAtDate = DateTimeOffset.UtcNow,
                Metadata = options.Metadata,
                ChunkSizeBytes = options.ChunkSizeBytes
            };

            var result = await fileTable.insert(fileInfo).runResultAsync(conn)
                .ConfigureAwait(false);

            result.AssertInserted(1);

            var fileInfoId = result.GeneratedKeys[0];

            return new UploadStream(this.conn, fileInfoId, fileInfo, this.fileTable, this.chunkTable, options);
        }
    }
}
