using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using FakeItEasy;
using FluentAssertions;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using NUnit.Framework;
using RethinkDb.Driver.Ast;
using RethinkDb.Driver.Model;
using RethinkDb.Driver.Net;
using RethinkDb.Driver.Tests.Utils;
using RethinkDb.Driver.Utils;


namespace RethinkDb.Driver.Tests.ReQL
{
    public class Basket
    {
        public int id { get; set; }
        public List<string> Items { get; set; }
        public List<Revision> Revisions { get; set; }
        public int[][] ArrayOfInts { get; set; }
    }

    public class Revision
    {
        public byte[] Bytes { get; set; }
    }

    [TestFixture]
    public class GitHubIssues : QueryTestFixture
    {
        [Test]
        public void issue_12()
        {
            var table = R.Db(DbName).Table(TableName);
            table.Delete().Run(conn);

            var basket = new Basket {id = 99};

            table.Insert(basket).Run(conn);

            basket.Items = new List<string>
                {
                    "Apple",
                    "Orange",
                    "Kiwi"
                };

            basket.Revisions = new List<Revision>
                {
                    new Revision {Bytes = new byte[] {1, 2, 3, 255}}
                };

            basket.ArrayOfInts = new[] {new[] {1, 2, 3}, new[] {4, 5, 6}};

            table.update(basket).Run(conn);

            Basket fromDb = table.Get(99).Run<Basket>(conn);

            fromDb.Dump();

            fromDb.id.Should().Be(99);
            fromDb.Items.Should().Equal("Apple", "Orange", "Kiwi");
            fromDb.Revisions.ShouldBeEquivalentTo(basket.Revisions);
            fromDb.ArrayOfInts.ShouldBeEquivalentTo(new[] { new[] {1, 2, 3}, new[] {4, 5, 6}});
        }

        [Test]
        public void issue_20()
        {
            var table = R.Db(DbName).Table(TableName);
            table.Delete().Run(conn);

            Console.WriteLine(">>> INSERT");
            var result = table.Insert(new {foo = "bar"}).RunWrite(conn);
            var id = result.GeneratedKeys[0];
            result.AssertInserted(1);

            Console.WriteLine(">>> UPDATE 1 / VALUE 1");
            var value = "VALUE1";
            result = table.Get(id).update(new {Target = value}).RunWrite(conn);
            result.Dump();

            Console.WriteLine(">>> UPDATE 2 / VALUE 2");
            value = "VALUE2";
            var optResult = table.Get(id).update(new {Target = value})
                .optArg("return_changes", true).Run(conn);
            ExtensionsForTesting.Dump(optResult);
        }


     

        [Test]
        public void issue_21_raw_json_test()
        {
            var json = @"
            {
                ""Entered"": ""2012 - 08 - 18T13: 26:37.7137482 - 10:00"",
                ""AlbumName"": ""Dirty Deeds Done Dirt Cheap"",
                ""Artist"": ""AC/DC"",
                ""YearReleased"": 1976,
                ""Songs"": [
                {
                    ""SongName"": ""Dirty Deeds Done Dirt Cheap"",
                    ""SongLength"": ""4:11""
                },
                {
                    ""SongName"": ""Love at First Feel"",
                    ""SongLength"": ""3:10""
                }
                ]
            }";

            var jObject = JObject.Parse(json);

            var table = R.Db(DbName).Table(TableName);
            table.Delete().Run(conn);

            var result = table.Insert(jObject).RunWrite(conn);
            var id = result.GeneratedKeys[0];
            result.Dump();

            var check = table.Get(id).RunAtom<JObject>(conn);
            check.Dump();
        }

        [Test]
        [Explicit]
        public void issue_24()
        {
            Parallel.For(1, 4, (i) =>
                {
                    while( true )
                    {
                        Console.WriteLine("START");
                        var _r = RethinkDB.R;
                        var conn = _r.Connection()
                            .Hostname("192.168.0.11")
                            .Port(RethinkDBConstants.DefaultPort)
                            .Timeout(60)
                            .Connect();
                        var x = _r.Db(DbName)
                            .Table(TableName)
                            .Count();
                        Console.WriteLine(">>>>>");
                        long resCount = x.Run(conn);
                        Console.WriteLine("<<<<<");
                        Console.WriteLine(" - C: " + resCount);
                        conn.Close();
                        conn = null;
                        _r = null;
                        Console.WriteLine("FINISH");
                        Console.WriteLine();
                        Console.WriteLine();
                    }
                });
        }

