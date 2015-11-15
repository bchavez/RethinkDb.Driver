using System;
using System.Collections.Generic;
using FluentAssertions;
using NUnit.Framework;
using RethinkDb.Driver.Net;
using RethinkDb.Driver.Tests.Utils;

namespace RethinkDb.Driver.Tests
{
    [TestFixture]
    public class GitHubIssues : QueryTest
    {
        [Test]
        public void issue_10()
        {
            r.db(DbName).table(TableName)
                .delete().run(conn);

            var insert = r.db(DbName).table(TableName)
                .insert(new User
                    {
                        Birthday = new DateTime(1990, 8, 18, 0, 0, 0, DateTimeKind.Utc),
                        DisplayName = "Filip",
                        LastName = "Tomren",
                        MiddleName = "Andre Larsen",
                        Phone = "+47123455678",
                    }).run(conn);

            
            Cursor<User> result = r.db(DbName).table(TableName)
                .run<User>(conn);

            var bufferedItems = result.BufferedItems;

            bufferedItems.Dump();

            var user = bufferedItems[0];
            user.Birthday.Should().BeCloseTo(new DateTime(1990, 8, 18, 0, 0, 0, DateTimeKind.Utc));
        }
    }

    public class User
    {
        public IList<object> TenantPermissions { get; set; }
        public string Email { get; set; }
        public string Phone { get; set; }
        public string Picture { get; set; }
        public DateTime Birthday { get; set; }
        public string DisplayName { get; set; }
        public string FirstName { get; set; }
        public string MiddleName { get; set; }
        public string LastName { get; set; }
        public Address Address { get; set; }
    }

    public class Address
    {

    }
}