using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using RethinkDb.Driver.Ast;

namespace RethinkDb.Driver.Net.Clustering
{
    public interface IPoolingStrategy : IConnection
    {
        void AddHost(string host, Connection conn);
    }

    public abstract class HostPool : IPoolingStrategy
    {
        protected TimeSpan initialRetryDelay;
        protected TimeSpan maxRetryInterval;

        //this array should be monotonically increasing, to avoid 
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
            lock( hostLock )
            {
                foreach (var h in hostList)
                {
                    h.Dead = false;
                }
            }
        }

        private object hostLock = new object();

        public void AddHost(string host, Connection conn)
        {
            lock( hostLock )
            {
                var oldHostList = this.hostList;
                var nextHostList = new HostEntry[oldHostList.Length + 1];
                Array.Copy(oldHostList, nextHostList, oldHostList.Length);

                //add new host to the end of the array.
                var he = new HostEntry(host, maxRetryInterval) { conn = conn };
                nextHostList[nextHostList.Length - 1] = he;
                this.hostList = nextHostList;
            }
        }

        public abstract Task<dynamic> RunAsync<T>(ReqlAst term, object globalOpts);
        public abstract Task<Cursor<T>> RunCursorAsync<T>(ReqlAst term, object globalOpts);
        public abstract Task<T> RunAtomAsync<T>(ReqlAst term, object globalOpts);
        public abstract void RunNoReply(ReqlAst term, object globalOpts);
    }

    // This is the main HostPool interface. Structs implementing this interface
    // allow you to Get a HostPoolResponse (which includes a hostname to use),
    // get the list of all Hosts, and use ResetAll to reset state.
    public class RoundRobinHostPool : HostPool
    {
        protected int nextHostIndex;

        public RoundRobinHostPool()
        {
        }

        public virtual HostEntry GetRoundRobin()
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
                    return h;
                }
            }

            ResetAll();

            return hostList[0];
        }

        protected internal void MarkFailed(HostEntry h)
        {
            if( !h.Dead )
            {
                h.RetryCount = 0;
                h.RetryDelay = initialRetryDelay;
                h.NextRetry = DateTime.Now.Add(h.RetryDelay);
                h.Dead = true;
            }
        }

        public override Task<dynamic> RunAsync<T>(ReqlAst term, object globalOpts)
        {
            HostEntry host = GetRoundRobin();
            try
            {
                return host.conn.RunAsync<T>(term, globalOpts);
            }
            catch
            {
                MarkFailed(host);
                throw;
            }
        }

        public override Task<Cursor<T>> RunCursorAsync<T>(ReqlAst term, object globalOpts)
        {
            HostEntry host = GetRoundRobin();
            try
            {
                return host.conn.RunCursorAsync<T>(term, globalOpts);
            }
            catch
            {
                MarkFailed(host);
                throw;
            }
        }

        public override Task<T> RunAtomAsync<T>(ReqlAst term, object globalOpts)
        {
            HostEntry host = GetRoundRobin();
            try
            {
                return host.conn.RunAtomAsync<T>(term, globalOpts);
            }
            catch
            {
                MarkFailed(host);
                throw;
            }
        }

        public override void RunNoReply(ReqlAst term, object globalOpts)
        {
            HostEntry host = GetRoundRobin();
            try
            {
                host.conn.RunNoReply(term, globalOpts);
            }
            catch
            {
                MarkFailed(host);
                throw;
            }
        }
    }

}