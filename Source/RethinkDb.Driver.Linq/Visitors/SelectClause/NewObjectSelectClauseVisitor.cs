using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using Remotion.Linq;
using Remotion.Linq.Clauses.Expressions;
using RethinkDb.Driver.Ast;
using RethinkDb.Driver.Model;
using RethinkDb.Driver.Utils;

namespace RethinkDb.Driver.Linq.Visitors.SelectClause
{
    internal class NewObjectSelectClauseVisitor : ISelectClauseVisitor
    {
        public bool IsAppropriate( Remotion.Linq.Clauses.SelectClause selectClause, QueryModel queryModel )
        {
            return selectClause.Selector is MemberInitExpression || selectClause.Selector is NewExpression;
        }

        private class Binding
        {
            public Binding( MemberInfo member, Expression expression )
            {
                Member = member;
                Expression = expression;
            }

            public MemberInfo Member { get; }
            public Expression Expression { get; }
        }

        private static List<Binding> GetBindings( Remotion.Linq.Clauses.SelectClause selectClause )
        {
            switch ( selectClause.Selector )
            {
                case MemberInitExpression memberInitExpression:
                    return memberInitExpression.Bindings
                        .OfType<MemberAssignment>()
                        .Select( x => new Binding( x.Member, x.Expression ) )
                        .ToList();
                case NewExpression newExpression:
                    return newExpression.Members
                        .Select( x => new Binding( x, newExpression.Arguments[newExpression.Members.IndexOf( x )] ) )
                        .ToList();
                default:
                    throw new NotImplementedException();
            }
        }

        public void Visit( Remotion.Linq.Clauses.SelectClause selectClause, QueryModel queryModel,
            Stack<ReqlExpr> stack )
        {
            var bindings = GetBindings( selectClause );

            var reql = stack.Pop();

            var map = reql.Map( mapReqlExpr =>
            {
                var mapObject = default(MapObject);
                foreach ( var sourceBinding in bindings )
                {
                    var member = sourceBinding.Member;
                    var expression = sourceBinding.Expression;

                    var destinationMemberName = QueryHelper.GetJsonMemberName( member );

                    var resultExpr = default(ReqlAst);

                    switch ( expression )
                    {
                        case MemberExpression sourceMemberExpression:
                        {
                            resultExpr = ParseMemberExpression( sourceMemberExpression, mapReqlExpr );
                            break;
                        }
                        case SubQueryExpression subQueryExpression:
                        {
                            resultExpr = ParseSubQuery( mapReqlExpr, subQueryExpression.QueryModel );
                            break;
                        }
                        case MethodCallExpression methodCallExpression:
                        {
                            if ( methodCallExpression.Method.Name == "ToList" )
                            {
                                var subQuery = (SubQueryExpression) methodCallExpression.Arguments[0];
                                resultExpr = ParseSubQuery(mapReqlExpr, subQuery.QueryModel);
                            }
                            else
                            {
                                throw new NotImplementedException();
                            }

                            break;
                        }
                    }

                    if ( mapObject == null )
                        mapObject = RethinkDB.R.HashMap( destinationMemberName, resultExpr );
                    else
                        mapObject = mapObject.With( destinationMemberName, resultExpr );
                }

                return mapObject;
            } );

            stack.Push( map );
        }

        private static ReqlAst ParseSubQuery( ReqlExpr reqlExpr, QueryModel model )
        {
            var queryVisitor = new RethinkDbQueryModelVisitor( reqlExpr["reduction"] );
            queryVisitor.VisitQueryModel( model );
            return queryVisitor.Query;
        }

        private static ReqlAst ParseMemberExpression( MemberExpression sourceMemberExpression, ReqlExpr reqlExpr )
        {
            var sourceMemberName = QueryHelper.GetJsonMemberName( sourceMemberExpression.Member );
            var sourceClassType = sourceMemberExpression.Expression.Type;
            if ( sourceClassType.IsGenericType
                 && sourceClassType.GetGenericTypeDefinition() == typeof(IGrouping<,>)
                 && sourceMemberExpression.Member.Name == "Key" )
            {
                sourceMemberName = "group";
            }

            return reqlExpr[sourceMemberName];
        }
    }
}