using System;
using System.Collections.Concurrent;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using RethinkDb.Driver.Utils;

namespace RethinkDb.Driver.Net
{
    internal class SocketWrapper
    {
        private readonly Socket socket;

        private readonly TimeSpan timeout;

        private readonly string hostname;
        private readonly int port;
        private readonly Action<Exception> errorCallback;

        private NetworkStream ns = null;
        private BinaryWriter bw = null;
        private BinaryReader br = null;

        public SocketWrapper(string hostname, int port, TimeSpan? timeout, Action<Exception> errorCallback)
        {
            this.hostname = hostname;
            this.port = port;
            this.errorCallback = errorCallback;

            this.timeout = timeout ?? TimeSpan.FromSeconds(60);

            this.socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
        }
        
        private Exception currentException;

        public virtual async Task ConnectAsync(Handshake handshake)
        {
            try
            {
                socket.NoDelay = true;
                socket.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.KeepAlive, true);
#if STANDARD
                await socket.ConnectAsync(this.hostname, this.port).ConfigureAwait(false);
#else
                await Task.Factory.FromAsync(
                    (targetHost, targetPort, callback, state) => ((Socket)state).BeginConnect(targetHost, targetPort, callback, state),
                    asyncResult => ((Socket)asyncResult.AsyncState).EndConnect(asyncResult),
                    this.hostname,
                    this.port,
                    state: this.socket).ConfigureAwait(false);
#endif
                this.ns = new NetworkStream(this.socket);
                this.bw = new BinaryWriter(this.ns);
                this.br = new BinaryReader(this.ns);

                // execute RethinkDB handshake
                ExecuteHandshake(handshake);

                //http://blog.i3arnon.com/2015/07/02/task-run-long-running/
                //LongRunning creates a new thread and marks it as a background thread
                //(ie: does not block application shutdown, when all foreground threads finish)

                pump = new CancellationTokenSource();
                // GitHub Issue #24 - Set PUMP token first before starting thread.
                //      If the pump token is set in ResponsePump, and the thread is scheduled
                //      late, and we return immediately to the caller indicating the connection
                //      is ready, the caller will immediately SendQuery. However, SendQuery
                //      is dependent on pump, and will encounter a null reference exception
                //      because the ResponsePump thread is late setting the token.
                //
                //      So, set the token so that we're *really* ready to begin sending
                //      queries.

#pragma warning disable 4014 // We know what' we're doing. It's intentional.
                Task.Factory.StartNew(ResponsePump, TaskCreationOptions.LongRunning);
#pragma warning restore 4014
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

        private void ExecuteHandshake(Handshake handshake)
        {
            // initialize handshake
            var toWrite = handshake.NextMessage(null);
            // Sit in the handshake until it's completed. Exceptions will be thrown if
            // anything goes wrong.
            while (!handshake.IsFinished)
            {
                if (toWrite != null)
                {
                    bw.Write(toWrite);
                }
                var serverMsg = ReadNullTerminatedString(this.timeout);
                toWrite = handshake.NextMessage(serverMsg);
            }
        }

        public virtual void Connect(Handshake handshake)
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
            while( true )
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
                                //try, because it's possible
                                //the awaiting task was canceled.
                                if( !awaitingTask.TrySetResult(response) )
                                {
                                    Log.Debug($"Response Pump: The awaiter waiting for response token {response.Token} could not be set. The task was probably canceled.");
                                }
                            });
                    }
                    else
                    {
                        //Wow, there's nobody waiting for this response.
                        Log.Debug(
                            $"Response Pump: There are no awaiters waiting for {response.Token} token. A cursor was probably closed and this might be a response to a QUERY:STOP. The response will be ignored.");
                        //I guess we'll ignore for now, perhaps a cursor was killed
                    }
                }
                catch( Exception e )
                {
                    currentException = e;
                    this.errorCallback?.Invoke(currentException);
                    Log.Trace($"Response Pump: {e.GetType().Name} - {e.Message} The connection can no longer be used. {nameof(ResponsePump)} is preparing to shutdown.");
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


        public virtual Task<Response> SendQuery(long token, string json, bool awaitResponse, CancellationToken cancelToken)
        {
            cancelToken.ThrowIfCancellationRequested();
            if( pump.IsCancellationRequested )
            {
                throw new ReqlDriverError($"Threads may not {nameof(SendQuery)} because the connection is shutting down.");
            }

            Awaiter awaiter = null;
            if( awaitResponse )
            {
                //Assign a new awaiter for this token,
                //The caller is expecting a response.
                awaiter = new Awaiter(cancelToken);
                awaiters[token] = awaiter;
            }

            lock( writeLock )
            {
                // Everyone can write their query as fast as they can; block if needed.
                cancelToken.ThrowIfCancellationRequested();
                //We could probably use a semaphore slim as a lock
                //and Wait(cancelToken), but using semaphore slim
                //is slower and requires more overhead. So, instead
                //we just cancel at earliest possible time when the lock
                //is free.

                try
                {
                    //using bw is fast and convenient
                    this.bw.Write(token);
                    var jsonBytes = Encoding.UTF8.GetBytes(json);
                    this.bw.Write(jsonBytes.Length);
                    this.bw.Write(jsonBytes);
                    Log.Trace($"JSON Send: Token: {token}, JSON: {json}");
                }
                catch( Exception e )
                {
                    currentException = e;
                    this.errorCallback?.Invoke(currentException);
                    Log.Trace($"Write Query failed for Token {token}. Exception: {e.Message}");
                    throw;
                }
            }
            return awaiter?.Task ?? TaskHelper.CompletedResponse;
        }

        public virtual bool Closed => !socket.Connected;

        public virtual bool Open => socket.Connected;

        public virtual IPEndPoint ClientEndPoint => this.socket.LocalEndPoint as IPEndPoint;

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
                socket.Shutdown(SocketShutdown.Both);
                socket.Dispose();
            }
            catch
            {
            }

            currentException = currentException ?? new EndOfStreamException("The driver connection is closed.");
        }
    }
}