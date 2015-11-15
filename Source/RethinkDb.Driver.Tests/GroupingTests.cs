using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using RethinkDb.Driver.Model;
using RethinkDb.Driver.Tests.Utils;

namespace RethinkDb.Driver.Tests
{
    public class Game
    {
        public int id { get; set; }
        public string player { get; set; }
        public int points { get; set; }
        public string type { get; set; }
    }

    [TestFixture]
    public class GroupingTests : QueryTest
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

            IEnumerable<GroupedResult<string,Game>> result = r.expr(games).group("player").run<GroupedResult<string, Game>>(conn);

            foreach( var group in result )
            {
                Console.WriteLine($">>>> KEY:{group.Key}");
                group.Dump();
            }
        }
    }

}