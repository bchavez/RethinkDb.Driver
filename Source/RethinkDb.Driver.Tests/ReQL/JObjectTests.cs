using System;
using FluentAssertions;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using NUnit.Framework;
using RethinkDb.Driver.Net;
using RethinkDb.Driver.Tests.Utils;

namespace RethinkDb.Driver.Tests.ReQL
{
    [TestFixture]
    public class JObjectTests : QueryTestFixture
    {
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

        [SetUp]
        public void BeforeEachTest2()
        {
            Converter.InitializeDefault();
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
                ["TheBinary"] = new byte[] { 0, 2, 3, 255 },
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
            var result = table.Insert(state).RunWrite(conn);
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
        public void issue_39()
        {
            var dateTime = DateTime.Parse("2016-09-03T10:30:20Z");
            var obj = new JObject(new JProperty("timestamp", dateTime));

            var table = R.Db(DbName).Table(TableName);
            table.Delete().Run(conn);

            var result = table.Insert(obj).RunWrite(conn);
            var id = result.GeneratedKeys[0];

            var check = table.Get(id).RunAtom<JObject>(conn);
            check.Dump();

            var dt = (DateTime)check["timestamp"];
            dt.Should().Be(dateTime);
        }

        [Test]
        public void ensure_we_get_datetime_offset()
        {
            var json = @"{
                            ""name"":""hello kitty"",
                            ""dob"":""2016-09-03T10:30:20Z""
                          }";

            var settings = new JsonSerializerSettings
                {
                    DateParseHandling = DateParseHandling.DateTimeOffset
                };

            var fromJson = JsonConvert.DeserializeObject<JObject>(json, settings);

            var fromDb = R.Expr(fromJson).RunResult<JObject>(conn);
            var dateTimeValue = fromDb["dob"] as JValue;
            dateTimeValue.Type.Should().Be(JTokenType.Date);

            dateTimeValue.Value.Should().BeOfType<DateTime>();

            Converter.Serializer.DateParseHandling = DateParseHandling.DateTimeOffset;

            fromDb = R.Expr(fromJson).RunResult<JObject>(conn);

            var dateTimeOffsetValue = fromDb["dob"] as JValue;
            dateTimeOffsetValue.Type.Should().Be(JTokenType.Date);
            dateTimeOffsetValue.Value.Should().BeOfType<DateTimeOffset>();
        }

        [Test]
        public void should_not_see_any_pseudo_type_when_ser_deser_jtoken_by_default()
        {
            var vals = R.Expr(new
            {
                keya = R.Now(),
                keyb = "foo"
            }).values().RunResult<JArray>(conn);

            var raw = vals.ToString();
            raw.Dump();
            raw.Should().Contain("foo");
            raw.Should().NotContain(Converter.PseudoTypeKey);
        }

        [Test]
        public void should_be_able_to_get_raw_values_if_we_want_them()
        {
            var vals = R.Expr(new
            {
                keya = R.Now(),
                keyb = "foo"
            }).values().RunResult<JArray>(conn, new {time_format = "raw"});

            var raw = vals.ToString();
            raw.Dump();
            raw.Should().Contain("foo");
            raw.Should().Contain(Converter.PseudoTypeKey);
        }


        [Test]
        public void try_getting_an_object_that_doesnt_exist()
        {
            var result = table.Get(Guid.NewGuid()).RunAtom<JObject>(conn);
            result.Should().BeNull();
        }

        [Test]
        public void try_basic_datetime_deseralization()
        {
            var obj = new JObject
                {
                    ["Name"] = "Brian",
                    ["dob"] = DateTime.Parse("8/24/2016")
                };

            var fromDb = R.Expr(obj).RunResult<JObject>(conn);
            var dateTimeValue = fromDb["dob"] as JValue;
            dateTimeValue.Type.Should().Be(JTokenType.Date);
            dateTimeValue.Value.Should().BeOfType<DateTime>();
        }
        
    }

}