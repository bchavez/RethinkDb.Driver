using System.Linq;
using NUnit.Framework;
using RethinkDb.Driver.Tests;
using RethinkDb.Driver.Tests.Utils;

namespace RethinkDb.Driver.Linq.Tests
{
    [TestFixture]
    public class LinqExamples : QueryTestFixture
    {
        public class Game
        {
            public int id { get; set; }
            public string Player { get; set; }
            public int Points { get; set; }
            public string Type { get; set; }
        }
        
        [Test]
        public void Test()
        {

            var games = new[]
                        {
                            new Game {id = 2, Player = "Bob", Points = 15, Type = "ranked"},
                            new Game {id = 5, Player = "Alice", Points = 7, Type = "free"},
                            new Game {id = 11, Player = "Bob", Points = 10, Type = "free"},
                            new Game {id = 12, Player = "Alice", Points = 2, Type = "free"},
                        };

            R.Db(DbName)
                .Table(TableName)
                .Insert(games)
                .RunResult(conn);

            // LINQ TO REQL
            var results = R.Db(DbName).Table<Game>(TableName, conn)
                .Where(g => g.Player == "Alice" && g.Points > 5)
                .ToList();

            results.Dump();

            /*
  {
    "id": 5,
    "Player": "Alice",
    "Points": 7,
    "Type": "free"
  }
            */
        }
    }
}
 