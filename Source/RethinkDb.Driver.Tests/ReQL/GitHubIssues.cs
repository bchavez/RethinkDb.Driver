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


        public class TheJObject
        {
            public string TheString { get; set; }
            public float TheFloat { get; set; }
            public double TheDouble { get; set; }
            public decimal TheDecimal { get; set; }
            public byte[] TheBinary { get; set; }
            public bool TheBoolean { get; set; }
            public DateTime TheDateTime { get; set; }
            public DateTimeOffset TheDateTimeOffset { get; set; }
            public Guid TheGuid { get; set; }
            public TimeSpan TheTimeSpan { get; set; }
            public int TheInt { get; set; }
            public long TheLong { get; set; }
        }
        [Test]
        public void issue_21_allow_JObject_inserts()
        {
            var table = R.Db(DbName).Table(TableName);
            table.Delete().Run(conn);

            var state = new JObject
                {
                    ["TheString"] = "issue 21",
                    ["TheFloat"] = 25.2f,
                    ["TheDouble"] = 25.3d,
                    ["TheDecimal"] = 25.4m,
                    ["TheBinary"] = new byte[] {0, 2, 3, 255},
                    ["TheBoolean"] = true,
                    ["TheDateTime"] = new DateTime(2011, 11, 1, 11, 11, 11, DateTimeKind.Local),
                    ["TheDateTimeOffset"] = new DateTimeOffset(2011, 11, 1, 11, 11, 11, 11, TimeSpan.FromHours(-8)).ToUniversalTime(),
                    ["TheGuid"] = Guid.Empty,
                    ["TheTimeSpan"] = TimeSpan.FromHours(3),
                    ["TheInt"] = 25,
                    ["TheLong"] = 82342342234,
                    ["NestedObject"] = new JObject
                        {
                            ["NestedString"] = "StringValue",
                            ["NestedDate"] = new DateTime(2011, 11, 1, 11, 11, 11, DateTimeKind.Local)
                        },
                    ["NestedArray"] = new JArray
                        {
                            {
                                new JObject
                                    {
                                        ["SongName"] = "Song 1",
                                        ["SongLength"] = "4:14"
                                    }
                            },
                            new JObject
                                    {
                                        ["SongName"] = "Song 2",
                                        ["SongLength"] = "3:10"
                                    }
                        }
                };
            
            Console.WriteLine(">>> INSERT");
            var result = table.Insert(state).RunResult(conn);
            var id = result.GeneratedKeys[0];
            result.Dump();

            var check = table.Get(id).RunAtom<TheJObject>(conn);
            check.Dump();

            check.TheString.Should().Be((string)state["TheString"]);
            check.TheFloat.Should().Be((float)state["TheFloat"]);
            check.TheDouble.Should().Be((double)state["TheDouble"]);
            check.TheDecimal.Should().Be((decimal)state["TheDecimal"]);
            check.TheBinary.Should().BeEquivalentTo((byte[])state["TheBinary"]);
            check.TheBoolean.Should().Be((bool)state["TheBoolean"]);
            check.TheDateTime.Should().Be((DateTime)state["TheDateTime"]);
            check.TheDateTimeOffset.Should().Be((DateTimeOffset)state["TheDateTimeOffset"]);
            check.TheGuid.Should().Be((Guid)state["TheGuid"]);
            check.TheTimeSpan.Should().Be((TimeSpan)state["TheTimeSpan"]);
            check.TheInt.Should().Be((int)state["TheInt"]);
            check.TheLong.Should().Be((long)state["TheLong"]);
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
        public void issue_39()
        {
            var dateTime = DateTime.Parse("2016-09-03T10:30:20Z");
            var obj = new JObject(new JProperty("timestamp", dateTime));

            var table = R.Db(DbName).Table(TableName);
            table.Delete().Run(conn);

            var result = table.Insert(obj).RunResult(conn);
            var id = result.GeneratedKeys[0];

            var check = table.Get(id).RunAtom<JObject>(conn);
            check.Dump();

            var dtProper = check["timestamp"].ToObject<DateTime>(Net.Converter.Serializer);
            dtProper.Should().Be(dateTime);

            //var dt = (DateTime)check["timestamp"];
            //dt.Should().Be(dateTime);
        }

    }

}
