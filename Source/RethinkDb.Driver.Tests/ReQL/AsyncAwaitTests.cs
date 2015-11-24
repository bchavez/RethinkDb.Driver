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
            bool b = await r.expr(true).runAsync<bool>(conn);

            b.Should().Be(true);
        }

        [Test]
        public async void async_insert()
        {
            ClearDefaultTable();

            var games = new[]
                {
                    new Game {id = 2, player = "Bob", points = 15, type = "ranked"},
                    new Game {id = 5, player = "Alice", points = 7, type = "free"},
                    new Game {id = 11, player = "Bob", points = 10, type = "free"},
                    new Game {id = 12, player = "Alice", points = 2, type = "free"},
                };

            var result = await r.db(DbName).table(TableName)
                .insert(games).runResultAsync(conn);

            result.AssertInserted(4);
        }
    }


    [TestFixture]
    public class ChangeFeedTests : QueryTestFixture
    {  
        [Test]
        public void Test()
        {
            
        }
    }

}