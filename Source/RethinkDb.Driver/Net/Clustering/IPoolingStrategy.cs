namespace RethinkDb.Driver.Net.Clustering
{
    public interface IPoolingStrategy : IConnection
    {
        HostEntry[] HostList { get; }
        void AddHost(string host, Connection conn);
        void Shutdown();
    }
}