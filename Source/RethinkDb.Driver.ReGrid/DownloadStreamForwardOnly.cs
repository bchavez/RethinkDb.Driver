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
    public class DownloadStreamForwardOnly : DownloadStream
    {
        private static readonly RethinkDB r = RethinkDB.r;

        private readonly IConnection conn;
        private readonly Table chunkTable;
        private readonly string chunkIndexName;
        private SHA256 sha256;
        private bool checkSHA256;

        private bool closed;

        private Cursor<Chunk> cursor;

        private List<Chunk> batch;

        public DownloadStreamForwardOnly(Bucket bucket, IConnection conn, FileInfo fileInfo, Table chunkTable, string chunkIndexName, DownloadOptions options)
            : base(bucket, fileInfo)
        {
            this.conn = conn;
            this.chunkTable = chunkTable;
            this.chunkIndexName = chunkIndexName;
            if( options.CheckSHA256 )
            {
                this.sha256 = SHA256.Create();
                this.checkSHA256 = true;
            }

            lastChunkNumber = (int)((fileInfo.Length - 1) / fileInfo.ChunkSizeBytes);
            lastChunkSize = (int)(fileInfo.Length % fileInfo.ChunkSizeBytes);

            if( lastChunkSize == 0 )
            {
                lastChunkSize = fileInfo.ChunkSizeBytes;
            }
        }

        public override void Close()
        {
            CloseHelper();
            base.Close();
        }

        public override Task CloseAsync()
        {
            CloseHelper();
            return base.CloseAsync();
        }


        public override bool CanSeek => false;

        public override long Seek(long offset, SeekOrigin origin)
        {
            throw new NotSupportedException();
        }

        public override int Read(byte[] buffer, int offset, int count)
        {
            return ReadAsync(buffer, offset, count).WaitSync();
        }

        public override async Task<int> ReadAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken)
        {
            ThrowIfDisposed();
            var bytesRead = 0;
            while (count > 0 && position < FileInfo.Length)
            {
                var segment = await GetSegmentAsync().ConfigureAwait(false);

                var partialCount = Math.Min(count, segment.Count);
                Buffer.BlockCopy(segment.Array, segment.Offset, buffer, offset, partialCount);

                bytesRead += partialCount;
                offset += partialCount;
                count -= partialCount;
                position += partialCount;
            }

            return bytesRead;
        }


        private async Task<ArraySegment<byte>> GetSegmentAsync()
        {
            var batchIndex = (int)((position - batchPosition) / FileInfo.ChunkSizeBytes);

            if (cursor == null)
            {
                await GetFirstBatchAsync().ConfigureAwait(false);
            }
            else if (batchIndex == batch.Count)
            {
                await GetNextBatchAsync().ConfigureAwait(false);
                batchIndex = 0;
            }

            return GetSegmentHelper(batchIndex);
        }

        private async Task GetFirstBatchAsync()
        {
            //var index = new {index = this.chunkIndexName};
            //this.cursor = await chunkTable.between(r.array(this.FileInfo.Id, r.minval()), r.array(this.FileInfo.Id, r.maxval()))[index]
            //    .orderBy("n")[index]
            //    .runCursorAsync<Chunk>(conn)
            //    .ConfigureAwait(false);

            this.cursor = await GridUtility.EnumerateChunksAsync(this.bucket, this.FileInfo.Id)
                .ConfigureAwait(false);

            GetNextBatchFromCursor(cursor.BufferedSize > 0);
        }

        private async Task GetNextBatchAsync()
        {
            var hasMore = await cursor.MoveNextAsync().ConfigureAwait(false);
            GetNextBatchFromCursor(hasMore);
        }

        private void GetNextBatchFromCursor(bool hasMore)
        {
            if (!hasMore)
            {
                throw new ChunkException(FileInfo.Id, nextChunkNumber, "missing");
            }

            var previousBatch = batch;
            batch = cursor.Take(cursor.BufferedSize).ToList();

            if (previousBatch != null)
            {
                batchPosition += previousBatch.Count * FileInfo.ChunkSizeBytes ;
            }

            var lastChunkInBatch = batch.Last();
            if (lastChunkInBatch.Num == lastChunkNumber + 1 && lastChunkInBatch.Data.Length == 0)
            {
                batch.RemoveAt(batch.Count - 1);
            }

            foreach (var chunk in batch)
            {
                var n = chunk.Num;
                var bytes = chunk.Data;

                if (n != nextChunkNumber)
                {
                    throw new ChunkException(FileInfo.Id, nextChunkNumber, "missing");
                }
                nextChunkNumber++;

                var expectedChunkSize = n == lastChunkNumber ? lastChunkSize : FileInfo.ChunkSizeBytes;
                if (bytes.Length != expectedChunkSize)
                {
                    throw new ChunkException(FileInfo.Id, nextChunkNumber, "the wrong size");
                }

                if (checkSHA256)
                {
                    sha256.TransformBlock(bytes, 0, bytes.Length, null, 0);
                }
            }
        }


        private ArraySegment<byte> GetSegmentHelper(int batchIndex)
        {
            var bytes = batch[batchIndex].Data;
            var segmentOffset = (int)(position % FileInfo.ChunkSizeBytes);
            var segmentCount = bytes.Length - segmentOffset;
            return new ArraySegment<byte>(bytes, segmentOffset, segmentCount);
        }

        private void CloseHelper()
        {
            if (!closed)
            {
                closed = true;

                if (checkSHA256 && position == FileInfo.Length)
                {
                    this.sha256.TransformFinalBlock(new byte[0], 0, 0);
                    var sig = Util.GetHexString(this.sha256.Hash);

                    if (!sig.Equals(FileInfo.SHA256, StringComparison.OrdinalIgnoreCase))
                    {
                        throw new SHA256Exception(FileInfo.Id);
                    }
                }
            }
        }

        private long position;
        private int lastChunkNumber;
        private int lastChunkSize;
        private long batchPosition;
        private long nextChunkNumber;

        public override long Position
        {
            get { return position; }
            set { throw new NotSupportedException(); }
        }   
    }
}
