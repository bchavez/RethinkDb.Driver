using System;
using System.Linq;
using FluentAssertions;
using NUnit.Framework;
using RethinkDb.Driver.Linq.Attributes;
using RethinkDb.Driver.Model;
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

            [SecondaryIndex]
            public int Points { get; set; }
            public string Type { get; set; }
        }
        
        [Test]
        public void basic_example()
        {
            ClearDefaultTable();
            var games = new[]
                        {
                            new Game {id = 2, Player = "Bob", Points = 15, Type = "ranked"},
                            new Game {id = 5, Player = "Alice", Points = 7, Type = "free"},
                            new Game {id = 11, Player = "Bob", Points = 10, Type = "free"},
                            new Game {id = 12, Player = "Alice", Points = 2, Type = "free"},
                        };

            //Insert some games
            R.Db(DbName)
                .Table(TableName)
                .Insert(games)
                .RunWrite(conn)
                .AssertInserted(4);

            // Query games table via LINQ to ReQL
            var results = R.Db(DbName).Table<Game>(TableName, conn)
                .Where(g => g.Type == "free" && g.Points > 5)
                .OrderBy(g => g.Points)
                .ToList();

            results.Dump();
            results.Count.Should().Be(2);
/* OUTPUT:
[
  {
    "id": 5,
    "Player": "Alice",
    "Points": 7,
    "Type": "free"
  },
  {
    "id": 11,
    "Player": "Bob",
    "Points": 10,
    "Type": "free"
  }
]
*/
        }


        [Test]
        public void linq_can_query_by_index()
        {
            DropTable(DbName, TableName);
            CreateTable(DbName, TableName);
            var games = new[]
                        {
                            new Game {id = 2, Player = "Bob", Points = 15, Type = "ranked"},
                            new Game {id = 5, Player = "Alice", Points = 7, Type = "free"},
                            new Game {id = 11, Player = "Bob", Points = 10, Type = "free"},
                            new Game {id = 12, Player = "Alice", Points = 2, Type = "free"},
                        };

            R.Db(DbName).Table(TableName)
                .IndexCreate("Points")
                .Run(conn);

            R.Db(DbName).Table(TableName)
                .IndexWait()
                .Run(conn);

            //Insert some games
            R.Db(DbName).Table(TableName)
                .Insert(games)
                .RunWrite(conn)
                .AssertInserted(4);

            // Query games table via LINQ to ReQL
            var results = R.Db(DbName).Table<Game>(TableName, conn)
                .Where(g => g.Points == 10)
                .ToList();

            results.Dump();
            results.Count.Should().Be(1);
/* OUTPUT:
  

*/
        }
    }
}
 