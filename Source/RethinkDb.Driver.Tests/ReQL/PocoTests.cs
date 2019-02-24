using System;
using System.Collections.Generic;
using FluentAssertions;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using NUnit.Framework;
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
        public void can_bracket_on_table()
        {
            string sessions = "sessions";
            string speakers = "speakers";

            try
            {
                DropTable(DbName, sessions);
            }
            catch
            {
            }
            try
            {
                DropTable(DbName, speakers);
            }
            catch
            {
            }

            CreateTable(DbName, sessions);
            CreateTable(DbName, speakers);


            R.Db(DbName).Table(sessions)
                .Insert(new[]
                    {
                        new {name = "Session 1", track = "trackA"},
                        new {name = "Session 2", track = "trackA"},
                        new {name = "Session 3", track = "trackA"},
                        new {name = "Session 4", track = "trackB"},
                        new {name = "Session 5", track = "trackC"},
                        new {name = "Session 6", track = "trackD"},
                    }).RunWrite(conn);

            R.Db(DbName).Table(speakers)
                .Insert(new[]
                    {
                        new {name = "Brian Chavez"},
                        new {name = "Name B"},
                        new {name = "Name C"},
                        new {name = "Name D"},
                        new {name = "Name E"},
                        new {name = "Name F"},
                    }).RunWrite(conn);
            

            var projection = new
                {
                    tracks = R.Db(DbName).Table(sessions)["track"].Distinct().CoerceTo("array"),
                    speakers = R.Db(DbName).Table(speakers)["name"].CoerceTo("array")
                };

            var result = R.Expr(projection).RunResult<JObject>(conn);

            var speakersArray = result["speakers"].ToObject<string[]>();
            var tracksArray = result["tracks"].ToObject<string[]>();

            speakersArray.Should().HaveCount(6).And
                .Contain("Brian Chavez");

            tracksArray.Should().HaveCount(4).And
                .Contain("trackC");

        }


        public class PocoWithIgnoredGuidId
        {
            [JsonIgnore]
            public Guid id { get; set; }
            public string FirstName { get; set; }
            public string LastName { get; set; }
        }

        [Test]
        public void serdes_poco_with_ignored_guid_id()
        {
            var poco = new PocoWithIgnoredGuidId()
                {
                    FirstName = "Brian",
                    LastName = "Chavez"
                };

            var result = table.Insert(poco)
                .RunWrite(conn);

            result.Dump();
            result.GeneratedKeys[0].Should().NotBeEmpty();
            var get = table.Get(result.GeneratedKeys[0])
                .RunResult<PocoWithIgnoredGuidId>(conn);
            get.id.Should().BeEmpty();
            get.Dump();
        }

        public class PocoWithGuidId
        {
            [JsonProperty("id", DefaultValueHandling = DefaultValueHandling.Ignore)]
            public Guid Id { get; set; }
            public string FirstName { get; set; }
            public string LastName { get; set; }
        }

        [Test]
        public void serdes_poco_with_proper_guid()
        {
            var poco = new PocoWithGuidId()
                {
                    FirstName = "Brian",
                    LastName = "Chavez"
                };

            var result = table.Insert(poco)
                .RunWrite(conn);

            result.Dump();
            var id = result.GeneratedKeys[0];
            id.Should().NotBeEmpty();
            var get = table.Get(id)
                .RunResult<PocoWithGuidId>(conn);
            get.Dump();
            get.Id.Should().NotBeEmpty();

        }
    }
}
