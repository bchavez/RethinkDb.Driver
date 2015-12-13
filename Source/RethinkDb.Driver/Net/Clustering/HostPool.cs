using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using RethinkDb.Driver.Utils.NonBlocking.ConcurrentDictionary;

namespace RethinkDb.Driver.Net.Clustering
{
    public interface IPoolingStrategy
    {
        void MarkSuccess();
        void MarkFailure();
    }

    public abstract class HostPool
    {
        protected TimeSpan initialRetryDelay;
        protected TimeSpan maxRetryInterval;

        //protected Dictionary<string, HostEntry> hosts;
        protected ConcurrentDictionary<string, HostEntry> hosts;

        //this list should be monotonically increasing, to avoid 
        //unnecessary thread locking and synch problems when iterating
        //over the length of the list.
        protected HostEntry[] hostList;

        public HostPool()
        {
            initialRetryDelay = TimeSpan.FromSeconds(30);
            maxRetryInterval = TimeSpan.FromSeconds(900);
        }

        public virtual void ResetAll()
        {
            foreach (var h in hosts.Values)
            {
                h.Dead = false;
            }
        }
        
        public virtual void SetHosts(string[] hosts)
        {
            this.hosts = new ConcurrentDictionary<string, HostEntry>();
            this.hostList = new HostEntry[hosts.Length];
            for (int i = 0; i < hosts.Length; i++)
            {
                var h = hosts[i];
                var he = new HostEntry(h);
                this.hosts[h] = he;
                this.hostList[i] = he;
            }
        }
    }

    // This is the main HostPool interface. Structs implementing this interface
    // allow you to Get a HostPoolResponse (which includes a hostname to use),
    // get the list of all Hosts, and use ResetAll to reset state.
    public class RoundRobinHostPool : HostPool
    {
        protected int nextHostIndex;

        public RoundRobinHostPool(string[] hosts)
        {
            SetHosts(hosts);
        }

        public RoundRobinHostPoolResponse Get()
        {
            var host = GetRoundRobin();
            return new RoundRobinHostPoolResponse {Host = host, HostPool = this};
        }

        public virtual string GetRoundRobin()
        {
            var now = DateTime.Now;
            var hostCount = this.hostList.Length; //thread capture

            for( var i = 0; i < hostCount; i++ )
            {
                var next = Interlocked.Increment(ref nextHostIndex);
                var currentIndex = next % hostCount;
                
                var h = hostList[currentIndex];
                if( !h.Dead )
                {
                    return h.Host;
                }
                if( h.NextRetry < now )
                {
                    h.WillRetryHost(maxRetryInterval);
                    return h.Host;
                }
            }

            ResetAll();

            return hostList[0].Host;
        }


        // keep the marks separate so we can override independently
        public virtual void MarkSuccess(RoundRobinHostPoolResponse hostR)
        {
            var host = hostR.Host;
            MarkSuccessInternal(host);
        }

        internal void MarkSuccessInternal(string host)
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

        public virtual void MarkFailed(RoundRobinHostPoolResponse hostR)
        {
            var host = hostR.Host;
            MarkFailedInternal(host);
        }

        internal void MarkFailedInternal(string host)
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

        public virtual void DoMark(Exception e, RoundRobinHostPoolResponse r)
        {
            if (e == null)
            {
                r.HostPool.MarkSuccess(r);
            }
            else
            {
                r.HostPool.MarkFailed(r);
            }
        }

    }

}