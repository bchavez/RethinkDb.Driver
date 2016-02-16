
#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member

namespace RethinkDb.Driver.Net.Clustering
{
    public interface IPoolingStrategy : IConnection
    {
        HostEntry[] HostList { get; }
        void AddHost(string host, Connection conn);
        void Shutdown();
    }
}