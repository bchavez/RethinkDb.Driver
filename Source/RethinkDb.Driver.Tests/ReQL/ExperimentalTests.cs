using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using FluentAssertions;
using NUnit.Framework;
using RethinkDb.Driver.Ast;
using RethinkDb.Driver.Model;
using RethinkDb.Driver.Tests.Utils;

namespace RethinkDb.Driver.Tests.ReQL
{
    [TestFixture]
    [Explicit]
    public class ExperimentalTests : QueryTestFixture
    {
        [Test]
        public void Test()
        {
            var games = new[]
                {
                    new Game {id = 2, player = "Bob", points = 15, type = "ranked"},
                    new Game {id = 5, player = "Alice", points = 7, type = "free"},
                    new Game {id = 11, player = "Bob", points = 10, type = "free"},
                    new Game {id = 12, player = "Alice", points = 2, type = "free"},
                };

            var result =
                R.expr(games)
                    //.filter(g => g["points"] == 10)
                    .filter<Game>(g => g.points > 9)
                    .map(g => new {PlayerId = g["id"]})
                    .runAtom<List<TopPlayer>>(conn);

            result.ShouldBeEquivalentTo(new[]
    {
                    new TopPlayer {PlayerId = 2},
                    new TopPlayer {PlayerId = 11}
                });

        }

        [Test]
        public void expression_tree_test()
        {
            //var e = GetReqlExpr<Game>(g => g.points > 9 && g.points < 12);
            var e = GetReqlExpr<Game>(g => g.points > 9);
        }

        private Expression GetReqlExpr<T>(Expression<Func<T, object>> expr)
        {
            //var v = new ExprVisit();
            var v = new MemberExprReplacer();
            return v.Visit(expr);
        }
    }
    public class ExprVisit : ExpressionVisitor
    {
        public ExprVisit()
        {
            
        }

        private ReqlExpr expr;

        protected override Expression VisitBinary(BinaryExpression node)
        {
            switch( node.NodeType )
            {
                case ExpressionType.GreaterThan:

                    break;
            }

            return base.VisitBinary(node);
        }

        protected override Expression VisitConstant(ConstantExpression node)
        {
            return base.VisitConstant(node);
        }

        protected override Expression VisitMember(MemberExpression node)
        {
            return base.VisitMember(node);
        }
    }

    public static class TestExternsion
    {
        public static ReqlExpr filter<T>(this ReqlExpr _this, Expression<Func<T, object>> expr)
        {
            var arguments = new Arguments(_this);
            arguments.CoerceAndAdd(expr);
            return new Filter(arguments);
        }
    }

    public class MemberExprReplacer : ExpressionVisitor
    {
        private Expression<Func<Var,string, ReqlExpr>> bracket = (var,s) => var[s];
        private Expression<Func<object, ReqlAst>> util = (o) => Util.ToReqlAst(o);

        private Expression<Func<Var, ReqlAst>> op = (o) => o["foo"] > o["joo"];


        public override Expression Visit(Expression node)
        {
            if( node.NodeType == ExpressionType.MemberAccess )
            {
                var mem = node as MemberExpression;
                var varname = mem.Expression as ParameterExpression;
                var name = mem.Member.Name;
                var varparam = Expression.Parameter(typeof(Var), varname.Name);
                var constant = Expression.Constant(name);

                var body = bracket.Body as MethodCallExpression;

                var body2= Expression.Call(varparam, body.Method, constant);

                return body2;
            }
            if( node.NodeType == ExpressionType.Lambda )
            {
                var l = node as LambdaExpression;
                var x = nameof(Game.points);
                var newParams = l.Parameters.Select(r => Expression.Parameter(typeof(Var), r.Name));

                var tmp= Expression.Lambda(base.Visit(l.Body), newParams);
                return tmp;
            }
            if( node.NodeType == ExpressionType.GreaterThan )
            {
                var b = node as BinaryExpression;

                var temp4 = Expression.GreaterThan(base.Visit(b.Right), base.Visit(b.Left), b.IsLiftedToNull, (op.Body as BinaryExpression).Method);
                return temp4;
            }

            var tmp2 =  base.Visit(node);
            return tmp2;
        }
    }
}