using System.Collections;

namespace RethinkDb.Driver.Net
{
    internal interface ICursor : IEnumerable, IEnumerator
    {
        void SetError(string msg);
        long Token { get; }
    }
}