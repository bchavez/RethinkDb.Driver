using System;
using System.Threading;
using System.Threading.Tasks;
using RethinkDb.Driver.Utils;

namespace RethinkDb.Driver.ReGrid
{
    public abstract class DownloadStream : BaseStream
    {
        protected readonly Bucket bucket;

        protected DownloadStream(Bucket bucket, FileInfo fileInfo) : base(fileInfo)
        {
            this.bucket = bucket;
        }

        private bool disposed;

        public override bool CanRead => true;

        public override bool CanWrite => false;

        public override long Length => FileInfo.Length;


        // public methods
        public override void Close()
        {
            CloseAsync().WaitSync();
        }

        public override Task CloseAsync()
        {
            base.Close();
            return TaskHelper.CompletedTask;
        }

        public override void Flush()
        {
            throw new NotSupportedException();
        }

        public override Task FlushAsync(CancellationToken cancellationToken)
        {
            throw new NotSupportedException();
        }

        public override void SetLength(long value)
        {
            throw new NotSupportedException();
        }

        public override void Write(byte[] buffer, int offset, int count)
        {
            throw new NotSupportedException();
        }

        public override Task WriteAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken)
        {
            throw new NotSupportedException();
        }

        // protected methods
        protected override void Dispose(bool disposing)
        {
            if( !disposed )
            {
                disposed = true;
            }

            base.Dispose(disposing);
        }

        protected virtual void ThrowIfDisposed()
        {
            if (disposed)
            {
                throw new ObjectDisposedException(GetType().Name);
            }
        }
    }
}
