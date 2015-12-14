using System;
using System.Threading;
using RethinkDb.Driver.Ast;

namespace RethinkDb.Driver.Net.Clustering
{
    public class HostEntry
    {
        private readonly TimeSpan maxRetryInterval;
        public IConnection conn;

        public HostEntry(string host, TimeSpan maxRetryInterval)
        {
            this.maxRetryInterval = maxRetryInterval;
            this.Host = host;
            this.EpsilonValues = new long[EpsilonGreedyHostPool.EpsilonBuckets];
            this.EpsilonCounts = new long[EpsilonGreedyHostPool.EpsilonBuckets];
            this.EpsilonAvg = new float[EpsilonGreedyHostPool.EpsilonBuckets];
        }

        public string Host { get; set; }
        public DateTime NextRetry { get; set; }
        public int RetryCount { get; set; }
        public TimeSpan RetryDelay { get; set; }
        public bool Dead { get; set; }
        public long[] EpsilonCounts { get; set; }
        public long[] EpsilonValues { get; set; }
        public float[] EpsilonAvg { get; set; }

        public int EpsilonIndex;

        public float EpsilonValue;
        public float EpsilonPercentage;
        public float EpsilonWeightAverage ;

        public void UpdateRetry()
        {
            this.RetryCount += 1;
            var newDelay = TimeSpan.FromTicks(this.RetryDelay.Ticks * 2);
            if( newDelay < maxRetryInterval )
            {
                this.RetryDelay = newDelay;
            }
            else
            {
                this.RetryDelay = maxRetryInterval;
            }
            this.NextRetry = DateTime.Now + this.RetryDelay;
        }
    }
}