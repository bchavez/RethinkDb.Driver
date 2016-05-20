using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using RethinkDb.Driver.Net;
using RethinkDb.Driver.Utils;

namespace RethinkDb.Driver.ReGrid
{
    internal class DownloadStreamForwardOnly : DownloadStream
    {
        private long position;
        private readonly int lastChunkNumber;
        private readonly int lastChunkSize;
        private long batchPosition;
        private long nextChunkNumber;


        private IncrementalSHA256 sha256;
        private bool checkSHA256;

        private bool closed;

        private Cursor<Chunk> cursor;

        private List<Chunk> batch;

        public DownloadStreamForwardOnly(Bucket bucket, FileInfo fileInfo, DownloadOptions options)
            : base(bucket, fileInfo)
        {
            if( options.CheckSHA256 )
            {
                this.sha256 = new IncrementalSHA256();
                this.checkSHA256 = true;
            }

            lastChunkNumber = (int)((fileInfo.Length - 1) / fileInfo.ChunkSizeBytes);
            lastChunkSize = (int)(fileInfo.Length % fileInfo.ChunkSizeBytes);

            if( lastChunkSize == 0 )
            {
                lastChunkSize = fileInfo.ChunkSizeBytes;
            }
        }


#if !STANDARD
        public override void Close()
        {
            CloseHelper();
            base.Close();
        }
#endif  


        public override Task CloseAsync(CancellationToken cancelToken = default(CancellationToken))
        {
            CloseHelper();
            return base.CloseAsync(cancelToken);
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

        public override async Task<int> ReadAsync(byte[] buffer, int offset, int count, CancellationToken cancelToken)
        {
            ThrowIfDisposed();
            var bytesRead = 0;
            while( count > 0 && position < FileInfo.Length )
            {
                var segment = await GetSegmentAsync(cancelToken).ConfigureAwait(false);

                var partialCount = Math.Min(count, segment.Count);
                Buffer.BlockCopy(segment.Array, segment.Offset, buffer, offset, partialCount);

                bytesRead += partialCount;
                offset += partialCount;
                count -= partialCount;
                position += partialCount;
            }

            return bytesRead;
        }


        private async Task<ArraySegment<byte>> GetSegmentAsync(CancellationToken cancelToken)
        {
            var batchIndex = (int)((position - batchPosition) / FileInfo.ChunkSizeBytes);

            if( cursor == null )
            {
                await GetFirstBatchAsync(cancelToken).ConfigureAwait(false);
            }
            else if( batchIndex == batch.Count )
            {
                await GetNextBatchAsync(cancelToken).ConfigureAwait(false);
                batchIndex = 0;
            }

            return GetSegmentHelper(batchIndex);
        }

        private async Task GetFirstBatchAsync(CancellationToken cancelToken)
        {
            //var index = new {index = this.chunkIndexName};
            //this.cursor = await chunkTable.between(r.array(this.FileInfo.Id, r.minval()), r.array(this.FileInfo.Id, r.maxval()))[index]
            //    .orderBy("n")[index]
            //    .runCursorAsync<Chunk>(conn)
            //    .ConfigureAwait(false);

            this.cursor = await GridUtility.EnumerateChunksAsync(this.bucket, this.FileInfo.Id, cancelToken)
                .ConfigureAwait(false);

            await GetNextBatchAsync(cancelToken).ConfigureAwait(false);
        }

        private async Task GetNextBatchAsync(CancellationToken cancelToken)
        {
            var hasMore = await cursor.MoveNextAsync(cancelToken).ConfigureAwait(false);
            if( !hasMore )
            {
                throw new ChunkException(FileInfo.Id, nextChunkNumber, "missing");
            }
            GetNextBatchFromCursor();
        }

        private void GetNextBatchFromCursor()
        {
            var previousBatch = batch;
            batch = cursor.BufferedItems;
            //don't forget the current
            //that was just iterated on
            batch.Insert(0, cursor.Current);
            cursor.ClearBuffer();

            if( previousBatch != null )
            {
                batchPosition += previousBatch.Count * FileInfo.ChunkSizeBytes;
            }

            var lastChunkInBatch = batch.Last();
            if( lastChunkInBatch.Num == lastChunkNumber + 1 && lastChunkInBatch.Data.Length == 0 )
            {
                batch.RemoveAt(batch.Count - 1);
            }

            foreach( var chunk in batch )
            {
                var n = chunk.Num;
                var bytes = chunk.Data;

                if( n != nextChunkNumber )
                {
                    throw new ChunkException(FileInfo.Id, nextChunkNumber, "missing");
                }
                nextChunkNumber++;

                var expectedChunkSize = n == lastChunkNumber ? lastChunkSize : FileInfo.ChunkSizeBytes;
                if( bytes.Length != expectedChunkSize )
                {
                    throw new ChunkException(FileInfo.Id, nextChunkNumber, "the wrong size");
                }

                if( checkSHA256 )
                {
                    sha256.AppendData(bytes);
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
            if( !closed )
            {
                closed = true;

                if( checkSHA256 && position == FileInfo.Length )
                {
                    var sig = this.sha256.GetHashStringAndReset();

                    this.sha256.Dispose();

                    if( !sig.Equals(FileInfo.SHA256, StringComparison.OrdinalIgnoreCase) )
                    {
                        throw new SHA256Exception(FileInfo.Id);
                    }
                }
            }
        }

        public override long Position
        {
            get { return position; }
            set { throw new NotSupportedException(); }
        }
    }
}