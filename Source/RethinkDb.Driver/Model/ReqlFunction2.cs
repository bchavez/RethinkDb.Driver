using RethinkDb.Driver.Ast;

namespace RethinkDb.Driver.Model
{
	public interface ReqlFunction2 : ReqlLambda
	{
		ReqlAst apply(ReqlAst row1, ReqlAst row2);
	}

}