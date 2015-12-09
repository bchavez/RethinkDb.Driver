using System;
using System.Threading;
using RethinkDb.Driver.Ast;

namespace RethinkDb.Driver.Net.Clustering
{
    public class HostEntry
    {
        public HostEntry(string host)
        {
            this.Host = host;
            this.EpsilonValues = new long[EpsilonGreedy.EpsilonBuckets];
            this.EpsilonCounts = new long[EpsilonGreedy.EpsilonBuckets];
            this.EpsilonAvg = new float[EpsilonGreedy.EpsilonBuckets];
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

        public bool CanTryHost(DateTime now)
        {
            if( !this.Dead )
            {
                return true;
            }
            if( this.NextRetry < now )
            {
                return true;
            }
            return false;
        }

        public void WillRetryHost(TimeSpan maxRetryInterval)
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