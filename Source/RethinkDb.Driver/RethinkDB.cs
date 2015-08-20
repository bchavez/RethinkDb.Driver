using com.rethinkdb.net;
using RethinkDb.Driver.Ast.Gen;
using RethinkDb.Driver.Proto;

namespace RethinkDb.Driver
{


	public class RethinkDB : TopLevel
	{

		/// <summary>
		/// The Singleton to use to begin interacting with RethinkDB Driver
		/// </summary>
		public static readonly RethinkDB r = new RethinkDB();

		private RethinkDB() : base(null, TermType.DATUM, null, null)
		{
		}

		public virtual Connection.Builder<ConnectionInstance> connection()
		{
			return Connection.build();
		}
	}

}