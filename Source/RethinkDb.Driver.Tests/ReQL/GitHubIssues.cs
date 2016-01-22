using System;
using System.Collections.Generic;
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
            var table = r.db(DbName).table(TableName);
            table.delete().run(conn);

            var basket = new Basket {id = 99};

            table.insert(basket).run(conn);

            basket.Items = new List<string>
                {
                    "Apple",
                    "Orange",
                    "Kiwi"
                };

            basket.Revisions = new List<Revision>
                {
                    new Revision {Bytes = new byte[] {1, 2, 3}}
                };

            basket.ArrayOfInts = new[] {new[] {1, 2, 3}, new[] {4, 5, 6}};

            table.update(basket).run(conn);

            Basket fromDb = table.get(99).run<Basket>(conn);

            fromDb.Dump();

            fromDb.id.Should().Be(99);
            fromDb.Items.Should().Equal("Apple", "Orange", "Kiwi");
            fromDb.Revisions.ShouldBeEquivalentTo(basket.Revisions);
            fromDb.ArrayOfInts.ShouldBeEquivalentTo(new[] { new[] {1, 2, 3}, new[] {4, 5, 6}});
        }

        [Test]
        public void issue_20()
        {
            var table = r.db(DbName).table(TableName);
            table.delete().run(conn);

            Console.WriteLine(">>> INSERT");
            var result = table.insert(new {foo = "bar"}).runResult(conn);
            var id = result.GeneratedKeys[0];
            result.AssertInserted(1);

            Console.WriteLine(">>> UPDATE 1 / VALUE 1");
            var value = "VALUE1";
            result = table.get(id).update(new {Target = value}).runResult(conn);
            result.Dump();

            Console.WriteLine(">>> UPDATE 2 / VALUE 2");
            value = "VALUE2";
            var optResult = table.get(id).update(new {Target = value})
                .optArg("return_changes", true).run(conn);
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
            var table = r.db(DbName).table(TableName);
            table.delete().run(conn);

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
                                        ["SongName"] = "Dirty Deeds Done Dirt Cheap",
                                        ["SongLength"] = "4:14"
                                    }
                            },
                            new JObject
                                    {
                                        ["SongName"] = "Love at First Feel",
                                        ["SongLength"] = "3:10"
                                    }
                        }
                };
            
            Console.WriteLine(">>> INSERT");
            var result = table.insert(state).runResult(conn);
            var id = result.GeneratedKeys[0];
            result.Dump();

            var check = table.get(id).runAtom<TheJObject>(conn);
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

    }

}