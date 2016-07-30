using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
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
            this.conn = conn;
            this.IsFeed = firstResponse.IsFeed;
            this.Token = query.Token;
            this.fmt = FormatOptions.FromOptArgs(query.GlobalOptions);

            this.conn.AddToCache(this.Token, this);
            //is the first response all there is?
            PrepCursor(firstResponse);
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
                //if we get here, the next batch should be available.
                var response = await this.pendingResponse.OrCancelOn(cancelToken).ConfigureAwait(false);
                //if the current pendingResponse task is the task I received the
                //response from, then load "null" into the pendingResponse variable
                //interlockingly to prepare for firing off another CONTINUE.
                //
                //However, if the current pendingResponse task is *not* the
                //task I received the response from, then don't set "null"
                //because STOP was probably issued, and therefore, we 
                //cannot fire off a CONTINUE.
                Interlocked.CompareExchange(ref this.pendingResponse, null, this.pendingResponse);
                this.PrepCursor(response);
                this.MaybeSendContinue();
                this.ExtendBuffer(response);
            }

            //either way, we have something to advance.
            AdvanceCurrent();

            return true;
        }

        void PrepCursor(Response response)
        {
            if (response.IsError)
            {
                var error = response.MakeError(null); //this would be null anyway on CONTINUE or STOP.
                Log.Debug($"Cursor error. Token: {this.Token}, Error: {error}.");
                throw error;
            }
            if (response.IsSequence)
            {
                this.SequenceFinished();
            }
        }

        //we need this to interlock QUERY:CONTINUE
        //and QUERY:STOP so we don't get stuck in a 
        //situation where we CONTINUE, STOP, CONTINUE. lol.
        private readonly object interlock = new object();

        void MaybeSendContinue()
        {
            lock( interlock )
            {
                if( this.IsOpen && this.conn.Open && this.pendingResponse == null && !sequenceFinished )
                {
                    this.pendingResponse = this.conn.Continue(this);
                }
            }
        }

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

        bool sequenceFinished;

        void SequenceFinished()
        {
            sequenceFinished = true;
            this.Shutdown("The sequence is finished. There are no more items to iterate over.");
        }

        /// <summary>
        /// Disposes the Cursor so it cannot be used anymore.
        /// </summary>
        public void Dispose()
        {
            this.Shutdown("The Cursor was disposed. Iteration cannot continue. If the Cursor was used in a LINQ expression LINQ may have called .Dispose() on the Cursor.");
        }

        /// <summary>
        /// Forcibly closes the Cursor locally and on the server so it cannot be used anymore.
        /// </summary>
        /// <param name="waitForReplyOnThisThread">
        /// If waitForReplyOnThisThread = false (default), then it is expected 
        /// that the last server response will be read by a thread currently 
        /// enumerating over the cursor and will examine the last response for errors as
        /// a result of sending a STOP message.
        /// 
        /// If waitForReplyOnThisThread = true, then <see cref="Close"/> assumes that the there is no
        /// thread in the process of enumerating over the cursor. True assumes the thread calling
        /// <see cref="Close"/> will perform the last read from the server to check for errors
        /// as a result of sending a STOP message.
        /// 
        /// If the situation arises where there is no thread enumerating over the cursor
        /// and waitForReplyOnThisThread = false, the last response from the server will simply be ignored.
        /// No harm, no foul.
        /// </param>
        public void Close(bool waitForReplyOnThisThread = false)
        {
            this.CloseAsync(waitForReplyOnThisThread).WaitSync();
        }

        /// <summary>
        /// Asynchronously, forces closure of the Cursor locally and on the server.
        /// </summary>
        /// <param name="waitForReplyOnThisThread">
        /// If waitForReplyOnThisThread = false (default), then it is expected 
        /// that the last server response will be read by a thread currently 
        /// enumerating over the cursor and will examine the last response for errors as
        /// a result of sending a STOP message.
        /// 
        /// If waitForReplyOnThisThread = true, then <see cref="CloseAsync"/> assumes that the there is no
        /// thread in the process of enumerating over the cursor. True assumes the thread calling
        /// <see cref="CloseAsync"/> will perform the last read from the server to check for errors
        /// as a result of sending a STOP message.
        /// 
        /// If the situation arises where there is no thread enumerating over the cursor
        /// and waitForReplyOnThisThread = false, the last response from the server will simply be ignored.
        /// No harm, no foul.
        /// </param>
        public async Task CloseAsync(bool waitForReplyOnThisThread = false)
        {
            this.Shutdown("The Cursor was forcibly closed. Iteration cannot continue.");
            if( waitForReplyOnThisThread && this.pendingResponse != null )
            {
                var lastResponse = await this.pendingResponse.ConfigureAwait(false);
                this.PrepCursor(lastResponse);
            }
        }

        void Shutdown(string reason)
        {
            if( this.conn.Open && this.IsOpen )
            {
                conn.RemoveFromCache(this.Token);
                if( !sequenceFinished )
                {
                    // The awatier task would be exactly the same
                    // if there was an already in progress CONTINUE
                    // being awaited on by another thread.
                    // SocketWrapper will TryGetValue if an awater exists.
                    //
                    // If an awatier does not exist and no thread is awating any
                    // cursor response then we set the pending response here.
                    // If there is a thread a thread iterating over MoveNextAsync
                    // (but late that it has not yet sent a CONTINUE)
                    // the tread will use the pending response we made here as a
                    // result of sending STOP.
                    lock( interlock )
                    {
                        var stopResponse = conn.Stop(this);
                        Interlocked.Exchange(ref this.pendingResponse, stopResponse);
                    }
                }
                SetError(reason);
            }
            else
            {
                SetError(reason);
            }
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