using System.Linq;
using FluentAssertions;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json.Serialization;
using NUnit.Framework;
using RethinkDb.Driver.Model;
using RethinkDb.Driver.Tests.Utils;

namespace RethinkDb.Driver.Tests.ReQL
{
    public class SlackIssues : QueryTestFixture
    {

        public class Person
        {
            public string FirstName { get; set; }
            public string LastName { get; set; }
        }

        [Test]
        public void should_be_able_to_use_getall_with_listofguid()
        {
            ClearTable(DbName, TableName);
            var items = new[]
                {
                    new Person {FirstName = "Brian", LastName = "Chavez"},
                    new Person {FirstName = "Susie", LastName = "Baker"},
                    new Person {FirstName = "Donald", LastName = "Trump"}
                };


            var inserts = R.Db(DbName).Table(TableName)
                .Insert(items)
                .RunWrite(conn);

            var guids = inserts.GeneratedKeys.ToList();

            var people = R.Db(DbName).Table(TableName)
                .GetAll(guids)
                .RunCursor<Person>(conn)
                .ToList();

            people.Should().HaveCount(3);

        }


        public class SomePoco
        {
            public JObject Data { get; set; }
        }

        [Test]
        public void jakes_serilization_of_jobject_issue()
        {
            ClearDefaultTable();

            var poco = new SomePoco
                {
                    Data = new JObject
                        {
                            ["blah"] = new JArray("one", "two", "three")
                        }
                };

            var result = R.Db(DbName).Table(TableName).Insert(poco).RunWrite(conn);
            result.AssertInserted(1);
        }


        [Test]
        public void olivers_datetimeoffset_issue()
        {
            ClearDefaultTable();

            var settings = new JsonSerializerSettings
                {
                    ContractResolver = new CamelCasePropertyNamesContractResolver(),
                    DateFormatHandling = DateFormatHandling.IsoDateFormat,
                    DateParseHandling = DateParseHandling.None,
                    DateTimeZoneHandling = DateTimeZoneHandling.Unspecified,
                    Converters = new JsonConverter[] {new StringEnumConverter()},
                    NullValueHandling = NullValueHandling.Ignore,
                };

            Net.Converter.Converters = settings.Converters.ToArray();

            Net.Converter.Serializer = JsonSerializer.Create(settings);


            var obj = new JObject
                {
                    ["time"] = "2016-08-04T00:00:00+00:00"
                };
            
            var insertResult = R.Db(DbName).Table(TableName).Insert(obj)
                .RunWrite(conn);

            insertResult.Dump();

            var key = insertResult.GeneratedKeys[0];

            var getResult = R.Db(DbName).Table(TableName).Get(key)
                .RunResult<JObject>(conn);

            var timeThing = getResult["time"];

            timeThing.Type.Should().Be(JTokenType.String);

            var putBack = R.Db(DbName).Table(TableName).Update(getResult)
                .RunWrite(conn);

            putBack.Dump();
        }
    }
}