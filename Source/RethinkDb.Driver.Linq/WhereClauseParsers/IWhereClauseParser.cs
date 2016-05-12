using System;
using System.Linq.Expressions;
using Remotion.Linq;
using RethinkDb.Driver.Ast;

namespace RethinkDb.Driver.Linq.WhereClauseParsers
{
    public interface IWhereClauseParser
    {
        bool IsAppropriate( ReqlAst reql, Expression expression, Type resultType );
        ReqlExpr Parse( ReqlExpr expression, QueryModel queryModel, Expression predicate );
    }
}