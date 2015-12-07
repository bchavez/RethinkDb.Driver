using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

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
        private float[] weights = null;

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
            this.weights = Enumerable.Range(1, EpsilonBuckets)
                .Select(i => i / Convert.ToSingle(EpsilonBuckets))
                .ToArray();
        }

        public void SetEpsilon(float epsilon)
        {
            this.epsilon = epsilon;
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
            foreach( var h in hostList )
            {
                var nextIndex = (h.EpsilonIndex + 1) % EpsilonBuckets;
                h.EpsilonCounts[nextIndex] = 0; //write ahead, before other threads
                h.EpsilonValues[nextIndex] = 0; //see the next index
                h.EpsilonIndex = nextIndex; //trigger the next index.
            }
        }

        public EpsilonHostPoolResponse Get()
        {
            var h = GetEpsilonGreedy();
            var started = DateTime.Now;
            return new EpsilonHostPoolResponse {Started = started, Host = h, HostPool = this};
        }

        private float GetWeightedAverageResponseTime(HostEntry h)
        {
            var value = 0f;
            var lastValue = 0f;
            var epsilonIndex = h.EpsilonIndex; //capture the current index

            for (var i = 1; i <= EpsilonBuckets; i++)
            {
                var pos = (epsilonIndex + i) % EpsilonBuckets;
                var counts = h.EpsilonCounts[pos];
                // Changing the line below to what I think it should be to get the weights right
                var weight = weights[i - 1];
                if (counts > 0)
                {
                    //the average 
                    var currentValue = h.EpsilonValues[pos] / Convert.ToSingle(counts);
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
            var now = DateTime.Now;
            var sumValues = 0.0f;
            var hlist = this.hostList;
            for( var ix = 0; ix < hlist.Length; ix++ )
            {
                var h = hlist[ix];

                if( h.CanTryHost(now) )
                {
                    var v = GetWeightedAverageResponseTime(h);
                    h.EpsilonWeightAverage.Value = v;
                    if( v > 0 )
                    {
                        var ev = this.calc.CalcValueFromAvgResponseTime(v);
                        h.EpsilonValue.Value = ev;
                        sumValues += ev;
                    }
                }
            }
            //now normalize the 0..1 range to get percentage
            for ( var ix = 0; ix < hlist.Length; ix++ )
            {
                var h = hlist[ix];
                if( h.EpsilonWeightAverage.Value > 0 )
                {
                    h.EpsilonPercentage.Value = h.EpsilonValue.Value / sumValues;
                }
            }

            var ceiling = 0.0d;
            var pickPercentage = Random.NextDouble();

            //find best.
            for (var ix = 0; ix < hlist.Length; ix++)
            {
                var h = hlist[ix];
                if( h.EpsilonWeightAverage.Value > 0 )
                {
                    ceiling += h.EpsilonPercentage.Value;
                    if( pickPercentage <= ceiling )
                    {
                        hostToUse = h;
                        break;
                    }
                }
            }

            if( hostToUse == null )
            {
                //if( possibleHosts.Any() )
                //{
                    Log.Trace("Failed to randomly chose a host");
                //}
                return this.GetRoundRobin();
            }
            if( hostToUse.Dead )
            {
                hostToUse.WillRetryHost(maxRetryInterval);
            }
            return hostToUse.Host;
        }

        public void MarkSuccess(EpsilonHostPoolResponse eHostR)
        {
            MarkSuccessInternal(eHostR.Host);

            var host = eHostR.Host;

            var duration =  eHostR.Ended.Value - eHostR.Started;
            var h = hosts[host];
            var index = h.EpsilonIndex;
            var counts = h.EpsilonCounts;
            Interlocked.Increment(ref counts[index]);
            var values = h.EpsilonValues;
            Interlocked.Add(ref values[index], Convert.ToInt64(duration.TotalMilliseconds));
        }

        public void MarkFailed(EpsilonHostPoolResponse eHostR)
        {
            MarkFailedInternal(eHostR.Host);
        }
    }
}