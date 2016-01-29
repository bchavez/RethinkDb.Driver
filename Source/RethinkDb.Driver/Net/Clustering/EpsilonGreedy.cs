using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using RethinkDb.Driver.Ast;
using Random = System.Random;

namespace RethinkDb.Driver.Net.Clustering
{
    /// <summary>
    /// Epsilon Greedy is an algorithm that allows HostPool not only to track failure state,
    /// but also to learn about "better" options in terms of speed, and to pick from available hosts
    /// based on how well they perform. This gives a weighted request rate to better
    /// performing hosts, while still distributing requests to all hosts (proportionate to their performance).
    ///
    /// A good overview of Epsilon Greedy is here http://stevehanov.ca/blog/index.php?id=132
    ///
    /// To compute the weighting scores, we perform a weighted average of recent response times, over the course of
    /// `decayDuration`. decayDuration may be set to null to use the default value of 5 minutes
    /// We then use the supplied EpsilonValueCalculator to calculate a score from that weighted average response time.
    /// </summary>
    public class EpsilonGreedyHostPool : RoundRobinHostPool
    {
        protected static readonly TimeSpan DefaultDecayDuration = TimeSpan.FromMinutes(5);
        protected const double InitialEpsilon = 0.3;
        protected const double MinEpsilon = 0.01; // explore one percent of the time
        protected const double EpsilonDecay = 0.90; // decay the exploration rate
        public const int EpsilonBuckets = 120; //measurement slots

        /// <summary>
        /// Epsilon threshold that controls exploration. If a random double is
        /// less than &lt; (or below) epsilon, then an round-robin exploration is performed.
        /// IE: Higher the epsilon, the more chances for exploration of other hosts.
        /// In the long run, epsilon will EpsilonDecay on each exploration until reaching MinEpsilon.
        /// Additionally, in the long run, exploration will only take place MinEpsilon % of the time.
        /// </summary>
        private double epsilon;
        private TimeSpan decayDuration;
        private EpsilonValueCalculator calc;
        private float[] weights = null;

        private Timer timer;

        public static Random Random { get; set; }

        static EpsilonGreedyHostPool()
        {
            Random = new Random();
        }

        /// <summary>
        /// Construct an Epsilon Greedy HostPool
        ///
        /// Epsilon Greedy is an algorithm that allows HostPool not only to track failure state,
        /// but also to learn about "better" options in terms of speed, and to pick from available hosts
        /// based on how well they perform. This gives a weighted request rate to better
        /// performing hosts, while still distributing requests to all hosts (proportionate to their performance).
        ///
        /// A good overview of Epsilon Greedy is here http://stevehanov.ca/blog/index.php?id=132
        ///
        /// To compute the weighting scores, we perform a weighted average of recent response times, over the course of
        /// `decayDuration`. decayDuration may be set to null to use the default value of 5 minutes
        /// We then use the supplied EpsilonValueCalculator to calculate a score from that weighted average response time.
        /// </summary>
        /// <param name="retryDelayInitial">The initial retry delay when a host goes down. Default, null, is 30 seconds.</param>
        /// <param name="retryDelayMax">The maximum retry delay when a host goes down. Default, null, is 15 minutes.</param>
        /// <param name="decayDuration">The amount of time to cycle through all EpsilonBuckets (0...120). 
        /// This decay duration is divided by EpsilonBuckets (default: 5 min / 120 buckets = 2.5 seconds per bucket).
        /// IE: The average will be taken every decayDuration/EpsilonBuckets seconds.</param>
        /// <param name="calc">Given the weighted average among EpsilonBuckets slot measurements, calculate the host's EpsilonValue using EpsilonCalculators.Linear/Logarithmic/Polynomial(exponent)</param>
        /// <param name="autoStartDecayTimer">Automatically starts the decay timer. If false, you need to call StartDecayTimer manually for epsilon values to be calculated correctly.</param>
        public EpsilonGreedyHostPool(
            TimeSpan? retryDelayInitial,
            TimeSpan? retryDelayMax,
            TimeSpan? decayDuration,
            EpsilonValueCalculator calc,
            bool autoStartDecayTimer = true)
            : base(retryDelayInitial, retryDelayMax)
        {
            this.decayDuration = decayDuration ?? DefaultDecayDuration;
            this.calc = calc;
            this.epsilon = InitialEpsilon;
            this.weights = Enumerable.Range(1, EpsilonBuckets)
                .Select(i => i / Convert.ToSingle(EpsilonBuckets))
                .ToArray();

            if( autoStartDecayTimer )
            {
                StartDecayTimer();
            }
        }

