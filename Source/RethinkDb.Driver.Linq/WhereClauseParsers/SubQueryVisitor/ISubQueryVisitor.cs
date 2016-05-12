using Remotion.Linq;
using RethinkDb.Driver.Ast;

namespace RethinkDb.Driver.Linq.WhereClauseParsers.SubQueryVisitor
{
    public interface ISubQueryVisitor
    {
        bool CanVisit( QueryModel queryModel );
        ReqlExpr Visit( ReqlExpr reqlExpr, QueryModel queryModel );
    }
}