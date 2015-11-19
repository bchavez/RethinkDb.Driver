using FluentAssertions;
using NUnit.Framework;
using RethinkDb.Driver.Tests.Utils;

namespace RethinkDb.Driver.Tests.ReQL
{
    [TestFixture]
    [Explicit]
    public class GitHubIssues : QueryTestFixture
    {
        [Test]
        public void issue_12()
        {
            var table = r.db(DbName).table(TableName);
            table.delete().run(conn);

            var game = new Game {id = 99, player = "cowboy", points = 5, type = "open"};

            table.insert(game).run(conn);

            game.type = "close";

            table.update(game).run(conn);

            Game fromDb = table.get(99).run<Game>(conn);

            fromDb.Dump();

            fromDb.id.Should().Be(99);
            fromDb.type.Should().Be("close");
        }
    }

}