using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;
using RethinkDb.Driver.Ast;
using RethinkDb.Driver.Model;
using RethinkDb.Driver.Proto;
using RethinkDb.Driver.Utils;

namespace RethinkDb.Driver.Net
{
    /// <summary>
    /// Cursor that handles stream types of responses.
    /// </summary>
    /// <typeparam name="T">The underlying item being iterated over.</typeparam>
    public class Cursor<T> : IEnumerable<T>, IEnumerator<T>, ICursor
    {
        private readonly Connection conn;

        internal Cursor(Connection conn, Query query, Response firstResponse)
        {
            this.closeTask = new CancellableTask(this.closeTokenSource.Token);

            this.conn = conn;
            this.IsFeed = firstResponse.IsFeed;
            this.Token = query.Token;
            this.fmt = FormatOptions.FromOptArgs(query.GlobalOptions);

            this.conn.AddToCache(this.Token, this);
            //is the first response all there is?
            this.sequenceFinished = firstResponse.Type == ResponseType.SUCCESS_SEQUENCE;
            MaybeSendContinue();
            ExtendBuffer(firstResponse);
        }

        /// <summary>
        /// The size of the buffered items.
        /// </summary>
        public int BufferedSize => this.items.Count;

        /// <summary>
        /// The list of items in the queue. This does not include the Current item.
        /// </summary>
        public List<T> BufferedItems => items.Select(Convert).ToList();

        /// <summary>
        /// Whether the Cursor is any kind of feed.
        /// </summary>
        public bool IsFeed { get; private set; }

        /// <summary>
        /// A flag to determine if the cursor can still be used.
        /// </summary>
        public bool IsOpen => this.Error == null;

        /// <summary>
        /// If any, the error that disabled the cursor.
        /// </summary>
        public Exception Error { get; private set; }

        /// <summary>
        /// The token of the query that the cursor represents. This is a unique cursor ID.
        /// </summary>
        public long Token { get; }

        private Task<Response> pendingResponse;

        private readonly Queue<JToken> items = new Queue<JToken>();

        private CancellationTokenSource closeTokenSource = new CancellationTokenSource();
        private CancellableTask closeTask;

        //we need these to keep track of the run options
        //for things like "time_format: 'raw'"
        private FormatOptions fmt;

        void AdvanceCurrent()
        {
            var item = items.Dequeue();
            this.Current = Convert(item);
        }

        /// <summary>
        /// Advances the cursor to the next item. This is a blocking operation 
        /// if there are no buffered items to advance on and a response from 
        /// the server is needed.
        /// </summary>
        public bool MoveNext()
        {
            return MoveNextAsync().WaitSync();
        }

