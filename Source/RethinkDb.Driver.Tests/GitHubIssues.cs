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
                        FirstName = null,
                        DisplayName = "Filip",
                        LastName = "Tomren",
                        MiddleName = "Andre Larsen",
                        Phone = "+47123455678",
                        NickNames =
                            {
                                "Foo",
                                "Bar"
                            },
                        Address = new Address
                            {
                                Street = "1234 Test Ave",
                                Zipcode = "54321"
                            },
                        ShippingAddresses =
                            {
                                {new Address {Street = "Shipping 1", Zipcode = "Zip 1"}},
                                {new Address {Street = "Shipping 2", Zipcode = "Zip 2"}},
                                {new Address {Street = "Shipping 3", Zipcode = "Zip 3"}}
                            }
                    }).run(conn);

            
            Cursor<User> result = r.db(DbName).table(TableName)
                .run<User>(conn);

            var bufferedItems = result.BufferedItems;

            bufferedItems.Dump();

            var user = bufferedItems[0];
            user.Birthday.Should().BeCloseTo(new DateTime(1990, 8, 18, 0, 0, 0, DateTimeKind.Utc));
            user.DisplayName.Should().Be("Filip");
            user.MiddleName.Should().Be("Andre Larsen");
            user.Phone.Should().Be("+47123455678");
            user.NickNames.Should().Equal("Foo", "Bar");
            user.Address.ShouldBeEquivalentTo(new Address {Street = "1234 Test Ave", Zipcode = "54321"});
            user.ShippingAddresses.ShouldBeEquivalentTo(new Address[]
                {
                    new Address {Street = "Shipping 1", Zipcode = "Zip 1"},
                    new Address {Street = "Shipping 2", Zipcode = "Zip 2"},
                    new Address {Street = "Shipping 3", Zipcode = "Zip 3"}
                });
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
        public List<string> NickNames { get; set; } = new List<string>();
        public List<Address> ShippingAddresses { get; set; } = new List<Address>();
    }

    public class Address
    {
        public string Street { get; set; }
        public string Zipcode { get; set; }
    }
}