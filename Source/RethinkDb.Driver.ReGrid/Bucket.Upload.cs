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
        /// <summary>
        /// Upload a file from a byte array.
        /// </summary>
        /// <param name="filename">The file name</param>
        /// <param name="source">Source bytes</param>
        public async Task<Guid> UploadAsync(string filename, byte[] source, UploadOptions options = null)
        {
            using (var ms = new MemoryStream(source))
            {
                return await UploadAsync(filename, ms, options)
                    .ConfigureAwait(false);
            }
        }

        /// <summary>
        /// Upload a file from a stream source.
        /// </summary>
        /// <param name="filename">The file name</param>
        /// <param name="source">Source stream</param>
        public async Task<Guid> UploadAsync(string filename, Stream source, UploadOptions options = null)
        {
            options = options ?? new UploadOptions();

            using (var destination = OpenUploadStream(filename, options))
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

        /// <summary>
        /// Upload a file from a stream
        /// </summary>
        /// <param name="filename">The file name</param>
        /// <param name="stream">source stream</param>
        public Guid Upload(string filename, Stream stream, UploadOptions options = null)
        {
            return UploadAsync(filename, stream, options).WaitSync();
        }

        /// <summary>
        /// Upload a file from a byte[]
        /// </summary>
        /// <param name="filename">The file name</param>
        /// <param name="bytes">source bytes</param>
        public Guid Upload(string filename, byte[] bytes, UploadOptions options = null)
        {
            return UploadAsync(filename, bytes, options).WaitSync();
        }

        /// <summary>
        /// Open an upload stream to write to.
        /// </summary>
        /// <param name="fileName">The file name</param>
        public UploadStream OpenUploadStream(string fileName, UploadOptions options = null)
        {
            options = options ?? new UploadOptions();
            return CreateUploadStreamAsync(fileName, options).WaitSync();
        }

        /// <summary>
        /// Open an upload stream to write to.
        /// </summary>
        /// <param name="fileName">The file name</param>
        public async Task<UploadStream> OpenUploadStreamAsync(string fileName,UploadOptions options)
        {
            options = options ?? new UploadOptions();

            return await CreateUploadStreamAsync(fileName, options)
                .ConfigureAwait(false);
        }





        // PRIVATE
        private async Task<UploadStream> CreateUploadStreamAsync(string filename, UploadOptions options)
        {
            Ensure.IsNotNull(options, nameof(options));

            var fileInfo = new FileInfo()
                {
                    Status = Status.Incomplete,
                    FileName = filename.SafePath(),
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
