using System;
using System.Collections.Generic;
using FluentAssertions;
using NUnit.Framework;
using RethinkDb.Driver.Model;
using RethinkDb.Driver.Tests.Utils;

namespace RethinkDb.Driver.Tests.ReQL
{
    public class Game
    {
        public int id { get; set; }
        public string player { get; set; }
        public int points { get; set; }
        public string type { get; set; }
    }

    [TestFixture]
    public class GroupingTests : QueryTestFixture
    {
        [Test]
        public void can_read_grouping_reql_type()
        {
            var games = new[]
                {
                    new Game { id=2, player = "Bob", points = 15, type = "ranked"},
                    new Game { id=5, player = "Alice", points = 7, type = "free"},
                    new Game { id=11, player = "Bob", points = 10, type = "free"},
                    new Game { id=12, player = "Alice", points = 2, type = "free"},
                };

            IEnumerable<GroupedResult<string,Game>> result = 
                R.Expr(games).Group("player")
                .Run<GroupedResult<string, Game>>(conn);

            var groups = 0;
            foreach( var group in result )
            {
                Console.WriteLine($">>>> KEY:{group.Key}");
                group.Dump();
                groups++;

                if( group.Key == "Bob" )
                {
                    group.Items.ShouldBeEquivalentTo(new[] {games[0], games[2]});
                }
                else
                {
                    group.Items.ShouldBeEquivalentTo(new[] {games[1], games[3]});
                }
            }
            groups.Should().Be(2);
        }

        [Test]
        public void can_group_with_helper()
        {
            var games = new[]
                {
                    new Game { id=2, player = "Bob", points = 15, type = "ranked"},
                    new Game { id=5, player = "Alice", points = 7, type = "free"},
                    new Game { id=11, player = "Bob", points = 10, type = "free"},
                    new Game { id=12, player = "Alice", points = 2, type = "free"},
                };

            var result = R.Expr(games).Group("player")
                .RunGrouping<string, Game>(conn);

            var groups = 0;
            foreach (var group in result)
            {
                Console.WriteLine($">>>> KEY:{group.Key}");
                group.Dump();

                if (group.Key == "Bob")
                {
                    group.Items.ShouldBeEquivalentTo(new[] { games[0], games[2] });
                }
                else
                {
                    group.Items.ShouldBeEquivalentTo(new[] { games[1], games[3] });
                }
                groups++;
            }
            groups.Should().Be(2);
        }

        [Test]
        public async void can_group_with_async_helper()
        {
            var games = new[]
                {
                    new Game { id=2, player = "Bob", points = 15, type = "ranked"},
                    new Game { id=5, player = "Alice", points = 7, type = "free"},
                    new Game { id=11, player = "Bob", points = 10, type = "free"},
                    new Game { id=12, player = "Alice", points = 2, type = "free"},
                };

            var result = await R.Expr(games).Group("player")
                .RunGroupingAsync<string, Game>(conn);

            var groups = 0;
            foreach (var group in result)
            {
                Console.WriteLine($">>>> KEY:{group.Key}");
                group.Dump();
                if (group.Key == "Bob")
                {
                    group.Items.ShouldBeEquivalentTo(new[] { games[0], games[2] });
                }
                else
                {
                    group.Items.ShouldBeEquivalentTo(new[] { games[1], games[3] });
                }
                groups++;
            }
            groups.Should().Be(2);
        }
    }
}