using System.Collections.Generic;
using Newtonsoft.Json.Linq;
using NUnit.Framework;
using RethinkDb.Driver.Tests.Utils;

namespace RethinkDb.Driver.Tests
{
    [TestFixture]
    [Explicit]
    public class ExperimentalTests : QueryTest
    {
        //* .map() projections with anonymous types. games.map( g => new {points = g["points"]} )

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

            List<JObject> result =
                r.expr(games)
                    .filter(g => g["points"].gt(9))
                    .map(g => new { dodad = g["id"] })
                    .run<List<JObject>>(conn);

            foreach (var item in result)
            {
                item.Dump();
            }
        }

        [Test]
        public void mapping_test_2()
        {
            List<int> result = r.expr(new[] { 1, 2, 3, 4, 5 })
                .map(v => v.mul(v))
                .run<List<int>>(conn);

            result.Dump();
        }
    }
}