using System;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.IO;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace RethinkDb.Driver.Net
{
    public class Awaiter : TaskCompletionSource<Response>
    {
        public bool IsWaiting { get; set; }
    }

    public class SocketWrapper
    {
        private readonly TcpClient socketChannel;
        private readonly TimeSpan timeout;

        private readonly string hostname;
        private readonly int port;

        private NetworkStream ns = null;
        private BinaryWriter bw = null;
        private BinaryReader br = null;

        public SocketWrapper(string hostname, int port, TimeSpan? timeout)
        {
            this.hostname = hostname;
            this.port = port;

            this.timeout = timeout ?? TimeSpan.FromSeconds(60);

            this.socketChannel = new TcpClient();
        }

        public virtual void Connect(byte[] handshake)
        {
            var deadline = NetUtil.Deadline(this.timeout);
            var taskComplete = false;
            try
            {
                socketChannel.NoDelay = true;
                socketChannel.Client.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.KeepAlive, true);
                socketChannel.Client.Blocking = true;
                taskComplete = socketChannel.ConnectAsync(this.hostname, this.port).Wait(this.timeout);
                if( deadline < DateTime.UtcNow.Ticks || (taskComplete && !socketChannel.Connected) )
                {
                    throw new ReqlDriverError("Connection timed out.");
                }
                this.ns = socketChannel.GetStream();
                this.bw = new BinaryWriter(this.ns);
                this.br = new BinaryReader(this.ns);

                this.bw.Write(handshake);
                this.bw.Flush();

                var msg = this.ReadNullTerminatedString(timeout);

                if( !msg.Equals("SUCCESS") )
                {
                    throw new ReqlDriverError($"Server dropped connection with message: '{msg}'");
                }

                pump = new CancellationTokenSource();
                Task.Run(() => ResponsePump(pump.Token), pump.Token);
            }
            catch when( !taskComplete )
            {
                try
                {
                    this.Close();
                }
                catch
                {
                    // attempt to close, ignored,
                    // and re-throw the original exception
                }
                throw;
            }
        }

        private string ReadNullTerminatedString(TimeSpan? deadline)
        {
            var deadlineInstant = deadline.HasValue ? DateTime.Now.Add(deadline.Value) : (DateTime?)null;

            var sb = new StringBuilder();
            char c;
            while( (c = this.br.ReadChar()) != '\0' )
            {
                if( deadlineInstant.HasValue )
                {
                    if( deadlineInstant <= DateTime.Now )
                    {
                        throw new ReqlDriverError("Connection timed out.");
                    }
                }
                sb.Append(c);
            }
            return sb.ToString();
        }


        private CancellationTokenSource pump = null;

        /// <summary>
        /// Started just after connect.
        /// </summary>
        private void ResponsePump(CancellationToken signal)
        {
            while( true )
            {
                if( signal.IsCancellationRequested )
                {
                    Log.Trace("Response Pump: shutting down. Cancel is requested.");
                    break;
                }
                if( this.Closed )
                {
                    Log.Trace("Response Pump: The connected socket is not open. Response Loop exiting.");
                    break;
                }

                try
                {
                    var response = this.Read();
                    Awaiter awaitingTask;
                    if( awaiters.TryGetValue(response.Token, out awaitingTask) )
                    {
                        if( awaitingTask.IsWaiting )
                        {
                            awaitingTask.IsWaiting = false;
                            //Push, don't block.
                            Task.Run(() => awaitingTask.SetResult(response));
                            //See ya later alligator
                        }
                        else
                        {
                            Log.Debug($"Response Pump: An attempt SetResult on an awaiter that is not waiting {response.Token} token. Are we out of sync?");
                            //Wow, we already set this awaiter. Why are we here? Are we out of sync?
                            Debugger.Break();
                        }
                    }
                    else
                    {
                        //Wow, there's nobody waiting for this response.
                        Log.Debug($"Response Pump: There are no awaiters waiting for {response.Token} token.");
                        //I guess we'll ignore for now, perhaps a cursor was killed
                    }
                }
                catch( Exception e ) when( !signal.IsCancellationRequested )
                {
                    Log.Debug($"Response Pump: Exception - {e.Message}");
                }
            }

            //clean up.
            awaiters.Clear();
        }

        private Response Read()
        {
            var token = this.br.ReadInt64();
            var responseLength = this.br.ReadInt32();
            var response = this.br.ReadBytes(responseLength);
            return Response.ParseFrom(token, Encoding.UTF8.GetString(response));
        }

        private ConcurrentDictionary<long, Awaiter> awaiters = new ConcurrentDictionary<long, Awaiter>();

        public virtual Task<Response> AwaitResponseAsync(long token)
        {
            //The token in awaiters should already be available
            //because it was set when the query was written by the original thread.
            //If they indeed requested it, they should be waiting for the response.
            //So, all we're doing is giving them the Task that they were assigned 
            //when they wrote the query.
            var task = awaiters[token].Task.ContinueWith(r =>
                {
                    //and,.... after you're done, finish by
                    //removing yourself from the list of awaiters.
                    Awaiter finished;
                    awaiters.TryRemove(token, out finished);
                    return r.Result;
                });
            return task;
        }

        private object writeLock = new object();

        public virtual void WriteQuery(long token, string json, bool assignAwaiter = true)
        {
            if( assignAwaiter )
            {
                //Thanks for your query, you get assigned a TCS as well.
                var tcs = new Awaiter {IsWaiting = true};
                awaiters[token] = tcs;
            }

            lock( writeLock ) // Everyone can write their query as fast as they can; block if needed.
            {
                this.bw.Write(token);
                var jsonBytes = Encoding.UTF8.GetBytes(json);
                this.bw.Write(jsonBytes.Length);
                this.bw.Write(jsonBytes);
            }
        }

        public virtual bool Closed => !socketChannel.Connected;

        public virtual bool Open => socketChannel.Connected;

        public virtual void Close()
        {
            this.pump?.Cancel();
            try
            {
                this.br?.Dispose();

            }
            catch { }

    try
            {
#if DNXCORE50
                socketChannel.Dispose();
#else
                socketChannel.Close();
#endif
            }
            catch( IOException e )
            {
                throw new ReqlError(e);
            }
        }
    }
}