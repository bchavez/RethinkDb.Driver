using System.Threading;
using RethinkDb.Driver.Utils;

namespace RethinkDb.Driver.Net
{
    internal class Awaiter : CancellableTask
    {
        public Awaiter(CancellationToken cancelToken) : base(cancelToken)
        {
        }
    }
}