using RethinkDb.Driver.Ast;

namespace RethinkDb.Driver.Model
{
	public interface ReqlFunction2 : ReqlLambda
	{
		ReqlAst Apply(ReqlAst row1, ReqlAst row2);
	}

}