using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using com.rethinkdb.net;
using Newtonsoft.Json.Linq;
using RethinkDb.Driver.Ast;
using RethinkDb.Driver.Proto;
using Util = com.rethinkdb.net.Util;

namespace RethinkDb.Driver.Net
{
    internal interface ICursor
    {
        void Extend(Response response);
        void SetError(string msg);
        long Token { get; }
    }

    internal abstract class Cursor<T> : IEnumerable<T>, IEnumerator<T>, ICursor
	{
		// public immutable members
		public long Token { get; }

		// immutable members
		protected internal readonly Connection connection;
		protected internal readonly Query query;

		// mutable members
		protected internal List<JToken> items = new List<JToken>();
		protected internal int outstandingRequests = 1;
		protected internal int threshold = 0;
		protected internal Exception error = null;

		public Cursor(Connection connection, Query query)
		{
			this.connection = connection;
			this.query = query;
			this.Token = query.token;
			connection.addToCache(query.token, this);
		}

        public void SetError(string msg)
        {
            if( this.error != null ) return;

            this.error = new ReqlRuntimeError(msg);

            var dummyResponse = Response.make(query.token, ResponseType.SUCCESS_SEQUENCE)
                .build();

            Extend(dummyResponse);
        }

		public virtual void close()
		{
			if (error == null)
			{
				error = new Exception("No such element.");
				if (connection.Open)
				{
					outstandingRequests += 1;
					connection.stop(this);
				}
			}
		}

        public virtual void Extend(Response response)
		{
			outstandingRequests -= 1;
			threshold = response.data.Count;
			if (error == null)
			{
				if (response.Partial)
				{
				    foreach( var item in response.data )
				        items.Add(item);
				}
				else if (response.Sequence)
				{
                    foreach( var item in response.data )
                        items.Add(item);
				    error = new InvalidOperationException("No such element");
				}
				else
				{
				    error = response.makeError(query);
				}
			}
			maybeFetchBatch();
			if (outstandingRequests == 0 && error != null)
			{
				connection.removeFromCache(response.token);
			}
		}

		protected internal virtual void maybeFetchBatch()
		{
			if (error == null && items.Count <= threshold && outstandingRequests == 0)
			{
				outstandingRequests += 1;
				connection.continue_(this);
			}
		}

		internal virtual string Error
		{
			set
			{
				if (error != null)
				{
				    error = new ReqlRuntimeError(value);
					Response dummyResponse = Response.make(query.token, ResponseType.SUCCESS_SEQUENCE).build();
					Extend(dummyResponse);
				}
			}
		}

		public static Cursor<T> empty(Connection connection, Query query)
		{
			return new DefaultCursor<T>(connection, query);
		}

		public T next()
		{
			return getNext(null);
		}

		public virtual T next(int timeout)
		{
			return getNext(timeout);
		}

		// Abstract methods
		internal abstract T getNext(int? timeout);

		private class DefaultCursor<T> : Cursor<T>
		{
			public DefaultCursor(Connection connection, Query query) : base(connection, query)
			{
			}

			internal override T getNext(int? timeout)
			{
				while (items.Count == 0)
				{
					maybeFetchBatch();
				    if( error != null )
				        throw error;

				    connection.readResponse(query.token, Util.deadline(timeout.GetValueOrDefault(60)));
				}
			    object element = items.First();
			    items.RemoveAt(0);
				return (T) Converter.convertPseudo(element, query.globalOptions);
			}

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

        public bool MoveNext()
        {
            this.Current = this.next();
            return this.Current != null;
        }

        public void Reset()
        {
            this.Current = default(T);
        }

        public T Current { get; private set; }

        object IEnumerator.Current
        {
            get { return Current; }
        }
	}
}