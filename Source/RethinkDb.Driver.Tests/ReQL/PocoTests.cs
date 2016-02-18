using System;
using System.Collections.Generic;
using System.Linq;
using FluentAssertions;
using Newtonsoft.Json.Linq;
using NUnit.Framework;
using RethinkDb.Driver.Ast;
using RethinkDb.Driver.Net;
using RethinkDb.Driver.Tests.Utils;

namespace RethinkDb.Driver.Tests.ReQL
{
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

    [TestFixture]
    public class PocoTests : QueryTestFixture
    {
        [Test]
        public void issue_10()
        {
            R.Db(DbName).Table(TableName)
                .Delete().Run(conn);

            var insert = R.Db(DbName).Table(TableName)
                .Insert(new User
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
                    }).Run(conn);

            
            Cursor<User> result = R.Db(DbName).Table(TableName)
                .Run<User>(conn);

            var bufferedItems = result.BufferedItems;

            bufferedItems.Dump();

            var user = bufferedItems[0];
            user.Birthday.Should().BeCloseTo(new DateTime(1990, 8, 18, 0, 0, 0, DateTimeKind.Utc));
            user.DisplayName.Should().Be("Filip");
            user.MiddleName.Should().Be("Andre Larsen");
            user.Phone.Should().Be("+47123455678");
            user.NickNames.Should().Equal("Foo", "Bar");
            user.Address.ShouldBeEquivalentTo(new Address {Street = "1234 Test Ave", Zipcode = "54321"});
            user.ShippingAddresses.ShouldBeEquivalentTo(new[]
                {
                    new Address {Street = "Shipping 1", Zipcode = "Zip 1"},
                    new Address {Street = "Shipping 2", Zipcode = "Zip 2"},
                    new Address {Street = "Shipping 3", Zipcode = "Zip 3"}
                });
        }

        [Test]
        public void anonymous_type_is_an_expr_too()
        {
            var obj = R.Expr(new
                {
                    keya = "foo",
                    keyb = "bar"
                }).keys().RunResult<string[]>(conn);

            obj.Should().BeEquivalentTo("keya", "keyb");
        }

        [Test]
        public void can_ser_deser_reql_expr_anon_type()
        {
            
            var vals = R.Expr(new
            {
                keya = R.Now(),
                keyb = "foo"
            }).values().RunResult<JArray>(conn);

            var raw = vals.ToString();
            raw.Dump();
            raw.Should().Contain("foo");
            raw.Should().Contain(Converter.PseudoTypeKey);
        }

        [Test]
        [Explicit]
        public void can_bracket_on_table()
        {
            R.Db(DbName).TableCreate("sessions").RunResult(conn);
            R.Db(DbName).TableCreate("speakers").RunResult(conn);

            var projection = new
                {
                    track = R.Db("codecamp_organizer").Table("sessions")["track"].Distinct().CoerceTo("array"),
                    speakers = R.Db("codecamp_organizer").Table("speakers")["name"].CoerceTo("array")
                };

            var result = R.Expr(projection).RunResult<JObject>(conn);

            result.Dump();
        }
    }
}
