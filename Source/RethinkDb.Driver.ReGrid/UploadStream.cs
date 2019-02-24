using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using RethinkDb.Driver.Ast;
using RethinkDb.Driver.Net;
using RethinkDb.Driver.Utils;

namespace RethinkDb.Driver.ReGrid
{
    /// <summary>
    /// A ReGrid upload stream
    /// </summary>
    public partial class UploadStream : BaseStream
    {
        private readonly Table chunkTable;
        private readonly Table fileTable;
        private readonly object chunkInsertOpts;
        private readonly IConnection conn;
        private readonly Guid filesInfoId;

        private readonly List<byte[]> batch;
        private long batchPosition;
        private readonly int batchSize;
        private readonly int chunkSizeBytes;

        private bool closed = false;
        private bool disposed = false;
        private long length;
        private bool aborted = false;

        //private SHA256 sha256;
        private IncrementalSHA256 sha256;

        /// <summary>
        /// Creates an UploadStream to ReGrid.
        /// </summary>
        /// <param name="conn">RethinkDB Connection or ConnectonPool.</param>
        /// <param name="filesInfoId">The ID of the FileInfo in the database.</param>
        /// <param name="fileInfo">An incomplete FileInfo that exists in the database waiting to be finalized.</param>
        /// <param name="fileTable">An already namespaced Table term for FileInfo.</param>
        /// <param name="chunkTable">An already namespaced Table term for Chunk</param>
        /// <param name="options">Upload options</param>
        internal UploadStream(IConnection conn, Guid filesInfoId, FileInfo fileInfo, Table fileTable, Table chunkTable, UploadOptions options) : base(fileInfo)
        {
            this.conn = conn;
            this.filesInfoId = filesInfoId;
            this.fileTable = fileTable;
            this.chunkTable = chunkTable;
            this.chunkInsertOpts = options.ChunkInsertOptions;
            this.chunkSizeBytes = options.ChunkSizeBytes;

            this.batchSize = options.BatchSize;

            this.batch = new List<byte[]>();

            sha256 = new IncrementalSHA256();
        }

        /// <summary>
        /// Aborts an upload.
        /// </summary>
        public void Abort()
        {
            AbortAsync().WaitSync();
            //we could clean up, but for now, just leave the
            //FileInfo as Incomplete and it's chunks, fsck will clean up.
        }

#if !STANDARD
        /// <summary>
        /// Closes the stream.
        /// </summary>
        public override void Close()
        {
            CloseAsync().WaitSync();
        }
#endif

        /// <summary>
        /// Async close the upload stream.
        /// </summary>
        public override async Task CloseAsync(CancellationToken cancelToken = default)
        {
            await this.CloseInternalAsync(cancelToken).ConfigureAwait(false);
#if !STANDARD
            base.Close();
#endif
        }

        /// <summary>
        /// Aborts an upload in progress
        /// </summary>
        /// <param name="cancelToken"></param>
        /// <returns></returns>
        public async Task AbortAsync(CancellationToken cancelToken = default)
        {
            if( aborted ) return;

            ThrowIfClosedOrDisposed();
            aborted = true;

            await this.CloseAsync(cancelToken).ConfigureAwait(false);
            //we could clean up, but for now, just leave the
            //FileInfo as Incomplete and it's chunks, fsck will clean up.
        }


        /// <summary>
        /// Write to the upload stream
        /// </summary>
        public override void Write(byte[] buffer, int offset, int count)
        {
            WriteAsync(buffer, offset, count).WaitSync();
        }

        /// <summary>
        /// Async write to the upload stream
        /// </summary>
        public override async Task WriteAsync(byte[] buffer, int offset, int count, CancellationToken cancelToken)
        {
            ThrowIfClosedOrDisposed();
            while( count > 0 )
            {
                var chunk = await GetCurrentChunkAsync(cancelToken).ConfigureAwait(false);
                var partialCount = Math.Min(count, chunk.Count);
                Buffer.BlockCopy(buffer, offset, chunk.Array, chunk.Offset, partialCount);
                offset += partialCount;
                count -= partialCount;
                length += partialCount;
            }
        }

        private async Task<ArraySegment<byte>> GetCurrentChunkAsync(CancellationToken cancelToken)
        {
            var batchIndex = (int)((length - batchPosition) / chunkSizeBytes);
            if( batchIndex == batchSize ) // batch size, default 16 * 1024 * 1024 / ChunkSize
            {
                await WriteBatchAsync(cancelToken).ConfigureAwait(false);
                batch.Clear();
                batchIndex = 0;
            }
            return GetCurrentChunkSegment(batchIndex);
        }

        private ArraySegment<byte> GetCurrentChunkSegment(int batchIndex)
        {
            if( batch.Count <= batchIndex )
            {
                batch.Add(new byte[chunkSizeBytes]);
            }
            var chunk = batch[batchIndex];
            var offset = (int)(length % chunkSizeBytes);
            var count = chunkSizeBytes - offset;
            return new ArraySegment<byte>(chunk, offset, count);
        }

