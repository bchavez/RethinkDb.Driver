using System;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using Remotion.Linq;
using Remotion.Linq.Clauses;
using Remotion.Linq.Clauses.Expressions;
using Remotion.Linq.Clauses.ResultOperators;
using RethinkDb.Driver.Ast;

namespace RethinkDb.Driver.Linq.WhereClauseParsers
{
    public class GroupItemsParser : IWhereClauseParser
    {
        public bool IsAppropriate( ReqlAst reql, Expression expression, Type resultType )
        {
            var subQueryExpression = expression as SubQueryExpression;
            if( subQueryExpression == null )
                return false;

            var type = subQueryExpression.QueryModel.MainFromClause.FromExpression.Type;

            if( type.GetTypeInfo().IsGenericType
                && type.GetGenericTypeDefinition() == typeof( IGrouping<,> ) )
            {
                if( subQueryExpression.QueryModel.ResultOperators[0] is AnyResultOperator )
                    return true;
                throw new NotImplementedException( "This filter is not supported for GroupBy" );
            }

            return false;
        }
        
        public ReqlExpr Parse( ReqlExpr expression, QueryModel queryModel, Expression predicate )
        {
            return expression.Filter( x => x["reduction"].Contains( reqlExpr => GetWhereReqlAst( reqlExpr, predicate ) ) );
        }

        private static ReqlExpr GetWhereReqlAst( ReqlExpr reqlExpr, Expression predicate )
        {
            var subQueryExpression = predicate as SubQueryExpression;
            var where = subQueryExpression.QueryModel.BodyClauses[0] as WhereClause;

            var visitor = new ExpressionVisitor( reqlExpr, subQueryExpression.QueryModel.MainFromClause.ItemType );
            visitor.Visit( where.Predicate );
            return visitor.Current;
        }
    }
}
