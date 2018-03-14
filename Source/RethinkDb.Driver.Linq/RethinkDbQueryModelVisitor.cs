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
using RethinkDb.Driver.Linq.WhereClauseParsers;
using ExpressionVisitor = RethinkDb.Driver.Linq.WhereClauseParsers.ExpressionVisitor;
using RethinkDb.Driver.Utils;

namespace RethinkDb.Driver.Linq
{
    public class RethinkDbQueryModelVisitor : QueryModelVisitorBase
    {
        private readonly Table _table;
        
        public ReqlAst Query => Stack.Peek();
        public Stack<ReqlExpr> Stack = new Stack<ReqlExpr>();

        public RethinkDbQueryModelVisitor( Table table )
        {
            _table = table;
        }

        public override void VisitQueryModel( QueryModel queryModel )
        {
            Stack.Push( _table );
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

            foreach( var expression in expressions.ToList() )
            {
                var whereClauseParsers = new List<IWhereClauseParser>
                {
                    new GroupItemsParser(),
                    new PrimaryIndexParser(),
                    new SecondaryIndexParser(),
                    new DefaultParser()
                };

                var reql = Stack.Pop();
                var matchingParser = whereClauseParsers.FirstOrDefault( x => x.IsAppropriate( reql, expression, queryModel.ResultTypeOverride ) );
                if( matchingParser != null )
                    Stack.Push( matchingParser.Parse( reql, queryModel, expression ) );
                else
                    throw new NotSupportedException( "Unable to vist Where clause" );
            }
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

        private static ReqlExpr GetSelectReqlAst( Expression selector )
        {
            var visitor = new SelectionProjector();
            visitor.Visit( selector );
            return visitor.Current;
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
            if( queryModel.ResultOperators.FirstOrDefault() is AverageResultOperator )
            {
                var memberExpression = selectClause.Selector as MemberExpression;
                var memberNameResolver = new MemberNameResolver( memberExpression );
                Stack.Push( Stack.Pop().Avg( x => memberNameResolver.Resolve( x ) ) );
                return;
            }

            if( queryModel.ResultOperators.FirstOrDefault() is CountResultOperator )
            {
                Stack.Push( Stack.Pop().Count( ) );
                return;
            }

            var expr = GetSelectReqlAst( selectClause.Selector );
            if( !ReferenceEquals( expr, null ) )
            {
                
            }

            base.VisitSelectClause( selectClause, queryModel );
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