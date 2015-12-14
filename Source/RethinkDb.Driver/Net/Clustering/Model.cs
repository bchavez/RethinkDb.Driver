using Newtonsoft.Json;

namespace RethinkDb.Driver.Net.Clustering
{
    public class Server
    {
        public string Id { get; set; }
        public string Name { get; set; }
        public Network Network { get; set; }
    }

    public class Network
    {
        [JsonProperty("canonical_addresses")]
        public CanonicalAddress[] CanonicalAddress { get; set; }
        public string Hostname { get; set; }
        [JsonProperty("reql_port")]
        public int ReqlPort { get; set; }
    }

    public class CanonicalAddress
    {
        public string Host { get; set; }
        public int Port { get; set; }
    }
}