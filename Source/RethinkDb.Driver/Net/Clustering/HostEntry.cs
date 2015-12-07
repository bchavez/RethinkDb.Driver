using System;

namespace RethinkDb.Driver.Net.Clustering
{
    public class HostEntry
    {
        public HostEntry(string host)
        {
            this.Host = host;
        }

        public string Host { get; set; }
        public DateTime NextRetry { get; set; }
        public int RetryCount { get; set; }
        public TimeSpan RetryDelay { get; set; }
        public bool Dead { get; set; }
        public ulong[] EpsilonCounts { get; set; }
        public ulong[] EpsilonValues { get; set; }
        public int EpsilonIndex { get; set; }
        public double EpsilonValue { get; set; }
        public double EpsilonPercentage { get; set; }

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