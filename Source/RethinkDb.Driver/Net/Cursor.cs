using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;
using RethinkDb.Driver.Ast;
using RethinkDb.Driver.Proto;
using RethinkDb.Driver.Utils;

namespace RethinkDb.Driver.Net
{
    public class Cursor<T> : IEnumerable<T>, IEnumerator<T>, ICursor
    {
        private readonly Connection conn;

        public Cursor(Connection conn, Query query, Response firstResponse)
        {
            this.conn = conn;
            this.IsFeed = firstResponse.IsFeed;
            this.Token = query.Token;

            this.conn.AddToCache(this.Token, this);
            //is the first response all there is?
            this.sequenceFinished = firstResponse.Type == ResponseType.SUCCESS_SEQUENCE;
            MaybeSendContinue();
            ExtendBuffer(firstResponse);
        }

        public int BufferedSize => this.items.Count;

        public List<T> BufferedItems => items.Select(Convert).ToList();

        public bool IsFeed { get; set; }

        public bool IsOpen => this.Error == null;

        public Exception Error { get; private set; }

        public long Token { get; }

        private Task<Response> pendingContinue;

        private readonly Queue<JToken> items = new Queue<JToken>();
        
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
        ///     Used to cancel the operation if it takes too long.
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
            while (items.Count == 0)
            {
                if (!this.IsOpen) return false;

                //our buffer is empty, we need to expect the next batch of items.

                if (!this.pendingContinue.IsCompleted)
                {
                    //the next batch isn't here yet. so,
                    //let's await and honor the cancelToken.

                    //create a task that is controlled by the token.
                    using (var cancelTask = new CancellableTask(cancelToken))
                    {
                        //now await on either task, pending or the cancellation of the CancellableTask.
                        await Task.WhenAny(this.pendingContinue, cancelTask.Task).ConfigureAwait(false);
                        //if it was the cancelTask that triggered the continuation... throw if requested.
                        cancelToken.ThrowIfCancellationRequested();
                        //else, no cancellation was requested.
                        //we can proceed by processing the results we awaited for.
                    }//ensure the disposal of the cancelToken registration upon exiting scope
                }

                //if we get here, the next batch should be available.
                var nextBatch = this.pendingContinue.Result;
                this.pendingContinue = null;
                MaybeSendContinue();
                this.ExtendBuffer(nextBatch);
            }

            //either way, we have something to advance.
            AdvanceCurrent();

            return await TaskHelper.CompletedTaskTrue.ConfigureAwait(false);
        }

        void MaybeSendContinue()
        {
            if (this.IsOpen && this.conn.Open && pendingContinue == null && !sequenceFinished)
            {
                this.pendingContinue = this.conn.Continue(this);
            }
        }

        void ExtendBuffer(Response response)
        {
            if (this.IsOpen)
            {
                if (response.IsPartial)
                {
                    //SUCCESS_PARTIAL
                    foreach (var jToken in response.Data)
                    {
                        items.Enqueue(jToken);
                    }
                }
                else if (response.IsSequence)
                {
                    //SUCCESS_SEQUENCE
                    foreach (var jToken in response.Data)
                    {
                        items.Enqueue(jToken);
                    }
                    this.SequenceFinished();
                }
                else
                {
                    throw new NotSupportedException("Cursor cannot extend the response. The response was not a SUCCESS_PARTIAL or SUCCESS_SEQUENCE.");
                }
            }
        }

        T Convert(JToken token)
        {
            return token.ToObject<T>(Converter.Serializer);
        }

        bool sequenceFinished;
        void SequenceFinished()
        {
            sequenceFinished = true;
            this.Shutdown("The sequence is finished. There are no more items to iterate over.");
        }

        public void Dispose()
        {
            this.Shutdown("The Cursor was disposed. Iteration cannot continue. If the Cursor was used in a LINQ expression LINQ may have called .Dispose() on the Cursor.");
        }

        public void Close()
        {
            this.Shutdown("The Cursor was forcibly closed. Iteration cannot continue.");
        }

        void Shutdown(string reason)
        {
            if (this.conn.Open && this.IsOpen)
            {
                conn.RemoveFromCache(this.Token);
                if(!sequenceFinished)
                {
                    conn.Stop(this);
                }
                SetError(reason);
            }
            else
            {
                SetError(reason);
            }
        }


        public void SetError(string msg)
        {
            if (this.Error != null) return;
            this.Error = new InvalidOperationException(msg);
        }


        public void ClearBuffer()
        {
            this.items.Clear();
        }

        public void Reset()
        {
            throw new InvalidOperationException("A Cursor cannot be reset.");
        }

        public T Current { get; private set; }

        object IEnumerator.Current => this.Current;

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
