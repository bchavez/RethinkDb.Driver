using System;

namespace RethinkDb.Driver.Net.Clustering
{
    // This interface represents the response from HostPool. You can retrieve the
    // hostname by calling Host(), and after making a request to the host you should
    // call Mark with any error encountered, which will inform the HostPool issuing
    // the HostPoolResponse of what happened to the request and allow it to update.
    public struct RoundRobinHostPoolResponse
    {
        public string Host { get; set; }
        public RoundRobinHostPool HostPool { get; set; }

        public void Mark(Exception e)
        {
            if( e == null )
            {
                HostPool.MarkSuccess(this);
            }
            else
            {
                HostPool.MarkFailed(this);
            }
        }
    }
    public struct EpsilonHostPoolResponse
    {
        public string Host { get; set; }
        public EpsilonGreedy HostPool { get; set; }
        public DateTime Started { get; set; }
        public DateTime? Ended { get; set; }

        public void Mark(Exception e)
        {
            if (e == null)
            {
                HostPool.MarkSuccess(this);
            }
            else
            {
                HostPool.MarkFailed(this);
            }
        }
    }
}