        /// <summary>
        /// Construct an Epsilon Greedy HostPool
        ///
        /// Epsilon Greedy is an algorithm that allows HostPool not only to track failure state,
        /// but also to learn about "better" options in terms of speed, and to pick from available hosts
        /// based on how well they perform. This gives a weighted request rate to better
        /// performing hosts, while still distributing requests to all hosts (proportionate to their performance).
        ///
        /// A good overview of Epsilon Greedy is here http://stevehanov.ca/blog/index.php?id=132
        ///
        /// To compute the weighting scores, we perform a weighted average of recent response times, over the course of
        /// `decayDuration`. decayDuration may be set to null to use the default value of 5 minutes
        /// We then use the supplied EpsilonValueCalculator to calculate a score from that weighted average response time.
        /// </summary>
        /// <param name="decayDuration">The amount of time to cycle though all EpsilonBuckets (0...120). 
        /// This decay duration is divided by EpsilonBuckets (default: 5 min / 120 buckets = 2.5 seconds per bucket).
        /// IE: The average will be taken every decayDuration/EpsilonBuckets seconds.</param>
        /// <param name="calc">Given the weighted average among EpsilonBuckets slot measurements, calculate the host's EpsilonValue using EpsilonCalculators.Linear/Logarithmic/Polynomial(exponent)</param>
        /// <param name="autoStartDecayTimer">Automatically starts the decay timer. If false, you need to call StartDecayTimer manually for epsilon values to be calculated correctly.</param>
        public EpsilonGreedyHostPool(TimeSpan? decayDuration,
            EpsilonValueCalculator calc,
            bool autoStartDecayTimer = true) :
            this(null, null, decayDuration, calc, autoStartDecayTimer)
        {
            
        }


        /// <summary>
        /// Epsilon threshold that controls exploration. If a random double is
        /// less than &lt; (or below) epsilon, then an round-robin exploration is performed.
        /// IE: Higher the epsilon, the more chances for exploration of other hosts.
        /// In the long run, epsilon will EpsilonDecay on each exploration to MinEpsilon.
        /// </summary>
        public void SetEpsilon(float epsilon)
        {
            this.epsilon = epsilon;
        }


        /// <summary>
        /// If the autoStartDecayTimer was false, you need to start
        /// the decay timer manually using this method.
        /// </summary>
        public void StartDecayTimer()
        {
            var durationPerBucket = TimeSpan.FromTicks(decayDuration.Ticks / EpsilonBuckets);
            this.timer = new Timer(PerformEpsilonGreedyDecay, null, Timeout.Infinite, Timeout.Infinite);

            //fire now.
            this.timer.Change(TimeSpan.Zero, durationPerBucket);
        }

        internal void PerformEpsilonGreedyDecay(object state)
        {
            lock( hostLock )
            {
                if( shuttingDown )
                {
                    this.timer.Change(Timeout.Infinite, Timeout.Infinite);
                    this.timer.Dispose();
                    return;
                }
            }

            //Parallel.For()
            //basically advance the index
            var hlist = this.hostList;
            foreach( var h in hlist )
            {
                var nextIndex = (h.EpsilonIndex + 1) % EpsilonBuckets;
                h.EpsilonCounts[nextIndex] = 0; //write ahead, before other threads
                h.EpsilonValues[nextIndex] = 0; //before they see the next index
                h.EpsilonAvg[nextIndex] = 0;

                //advance the index, so all threads begin writing
                //and recording their speed results to the new bucket
                var currentIndex = Interlocked.Increment(ref h.EpsilonIndex);
                
                //not done yet....
                //now calculate the new average for the previous index now
                //that threads have advanced to the new epsilon index bucket
                var prevIndex = (currentIndex - 1) % EpsilonBuckets;
                var averages = h.EpsilonAvg;
                if( h.EpsilonCounts[prevIndex] > 0 )
                {
                    averages[prevIndex] = h.EpsilonValues[prevIndex] / Convert.ToSingle(h.EpsilonCounts[prevIndex]);
                }
            }
            //finally, update percentages
            Update();
        }

        public virtual HostEntry GetEpsilonGreedy()
        {
            HostEntry hostToUse = null;

            //this is our exploration phase
            var rand = Random.NextDouble();
            if( rand < this.epsilon )
            {
                this.epsilon = this.epsilon * EpsilonDecay;
                if( this.epsilon < MinEpsilon )
                {
                    this.epsilon = MinEpsilon;
                }
                return this.GetRoundRobin();
            }

            var hlist = this.hostList;
            
            var ceiling = 0.0d;
            
            //find best.
            for (var i = 0; i < hlist.Length; i++)
            {
                var h = hlist[i];
                if( !h.Dead && h.EpsilonWeightAverage > 0)
                {
                    ceiling += h.EpsilonPercentage;
                    if( rand <= ceiling )
                    {
                        hostToUse = h;
                        break;
                    }
                }
            }

            if( hostToUse == null || hostToUse.Dead )
            {
                return this.GetRoundRobin();
            }
            
            return hostToUse;
        }

