using RethinkDb.Driver.Ast;
using RethinkDb.Driver.Model;

namespace com.rethinkdb.model
{
	public interface ReqlFunction2 : ReqlLambda
	{
		ReqlAst apply(ReqlAst row1, ReqlAst row2);
	}

}