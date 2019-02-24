using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using RethinkDb.Driver.Model;
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
        /// <param name="options"><see cref="UploadOptions"/></param>
        /// <param name="cancelToken"><see cref="CancellationToken"/></param>
        public async Task<Guid> UploadAsync(string filename, byte[] source, UploadOptions options = null, CancellationToken cancelToken = default)
        {
            using( var ms = new MemoryStream(source) )
            {
                return await UploadAsync(filename, ms, options, cancelToken)
                    .ConfigureAwait(false);
            }
        }

        /// <summary>
        /// Upload a file from a byte array.
        /// </summary>
        /// <param name="filename">The file name</param>
        /// <param name="source">Source bytes</param>
        /// <param name="cancelToken"><see cref="CancellationToken"/></param>
        public Task<Guid> UploadAsync(string filename, byte[] source, CancellationToken cancelToken = default)
        {
            return UploadAsync(filename, source, null, cancelToken);
        }

        /// <summary>
        /// Upload a file from a stream source.
        /// </summary>
        /// <param name="filename">The file name</param>
        /// <param name="source">Source stream</param>
        /// <param name="options"><see cref="UploadOptions"/></param>
        /// <param name="cancelToken"><see cref="CancellationToken"/></param>
        public async Task<Guid> UploadAsync(string filename, Stream source, UploadOptions options = null, CancellationToken cancelToken = default)
        {
            options = options ?? new UploadOptions();
            var uploadStream = await OpenUploadStreamAsync(filename, options, cancelToken).ConfigureAwait(false);
            using( var destination = uploadStream )
            {
                var chunkSize = options.ChunkSizeBytes;
                var buffer = new byte[chunkSize];

                while( true )
                {
                    int bytesRead = 0;
                    Exception sourceException = null;
                    try
                    {
                        bytesRead = await source.ReadAsync(buffer, 0, buffer.Length, cancelToken)
                            .ConfigureAwait(false);
                    }
                    catch( Exception ex )
                    {
                        sourceException = ex;
                    }
                    if( sourceException != null )
                    {
                        try
                        {
                            await destination.AbortAsync(cancelToken)
                                .ConfigureAwait(false);
                        }
                        catch
                        {
                        }
                        throw sourceException;
                    }
                    if( bytesRead == 0 )
                    {
                        break;
                    }
                    await destination.WriteAsync(buffer, 0, bytesRead, cancelToken)
                        .ConfigureAwait(false);
                }

                await destination.CloseAsync(cancelToken)
                    .ConfigureAwait(false);

                return destination.Id;
            }
        }

        /// <summary>
        /// Upload a file from a stream source.
        /// </summary>
        /// <param name="filename">The file name</param>
        /// <param name="source">Source stream</param>
        /// <param name="cancelToken"><see cref="CancellationToken"/></param>
        public Task<Guid> UploadAsync(string filename, Stream source, CancellationToken cancelToken = default)
        {
            return UploadAsync(filename, source, null, cancelToken);
        }


        /// <summary>
        /// Upload a file from a stream
        /// </summary>
        /// <param name="filename">The file name</param>
        /// <param name="stream">source stream</param>
        /// <param name="options"><see cref="UploadOptions"/></param>
        public Guid Upload(string filename, Stream stream, UploadOptions options = null)
        {
            return UploadAsync(filename, stream, options).WaitSync();
        }

        /// <summary>
        /// Upload a file from a byte[]
        /// </summary>
        /// <param name="filename">The file name</param>
        /// <param name="bytes">source bytes</param>
        /// <param name="options"><see cref="UploadOptions"/></param>
        public Guid Upload(string filename, byte[] bytes, UploadOptions options = null)
        {
            return UploadAsync(filename, bytes, options).WaitSync();
        }

        /// <summary>
        /// Open an upload stream to write to.
        /// </summary>
        /// <param name="fileName">The file name</param>
        /// <param name="options"><see cref="UploadOptions"/></param>
        public UploadStream OpenUploadStream(string fileName, UploadOptions options = null)
        {
            return OpenUploadStreamAsync(fileName, options).WaitSync();
        }

        /// <summary>
        /// Open an upload stream to write to.
        /// </summary>
        /// <param name="fileName">The file name</param>
        /// <param name="options"><see cref="UploadOptions"/></param>
        /// <param name="cancelToken"><see cref="CancellationToken"/></param>
        public async Task<UploadStream> OpenUploadStreamAsync(string fileName, UploadOptions options = null, CancellationToken cancelToken = default)
        {
            options = options ?? new UploadOptions();

            return await CreateUploadStreamAsync(fileName, options, cancelToken)
                .ConfigureAwait(false);
        }

        /// <summary>
        /// Open an upload stream to write to.
        /// </summary>
        /// <param name="fileName">The file name</param>
        /// <param name="cancelToken"><see cref="CancellationToken"/></param>
        public Task<UploadStream> OpenUploadStreamAsync(string fileName, CancellationToken cancelToken = default )
        {
            return OpenUploadStreamAsync(fileName, null, cancelToken);
        }


        // PRIVATE
        private async Task<UploadStream> CreateUploadStreamAsync(string filename, UploadOptions options, CancellationToken cancelToken)
        {
            Ensure.IsNotNull(options, nameof(options));

            var fileInfo = new FileInfo()
                {
                    Id = options.ForceFileId,
                    Status = Status.Incomplete,
                    FileName = filename.SafePath(),
                    StartedAtDate = DateTimeOffset.UtcNow,
                    Metadata = options.Metadata,
                    ChunkSizeBytes = options.ChunkSizeBytes
                };

            var result = await fileTable.Insert(fileInfo).RunWriteAsync(conn, cancelToken)
                .ConfigureAwait(false);

            result.AssertNoErrors();
            result.AssertInserted(1);

            //Return the real ID for the FileInfo, either generated by RethinkDB
            //or specified by the caller.
            var fileInfoId = result.GeneratedKeys?[0] ?? options.ForceFileId;

            return new UploadStream(this.conn, fileInfoId, fileInfo, this.fileTable, this.chunkTable, options);
        }
    }
}