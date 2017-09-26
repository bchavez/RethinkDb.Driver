using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using RethinkDb.Driver.Utils;

namespace RethinkDb.Driver.ReGrid
{
    public partial class Bucket
    {
        //PUBLIC
        //AS BYTE ARRAY
        /// <summary>
        /// Download file as byte[].
        /// </summary>
        /// <param name="filename">The file name</param>
        /// <param name="revision">-1: The most recent revision. -2: The second most recent revision. -3: The third most recent revision. 0: The original stored file. 1: The first revision. 2: The second revision. etc...</param>
        /// <param name="options"><see cref="DownloadOptions"/></param>
        public byte[] DownloadAsBytesByName(string filename, int revision = -1, DownloadOptions options = null)
        {
            return DownloadAsBytesByNameAsync(filename, revision, options).WaitSync();
        }

        /// <summary>
        /// Download file as byte[].
        /// </summary>
        /// <param name="filename">The file name</param>
        /// <param name="revision">-1: The most recent revision. -2: The second most recent revision. -3: The third most recent revision. 0: The original stored file. 1: The first revision. 2: The second revision. etc...</param>
        /// <param name="options"><see cref="DownloadOptions"/></param>
        /// <param name="cancelToken"><see cref="CancellationToken"/></param>
        public async Task<byte[]> DownloadAsBytesByNameAsync(string filename, int revision = -1, DownloadOptions options = null,
            CancellationToken cancelToken = default)
        {
            options = options ?? new DownloadOptions();
            var fileInfo = await this.GetFileInfoByNameAsync(filename, revision, cancelToken)
                .ConfigureAwait(false);
            return await DownloadBytesHelperAsync(fileInfo, options, cancelToken)
                .ConfigureAwait(false);
        }

        /// <summary>
        /// Download file as byte[].
        /// </summary>
        /// <param name="filename">The file name</param>
        /// <param name="revision">-1: The most recent revision. -2: The second most recent revision. -3: The third most recent revision. 0: The original stored file. 1: The first revision. 2: The second revision. etc...</param>
        /// <param name="cancelToken"><see cref="CancellationToken"/></param>
        public Task<byte[]> DownloadAsBytesByNameAsync(string filename, CancellationToken cancelToken = default, int revision = -1)
        {
            return DownloadAsBytesByNameAsync(filename, revision, null, cancelToken);
        }


        /// <summary>
        /// Download file as byte[] by fileId.
        /// </summary>
        /// <param name="fileId">The fileId</param>
        /// <param name="options"><see cref="DownloadOptions"/></param>
        public byte[] DownloadAsBytes(Guid fileId, DownloadOptions options = null)
        {
            return DownloadAsBytesAsync(fileId, options).WaitSync();
        }

        /// <summary>
        /// Download file as byte[] by fileId.
        /// </summary>
        /// <param name="fileId">The fileId</param>
        /// <param name="options"><see cref="DownloadOptions"/></param>
        /// <param name="cancelToken"><see cref="CancellationToken"/></param>
        public async Task<byte[]> DownloadAsBytesAsync(Guid fileId, DownloadOptions options = null, CancellationToken cancelToken = default)
        {
            options = options ?? new DownloadOptions();
            return await DownloadBytesHelperAsync(fileId, options, cancelToken)
                .ConfigureAwait(false);
        }

        /// <summary>
        /// Download file as byte[] by fileId.
        /// </summary>
        /// <param name="fileId">The fileId</param>
        /// <param name="cancelToken"><see cref="CancellationToken"/></param>
        public Task<byte[]> DownloadAsBytesAsync(Guid fileId, CancellationToken cancelToken = default)
        {
            return DownloadAsBytesAsync(fileId, null, cancelToken);
        }


        // TO STREAM
        /// <summary>
        /// Download a file to a stream.
        /// </summary>
        /// <param name="fileId">The fileId</param>
        /// <param name="destination"></param>
        /// <param name="options"><see cref="DownloadOptions"/></param>
        public void DownloadToStream(Guid fileId, Stream destination, DownloadOptions options = null)
        {
            DownloadToStreamAsync(fileId, destination, options).WaitSync();
        }

