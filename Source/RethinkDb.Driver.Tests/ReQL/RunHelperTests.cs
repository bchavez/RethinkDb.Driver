using System;
using FluentAssertions;
using NUnit.Framework;

namespace RethinkDb.Driver.Tests.ReQL
{
    [TestFixture]
    public class RunHelperTests : QueryTestFixture
    {
        [Test]
        public void check_atom_respone()
        {
            var result = r.expr(true).runAtom<bool>(conn);

            result.Should().BeTrue();
        }

        [Test]
        public void check_get_atom_response()
        {
            r.db(DbName).table(TableName)
                .insert(new Foo {id = "check_helper", Baz = 33, Bar = 11})
                .run(conn);

            var result = r.db(DbName).table(TableName).get("check_helper")
                .runAtom<Foo>(conn);

            result.id.Should().Be("check_helper");
            result.Baz.Should().Be(33);
            result.Bar.Should().Be(11);
        }

        [Test]
        public void cant_use_atom_helper_on_squence()
        {
            Action act = () =>
                {
                    var result = r.range(1, 5).runAtom<int[]>(conn);
                };

            act.ShouldThrow<ReqlDriverError>();
        }

       
    }
}