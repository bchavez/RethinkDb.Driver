using System.Collections.Generic;
using FluentAssertions;
using NUnit.Framework;
using RethinkDb.Driver.Ast;
using RethinkDb.Driver.Tests.ReQL;
using RethinkDb.Driver.Tests.Utils;

namespace RethinkDb.Driver.Tests
{
    [TestFixture]
    public class ReqlRawTests : QueryTestFixture
    {
        /// <summary>
        /// Example shows how to serialize a ReqlFunction expression
        /// </summary>
        [Test]
        public void can_stich_together_some_crazy_reql_expr_thing()
        {
            ClearDefaultTable();

            var foos = new[]
                {
                    new Foo {id = "a", Baz = 1, Bar = 1, Idx = "qux"},
                    new Foo {id = "b", Baz = 2, Bar = 2, Idx = "bub"},
                    new Foo {id = "c", Baz = 3, Bar = 3, Idx = "qux"}
                };

            R.Db(DbName).Table(TableName).Insert(foos).Run(conn);

            ReqlFunction1 filter = expr => expr["Bar"].Gt(2);

            var str = ReqlRaw.ToRawString(filter);

            str.Dump();

            var filterTerm = ReqlRaw.FromRawString(str);

            var result = table.Filter(filterTerm).RunResult<List<Foo>>(conn);

            result.Dump();

            result[0].id.Should().Be(foos[2].id);
            result[0].Baz.Should().Be(3);
            result.Count.Should().Be(1);
        }

        [Test]
        public void avoid_shadowing_seralized_ast_lambda_expressions()
        {
            //Full Query / No Seralization
            var filter = R.Expr(R.Array(5, 4, 3)).Filter(doc => IsForbidden(doc).Not());

            var result = filter.RunResult<List<int>>(conn);
            result.Dump();
            result.Should().BeEquivalentTo(5, 4);
            //RESULT:
            //[5,4]

            //This is unbound?
            ReqlFunction1 func = expr => IsForbidden(expr).Not();
            var str = ReqlRaw.ToRawString(func);
            str.Dump();
            var rawFilter = ReqlRaw.FromRawString(str);
            var filterWithRaw = R.Expr(R.Array(5, 4, 3)).Filter(rawFilter);
            //Not Allowed in C#
            //var filterWithRaw = R.Expr(R.Array(5, 4, 3)).Filter( x => rawFilter.SomethingElse );
            var result2 = filterWithRaw.RunResult<List<int>>(conn);
            result2.Should().BeEquivalentTo(5, 4);
            //ExtensionsForTesting.Dump(result2);
        }


        [Test]
        public void can_seralize_the_while_ast_term()
        {
            var query = R.Expr(R.Array(5,4,3)).Filter(n => IsForbidden(n).Not());
            var result = query.RunResult<List<int>>(conn);
            //result [5,4]
            result.Dump();
            result.Should().BeEquivalentTo(5, 4);
            //ExtensionsForTesting.Dump(result);


            var query2 = R.Expr(R.Array(5, 4, 3)).Filter(n => IsForbidden(n).Not());
            var queryStr = query2.ToRawString();

            queryStr.Dump();

            var queryAst = R
                .FromRawString(queryStr)
                .Filter(x => x.Eq(5));

            var result2 = queryAst.RunResult<List<int>>(conn);
            //result [5]
            result2.Dump();
            result2.Should().BeEquivalentTo(5);
        }

        private ReqlExpr IsForbidden(ReqlExpr x)
        {
            return R.Expr(R.Array(1, 2, 3)).Contains(number => number.Eq(x));
        }
    }
}