        private void SetWeightedAverageResponseTime(HostEntry h)
        {
            var value = 0f;
            var lastValue = 0f;
            var prevIndex = (h.EpsilonIndex -1)% EpsilonBuckets; //capture the current index

            //start from 2, skipping the current epsilon index
            for( var i = 2; i <= EpsilonBuckets; i++ )
            {
                var pos = (prevIndex + i) % EpsilonBuckets;
                var counts = h.EpsilonCounts[pos];
                // Changing the line below to what I think it should be to get the weights right
                var weight = weights[i - 1];
                if( counts > 0 )
                {
                    //the average 
                    //var currentValue = h.EpsilonValues[pos] / Convert.ToSingle(counts);
                    var currentValue = h.EpsilonAvg[pos];
                    value += currentValue * weight;
                    lastValue = currentValue;
                }
                else
                {
                    value += lastValue * weight;
                }
            }
            h.EpsilonWeightAverage = value;
        }

        private void SetEpsilonValue(HostEntry h)
        {
            h.EpsilonValue = this.calc.CalcValueFromAvgResponseTime(h.EpsilonWeightAverage);
        }

        public void MarkSuccess( HostEntry h, long start, long end )
        {
            var duration = TimeSpan.FromTicks(end - start);
            var index = h.EpsilonIndex % EpsilonBuckets;
            var counts = h.EpsilonCounts;
            var values = h.EpsilonValues;

            Interlocked.Increment(ref counts[index]);
            Interlocked.Add(ref values[index], Convert.ToInt64(duration.TotalMilliseconds));
        }

        public void Update()
        {
            var hlist = this.hostList;
            var sumValues = 0f;

            // calculate values for each host in the 0..1 range (but not normalized)
            for( int i = 0; i < hlist.Length; i++ )
            {
                var h = hlist[i];
                SetWeightedAverageResponseTime(h);
                SetEpsilonValue(h);
                if( !h.Dead && h.EpsilonWeightAverage > 0 )
                {
                    sumValues += h.EpsilonValue;
                }
            }
            //now normalize the 0..1 range to get percentage
            //need to know the sum before normalizing
            for ( int i = 0; i < hlist.Length; i++ )
            {
                var h = hlist[i];
                if( !h.Dead && h.EpsilonWeightAverage > 0 )
                {
                    h.EpsilonPercentage = h.EpsilonValue / sumValues;
                }
            }
        }

        #region CONNECTION RUNNERS

        public override async Task<dynamic> RunAsync<T>(ReqlAst term, object globalOpts)
        {
            HostEntry host = GetEpsilonGreedy();
            try
            {
                var start = DateTime.Now.Ticks;
                var result = await host.conn.RunAsync<T>(term, globalOpts).ConfigureAwait(false);
                var end = DateTime.Now.Ticks;
                MarkSuccess(host, start, end);
                return result;
            }
            catch (Exception e) when (ExceptionIs.NetworkError(e))
            {
                host.MarkFailed();
                throw;
            }
        }

        public override async Task<T> RunAtomAsync<T>(ReqlAst term, object globalOpts)
        {
            HostEntry host = GetEpsilonGreedy();
            try
            {
                var start = DateTime.Now.Ticks;
                var result = await host.conn.RunAtomAsync<T>(term, globalOpts).ConfigureAwait(false);
                var end = DateTime.Now.Ticks;
                MarkSuccess(host, start, end);
                return result;
            }
            catch (Exception e) when (ExceptionIs.NetworkError(e))
            {
                host.MarkFailed();
                throw;
            }
        }

        public override void RunNoReply(ReqlAst term, object globalOpts)
        {
            HostEntry host = GetEpsilonGreedy();
            try
            {
                var start = DateTime.Now.Ticks;
                host.conn.RunNoReply(term, globalOpts);
                var end = DateTime.Now.Ticks;
                MarkSuccess(host, start, end);
            }
            catch (Exception e) when (ExceptionIs.NetworkError(e))
            {
                host.MarkFailed();
                throw;
            }
        }

        public override async Task<Cursor<T>> RunCursorAsync<T>(ReqlAst term, object globalOpts)
        {
            HostEntry host = GetEpsilonGreedy();
            try
            {
                var start = DateTime.Now.Ticks;
                var result = await host.conn.RunCursorAsync<T>(term, globalOpts).ConfigureAwait(false);
                var end = DateTime.Now.Ticks;
                MarkSuccess(host, start, end);
                return result;
            }
            catch (Exception e) when (ExceptionIs.NetworkError(e))
            {
                host.MarkFailed();
                throw;
            }
        }

        #endregion
    }
}