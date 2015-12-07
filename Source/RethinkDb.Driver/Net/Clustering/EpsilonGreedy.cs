using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;

namespace RethinkDb.Driver.Net.Clustering
{
    /// <summary>
    /// Construct an Epsilon Greedy HostPool
    ///
    /// Epsilon Greedy is an algorithm that allows HostPool not only to track failure state,
    /// but also to learn about "better" options in terms of speed, and to pick from available hosts
    /// based on how well they perform. This gives a weighted request rate to better
    /// performing hosts, while still distributing requests to all hosts (proportionate to their performance).
    /// The interface is the same as the standard HostPool, but be sure to mark the HostResponse immediately
    /// after executing the request to the host, as that will stop the implicitly running request timer.
    ///
    /// A good overview of Epsilon Greedy is here http://stevehanov.ca/blog/index.php?id=132
    ///
    /// To compute the weighting scores, we perform a weighted average of recent response times, over the course of
    /// `decayDuration`. decayDuration may be set to 0 to use the default value of 5 minutes
    /// We then use the supplied EpsilonValueCalculator to calculate a score from that weighted average response time.
    /// </summary>
    public class EpsilonGreedy : RoundRobinHostPool
    {
        protected static readonly TimeSpan DefaultDecayDuration = TimeSpan.FromMinutes(5);
        protected const double InitialEpsilon = 0.3;
        protected const double MinEpsilon = 0.01; // explore one percent of the time
        protected const double EpsilonDecay = 0.90; // decay the exploration rate
        public const int EpsilonBuckets = 120;
        
        private double epsilon;
        private TimeSpan decayDuration;
        private EpsilonValueCalculator calc;

        private Timer timer;

        public static Random Random { get; set; }

        static EpsilonGreedy()
        {
            Random = new Random();
        }

        public EpsilonGreedy(string[] hosts, TimeSpan? decayDuration, EpsilonValueCalculator calc) : base(hosts)
        {
            this.decayDuration = decayDuration ?? DefaultDecayDuration;
            this.calc = calc;
            this.epsilon = InitialEpsilon;

            foreach( var h in hostList )
            {
                h.EpsilonCounts = new ulong[EpsilonBuckets];
                h.EpsilonValues = new ulong[EpsilonBuckets];
            }
        }

        public void SetEpsilon(float epsilon)
        {
            this.epsilon = epsilon;
        }

        public override void SetHosts(string[] hosts)
        {
            lock( locker )
            {
                base.SetHosts(hosts);
                foreach( var h in hostList )
                {
                    h.EpsilonCounts = new ulong[EpsilonBuckets];
                    h.EpsilonValues = new ulong[EpsilonBuckets];
                }
            }
        }

        public void EpsilonGreedyDecay()
        {
            var durationPerBucket = TimeSpan.FromTicks(decayDuration.Ticks / EpsilonBuckets);
            this.timer = new Timer(PerformEpsilonGreedyDecay);

            //fire now.
            this.timer.Change(TimeSpan.Zero, durationPerBucket);
        }

        internal void PerformEpsilonGreedyDecay(object state)
        {
            //basically advance the index
            lock( locker )
            {
                foreach( var h in hostList )
                {
                    h.EpsilonIndex += 1;
                    h.EpsilonIndex = h.EpsilonIndex % EpsilonBuckets;
                    h.EpsilonCounts[h.EpsilonIndex] = 0;
                    h.EpsilonValues[h.EpsilonIndex] = 0;
                }
            }
        }

        public override HostPoolResponse Get()
        {
            lock( locker )
            {
                var h = GetEpsilonGreedy();
                var started = DateTime.Now;
                return new EpsilonHostPoolResponse {Started = started, Host = h, HostPool = this};
            }
        }

        private double GetWeightedAverageResponseTime(HostEntry h)
        {
            var value = 0d;
            var lastValue = 0d;

            for (var i = 1; i <= EpsilonBuckets; i++)
            {
                var pos = (h.EpsilonIndex + i) % EpsilonBuckets;
                var bucketCount = h.EpsilonCounts[pos];
                // Changing the line below to what I think it should be to get the weights right
                var weight = i / Convert.ToDouble(EpsilonBuckets);
                if (bucketCount > 0)
                {
                    var currentValue = h.EpsilonValues[pos] / Convert.ToDouble(bucketCount);
                    value += currentValue * weight;
                    lastValue = currentValue;
                }
                else
                {
                    value += lastValue * weight;
                }
            }
            return value;
        }

        private string GetEpsilonGreedy()
        {
            HostEntry hostToUse = null;

            //this is our exploration phase
            if( Random.NextDouble() < this.epsilon )
            {
                this.epsilon = this.epsilon * EpsilonDecay;
                if( this.epsilon < MinEpsilon )
                {
                    this.epsilon = MinEpsilon;
                }
                return this.GetRoundRobin();
            }

            // calculate values for each host in the 0..1 range (but not normalized)
            var possibleHosts = new List<HostEntry>();
            var now = DateTime.Now;
            var sumValues = 0.0d;
            foreach( var h in this.hostList )
            {
                if( h.CanTryHost(now) )
                {
                    var v = GetWeightedAverageResponseTime(h);
                    if( v > 0 )
                    {
                        var ev = this.calc.CalcValueFromAvgResponseTime(v);
                        h.EpsilonValue = ev;
                        sumValues += ev;
                        possibleHosts.Add(h);
                    }
                }
            }
            if( possibleHosts.Any() )
            {
                //now normalize the 0..1 range to get percentage
                foreach( var h in possibleHosts )
                {
                    h.EpsilonPercentage = h.EpsilonValue / sumValues;
                }

                //do a weighted random choice among hosts

                var ceiling = 0.0d;
                var pickPercentage = Random.NextDouble();
                foreach( var h in possibleHosts )
                {
                    ceiling += h.EpsilonPercentage;
                    if( pickPercentage <= ceiling )
                    {
                        hostToUse = h;
                        break;
                    }
                }
            }
            if( hostToUse == null )
            {
                if( possibleHosts.Any() )
                {
                    Log.Trace("Failed to randomly chose a host");
                }
                return this.GetRoundRobin();
            }
            if( hostToUse.Dead )
            {
                hostToUse.WillRetryHost(maxRetryInterval);
            }
            return hostToUse.Host;
        }

        public override void MarkSuccess(HostPoolResponse hostR)
        {
            base.MarkSuccess(hostR);

            var eHostR = hostR as EpsilonHostPoolResponse;

            var host = eHostR.Host;

            var duration =  eHostR.Ended.Value - eHostR.Started;
            lock( locker )
            {
                var h = hosts[host];
                h.EpsilonCounts[h.EpsilonIndex]++;
                h.EpsilonValues[h.EpsilonIndex] += Convert.ToUInt64(duration.TotalMilliseconds);
            }
        }
    }
}