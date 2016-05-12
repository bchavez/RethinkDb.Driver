using System.Linq.Expressions;
using Remotion.Linq;
using RethinkDb.Driver.Ast;
using RethinkDb.Driver.Linq.Attributes;

namespace RethinkDb.Driver.Linq.WhereClauseParsers
{
    public class PrimaryIndexParser : BaseIndexParser<PrimaryIndexAttribute>
    {
        public override ReqlExpr Parse( ReqlExpr expression, QueryModel queryModel, Expression predicate )
        {
            var binaryExpression = (BinaryExpression)predicate;
            var value = GetValue( binaryExpression );

            return ( (Table)expression ).Get( value );
        }
    }
}