        /// <summary>
        /// Asynchronously advances the cursor to the next item.
        /// </summary>
        /// 
        /// <param name="cancelToken">
        ///   <para>
        ///     Used to cancel the advancement of the next item if it takes too long.
        ///   </para>
        ///   <para>
        ///     The <paramref name="cancelToken"/> has no effect if the cursor still
        ///     has buffered items to draw from. Cancellation pertains to the
        ///     wait on an outstanding network request that is taking too long.
        ///   </para>
        ///   <para>
        ///     If the <paramref name="cancelToken"/> is canceled before
        ///     <see cref="MoveNextAsync"/> is called, <see cref="TaskCanceledException"/>
        ///     is thrown immediately before any operation begins.
        ///   </para>
        ///   <para>
        ///     Additionally, cancellation is a safe operation. When
        ///     a <see cref="TaskCanceledException"/> is thrown, the exception will 
        ///     not disrupt the ordering of cursor items. Cancellation only pertains to
        ///     the semantic *wait* on a pending network request. Cancellation will not 
        ///     cancel an already in-progress network request for more items. 
        ///     Therefore, a <see cref="TaskCanceledException"/>
        ///     will not disrupt the success of future precedent calls 
        ///     to <see cref="MoveNextAsync"/>. Network requests will still arrive 
        ///     in order at some later time.
        ///    </para>
        /// </param>
        /// 
        /// <exception cref="TaskCanceledException">
        ///   <para>
        ///       Thrown when <paramref name="cancelToken"/> is canceled before
        ///       <see cref="MoveNextAsync"/> is called.
        ///   </para> 
        ///   <para>
        ///       Thrown when there are no buffered items to draw from and operation
        ///       requires waiting on a response from the server.
        ///   </para> 
        ///   <para>
        ///       When <see cref="TaskCanceledException"/> is thrown the exception
        ///       will not disrupt the ordering of items. Any non-canceled precedent calls to
        ///       <see cref="MoveNextAsync"/> from an antecedent canceled <see cref="TaskCanceledException"/>
        ///       will advance the cursor normally and maintain ordering of items.
        ///   </para>
        /// </exception>
        public async Task<bool> MoveNextAsync(CancellationToken cancelToken = default(CancellationToken))
        {
            cancelToken.ThrowIfCancellationRequested();
            while( items.Count == 0 )
            {
                if( !this.IsOpen ) return false;

                //our buffer is empty, we need to expect the next batch of items.

                if( !this.pendingResponse.IsCompleted )
                {
                    //the next batch isn't here yet. so,
                    //let's await and honor the cancelToken.

                    //create a task that is controlled by the token.
                    using( var userCancel = new CancellableTask(cancelToken) )
                    {
                        //now await on *any* of the following tasks to complete:
                        // 1. the pending network request.
                        // 2. the user's request to cancel the network read operation.
                        // 3. the user's request to close the cursor.
                        await Task.WhenAny(this.pendingResponse, userCancel.Task, this.closeTask.Task).ConfigureAwait(false);
                        //if it was the cancelTask that triggered the continuation... throw if requested.
                        cancelToken.ThrowIfCancellationRequested();
                        if( cancelToken.IsCancellationRequested ) return false;
                    } //ensure the disposal of the cancelToken registration upon exiting scope
                }
                lock( locker )
                {
                    //double check we're allowed to process the results
                    if (this.closeTokenSource.IsCancellationRequested)
                    {
                        //if close was requested, politely return the thread
                        //to the user signaling that we don't have any more
                        //items available. the closer task will take care of
                        //the very last read.
                        return false;
                    }
                    //else, no cancellation was requested.
                    //we can proceed by processing the results we awaited for.

                    this.AdvanceCursor();
                }
            }

            //either way, we have an item to advance.
            AdvanceCurrent();

            return true;
        }

        void MaybeSendContinue()
        {
            if( this.IsOpen && this.conn.Open && pendingResponse == null && 
                !sequenceFinished &&
                !this.closeTokenSource.IsCancellationRequested)
            {
                this.pendingResponse = this.conn.Continue(this);
            }
        }

        void AdvanceCursor()
        {
            //if we get here, the next batch should be available.
            var response = this.pendingResponse.Result;
            this.pendingResponse = null;
            //is the sequence finished? read the response first
            this.ReadResponse(response);
            //read response should have checked if it's finished or not. so...
            this.MaybeSendContinue();
            //extend da buffer wit whatever we got.
            this.ExtendBuffer(response);
        }

        void ReadResponse(Response response)
        {
            if (response.IsError)
            {
                var error = response.MakeError(null); //this would be null anyway on CONTINUE or STOP.
                Log.Debug($"Cursor error. Token: {this.Token}, Error: {error}.");
                throw error;
            }
            if( response.IsSequence )
            {
                this.SequenceFinished();
            }
        }

        bool sequenceFinished;

        void SequenceFinished()
        {
            sequenceFinished = true;
            this.Shutdown("The sequence is finished. There are no more items to iterate over.");
        }

        private object locker = new object();
        void ExtendBuffer(Response response)
        {
            if( response.IsPartial )
            {
                //SUCCESS_PARTIAL
                foreach( var jToken in response.Data )
                {
                    items.Enqueue(jToken);
                }
            }
            else if( response.IsSequence )
            {
                //SUCCESS_SEQUENCE
                foreach( var jToken in response.Data )
                {
                    items.Enqueue(jToken);
                }
            }
            else
            {
                throw new NotSupportedException("Cursor cannot extend the response. The response was not a SUCCESS_PARTIAL or SUCCESS_SEQUENCE.");
            }
        }

