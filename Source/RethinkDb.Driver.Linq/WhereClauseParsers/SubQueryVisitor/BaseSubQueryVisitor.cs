using System;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using Remotion.Linq;
using RethinkDb.Driver.Ast;

namespace RethinkDb.Driver.Linq.WhereClauseParsers.SubQueryVisitor
{
    public abstract class BaseSubQueryVisitor<T> : ISubQueryVisitor
    {
        protected static ReqlExpr GetWhereReqlAst( ReqlExpr reqlExpr, Expression predicate, QueryModel queryModel )
        {
            var visitor = new ExpressionVisitor( reqlExpr, GetResultType( queryModel ) );
            visitor.Visit( predicate );
            return visitor.Current;
        }

        protected static Type GetResultType( QueryModel queryModel )
        {
            if( !queryModel.ResultTypeOverride.GetTypeInfo().IsGenericType )
                return queryModel.ResultTypeOverride;
            return queryModel.ResultTypeOverride.GetTypeInfo().GenericTypeArguments[0];
        }

        public virtual bool CanVisit( QueryModel queryModel )
        {
            return queryModel.ResultOperators.Any( x => x is T );
        }

        public abstract ReqlExpr Visit( ReqlExpr reqlExpr, QueryModel queryModel );
    }
}