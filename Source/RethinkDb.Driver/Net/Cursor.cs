using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json.Linq;
using RethinkDb.Driver.Ast;
using RethinkDb.Driver.Proto;

namespace RethinkDb.Driver.Net
{
    internal interface ICursor : IEnumerable, IEnumerator
    {
        void Extend(Response response);
        void SetError(string msg);
        long Token { get; }
    }

    public abstract class Cursor<T> : IEnumerable<T>, IEnumerator<T>, ICursor
    {
        // public immutable members
        public long Token { get; }

        // immutable members
        protected internal readonly Connection connection;
        protected internal readonly Query query;

        // mutable members
        protected internal List<JToken> items = new List<JToken>();
        protected internal int outstandingRequests = 0;
        protected internal int threshold = 1;
        protected internal Exception error = null;
        public bool IsFeed { get; }

        protected Cursor(Connection connection, Query query, Response firstResponse)
        {
            this.connection = connection;
            this.query = query;
            this.Token = query.Token;
            this.IsFeed = firstResponse.IsFeed;
            connection.AddToCache(query.Token, this);
            MaybeSendContinue();
            ExtendInternal(firstResponse);
        }


        public virtual void close()
        {
            if( error == null )
            {
                error = new Exception("No such element.");
                if( connection.Open )
                {
                    outstandingRequests += 1;
                    connection.Stop(this);
                }
            }
        }

        public int BufferedSize => this.items.Count;

        public List<T> BufferedItems => items.Select(Convert).ToList();

        private void ExtendInternal(Response response)
        {
            threshold = response.Data.Count;
            if( error == null )
            {
                if( response.IsPartial )
                {
                    foreach( var item in response.Data )
                        items.Add(item);
                }
                else if( response.IsSequence )
                {
                    foreach( var item in response.Data )
                        items.Add(item);
                    error = new InvalidOperationException("No such element");
                }
                else
                {
                    error = response.MakeError(query);
                }
            }
            if( outstandingRequests == 0 && error != null )
            {
                connection.RemoveFromCache(response.Token);
            }
        }

        public virtual void Extend(Response response)
        {
            outstandingRequests -= 1;
            MaybeSendContinue();
            ExtendInternal(response);
        }

        public void SetError(string msg)
        {
            if( this.error != null ) return;

            this.error = new ReqlRuntimeError(msg);

            var dummyResponse = Response.Make(query.Token, ResponseType.SUCCESS_SEQUENCE)
                .Build();

            ExtendInternal(dummyResponse);
        }

        protected internal virtual void MaybeSendContinue()
        {
            if( error == null && items.Count < threshold && outstandingRequests == 0 )
            {
                outstandingRequests += 1;
                connection.Continue(this);
            }
        }

        internal virtual string Error
        {
            set
            {
                if( error != null )
                {
                    error = new ReqlRuntimeError(value);
                    Response dummyResponse = Response.Make(query.Token, ResponseType.SUCCESS_SEQUENCE).Build();
                    Extend(dummyResponse);
                }
            }
        }

        public static Cursor<T> create(Connection connection, Query query, Response firstResponse)
        {
            return new DefaultCursor<T>(connection, query, firstResponse);
        }

        private class DefaultCursor<T> : Cursor<T>
        {
            public DefaultCursor(Connection connection, Query query, Response firstResponse) : base(connection, query, firstResponse)
            {

            }

            private TimeSpan? timeout;
            private T current;

            public override bool MoveNext()
            {
                while( items.Count == 0 )
                {
                    //if we're out of buffered items, poll until we get more.
                    MaybeSendContinue();
                    if( error != null )
                        return false; //we don't throw in .net

                    connection.ReadResponse(query, NetUtil.Deadline(timeout));
                }

                if( this.items.Count > 0 )
                {
                    var element = items[0];
                    items.RemoveAt(0);
                    this.current = Convert(element);
                    return true;
                }

                return false;
            }

            protected override T Convert(JToken token)
            {
                return token.ToObject<T>(Converter.Serializer);
            }

            public override T Current => this.current;
        }


        public IEnumerator<T> GetEnumerator()
        {
            return this;
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        public void Dispose()
        {
            this.close();
        }

        public abstract bool MoveNext();
        protected abstract T Convert(JToken token);

        public void Reset()
        {
            throw new ReqlDriverError("A Cursor can't be reset.");
        }

        public abstract T Current { get; }

        object IEnumerator.Current
        {
            get { return Current; }
        }
    }
}