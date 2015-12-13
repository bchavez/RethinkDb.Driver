using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using RethinkDb.Driver.Ast;
using RethinkDb.Driver.Utils.NonBlocking.ConcurrentDictionary;

namespace RethinkDb.Driver.Net.Clustering
{
    public class ConnectionPool : IConnection
    {
        Task<dynamic> IConnection.RunAsync<T>(ReqlAst term, object globalOpts)
        {
            throw new NotImplementedException();
        }

        Task<Cursor<T>> IConnection.RunCursorAsync<T>(ReqlAst term, object globalOpts)
        {
            throw new NotImplementedException();
        }

        Task<T> IConnection.RunAtomAsync<T>(ReqlAst term, object globalOpts)
        {
            throw new NotImplementedException();
        }

        void IConnection.RunNoReply(ReqlAst term, object globalOpts)
        {
            throw new NotImplementedException();
        }

        internal ConnectionPool()
        {
        }


        public static Builder build()
        {
            return new Builder();
        }

        public class Builder
        {
            internal bool _discover;
            private string[] seeds;

            /// <summary>
            /// Should be strings of the form "Host:Port".
            /// </summary>
            public Builder seed(string[] seeds)
            {
                this.seeds = seeds;
                return this;
            }

            /// <summary>
            /// discover() is used to enable host discovery, when true the driver
            /// will attempt to discover any new nodes added to the cluster and then
            /// start sending queries to these new nodes.
            /// </summary>
            public Builder discover(bool discoverNewHosts)
            {
                this._discover = discoverNewHosts;
                return this;
            }
        }

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

        //protected object locker = new object();

        public HostPool()
        {
            initialRetryDelay = TimeSpan.FromSeconds(30);
            maxRetryInterval = TimeSpan.FromSeconds(900);
        }


        public virtual void ResetAll()
        {
            DoResetAll();
        }

        public virtual void DoResetAll()
        {
            foreach (var h in hosts.Values)
            {
                h.Dead = true;
            }
        }

        public string[] Hosts => this.hosts.Keys.ToArray();


        public virtual void SetHosts(string[] hosts)
        {

            DoSetHosts(hosts);
        }

        protected void DoSetHosts(string[] hosts)
        {
            this.hosts = new ConcurrentDictionary<string, HostEntry>();
            this.hostList = new HostEntry[hosts.Length];
            for( int i = 0; i < hosts.Length; i++ )
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

            DoResetAll();

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