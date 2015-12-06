using System;

namespace RethinkDb.Driver.Net.Clustering
{
    public class EpsilonHostPoolResponse : HostPoolResponse
    {
        public DateTime Started { get; set; }
        public DateTime Ended { get; set; }
    }
}