using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using Remotion.Linq;
using Remotion.Linq.Clauses;
using Remotion.Linq.Clauses.Expressions;
using Remotion.Linq.Clauses.ResultOperators;
using RethinkDb.Driver.Ast;
using RethinkDb.Driver.Linq.Attributes;
using RethinkDb.Driver.Linq.Visitors.SelectClause;
using RethinkDb.Driver.Linq.Visitors.WhereClause;
using RethinkDb.Driver.Linq.WhereClauseParsers;
using RethinkDb.Driver.Model;
using ExpressionVisitor = RethinkDb.Driver.Linq.WhereClauseParsers.ExpressionVisitor;
using RethinkDb.Driver.Utils;

namespace RethinkDb.Driver.Linq
{

    public class RethinkDbQueryModelVisitor : QueryModelVisitorBase
    {
        private readonly ReqlExpr _expr;
        
        public ReqlAst Query => Stack.Peek();
        public Stack<ReqlExpr> Stack = new Stack<ReqlExpr>();

        public RethinkDbQueryModelVisitor( ReqlExpr expr )
        {
            _expr = expr;
        }

        public override void VisitQueryModel( QueryModel queryModel )
        {
            Stack.Push( _expr );
            base.VisitQueryModel( queryModel );
        }

        public override void VisitMainFromClause( MainFromClause fromClause, QueryModel queryModel )
        {
            var subQuery = fromClause.FromExpression as SubQueryExpression;
            if( subQuery != null )
            {
                var query = subQuery.QueryModel;
                VisitBodyClauses( query.BodyClauses, query );
                base.VisitMainFromClause( fromClause, queryModel );
            }

            base.VisitMainFromClause( fromClause, queryModel );
        }

        public override void VisitWhereClause( WhereClause whereClause, QueryModel queryModel, int index )
        {
            var expressions = SplitWhereClause( whereClause.Predicate );

            var visitors = new IWhereClauseVisitor[]
            {
                new BetweenWhereClauseVisitor(),
                new DefaultWhereClauseVisitor()
            };

            foreach ( var visitor in visitors )
                expressions = visitor.Visit( expressions, queryModel, Stack );

            if ( expressions.Any() )
                throw new NotSupportedException( "Unable to visit Where clause" );
        }

        private static IEnumerable<Expression> SplitWhereClause( Expression predicate )
        {
            if( predicate.NodeType != ExpressionType.AndAlso )
            {
                yield return predicate;
                yield break;
            }

            var binaryExpression = predicate as BinaryExpression;
            if( binaryExpression == null )
                yield break;


            foreach( var expression in SplitWhereClause( binaryExpression.Left ) )
                yield return expression;
            yield return binaryExpression.Right;
        }

        public override void VisitOrderByClause( OrderByClause orderByClause, QueryModel queryModel, int index )
        {
            var expression = orderByClause.Orderings[0].Expression as MemberExpression;
            if( expression == null )
                return;

            OrderBy reql;
            var currentStack = Stack.Pop();
            var memberName = QueryHelper.GetJsonMemberName( expression.Member );
            if( orderByClause.Orderings[0].OrderingDirection == OrderingDirection.Asc )
                reql = currentStack.OrderBy( memberName );
            else
                reql = currentStack.OrderBy( RethinkDB.R.Desc( memberName ) );

            if( currentStack is Table && expression.Member.CustomAttributes.Any( x => x.AttributeType == typeof( PrimaryIndexAttribute ) || x.AttributeType == typeof( SecondaryIndexAttribute ) ) )
                reql = reql.OptArg( "index", memberName );

            Stack.Push( reql );
        }

        public override void VisitSelectClause( SelectClause selectClause, QueryModel queryModel )
        {
            var selectClauseVisitors = new ISelectClauseVisitor[]
            {
                new MathematicalSelectClauseVisitor(),
                new AverageSelectClauseVisitor(),
                new CountSelectClauseVisitor(),
                new MemberSelectClauseVisitor(),
                new NewObjectSelectClauseVisitor(),
            };
            
            var visitor = selectClauseVisitors.FirstOrDefault( x => x.IsAppropriate( selectClause, queryModel ) );
            if ( visitor == null )
                base.VisitSelectClause( selectClause, queryModel );
            else
                visitor.Visit( selectClause, queryModel, Stack );
        }

        protected override void VisitBodyClauses( ObservableCollection<IBodyClause> bodyClauses, QueryModel queryModel )
        {
            if( queryModel.ResultTypeOverride.GetTypeInfo().IsGenericTypeDefinition && queryModel.ResultTypeOverride.GetGenericArguments()[0].GetGenericTypeDefinition() != typeof( IGrouping<,> ) )
            {
                base.VisitBodyClauses( bodyClauses, queryModel );
                return;
            }

            var group = queryModel.ResultOperators.FirstOrDefault() as GroupResultOperator;
            if( group == null )
            {
                base.VisitBodyClauses( bodyClauses, queryModel );
                return;
            }

            
            var keySelector = (MemberExpression)group.KeySelector;
            var groupReql = Stack.Pop().Group( QueryHelper.GetJsonMemberName( keySelector.Member ) );

            var memberAccess = group.ElementSelector as MemberExpression;
            Stack.Push( memberAccess != null ? groupReql.GetField( QueryHelper.GetJsonMemberName( memberAccess.Member ) ).Ungroup() : groupReql.Ungroup() );
        }

        public override void VisitResultOperator( ResultOperatorBase resultOperator, QueryModel queryModel, int index )
        {
            if( resultOperator is AnyResultOperator )
                Stack.Push( Stack.Pop().Count() );
            else if( resultOperator is AllResultOperator )
            {
                var allResultOperator = resultOperator as AllResultOperator;
                Stack.Push( Stack.Pop().Filter( x => GetWhereReqlAst( x, allResultOperator.Predicate ).Not() ).Count() );
            }
            else if( resultOperator is FirstResultOperator )
                Stack.Push( Stack.Pop().Nth( 0 ) );
            else if( resultOperator is LastResultOperator )
                Stack.Push( Stack.Pop().Nth( -1 ) );

            base.VisitResultOperator( resultOperator, queryModel, index );
        }

        private static ReqlExpr GetWhereReqlAst( ReqlExpr reqlExpr, Expression predicate )
        {
            var visitor = new ExpressionVisitor( reqlExpr, typeof( bool ) );
            visitor.Visit( predicate );
            return visitor.Current;
        }
    }
}