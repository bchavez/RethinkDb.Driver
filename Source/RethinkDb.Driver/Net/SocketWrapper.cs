using System;
using System.Collections.Concurrent;
using System.IO;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using RethinkDb.Driver.Utils;

namespace RethinkDb.Driver.Net
{
    public class Awaiter : TaskCompletionSource<Response>
    {
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

        private Exception currentException;

        public virtual async Task ConnectAsync(byte[] handshake)
        {
            try
            {
                socketChannel.NoDelay = true;
                //socketChannel.LingerState.Enabled = false;
                //socketChannel.LingerState.LingerTime = 500;
                //socketChannel.ReceiveTimeout = 250;
                //socketChannel.Client.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.KeepAlive, true);
                //socketChannel.Client.Blocking = true;
                
                await socketChannel.ConnectAsync(this.hostname, this.port).ConfigureAwait(false);
                
                this.ns = socketChannel.GetStream();
                this.bw = new BinaryWriter(this.ns);
                this.br = new BinaryReader(this.ns);

                this.bw.Write(handshake);
                this.bw.Flush();

                var msg = this.ReadNullTerminatedString(timeout);

                if (!msg.Equals("SUCCESS"))
                {
                    throw new ReqlDriverError($"Server dropped connection with message: '{msg}'");
                }

                //http://blog.i3arnon.com/2015/07/02/task-run-long-running/
                //LongRunning creates a new thread and marks it as a background thread
                //(ie: does not block application shutdown, when all foreground threads finish)
                Task.Factory.StartNew(ResponsePump, TaskCreationOptions.LongRunning);
            }
            catch
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

        public virtual void Connect(byte[] handshake)
        {
            if( !ConnectAsync(handshake).Wait(this.timeout) )
            {
                try
                {
                    this.Close();
                }
                catch
                {
                }
                throw new ReqlDriverError("Connection timed out.");
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
        /// Started just after connect. Do not use or async/await code in this pump
        /// because it is a long-running task.
        /// http://blog.i3arnon.com/2015/07/02/task-run-long-running/
        /// </summary>
        private void ResponsePump()
        {
            pump = new CancellationTokenSource();
            while ( true )
            {
                if( pump.IsCancellationRequested )
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
                    //WARN: Do not use or async/await code in this pump; because it is a
                    //long-running task.
                    //http://blog.i3arnon.com/2015/07/02/task-run-long-running/
                    var response = this.Read();
                    Awaiter awaitingTask;
                    if( awaiters.TryRemove(response.Token, out awaitingTask) )
                    {
                        Task.Run(() =>
                            {
                                awaitingTask.SetResult(response);
                            });
                    }
                    else
                    {
                        //Wow, there's nobody waiting for this response.
                        Log.Debug($"Response Pump: There are no awaiters waiting for {response.Token} token. A cursor was probably closed and this might be a response to a QUERY:STOP. The response will be ignored.");
                        //I guess we'll ignore for now, perhaps a cursor was killed
                    }
                }
                catch( Exception e )
                {
                    currentException = e;
                    Log.Trace($"Response Pump: {e.GetType().Name} - {e.Message}. The connection can no longer be used. {nameof(ResponsePump)} is preparing to shutdown.");
                    //shutdown all.
                    try
                    {
                        this.Close();
                    }
                    catch
                    {
                    }
                    break;
                }
            }

            Log.Trace($"Cleaning up Response Pump awaiters for {hostname}:{port}");
            //clean up.
            foreach( var a in awaiters.Values )
            {
                if( currentException != null )
                {
                    a.TrySetException(currentException);
                }
                else
                {
                    a.TrySetCanceled();
                }
            }
            awaiters.Clear(); 
        }


        /// <summary>
        /// Blocking Read by the ResponsePump
        /// </summary>
        private Response Read()
        {
            var token = this.br.ReadInt64();
            var responseLength = this.br.ReadInt32();
            var response = this.br.ReadBytes(responseLength);
            return Response.ParseFrom(token, Encoding.UTF8.GetString(response));
        }

        private ConcurrentDictionary<long, Awaiter> awaiters = new ConcurrentDictionary<long, Awaiter>();

        private object writeLock = new object();

        public virtual Task<Response> SendQuery(long token, string json, bool awaitResponse)
        {
            if (pump.IsCancellationRequested)
            {
                throw new ReqlDriverError($"Threads may not {nameof(SendQuery)} because the connection is shutting down.");
            }

            Awaiter awaiter = null;
            if (awaitResponse)
            {
                //Assign a new awaiter for this token,
                //The caller is expecting a response.
                awaiter = new Awaiter();
                awaiters[token] = awaiter;
            }
            lock (writeLock)
            {   // Everyone can write their query as fast as they can; block if needed.
                try
                {
                    this.bw.Write(token);
                    var jsonBytes = Encoding.UTF8.GetBytes(json);
                    this.bw.Write(jsonBytes.Length);
                    this.bw.Write(jsonBytes);
                }
                catch(Exception e)
                {
                    currentException = e;
                    Log.Trace($"Write Query failed for Token {token}. Exception: {e.Message}");
                    throw;
                }
            }
            return awaiter?.Task ?? TaskHelper.CompletedResponse;
        }

        public virtual bool Closed => !socketChannel.Connected;

        public virtual bool Open => socketChannel.Connected;

        public virtual bool HasError => currentException != null;

        public virtual void Close()
        {
            this.pump?.Cancel();

            try
            {
                this.ns.Dispose();
            }
            catch
            {
            }

            try
            {
                socketChannel.Shutdown();
            }
            catch { }

            currentException = currentException ?? new EndOfStreamException("The driver connection is closed.");
        }
    }
}