using System;
using System.Collections.Generic;
using System.Runtime.ExceptionServices;
using com.rethinkdb.net;
using RethinkDb.Driver;
using RethinkDb.Driver.Ast;
using RethinkDb.Driver.Proto;

namespace RethinkDb.Driver.Net
{

	public abstract class Cursor<T> : IEnumerator<T>
	{
		// public immutable members
		public readonly long token;

		// immutable members
		protected internal readonly Connection connection;
		protected internal readonly Query query;

		// mutable members
		protected internal List<T> items = new List<T>();
		protected internal int outstandingRequests = 1;
		protected internal int threshold = 0;
		protected internal Exception error = null;

		public Cursor(Connection<T> connection, Query query)
		{
			this.connection = connection;
			this.query = query;
			this.token = query.token;
			connection.addToCache(query.token, this);
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

        

		internal virtual void extend(Response response)
		{
			outstandingRequests -= 1;
			threshold = response.data.Count;
			if (error == null)
			{
				if (response.Partial)
				{
					items.addAll(response.data);
				}
				else if (response.Sequence)
				{
					items.addAll(response.data);
					error = Optional.of(new NoSuchElementException());
				}
				else
				{
					error = Optional.of(response.makeError(query));
				}
			}
			maybeFetchBatch();
			if (outstandingRequests == 0 && error.Present)
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
					extend(dummyResponse);
				}
			}
		}

		public static Cursor<T> empty<T>(Connection<T> connection, Query query)
		{
			return new DefaultCursor<T>(connection, query);
		}

		public override T next()
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
			public DefaultCursor(Connection<T> connection, Query query) : base(connection, query)
			{
			}

			internal override T getNext(Optional<int?> timeout)
			{
				while (items.Count == 0)
				{
					maybeFetchBatch();
					error.ifPresent(exc =>
					{
						throw exc;
					});
					connection.readResponse(query.token, timeout.map(Util::deadline));
				}
				object element = items.pop();
				return (T) Converter.convertPseudo(element, query.globalOptions);
			}

		}
	}

}