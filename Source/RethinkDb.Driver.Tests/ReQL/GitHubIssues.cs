using NUnit.Framework;

namespace RethinkDb.Driver.Tests.ReQL
{
    [TestFixture]
    [Explicit]
    public class TestFixture : QueryTestFixture
    {
        [Test]
        public void issue_12()
        {

            r.db(DbName).table(TableName).delete().run(conn);

            r.db(DbName).table(TableName)
                .insert(new { Foo = "Bar", id = "foo" });
        }
    }

}