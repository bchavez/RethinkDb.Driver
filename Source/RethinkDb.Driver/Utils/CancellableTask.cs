#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member

using System;
using System.Threading;
using System.Threading.Tasks;
using RethinkDb.Driver.Net;

namespace RethinkDb.Driver.Utils
{
    public class CancellableTask : TaskCompletionSource<Response>, IDisposable
    {
        private readonly CancellationToken cancelToken;
        private CancellationTokenRegistration registration;


        public CancellableTask(CancellationToken cancelToken)
        {
            this.cancelToken = cancelToken;
            this.registration = this.cancelToken.Register(OnCancellation, false);
        }

        private void OnCancellation()
        {
            this.SetCanceled();
        }

        private bool disposed = false;

        public void Dispose()
        {
            if( !disposed )
            {
                this.registration.Dispose();
            }
        }
    }
}