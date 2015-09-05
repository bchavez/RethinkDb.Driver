using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json.Linq;
using RethinkDb.Driver.Ast;
using RethinkDb.Driver.Proto;

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
			this.Token = query.Token;
			connection.AddToCache(query.Token, this);
		}

        public void SetError(string msg)
        {
            if( this.error != null ) return;

            this.error = new ReqlRuntimeError(msg);

            var dummyResponse = Response.Make(query.Token, ResponseType.SUCCESS_SEQUENCE)
                .Build();

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
					connection.Stop(this);
				}
			}
		}

        public virtual void Extend(Response response)
		{
			outstandingRequests -= 1;
			threshold = response.Data.Count;
			if (error == null)
			{
				if (response.IsPartial)
				{
				    foreach( var item in response.Data )
				        items.Add(item);
				}
				else if (response.IsSequence)
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
			MaybeFetchBatch();
			if (outstandingRequests == 0 && error != null)
			{
				connection.RemoveFromCache(response.Token);
			}
		}

		protected internal virtual void MaybeFetchBatch()
		{
			if (error == null && items.Count <= threshold && outstandingRequests == 0)
			{
				outstandingRequests += 1;
				connection.Continue(this);
			}
		}

		internal virtual string Error
		{
			set
			{
				if (error != null)
				{
				    error = new ReqlRuntimeError(value);
					Response dummyResponse = Response.Make(query.Token, ResponseType.SUCCESS_SEQUENCE).Build();
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

		public virtual T next(TimeSpan? timeout)
		{
			return getNext(timeout);
		}

		// Abstract methods
		internal abstract T getNext(TimeSpan? timeout);

		private class DefaultCursor<T> : Cursor<T>
		{
		    private FormatOptions fmt;
			public DefaultCursor(Connection connection, Query query) : base(connection, query)
			{
			    this.fmt = new FormatOptions(query.GlobalOptions);
			}

			internal override T getNext(TimeSpan? timeout)
			{
				while (items.Count == 0)
				{
					MaybeFetchBatch();
				    if( error != null )
				        throw error;

				    connection.ReadResponse(query.Token, NetUtil.Deadline(timeout));
				}
			    var element = items.First();
			    items.RemoveAt(0);
				return Converter3.ConvertPesudoTypes(element, fmt).Value<T>();
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