        /// <summary>
        /// Download a file to a stream.
        /// </summary>
        /// <param name="fileId">The fileId</param>
        /// <param name="destination"></param>
        /// <param name="options"><see cref="DownloadOptions"/></param>
        /// <param name="cancelToken"><see cref="CancellationToken"/></param>
        public async Task DownloadToStreamAsync(Guid fileId, Stream destination, DownloadOptions options = null,
            CancellationToken cancelToken = default)
        {
            options = options ?? new DownloadOptions();
            await DownloadToStreamHelperAsync(fileId, destination, options, cancelToken)
                .ConfigureAwait(false);
        }

        /// <summary>
        /// Download a file to a stream.
        /// </summary>
        /// <param name="fileId">The fileId</param>
        /// <param name="destination"></param>
        /// <param name="cancelToken"><see cref="CancellationToken"/></param>
        public Task DownloadToStreamAsync(Guid fileId, Stream destination, CancellationToken cancelToken = default)
        {
            return DownloadToStreamAsync(fileId, destination, null, cancelToken);
        }


        /// <summary>
        /// Download file to a stream.
        /// </summary>
        /// <param name="filename">The file name</param>
        /// <param name="revision">-1: The most recent revision. -2: The second most recent revision. -3: The third most recent revision. 0: The original stored file. 1: The first revision. 2: The second revision. etc...</param>
        /// <param name="destination">The destination stream to write to.</param>
        /// <param name="options"><see cref="DownloadOptions"/></param>
        public void DownloadToStreamByName(string filename, Stream destination, int revision = -1, DownloadOptions options = null)
        {
            DownloadToStreamByNameAsync(filename, destination, revision, options).WaitSync();
        }

        /// <summary>
        /// Download file to a stream.
        /// </summary>
        /// <param name="filename">The file name</param>
        /// <param name="revision">-1: The most recent revision. -2: The second most recent revision. -3: The third most recent revision. 0: The original stored file. 1: The first revision. 2: The second revision. etc...</param>
        /// <param name="destination">The destination stream to write to.</param>
        /// <param name="options"><see cref="DownloadOptions"/></param>
        /// <param name="cancelToken"><see cref="CancellationToken"/></param>
        public async Task DownloadToStreamByNameAsync(string filename, Stream destination, int revision = -1, DownloadOptions options = null,
            CancellationToken cancelToken = default)
        {
            Ensure.IsNotNull(filename, nameof(filename));
            Ensure.IsNotNull(destination, nameof(destination));
            options = options ?? new DownloadOptions();

            var fileInfo = await this.GetFileInfoByNameAsync(filename, revision, cancelToken)
                .ConfigureAwait(false);
            await DownloadToStreamHelperAsync(fileInfo, destination, options, cancelToken)
                .ConfigureAwait(false);
        }

        /// <summary>
        /// Download file to a stream.
        /// </summary>
        /// <param name="filename">The file name</param>
        /// <param name="revision">-1: The most recent revision. -2: The second most recent revision. -3: The third most recent revision. 0: The original stored file. 1: The first revision. 2: The second revision. etc...</param>
        /// <param name="destination">The destination stream to write to.</param>
        /// <param name="cancelToken"><see cref="CancellationToken"/></param>
        public Task DownloadToStreamByNameAsync(string filename, Stream destination, CancellationToken cancelToken = default, int revision = -1)
        {
            return DownloadToStreamByNameAsync(filename, destination, revision, null, cancelToken);
        }


        // OPEN AS STREAM
        /// <summary>
        /// Open a download stream to read from.
        /// </summary>
        /// <param name="filename">The file name</param>
        /// <param name="revision">-1: The most recent revision. -2: The second most recent revision. -3: The third most recent revision. 0: The original stored file. 1: The first revision. 2: The second revision. etc...</param>
        /// <param name="options"><see cref="DownloadOptions"/></param>
        public DownloadStream OpenDownloadStream(string filename, int revision = -1, DownloadOptions options = null)
        {
            return OpenDownloadStreamAsync(filename, options, revision).WaitSync();
        }

