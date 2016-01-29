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
        private MD5 md5;
        private bool checkMD5;

        private bool closed;

        private Cursor<Chunk> cursor;

        private List<Chunk> batch;

        public DownloadStreamForwardOnly(IConnection conn, FileInfo fileInfo, Table chunkTable, string chunkIndexName, DownloadOptions options)
            : base(fileInfo)
        {
            this.conn = conn;
            this.chunkTable = chunkTable;
            this.chunkIndexName = chunkIndexName;
            if( options.CheckMD5 )
            {
                this.md5 = MD5.Create();
                this.checkMD5 = true;
            }

            lastChunkNumber = (int)((fileInfo.Length - 1) / fileInfo.ChunkSize);
            lastChunkSize = (int)(fileInfo.Length % fileInfo.ChunkSize);

            if( lastChunkSize == 0 )
            {
                lastChunkSize = fileInfo.ChunkSize;
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
            var batchIndex = (int)((position - batchPosition) / FileInfo.ChunkSize);

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
            var index = new {index = this.chunkIndexName};
            this.cursor = chunkTable.between(r.array(this.FileInfo.Id, r.minval()), r.array(this.FileInfo.Id, r.maxval()))[index]
                .orderBy("n")[index]
                .runCursor<Chunk>(conn);
            
            //await GetNextBatchAsync().ConfigureAwait(false);
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
                batchPosition += previousBatch.Count * FileInfo.ChunkSize ;
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

                var expectedChunkSize = n == lastChunkNumber ? lastChunkSize : FileInfo.ChunkSize;
                if (bytes.Length != expectedChunkSize)
                {
                    throw new ChunkException(FileInfo.Id, nextChunkNumber, "the wrong size");
                }

                if (checkMD5)
                {
                    md5.TransformBlock(bytes, 0, bytes.Length, null, 0);
                }
            }
        }


        private ArraySegment<byte> GetSegmentHelper(int batchIndex)
        {
            var bytes = batch[batchIndex].Data;
            var segmentOffset = (int)(position % FileInfo.ChunkSize);
            var segmentCount = bytes.Length - segmentOffset;
            return new ArraySegment<byte>(bytes, segmentOffset, segmentCount);
        }

        private void CloseHelper()
        {
            if (!closed)
            {
                closed = true;

                if (checkMD5 && position == FileInfo.Length)
                {
                    this.md5.TransformFinalBlock(new byte[0], 0, 0);
                    var calculatedMd5 = Util.GetHexString(this.md5.Hash);

                    if (!calculatedMd5.Equals(FileInfo.MD5, StringComparison.OrdinalIgnoreCase))
                    {
                        throw new MD5Exception(FileInfo.Id);
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
