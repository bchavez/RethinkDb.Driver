using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;

namespace RethinkDb.Driver.Net.Clustering
{
    // This interface represents the response from HostPool. You can retrieve the
    // hostname by calling Host(), and after making a request to the host you should
    // call Mark with any error encountered, which will inform the HostPool issuing
    // the HostPoolResponse of what happened to the request and allow it to update.
    public class HostPoolResponse
    {
        public string Host { get; set; }

        public void Mark(Exception error)
        {
            HostPool.DoMark(error, this);
        }

        public HostPool HostPool { get; set; }
    }

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
            this.RetryCount ++;
            var newDelay = TimeSpan.FromTicks(RetryDelay.Ticks * 2);
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

        public double GetWeightedAverageResponseTime()
        {
            var value = 0d;
            var lastValue = 0d;

            for( var i = 1; i < HostPool.EpsilonBuckets; i++ )
            {
                var pos = (this.EpsilonIndex + i) % HostPool.EpsilonBuckets;
                var bucketCount = this.EpsilonCounts[pos];
                // Changing the line below to what I think it should be to get the weights right
                var weight = i / Convert.ToDouble(HostPool.EpsilonBuckets);
                if( bucketCount > 0 )
                {
                    var currentValue = this.EpsilonValues[pos] / Convert.ToDouble(bucketCount);
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
    }

    // This is the main HostPool interface. Structs implementing this interface
    // allow you to Get a HostPoolResponse (which includes a hostname to use),
    // get the list of all Hosts, and use ResetAll to reset state.
    public class HostPool
    {
        public const int EpsilonBuckets = 120;
        protected const double EpsilonDecay = 0.90; // decay the exploration rate
        protected const double MinEpsilon = 0.01; // explore one percent of the time
        protected const double InitialEpsilon = 0.3;
        protected static readonly TimeSpan DefaultDecayDuration = TimeSpan.FromMinutes(5);

        protected TimeSpan initialRetryDelay;
        protected TimeSpan maxRetryInterval;
        protected int nextHostIndex;
        protected Dictionary<string, HostEntry> hosts;
        protected HostEntry[] hostList;

        public HostPool(string[] hosts)
        {
            SetHosts(hosts);

            initialRetryDelay = TimeSpan.FromSeconds(30);
            maxRetryInterval = TimeSpan.FromSeconds(900);
        }

        public virtual HostPoolResponse Get()
        {
            lock( locker )
            {
                var host = GetRoundRobin();
                return new HostPoolResponse {Host = host, HostPool = this};
            }
        }


        public virtual void DoMark(Exception e, HostPoolResponse r)
        {
            if( e == null )
            {
                r.HostPool.MarkSuccess(r);
            }
            else
            {
                r.HostPool.MarkFailed(r);
            }
        }

        // keep the marks separate so we can override independently
        public virtual void MarkSuccess(HostPoolResponse hostR)
        {
            var host = hostR.Host;
            lock( locker )
            {
                HostEntry h;
                if( !hosts.TryGetValue(host, out h) )
                {
                    //log error, hosts not in host pool.
                    Log.Debug($"Host {host} not in HostPool");
                    return;
                }
                h.Dead = false;
            }
        }

        public virtual void MarkFailed(HostPoolResponse hostR)
        {
            var host = hostR.Host;
            lock( locker )
            {
                HostEntry h;
                if( !hosts.TryGetValue(host, out h) )
                {
                    Log.Debug($"Host {host} not in HostPool");
                    return;
                }

                if( !h.Dead )
                {
                    h.Dead = true;
                    h.RetryCount = 0;
                    h.RetryDelay = initialRetryDelay;
                    h.NextRetry = DateTime.Now.Add(h.RetryDelay);
                }
            }
        }

        public virtual string GetRoundRobin()
        {
            var now = DateTime.Now;
            var hostCount = this.hostList.Length;

            for( var i = 0; i < hostCount - 1; i++ )
            {
                var currentIndex = (i + nextHostIndex) % hostCount;

                var h = hostList[currentIndex];
                if( !h.Dead )
                {
                    nextHostIndex = currentIndex + 1;
                    return h.Host;
                }
                if( h.NextRetry < now )
                {
                    h.WillRetryHost(maxRetryInterval);
                    nextHostIndex = currentIndex + 1;
                    return h.Host;
                }
            }

            DoResetAll();
            nextHostIndex = 0;
            return hostList[0].Host;
        }

        public virtual void ResetAll()
        {
            lock( locker )
            {
                DoResetAll();
            }
        }

        public virtual void DoResetAll()
        {
            foreach (var h in hostList)
            {
                h.Dead = true;
            }
        }

        public string[] Hosts => this.hosts.Keys.ToArray();

        protected object locker = new object();

        public virtual void SetHosts(string[] hosts)
        {
            lock( locker )
            {
                DoSetHosts(hosts);
            }
        }

        protected void DoSetHosts(string[] hosts)
        {
            this.hosts = new Dictionary<string, HostEntry>();
            this.hostList = hosts
                .Select(h =>
                    {
                        var he = new HostEntry(h);
                        this.hosts[h] = he;
                        return he;
                    })
                .ToArray();
        }
    }

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
    public class EpsilonGreedy : HostPool
    {
        private float esilon;
        private TimeSpan decayDuration;
        private EpsilonValueCalculator calc;
        public EpsilonGreedy(string[] hosts, TimeSpan? decayDuration, EpsilonValueCalculator calc) : base(hosts)
        {
            this.decayDuration = decayDuration ?? DefaultDecayDuration;

        }
    }


    // Structs implementing this interface are used to convert the average response time for a host
    // into a score that can be used to weight hosts in the epsilon greedy hostpool. Lower response
    // times should yield higher scores (we want to select the faster hosts more often) The default
    // LinearEpsilonValueCalculator just uses the reciprocal of the response time. In practice, any
    // decreasing function from the positive reals to the positive reals should work.
    public abstract class EpsilonValueCalculator
    {
        public abstract double CalcValueFromAvgResponseTime(double v);

        public static double LinearEpsilonValueCalculator(double v)
        {
            return 1.0 / v;
        }
        public static double LogEpsilonValueCalculator(double v)
        {
            return LinearEpsilonValueCalculator(Math.Log(v + 1.0));
        }

        public static double PolynomialEpsilonValueCalculator(double v, double exp)
        {
            return LinearEpsilonValueCalculator(Math.Pow(v, exp));
        }
    }

    public class LinearEpsilonValueCalculator : EpsilonValueCalculator
    {
        public override double CalcValueFromAvgResponseTime(double v)
        {
            return LinearEpsilonValueCalculator(v);
        }
    }

    public class LogEpsilonValueCalculator : EpsilonValueCalculator
    {
        public override double CalcValueFromAvgResponseTime(double v)
        {
            return LogEpsilonValueCalculator(v);
        }
    }

    public class PolynomialEpsilonValueCalculator : EpsilonValueCalculator
    {
        private readonly double exponent;

        public PolynomialEpsilonValueCalculator(double exponent)
        {
            this.exponent = exponent;
        }

        public override double CalcValueFromAvgResponseTime(double v)
        {
            return PolynomialEpsilonValueCalculator(v, exponent);
        }
    }

}