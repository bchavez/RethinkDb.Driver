using System;
using System.Dynamic;
using System.Linq;
using FluentAssertions;
using Newtonsoft.Json.Linq;
using NUnit.Framework;
using RethinkDb.Driver.Ast;
using RethinkDb.Driver.Model;
using RethinkDb.Driver.Tests.Utils;

namespace RethinkDb.Driver.Tests.ReQL
{
    public class GitterQuestions : QueryTestFixture
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
                .RunResult(conn);

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

            var result = R.Db(DbName).Table(TableName).Insert(poco).RunResult(conn);
            result.AssertInserted(1);
        }

        [Test]
        public void olivers_serilization_of_jobject_issue()
        {
            ClearDefaultTable();

            var obj = new JObject
            {
                ["time"] = "2016-08-04T00:00:00+10:00"
            };

            var insertResult = R.Db(DbName).Table(TableName).Insert(obj)
                .RunResult(conn);

            insertResult.Dump();

            var key = insertResult.GeneratedKeys[0];

            var getResult = R.Db(DbName).Table(TableName).Get(key)
                .RunResult<JObject>(conn);

            var timeThing = getResult["time"];

            timeThing.Type.Should().Be(JTokenType.String);

            var putBack = R.Db(DbName).Table(TableName).Update(getResult)
                .RunResult(conn);

            putBack.Dump();
        }
    }
}