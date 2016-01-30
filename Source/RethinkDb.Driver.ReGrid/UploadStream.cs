using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Threading;
using System.Threading.Tasks;
using RethinkDb.Driver.Ast;
using RethinkDb.Driver.Net;
using RethinkDb.Driver.Utils;

namespace RethinkDb.Driver.ReGrid
{
    public class UploadStream : BaseStream
    {
        private readonly Table chunkTable;
        private readonly Table fileTable;
        private readonly object chunkInsertOpts;
        private readonly IConnection conn;
        private readonly Guid filesInfoId;

        private List<byte[]> batch;
        private long batchPosition;
        private int batchSize;
        private int chunkSizeBytes;

        private bool closed = false;
        private bool disposed = false;
        private long length;
        private bool aborted = false;

        private SHA256 sha256;

        /// <summary>
        /// Creates an UploadStream to ReGrid.
        /// </summary>
        /// <param name="conn">RethinkDB Connection or ConnectonPool.</param>
        /// <param name="filesInfoId">The ID of the FileInfo in the database.</param>
        /// <param name="fileInfo">An incomplete FileInfo that exists in the database waiting to be finalized.</param>
        /// <param name="fileTable">An already namespaced Table term for FileInfo.</param>
        /// <param name="chunkTable">An already namespaced Table term for Chunk</param>
        /// <param name="chunkInsertOpts">The insert options for chunks inserted into the chunk table.</param>
        /// <param name="chunkSize">The size of chunk documents in the chunk table.</param>
        /// <param name="batchSize">The size of the stream buffer before flushing chunks to the chunk table. Default 16MB.</param>
        /// <param name="options">Upload options</param>
        public UploadStream(IConnection conn, Guid filesInfoId, FileInfo fileInfo, Table fileTable, Table chunkTable, UploadOptions options) : base(fileInfo)
        {
            this.conn = conn;
            this.filesInfoId = filesInfoId;
            this.fileTable = fileTable;
            this.chunkTable = chunkTable;
            this.chunkInsertOpts = options.ChunkInsertOptions;
            this.chunkSizeBytes = options.ChunkSizeBytes;
            
            this.batchSize = options.BatchSize;

            this.batch = new List<byte[]>();

            sha256 = SHA256.Create();
        }

        public void Abort()
        {
            AbortAsync().WaitSync();
            //we could clean up, but for now, just leave the
            //FileInfo as Incomplete and it's chunks, fsck will clean up.
        }

        public async Task AbortAsync()
        {
            if( aborted ) return;

            ThrowIfClosedOrDisposed();
            aborted = true;

            //we could clean up, but for now, just leave the
            //FileInfo as Incomplete and it's chunks, fsck will clean up.
        }


        public override void Write(byte[] buffer, int offset, int count)
        {
            WriteAsync(buffer, offset, count).WaitSync();
        }


        public override async Task WriteAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken)
        {
            ThrowIfClosedOrDisposed();
            while( count > 0 )
            {
                var chunk = await GetCurrentChunkAsync().ConfigureAwait(false);
                var partialCount = Math.Min(count, chunk.Count);
                Buffer.BlockCopy(buffer, offset, chunk.Array, chunk.Offset, partialCount);
                offset += partialCount;
                count -= partialCount;
                length += partialCount;
            }
        }

        private async Task<ArraySegment<byte>> GetCurrentChunkAsync()
        {
            var batchIndex = (int)((length - batchPosition) / chunkSizeBytes);
            if (batchIndex == batchSize) // batch size, default 16 * 1024 * 1024 / ChunkSize
            {
                await WriteBatchAsync().ConfigureAwait(false);
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

        private async Task WriteBatchAsync()
        {
            var chunks = PrepareChunks();

            await chunkTable.insert(chunks.ToArray())[chunkInsertOpts].runResultAsync(conn)
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
                        FilesId = filesInfoId,
                        Num = n++,
                        Data = chunk
                    };
                chunks.Add(c);
                batchPosition += chunk.Length;
                sha256.TransformBlock(chunk, 0, chunk.Length, null, 0);
            }
            return chunks;
        }

        public override void Close()
        {
            CloseAsync().WaitSync();
        }

        public override async Task CloseAsync()
        {
            if (this.closed) return;

            ThrowIfDisposed();
            this.closed = true;

            if (!aborted)
            {
                await WriteFinalBatchAsync().ConfigureAwait(false);
                await WriteFinalFileInfoAsync().ConfigureAwait(false);
            }

            base.Close();
        }

        private async Task WriteFinalFileInfoAsync()
        {
            this.FileInfo.Id = this.filesInfoId;
            this.FileInfo.Length = this.length;
            this.FileInfo.SHA256 = Util.GetHexString(this.sha256.Hash);
            this.FileInfo.CreatedDate = DateTimeOffset.UtcNow;
            this.FileInfo.Status = Status.Completed;

            await this.fileTable.replace(this.FileInfo).runResultAsync(conn)
                .ConfigureAwait(false);
        }

        private async Task WriteFinalBatchAsync()
        {
            if (batch.Count > 0)
            {
                TruncateFinalChunk();
                await WriteBatchAsync().ConfigureAwait(false);
            }
            sha256.TransformFinalBlock(new byte[0], 0, 0);
        }

        public override bool CanRead => false;
        public override bool CanSeek => false;
        public override bool CanWrite => true;

        public override long Length => this.length;

        public override long Position
        {
            get { return this.length; }
            set { throw new NotSupportedException(); }
        }

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

        protected override void Dispose(bool disposing)
        {
            if (!disposed)
            {
                disposed = true;

                if (disposing)
                {
                    if (sha256 != null)
                    {
                        sha256.Dispose();
                    }
                }
            }

            base.Dispose(disposing);
        }

        #region Helpers
        private void ThrowIfAbortedClosedOrDisposed()
        {
            if (aborted)
            {
                throw new InvalidOperationException("The upload was aborted.");
            }
            ThrowIfClosedOrDisposed();
        }

        private void ThrowIfClosedOrDisposed()
        {
            if (closed)
            {
                throw new InvalidOperationException("The stream is closed.");
            }
            ThrowIfDisposed();
        }

        private void ThrowIfDisposed()
        {
            if (disposed)
            {
                throw new ObjectDisposedException(GetType().Name);
            }
        }
        #endregion


        #region UNUSED
        public override void Flush()
        {
        }

        public override Task FlushAsync(CancellationToken cancellationToken)
        {
            return TaskHelper.CompletedTask;
        }

        public override long Seek(long offset, SeekOrigin origin)
        {
            throw new NotSupportedException();
        }

        public override void SetLength(long value)
        {
            throw new NotSupportedException();
        }

        public override int Read(byte[] buffer, int offset, int count)
        {
            throw new NotSupportedException();
        }

        public override Task<int> ReadAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken)
        {
            throw new NotSupportedException();
        }
        #endregion

    }
}