        T Convert(JToken token)
        {
            if( typeof(T).IsJToken() )
            {
                Converter.ConvertPseudoTypes(token, fmt);
                return (T)(object)token; //ugh ugly. find a better way to do this.
            }
            return token.ToObject<T>(Converter.Serializer);
        }

        /// <summary>
        /// Disposes the Cursor so it cannot be used anymore.
        /// </summary>
        public void Dispose()
        {
            this.Shutdown("The Cursor was disposed. Iteration cannot continue. If the Cursor was used in a LINQ expression LINQ may have called .Dispose() on the Cursor.");
        }

        /// <summary>
        /// Forcibly closes the Cursor so it cannot be used anymore.
        /// </summary>
        public void Close()
        {
            CloseAsync().WaitSync();
        }

        /// <summary>
        /// Asynchronously close the cursor so it cannot be used anymore.
        /// Exercise caution when using a cancellation token with this method.
        /// If you cancel a Cursor.CloseAsync() operation and the STOP message
        /// is not sent to the server, then the cursor will not be closed
        /// on the server leading to a potential memory issue. Be sure this
        /// operation completes successfully.
        /// </summary>
        public async Task CloseAsync(CancellationToken cancelToken = default(CancellationToken))
        {
            await this.ShutdownAsync("The Cursor was forcibly closed. Iteration cannot continue.", cancelToken)
                .ConfigureAwait(false);
        }

        void Shutdown(string reason)
        {
            ShutdownAsync(reason).WaitSync();
        }
        
        async Task ShutdownAsync(string reason, CancellationToken cancelToken = default(CancellationToken))
        {
            if( this.conn.Open && this.IsOpen )
            {
                conn.RemoveFromCache(this.Token);
                if( !sequenceFinished )
                {
                    /* So, there could potentially be a continue
                     * in flight already over the wire, and an awaiter
                     * waiting for a continued response. Take for example,
                     * a ChangeFeed with nothing happening. We'd be waiting
                     * for some changes, but if there is no activity then
                     * a thread would potentially be blocking on that future
                     * change.
                     */
                    //So, let's shutdown any reading threads
                    this.closeTokenSource.Cancel();
                    Monitor.Enter(locker);
                    try
                    {
                        if( !this.sequenceFinished ) // is the cursor still not finished?
                        {
                            //now we should be free to read the response of any STOP action.
                            // https://github.com/rethinkdb/rethinkdb/issues/6014
                            conn.Stop(this);
                            //wait for the STOP response to come in.
                            await this.pendingResponse.OrCancelOn(cancelToken).ConfigureAwait(false);
                            this.AdvanceCursor(); // last time to read the response
                        }
                    }
                    finally
                    {
                        Monitor.Exit(locker);
                    }
                }
                SetError(reason);
            }
            else
            {
                SetError(reason);
            }
            this.closeTask.Dispose();
        }

        //Some trickery just so we don't expose SetError
        //on the public API surface.
        internal void SetError(string msg)
        {
            if( this.Error != null ) return;
            this.Error = new InvalidOperationException(msg);
        }

        void ICursor.SetError(string msg)
        {
            SetError(msg);
        }

        /// <summary>
        /// Clears <see cref="BufferedItems"/>. Any advancement after the buffer
        /// is cleared will cause a new batch of items to be buffered.
        /// </summary>
        public void ClearBuffer()
        {
            this.items.Clear();
        }

        /// <summary>
        /// Throws always. A Cursor cannot be reset. Hidden from public use.
        /// </summary>
        /// <exception cref="InvalidOperationException">Throws always. A Cursor cannot be reset.</exception>
        void IEnumerator.Reset()
        {
            throw new InvalidOperationException("A Cursor cannot be reset.");
        }

        /// <summary>
        /// The current item in iteration.
        /// </summary>
        public T Current { get; private set; }

        object IEnumerator.Current => this.Current;

        /// <summary>
        /// The cursor's enumerator
        /// </summary>
        public IEnumerator<T> GetEnumerator()
        {
            return this;
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }
    }
}