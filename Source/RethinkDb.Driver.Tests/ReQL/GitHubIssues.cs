using System.Collections.Generic;
using FluentAssertions;
using NUnit.Framework;
using RethinkDb.Driver.Tests.Utils;

namespace RethinkDb.Driver.Tests.ReQL
{
    public class Basket
    {
        public int id { get; set; }
        public List<string> Items { get; set; }
    }

    [TestFixture]
    [Explicit]
    public class GitHubIssues : QueryTestFixture
    {
        [Test]
        public void issue_12()
        {
            var table = r.db(DbName).table(TableName);
            table.delete().run(conn);

            var basket = new Basket {id = 99};

            table.insert(basket).run(conn);

            basket.Items = new List<string>
                {
                    "Apple",
                    "Orange",
                    "Kiwi"
                };

            table.update(basket).run(conn);

            Basket fromDb = table.get(99).run<Basket>(conn);

            fromDb.Dump();

            fromDb.id.Should().Be(99);
            fromDb.Items.Should().Equal("Apple", "Orange", "Kiwi");
        }
    }

}