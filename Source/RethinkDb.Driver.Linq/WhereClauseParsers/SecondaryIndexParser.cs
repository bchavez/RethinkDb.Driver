using System.Linq.Expressions;
using Remotion.Linq;
using RethinkDb.Driver.Ast;
using RethinkDb.Driver.Linq.Attributes;

namespace RethinkDb.Driver.Linq.WhereClauseParsers
{
    public class SecondaryIndexParser : BaseIndexParser<SecondaryIndexAttribute>
    {
        public override ReqlExpr Parse( ReqlExpr expression, QueryModel queryModel, Expression predicate )
        {
            var binaryExpression = (BinaryExpression)predicate;
            var value = GetValue( binaryExpression );

            return ( (Table)expression ).GetAll( value ).OptArg( "index", GetIndexName( binaryExpression ) );
        }

        private static string GetIndexName( BinaryExpression binaryExpression )
        {
            var left = binaryExpression.Left as MemberExpression;
            return left?.Member.Name ?? ( (MemberExpression)binaryExpression.Right ).Member.Name;
        }
    }
}
