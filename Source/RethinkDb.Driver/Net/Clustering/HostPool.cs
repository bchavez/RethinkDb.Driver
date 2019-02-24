#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member

using System;
using System.Threading;
using System.Threading.Tasks;
using RethinkDb.Driver.Ast;

namespace RethinkDb.Driver.Net.Clustering
{
    public abstract class HostPool : IPoolingStrategy
    {
        protected TimeSpan RetryDelayInitial;
        protected TimeSpan RetryDelayMax;

        //this array should be monotonically increasing, to avoid 
        //unnecessary thread locking and synch problems when iterating
        //over the length of the list.
        protected HostEntry[] hostList;

        public HostPool(TimeSpan? retryDelayInitial, TimeSpan? retryDelayMax)
        {
            this.RetryDelayInitial = retryDelayInitial ?? TimeSpan.FromSeconds(30);
            this.RetryDelayMax = retryDelayMax ?? TimeSpan.FromSeconds(900);

            if( this.RetryDelayInitial < TimeSpan.FromSeconds(30)
                || this.RetryDelayMax < TimeSpan.FromSeconds(30) )
            {
                throw new ArgumentOutOfRangeException(
                    $"{nameof(retryDelayInitial)} and {nameof(retryDelayMax)} must both be greater than 30 seconds. Anything less can cause threads to pile up on each other because the default socket timeout for windows is about 20 seconds.");
            }

            hostList = new HostEntry[0];
        }

        protected object hostLock = new object();

        public HostEntry[] HostList => hostList;

        public virtual void AddHost(string host, Connection conn)
        {
            lock( hostLock )
            {
                if( shuttingDown ) return;

                var oldHostList = this.hostList;
                var nextHostList = new HostEntry[oldHostList.Length + 1];
                Array.Copy(oldHostList, nextHostList, oldHostList.Length);

                //add new host to the end of the array. Initially, start off as a dead
                //host.
                var he = new HostEntry(host)
                    {
                        conn = conn,
                        Dead = true,
                        RetryDelayInitial = RetryDelayInitial,
                        RetryDelayMax = RetryDelayMax
                    };
                nextHostList[nextHostList.Length - 1] = he;
                this.hostList = nextHostList;
            }
        }

        protected bool shuttingDown = false;

        public virtual void Shutdown()
        {
            lock( hostLock )
            {
                shuttingDown = true;
            }
        }

        public abstract Task<dynamic> RunAsync<T>(ReqlAst term, object globalOpts, CancellationToken cancelToken);
        public abstract Task<Cursor<T>> RunCursorAsync<T>(ReqlAst term, object globalOpts, CancellationToken cancelToken);
        public abstract Task<T> RunAtomAsync<T>(ReqlAst term, object globalOpts, CancellationToken cancelToken);
        public abstract Task<T> RunResultAsync<T>(ReqlAst term, object globalOpts, CancellationToken cancelToken);
        public abstract void RunNoReply(ReqlAst term, object globalOpts);
        public abstract Task<Response> RunUnsafeAsync(ReqlAst term, object globalOpts, CancellationToken cancelToken);

        public abstract void Dispose();
    }
}