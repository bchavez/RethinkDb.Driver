using System;
using System.Diagnostics;

namespace RethinkDb.Driver.Net.Clustering
{
    public class EpsilonHostPoolResponse : HostPoolResponse
    {
        public DateTime Started { get; set; }
        public DateTime? Ended { get; set; }

        public override void Mark(Exception error)
        {
            this.Ended = this.Ended ?? DateTime.Now;
            base.Mark(error);
        }
    }
}