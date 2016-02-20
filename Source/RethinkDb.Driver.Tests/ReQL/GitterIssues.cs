using System;
using System.Linq;
using FluentAssertions;
using NUnit.Framework;
using RethinkDb.Driver.Ast;

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
    }
}