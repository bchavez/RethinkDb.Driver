using System.Collections.Generic;
using NUnit.Framework;
using RethinkDb.Driver.Ast;
using RethinkDb.Driver.Tests.ReQL;
using RethinkDb.Driver.Tests.Utils;

namespace RethinkDb.Driver.Tests
{
    [TestFixture]
    public class ASTSerializationTests : QueryTestFixture
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
        }

        [Test]
        public void avoid_shadowing_seralized_ast_lambda_expressions()
        {
            //Full Query / No Seralization
            var filter = R.Expr(R.Array(5, 4, 3)).Filter(doc => IsForbidden(doc).Not());

            var result = filter.Run(conn);
            ExtensionsForTesting.Dump(result);
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
            var result2 = filterWithRaw.Run(conn);
            ExtensionsForTesting.Dump(result2);
        }

        private ReqlExpr IsForbidden(ReqlExpr x)
        {
            return R.Expr(R.Array(1, 2, 3)).Contains(number => number.Eq(x));
        }
    }
}