using System;

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
            this.HostPool.DoMark(error, this);
        }

        public HostPool HostPool { get; set; }
    }
}