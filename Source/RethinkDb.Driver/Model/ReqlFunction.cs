using RethinkDb.Driver.Ast;

namespace RethinkDb.Driver.Model
{
	public interface ReqlFunction : ReqlLambda
	{
		ReqlAst apply(ReqlAst row);
	}

}