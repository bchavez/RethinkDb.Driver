using System.Threading;
using RethinkDb.Driver.Utils;

namespace RethinkDb.Driver.Net
{
    public class Awaiter : CancellableTask
    {
        public Awaiter(CancellationToken cancelToken) : base(cancelToken)
        {
        }
    }
}