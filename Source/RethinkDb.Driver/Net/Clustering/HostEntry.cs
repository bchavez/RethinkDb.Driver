using System;
using System.Threading;
using RethinkDb.Driver.Ast;

namespace RethinkDb.Driver.Net.Clustering
{
    public class HostEntry
    {
        public TimeSpan RetryDelayInitial { get; set; }
        public TimeSpan RetryDelayMax { get; set; }

        public HostEntry(string host)
        {
            this.Host = host;
            this.EpsilonValues = new long[EpsilonGreedyHostPool.EpsilonBuckets];
            this.EpsilonCounts = new long[EpsilonGreedyHostPool.EpsilonBuckets];
            this.EpsilonAvg = new float[EpsilonGreedyHostPool.EpsilonBuckets];
        }

        public IConnection conn;
        public string Host { get; set; }
        public bool Dead { get; set; }
        public object Tag { get; set; }
        
        public DateTime NextRetry { get; set; }
        public int RetryCount { get; set; }
        public TimeSpan RetryDelay { get; set; }

        public long[] EpsilonCounts { get; set; }
        public long[] EpsilonValues { get; set; }
        public float[] EpsilonAvg { get; set; }

        public int EpsilonIndex;

        public float EpsilonValue;
        public float EpsilonPercentage;
        public float EpsilonWeightAverage ;

        public virtual void RetryFailed()
        {
            this.RetryCount += 1;
            //double the retry delay
            var newDelay = this.RetryDelay.Add(this.RetryDelay);
            this.RetryDelay = newDelay < this.RetryDelayMax ? newDelay : this.RetryDelayMax;
            this.NextRetry = DateTime.Now + this.RetryDelay;
        }

        public virtual bool NeedsRetry()
        {
            return this.Dead && this.NextRetry < DateTime.Now;
        }

        public virtual void MarkFailed()
        {
            if (!this.Dead)
            {
                this.RetryCount = 0;
                this.RetryDelay = this.RetryDelayInitial;
                this.NextRetry = DateTime.Now.Add(this.RetryDelay);
                this.Dead = true;
                Log.Trace($"Host {this.Host} is DOWN.");
            }
        }
    }
}