        private async Task WriteBatchAsync(CancellationToken cancelToken)
        {
            var chunks = PrepareChunks();

            await chunkTable.Insert(chunks.ToArray())[chunkInsertOpts].RunWriteAsync(conn, cancelToken)
                .ConfigureAwait(false);

            this.batch.Clear();
        }

        private IEnumerable<Chunk> PrepareChunks()
        {
            var chunks = new List<Chunk>();
            var n = (int)(batchPosition / chunkSizeBytes);
            foreach( var chunk in batch )
            {
                var c = new Chunk
                    {
                        FileId = filesInfoId,
                        Num = n++,
                        Data = chunk
                    };
                chunks.Add(c);
                batchPosition += chunk.Length;
                sha256.AppendData(chunk);
            }
            return chunks;
        }

        private async Task CloseInternalAsync(CancellationToken cancelToken = default)
        {
            if (this.closed) return;

            ThrowIfDisposed();
            this.closed = true;

            if (!aborted)
            {
                await WriteFinalBatchAsync(cancelToken).ConfigureAwait(false);
                await WriteFinalFileInfoAsync(cancelToken).ConfigureAwait(false);
            }
        }

        private async Task WriteFinalFileInfoAsync(CancellationToken cancelToken)
        {
            this.FileInfo.Id = this.filesInfoId;
            this.FileInfo.Length = this.length;
            this.FileInfo.SHA256 = this.sha256.GetHashStringAndReset();
            this.FileInfo.FinishedAtDate = DateTimeOffset.UtcNow;
            this.FileInfo.Status = Status.Completed;

            await this.fileTable.Replace(this.FileInfo).RunWriteAsync(conn, cancelToken)
                .ConfigureAwait(false);
        }

        private async Task WriteFinalBatchAsync(CancellationToken cancelToken)
        {
            if( batch.Count > 0 )
            {
                TruncateFinalChunk();
                await WriteBatchAsync(cancelToken).ConfigureAwait(false);
            }
        }

        /// <summary>
        /// False, upload streams are not readable.
        /// </summary>
        public override bool CanRead => false;

        /// <summary>
        /// False, upload streams cannot be seeked.
        /// </summary>
        public override bool CanSeek => false;

        /// <summary>
        /// True, upload streams can be written to..
        /// </summary>
        public override bool CanWrite => true;

        /// <summary>
        /// The in-progress length of the upload.
        /// </summary>
        public override long Length => this.length;

        /// <summary>
        /// The in-progress length of the upload. Same as <see cref="Length"/>.
        /// </summary>
        public override long Position
        {
            get { return this.length; }
            set { throw new NotSupportedException(); }
        }

        /// <summary>
        /// The unique file id this upload stream represents.
        /// </summary>
        public Guid Id => filesInfoId;

        private void TruncateFinalChunk()
        {
            var finalChunkSize = (int)(length % chunkSizeBytes);
            if( finalChunkSize > 0 )
            {
                var finalChunk = this.batch[this.batch.Count - 1];
                if( finalChunk.Length != finalChunkSize )
                {
                    var truncatedFinalChunk = new byte[finalChunkSize];
                    Buffer.BlockCopy(finalChunk, 0, truncatedFinalChunk, 0, finalChunkSize);
                    this.batch[this.batch.Count - 1] = truncatedFinalChunk;
                }
            }
        }

        /// <summary>
        /// Disposes of the upload stream.
        /// </summary>
        protected override void Dispose(bool disposing)
        {
            if( !disposed )
            {
                disposed = true;

                if( disposing )
                {
                    sha256?.Dispose();
                }
            }

            base.Dispose(disposing);
        }

        #region Helpers

        private void ThrowIfAbortedClosedOrDisposed()
        {
            if( aborted )
            {
                throw new InvalidOperationException("The upload was aborted.");
            }
            ThrowIfClosedOrDisposed();
        }

        private void ThrowIfClosedOrDisposed()
        {
            if( closed )
            {
                throw new InvalidOperationException("The stream is closed.");
            }
            ThrowIfDisposed();
        }

        private void ThrowIfDisposed()
        {
            if( disposed )
            {
                throw new ObjectDisposedException(GetType().Name);
            }
        }

        #endregion

        #region UNUSED

        /// <summary>
        /// Not supported. Does nothing.
        /// </summary>
        public override void Flush()
        {
        }

        /// <summary>
        /// Not supported. Does nothing.
        /// </summary>
        public override Task FlushAsync(CancellationToken cancellationToken)
        {
            return TaskHelper.CompletedTask;
        }

        /// <summary>
        /// Not supported.
        /// </summary>
        public override long Seek(long offset, SeekOrigin origin)
        {
            throw new NotSupportedException();
        }

        /// <summary>
        /// Not supported.
        /// </summary>
        public override void SetLength(long value)
        {
            throw new NotSupportedException();
        }

        /// <summary>
        /// Not supported.
        /// </summary>
        public override int Read(byte[] buffer, int offset, int count)
        {
            throw new NotSupportedException();
        }

        /// <summary>
        /// Not supported.
        /// </summary>
        public override Task<int> ReadAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken)
        {
            throw new NotSupportedException();
        }

        #endregion
    }
}