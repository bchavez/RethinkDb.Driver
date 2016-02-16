using Newtonsoft.Json;

namespace RethinkDb.Driver.Net.Clustering
{
    internal class Server
    {
        public string Id { get; set; }
        public string Name { get; set; }
        public Network Network { get; set; }
    }

    internal class Network
    {
        [JsonProperty("canonical_addresses")]
        public CanonicalAddress[] CanonicalAddress { get; set; }

        public string Hostname { get; set; }

        [JsonProperty("reql_port")]
        public int ReqlPort { get; set; }
    }

    internal class CanonicalAddress
    {
        public string Host { get; set; }
        public int Port { get; set; }
    }
}