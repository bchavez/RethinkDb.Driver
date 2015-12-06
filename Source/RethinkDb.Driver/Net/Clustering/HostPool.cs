using System;
using System.Collections.Generic;
using System.Linq;

namespace RethinkDb.Driver.Net.Clustering
{
    // This is the main HostPool interface. Structs implementing this interface
    // allow you to Get a HostPoolResponse (which includes a hostname to use),
    // get the list of all Hosts, and use ResetAll to reset state.
    public class HostPool
    {
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


    


}