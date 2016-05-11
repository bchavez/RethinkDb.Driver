using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using FluentAssertions;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using NUnit.Framework;
using RethinkDb.Driver.Model;
using RethinkDb.Driver.Tests.Utils;


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
            var result = table.Insert(new {foo = "bar"}).RunResult(conn);
            var id = result.GeneratedKeys[0];
            result.AssertInserted(1);

            Console.WriteLine(">>> UPDATE 1 / VALUE 1");
            var value = "VALUE1";
            result = table.Get(id).update(new {Target = value}).RunResult(conn);
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

            var result = table.Insert(jObject).RunResult(conn);
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

            Action action = () => R.DbCreate(DbName).RunResult(conn);

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
                .RunResult(conn);

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
                .RunResult(conn);

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
    }

}
