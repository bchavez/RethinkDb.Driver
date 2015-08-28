using System.Collections.Generic;
using System.Linq;
using RethinkDb.Driver.Ast;

namespace RethinkDb.Driver.Model
{
	public class Arguments : List<ReqlAst>
	{

		public Arguments()
		{
		}
		public Arguments(object arg1)
		{
			this.Add(Util.ToReqlAst(arg1));
		}
		public Arguments(ReqlAst arg1)
		{
			this.Add(arg1);
		}

		public Arguments(object[] args) : this(args.ToList())
		{
		}

		public Arguments(IList<object> args)
		{
		    var ast = args.Select(o => Util.ToReqlAst(o)).ToList();
			this.AddRange(ast);
		}

		public static Arguments make(params object[] args)
		{
			return new Arguments(args);
		}
	}

}