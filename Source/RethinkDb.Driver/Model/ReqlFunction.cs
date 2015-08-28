using RethinkDb.Driver.Ast;

namespace RethinkDb.Driver.Model
{
	public interface ReqlFunction : ReqlLambda
	{
		ReqlAst Apply(ReqlAst row);
	}

}