using System.Net;

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
        public Seed(string ipAddress, int port = RethinkDBConstants.DefaultPort)
        {
            IPAddress.Parse(ipAddress);
            this.IpAddress = ipAddress;
            this.Port = port;
        }

        /// <summary>
        /// RethinkDB hostname
        /// </summary>
        public string IpAddress { get;  }

        /// <summary>
        /// RethinkDB port
        /// </summary>
        public int Port { get; }
    }
}