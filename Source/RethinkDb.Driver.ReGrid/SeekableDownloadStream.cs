using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using RethinkDb.Driver.Utils;

namespace RethinkDb.Driver.ReGrid
{
    internal class SeekableDownloadStream : DownloadStream
    {
        // private fields
        private byte[] chunk;
        private long num = -1;
        private long position;

        // constructors
        public SeekableDownloadStream(Bucket bucket, FileInfo fileInfo)
            : base(bucket, fileInfo)
        {
        }

        // public properties
        public override bool CanSeek => true;

        public override long Position
        {
            get { return position; }
            set
            {
                Ensure.IsGreaterThanOrEqualToZero(value, nameof(value));
                position = value;
            }
        }


        // methods
        public override int Read(byte[] buffer, int offset, int count)
        {
            Ensure.IsNotNull(buffer, nameof(buffer));
            Ensure.IsBetween(offset, 0, buffer.Length, nameof(offset));
            Ensure.IsBetween(count, 0, buffer.Length - offset, nameof(count));
            ThrowIfDisposed();

            var bytesRead = 0;
            while( count > 0 && position < FileInfo.Length )
            {
                var segment = GetSegment();

                var partialCount = Math.Min(count, segment.Count);
                Buffer.BlockCopy(segment.Array, segment.Offset, buffer, offset, partialCount);

                bytesRead += partialCount;
                offset += partialCount;
                count -= partialCount;
                position += partialCount;
            }

            return bytesRead;
        }

        public override async Task<int> ReadAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken)
        {
            Ensure.IsNotNull(buffer, nameof(buffer));
            Ensure.IsBetween(offset, 0, buffer.Length, nameof(offset));
            Ensure.IsBetween(count, 0, buffer.Length - offset, nameof(count));
            ThrowIfDisposed();

            var bytesRead = 0;
            while( count > 0 && position < FileInfo.Length )
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

        public override long Seek(long offset, SeekOrigin origin)
        {
            long newPosition;
            switch( origin )
            {
                case SeekOrigin.Begin:
                    newPosition = offset;
                    break;
                case SeekOrigin.Current:
                    newPosition = position + offset;
                    break;
                case SeekOrigin.End:
                    newPosition = position + offset;
                    break;
                default:
                    throw new ArgumentException("Invalid origin.", "origin");
            }
            if( newPosition < 0 )
            {
                throw new IOException("Position must be greater than or equal to zero.");
            }
            Position = newPosition;
            return newPosition;
        }

        // private methods


        private void GetChunk(long n)
        {
            GetChunkAsync(n).WaitSync();
        }

        private async Task GetChunkAsync(long n)
        {
            var chunk = await GridUtility.GetChunkAsync(this.bucket, this.FileInfo.Id, n).ConfigureAwait(false);
            this.chunk = GetChunkHelper(n, chunk);
            num = n;
        }

        private byte[] GetChunkHelper(long n, Chunk doc)
        {
            var data = doc.Data;

            var chunkSizeBytes = FileInfo.ChunkSizeBytes;
            var lastChunk = FileInfo.Length / FileInfo.ChunkSizeBytes;
            var expectedChunkSize = n == lastChunk ? FileInfo.Length % chunkSizeBytes : chunkSizeBytes;
            if( data.Length != expectedChunkSize )
            {
                throw new ChunkException(FileInfo.Id, n, "the wrong size");
            }

            return data;
        }

        private ArraySegment<byte> GetSegment()
        {
            var n = position / FileInfo.ChunkSizeBytes;
            if( num != n )
            {
                GetChunk(n);
            }

            var segmentOffset = (int)(position % FileInfo.ChunkSizeBytes);
            var segmentCount = chunk.Length - segmentOffset;

            return new ArraySegment<byte>(chunk, segmentOffset, segmentCount);
        }

        private async Task<ArraySegment<byte>> GetSegmentAsync()
        {
            var n = position / FileInfo.ChunkSizeBytes;
            if( num != n )
            {
                await GetChunkAsync(n).ConfigureAwait(false);
            }

            var segmentOffset = (int)(position % FileInfo.ChunkSizeBytes);
            var segmentCount = chunk.Length - segmentOffset;

            return new ArraySegment<byte>(chunk, segmentOffset, segmentCount);
        }
    }
}