        /// <summary>
        /// Open a download stream to read from.
        /// </summary>
        /// <param name="filename">The file name</param>
        /// <param name="revision">-1: The most recent revision. -2: The second most recent revision. -3: The third most recent revision. 0: The original stored file. 1: The first revision. 2: The second revision. etc...</param>
        /// <param name="options"><see cref="DownloadOptions"/></param>
        /// <param name="cancelToken"><see cref="CancellationToken"/></param>
        public async Task<DownloadStream> OpenDownloadStreamAsync(string filename, DownloadOptions options = null, int revision = -1,
            CancellationToken cancelToken = default)
        {
            options = options ?? new DownloadOptions();

            var fileInfo = await this.GetFileInfoByNameAsync(filename, revision, cancelToken)
                .ConfigureAwait(false);

            return CreateDownloadStream(fileInfo, options);
        }

        /// <summary>
        /// Open a download stream to read from.
        /// </summary>
        /// <param name="filename">The file name</param>
        /// <param name="revision">-1: The most recent revision. -2: The second most recent revision. -3: The third most recent revision. 0: The original stored file. 1: The first revision. 2: The second revision. etc...</param>
        /// <param name="cancelToken"><see cref="CancellationToken"/></param>
        public Task<DownloadStream> OpenDownloadStreamAsync(string filename, CancellationToken cancelToken = default, int revision = -1)
        {
            return OpenDownloadStreamAsync(filename, null, revision, cancelToken);
        }


        //PRIVATE
        private async Task<byte[]> DownloadBytesHelperAsync(Guid fileId, DownloadOptions options, CancellationToken cancelToken)
        {
            Ensure.IsNotNull(options, nameof(options));

            var fileInfo = await this.GetFileInfoAsync(fileId, cancelToken)
                .ConfigureAwait(false);

            return await DownloadBytesHelperAsync(fileInfo, options, cancelToken)
                .ConfigureAwait(false);
        }

        private async Task<byte[]> DownloadBytesHelperAsync(FileInfo fileinfo, DownloadOptions options, CancellationToken cancelToken)
        {
            Ensure.IsNotNull(options, nameof(options));

            if( fileinfo.Length > int.MaxValue )
            {
                throw new NotSupportedException("ReGrid stored file is too large to be returned as a byte array.");
            }

            using( var destination = new MemoryStream((int)fileinfo.Length) )
            {
                await DownloadToStreamHelperAsync(fileinfo, destination, options, cancelToken)
                    .ConfigureAwait(false);


                return destination.ToArray();
            }
        }

        private async Task DownloadToStreamHelperAsync(Guid id, Stream destination, DownloadOptions options, CancellationToken cancelToken)
        {
            var fileInfo = await this.GetFileInfoAsync(id, cancelToken)
                .ConfigureAwait(false);

            await DownloadToStreamHelperAsync(fileInfo, destination, options, cancelToken)
                .ConfigureAwait(false);
        }

        private async Task DownloadToStreamHelperAsync(FileInfo fileinfo, Stream destination, DownloadOptions options, CancellationToken cancelToken)
        {
            Ensure.IsNotNull(options, nameof(options));

            using( var source = new DownloadStreamForwardOnly(this, fileinfo, options) )
            {
                var count = source.Length;
                var buffer = new byte[fileinfo.ChunkSizeBytes];

                while( count > 0 )
                {
                    var partialCount = (int)Math.Min(buffer.Length, count);
                    await source.ReadAsync(buffer, 0, partialCount, cancelToken).ConfigureAwait(false);
                    await destination.WriteAsync(buffer, 0, partialCount, cancelToken).ConfigureAwait(false);
                    count -= partialCount;
                }

                await source.CloseAsync(cancelToken).ConfigureAwait(false);
            }
        }

        private DownloadStream CreateDownloadStream(FileInfo fileinfo, DownloadOptions options)
        {
            Ensure.IsNotNull(options, nameof(options));

            if( options.CheckSHA256 && options.Seekable )
            {
                throw new ArgumentException("CheckSHA256 can only be used when Seekable is false.");
            }

            if( options.Seekable )
            {
                //make seekable
                return new SeekableDownloadStream(this, fileinfo);
            }

            return new DownloadStreamForwardOnly(this, fileinfo, options);
        }
    }
}