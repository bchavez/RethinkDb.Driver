namespace RethinkDb.Driver.Net.Clustering
{
    /// <summary>
    /// Represents a RethinkDB server endpoint.
    /// </summary>
    public class Seed
    {
        /// <summary>
        /// Create a new RethinkDB seed endpoint.
        /// </summary>
        public Seed(string hostname, int port = RethinkDBConstants.DefaultPort)
        {
            this.Hostname = hostname;
            this.Port = port;
        }

        /// <summary>
        /// RethinkDB hostname
        /// </summary>
        public string Hostname { get;  }

        /// <summary>
        /// RethinkDB port
        /// </summary>
        public int Port { get; }

        internal string GetEndpoint()
        {
            return $"{this.Hostname}:{this.Port}";
        }
    }
}