        [Test]
        public void issue_41_ensure_run_helpers_throw_error_first_before_direct_conversion()
        {
            try
            {
                CreateDb(DbName);
            }
            catch
            {
                // ignored
            }

            Action action = () => R.DbCreate(DbName).RunWrite(conn);

            action.ShouldThrow<ReqlRuntimeError>();
        }


        public class Issue49
        {
            [JsonProperty("id")]
            public int Id = 1;
            public DateTime BigBang { get; set; }
        }

        [Test]
        public void issue_49_use_reqldatetimeconverter_for_dates_in_ast()
        {
            ClearDefaultTable();

            var mindate = DateTime.MinValue.ToUniversalTime();

            var insertResult = R
                .Db(DbName)
                .Table(TableName)
                .Insert(new Issue49 {BigBang = mindate})
                .RunWrite(conn);

            insertResult.Errors.Should().Be(0);

            var updateResult = R
                .Db(DbName)
                .Table(TableName)
                .Get(1)
                .Update(orig =>
                        {
                            var unchanged = orig["BigBang"].Eq(mindate);
                            return R.Error(unchanged.CoerceTo("string"));
                        })
                .OptArg("return_changes", true)
                .RunWrite(conn);

            updateResult.Errors.Should().Be(1);
            updateResult.FirstError.Should().Be("true");

            var filter =
                R.Db(DbName)
                 .Table(TableName)
                 .Filter(x => x["BigBang"].Eq(mindate))
                 .RunResult<List<Issue49>>(conn);

            filter.Count.Should().Be(1);
            filter[0].BigBang.Should().Be(mindate);
        }

        [Test]
        public void issue_78_make_sure_jtoken_is_a_jtoken_too()
        {
            typeof(JToken).IsJToken().Should().Be(true);
            typeof(JObject).IsJToken().Should().Be(true);
            typeof(JArray).IsJToken().Should().Be(true);
            typeof(JValue).IsJToken().Should().Be(true);
        }


        [Test]
        public void issue_86_grouping_count_transforms_reduction()
        {
            var games = new[]
                {
                    new Game {id = 2, player = "Bob", points = 15, type = "ranked"},
                    new Game {id = 5, player = "Alice", points = 7, type = "free"},
                    new Game {id = 12, player = "Alice", points = 2, type = "free"},
                };

            var result = R.Expr(games)
                .Group("player")
                .Count()
                .RunGrouping<string, int>(conn);

            var groupings = result.ToArray();
            groupings[0].Items[0].Should().Be(2);
            groupings[1].Items[0].Should().Be(1);
        }


        [Test]
        public void issue_86_group_using_index()
        {
            var issues = @"[{ 
""Expires"": """", 
""Issuer"": ""d04aad77448a"" , 
""id"": ""0276701a-3834-4375-b0fa-5be0a691db39"" 
},
{ 
""Expires"": """", 
""Issuer"": ""f6adf92273de"" , 
""id"": ""033325bf-c942-4fdf-af4c-0bb79f2fae3a"" 
},
{ 
""Expires"": """", 
""Issuer"": ""cf3d85de52b4"" , 
""id"": ""038c0583-f92e-45c1-a885-1650daaf11e1"" 
}]";

            DropTable(DbName, TableName);
            CreateTable(DbName, TableName);

            var insertResult = table.Insert(R.Json(issues)).RunWrite(conn);

            insertResult.AssertInserted(3);

            table.IndexCreate("Issuer").Run(conn);

            table.IndexWait("Issuer").Run(conn);

            //query using index

            var groupResults = table.Group()[new { index = "Issuer" }]
                .Count()
                .RunGrouping<string, int>(conn);

            var results = groupResults.ToArray();

            results.Any(g => g.Key == "d04aad77448a").Should().BeTrue();
            results.Any(g => g.Key == "f6adf92273de").Should().BeTrue();
            results.Any(g => g.Key == "cf3d85de52b4").Should().BeTrue();

            results[0].Items[0].Should().Be(1);
            results[1].Items[0].Should().Be(1);
            results[2].Items[0].Should().Be(1);
        }

        [Test]
        public async Task can_test_using_mock()
        {
            var expectedQuery = R.Db(DbName).Table(TableName).ToRawString();

            var testQuery = R.Db(DbName).Table(TableName);

            var conn = A.Fake<IConnection>();

            await testQuery.RunAtomAsync<Result>(conn);

            A.CallTo(() =>
                    conn.RunAtomAsync<Result>(
                        A<ReqlAst>.That.Matches(test =>
                            ReqlRaw.ToRawString(test) == expectedQuery),
                        A<object>._,
                        A<CancellationToken>._))
                .MustHaveHappened();

        }
    }

}
