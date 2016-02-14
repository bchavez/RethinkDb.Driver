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


        public void Dispose()
        {
            this.registration.Dispose();
        }
    }
}