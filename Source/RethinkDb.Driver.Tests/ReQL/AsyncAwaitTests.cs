using FluentAssertions;
using NUnit.Framework;
using RethinkDb.Driver.Model;

namespace RethinkDb.Driver.Tests.ReQL
{
    [TestFixture]
    public class AsyncAwaitTests : QueryTestFixture
    {
        [Test]
        public async void basic_test()
        {
            bool b = await R.Expr(true).runAsync<bool>(conn);

            b.Should().Be(true);
        }

        [Test]
        public async void async_insert()
        {
            //ClearDefaultTable();
            R.Db(DbName).Table(TableName).Delete().run(conn);

            var games = new[]
                {
                    new Game {id = 2, player = "Bob", points = 15, type = "ranked"},
                    new Game {id = 5, player = "Alice", points = 7, type = "free"},
                    new Game {id = 11, player = "Bob", points = 10, type = "free"},
                    new Game {id = 12, player = "Alice", points = 2, type = "free"},
                };

            var result = await R.Db(DbName).Table(TableName)
                                .Insert(games)
                                .runResultAsync(conn);

            result.AssertInserted(4);
        }
    }
}