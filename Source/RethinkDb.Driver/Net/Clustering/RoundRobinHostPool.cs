using System;
using System.Threading;
using System.Threading.Tasks;
using RethinkDb.Driver.Ast;

namespace RethinkDb.Driver.Net.Clustering
{
    /// <summary>
    /// Create a new RoundRobin host pool. Each host is used in a round robin
    /// strategy when processing each query.
    /// </summary>
    public class RoundRobinHostPool : HostPool
    {
        protected int nextHostIndex = -1;

        /// <summary>
        /// Create a new RoundRobin host pool. Each host is used in a round robin
        /// strategy when processing each query. When a host goes down, the supervisor will
        /// wait retryDelayInitial timespan before trying again. If the reconnect fails, the 
        /// retry delay is doubled. Doubling of the retry is stopped at once the retry delay
        /// is greater than retryDelayMax; thereafter, every subsequent retry is retryDelayMax.
        /// The retryDelayInitial is 30 seconds, and retryDelayMax is 15 minutes.
        /// </summary>
        /// <param name="retryDelayInitial">The initial retry delay when a host goes down. Default, null, is 30 seconds.</param>
        /// <param name="retryDelayMax">The maximum retry delay when a host goes down. Default, null, is 15 minutes.</param>
        public RoundRobinHostPool(TimeSpan? retryDelayInitial, TimeSpan? retryDelayMax) 
            : base(retryDelayInitial, retryDelayMax)
        {
        }
        /// <summary>
        /// Create a new RoundRobin host pool. Each host is used in a round robin
        /// strategy when processing each query. When a host goes down, the supervisor will
        /// wait retryDelayInitial timespan before trying again. If the reconnect fails, the 
        /// retry delay is doubled. Doubling of the retry is stopped at once the retry delay
        /// is greater than retryDelayMax; thereafter, every subsequent retry is retryDelayMax.
        /// The retryDelayInitial is 30 seconds, and retryDelayMax is 15 minutes.
        /// </summary>
        public RoundRobinHostPool() : this(null , null)
        {
        }


        public virtual HostEntry GetRoundRobin()
        {
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

            return hostList[0];
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
                host.MarkFailed();
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
                host.MarkFailed();
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
                host.MarkFailed();
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
                host.MarkFailed();
                throw;
            }
        }
    }
}