using System.Collections.Generic;
using FluentAssertions;
using NUnit.Framework;
using RethinkDb.Driver.Tests.Utils;

namespace RethinkDb.Driver.Tests.ReQL
{
    [TestFixture]
    public class MapTests : QueryTestFixture
    {
        [Test]
        public void mapping_test()
        {
            var games = new[]
                {
                    new Game {id = 2, player = "Bob", points = 15, type = "ranked"},
                    new Game {id = 5, player = "Alice", points = 7, type = "free"},
                    new Game {id = 11, player = "Bob", points = 10, type = "free"},
                    new Game {id = 12, player = "Alice", points = 2, type = "free"},
                };

            List<TopPlayer> result =
                R.expr(games)
                    .filter(g => g["points"].gt(9))
                    .map(g => new { PlayerId = g["id"] })
                    .run<List<TopPlayer>>(conn);

            result.Dump();

            result.ShouldBeEquivalentTo(new[]
                {
                    new TopPlayer {PlayerId = 2},
                    new TopPlayer {PlayerId = 11}
                });
        }


        [Test]
        public void mapping_test_2()
        {
            List<int> result = R.expr(new[] { 1, 2, 3, 4, 5 })
                .map(v => v.mul(v))
                .run<List<int>>(conn);

            result.Dump();

            result.Should().Equal(1, 4, 9, 16, 25);
        }
    }

    public class TopPlayer
    {
        public int PlayerId { get; set; }
    }
}