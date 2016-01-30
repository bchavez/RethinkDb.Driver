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
        //AS BYTE ARRAY
        public byte[] DownloadBytesByName(string fileName, int revision = -1, DownloadOptions options = null)
        {
            return DownloadBytesByNameAsync(fileName, revision, options).WaitSync();
        }

        public async Task<byte[]> DownloadBytesByNameAsync(string fileName, int revision = -1, DownloadOptions options = null)
        {
            options = options ?? new DownloadOptions();
            var fileInfo = await GetFileInfoByNameAsync(fileName, revision);
            return await DownloadBytesHelperAsync(fileInfo, options);
        }

        public byte[] DownloadBytes(Guid fileId, DownloadOptions options = null)
        {
            return DownloadBytesAsync(fileId, options).WaitSync();
        }

        public async Task<byte[]> DownloadBytesAsync(Guid fileId, DownloadOptions options = null)
        {
            options = options ?? new DownloadOptions();
            return await DownloadBytesHelperAsync(fileId, options);
        }



        // TO STREAM
        public void DownloadToStream(Guid fileId, Stream destination, DownloadOptions options = null)
        {
            DownloadToStreamAsync(fileId, destination, options).WaitSync();
        }

        public async Task DownloadToStreamAsync(Guid fileId, Stream destination, DownloadOptions options = null)
        {
            options = options ?? new DownloadOptions();
            await DownloadToStreamHelperAsync(fileId, destination, options)
                .ConfigureAwait(false);
        }


        public void DownloadToStreamByName(string filename, Stream destination, int revision = -1, DownloadOptions options = null)
        {
            DownloadToStreamByNameAsync(filename, destination, revision, options).WaitSync();
        }

        public async Task DownloadToStreamByNameAsync(string filename, Stream destination, int revision = -1, DownloadOptions options = null)
        {
            Ensure.IsNotNull(filename, nameof(filename));
            Ensure.IsNotNull(destination, nameof(destination));
            options = options ?? new DownloadOptions();

            var fileInfo = await GetFileInfoByNameAsync(filename, revision)
                .ConfigureAwait(false);
            await DownloadToStreamHelperAsync(fileInfo, destination, options)
                .ConfigureAwait(false);
        }

        




        // OPEN AS STREAM
        public DownloadStream OpenDownloadStream(string fileName, int revision = -1, DownloadOptions options = null)
        {
            options = options ?? new DownloadOptions();
            return OpenDownloadStreamAsync(fileName, options, revision).WaitSync();
        }

        public async Task<DownloadStream> OpenDownloadStreamAsync(string fileName, DownloadOptions options, int revision = -1)
        {
            var fileInfo = await GetFileInfoByNameAsync(fileName, revision)
                .ConfigureAwait(false);

            return CreateDownloadStream(fileInfo, options);
        }






        //PRIVATE
        private async Task<byte[]> DownloadBytesHelperAsync(Guid fileId, DownloadOptions options)
        {
            Ensure.IsNotNull(options, nameof(options));

            var fileInfo = await GetFileInfoAsync(fileId)
                .ConfigureAwait(false);

            return await DownloadBytesHelperAsync(fileInfo, options)
                .ConfigureAwait(false);
        }
        private async Task<byte[]> DownloadBytesHelperAsync(FileInfo fileInfo, DownloadOptions options)
        {
            Ensure.IsNotNull(options, nameof(options));

            if (fileInfo.Length > int.MaxValue)
            {
                throw new NotSupportedException("ReGrid stored file is too large to be returned as a byte array.");
            }

            using (var destination = new MemoryStream((int)fileInfo.Length))
            {
                await DownloadToStreamHelperAsync(fileInfo, destination, options)
                    .ConfigureAwait(false);

                return destination.GetBuffer();
            }
        }

        private async Task DownloadToStreamHelperAsync(Guid id, Stream destination, DownloadOptions options)
        {
            var fileInfo = await GetFileInfoAsync(id)
                .ConfigureAwait(false);

            await DownloadToStreamHelperAsync(fileInfo, destination, options)
                .ConfigureAwait(false);
        }

        private async Task DownloadToStreamHelperAsync(FileInfo fileInfo, Stream destination, DownloadOptions options)
        {
            Ensure.IsNotNull(options, nameof(options));

            using (var source = new DownloadStreamForwardOnly(conn, fileInfo, this.chunkTable, this.chunkIndexName, options))
            {
                var count = source.Length;
                var buffer = new byte[fileInfo.ChunkSizeBytes];

                while (count > 0)
                {
                    var partialCount = (int)Math.Min(buffer.Length, count);
                    await source.ReadAsync(buffer, 0, partialCount).ConfigureAwait(false);
                    await destination.WriteAsync(buffer, 0, partialCount).ConfigureAwait(false);
                    count -= partialCount;
                }

                await source.CloseAsync().ConfigureAwait(false);
            }
        }

        private DownloadStream CreateDownloadStream(FileInfo fileInfo, DownloadOptions options)
        {
            Ensure.IsNotNull(options, nameof(options));

            if (options.CheckSHA256 && options.Seekable)
            {
                throw new ArgumentException("CheckSHA256 can only be used when Seekable is false.");
            }

            if (options.Seekable)
            {
                //make seekable
                throw new NotImplementedException();
            }

            return new DownloadStreamForwardOnly(this.conn, fileInfo, this.chunkTable, this.chunkIndexName, options);
        }

    }
}
