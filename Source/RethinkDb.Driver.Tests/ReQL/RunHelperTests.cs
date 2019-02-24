using System;
using System.Threading.Tasks;
using FluentAssertions;
using NUnit.Framework;
using RethinkDb.Driver.Tests.Utils;

namespace RethinkDb.Driver.Tests.ReQL
{
    [TestFixture]
    public class RunHelperTests : QueryTestFixture
    {
        [Test]
        public void check_atom_respone()
        {
            var result = R.Expr(true).RunAtom<bool>(conn);

            result.Should().BeTrue();
        }

        [Test]
        public void check_get_atom_response()
        {
            R.Db(DbName).Table(TableName)
                .Insert(new Foo {id = "check_helper", Baz = 33, Bar = 11})
                .Run(conn);

            var result = R.Db(DbName).Table(TableName).Get("check_helper")
                .RunAtom<Foo>(conn);

            result.id.Should().Be("check_helper");
            result.Baz.Should().Be(33);
            result.Bar.Should().Be(11);
        }

        [Test]
        public void cant_use_atom_helper_on_squence()
        {
            Action act = () =>
                {
                    var result = R.Range(1, 5).RunAtom<int[]>(conn);
                };

            act.ShouldThrow<ReqlDriverError>();
        }

        [Test]
        public async Task can_run_unsafe_query()
        {
            var r = await R.Add(1, 2).RunUnsafeAsync(conn);

            r.IsError.Should().BeFalse();

            r.Data[0].ToString().Should().Be("3");
        }
    }
}
