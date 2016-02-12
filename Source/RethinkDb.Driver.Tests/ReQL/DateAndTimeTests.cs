using System;
using FluentAssertions;
using Newtonsoft.Json.Linq;
using NUnit.Framework;
using RethinkDb.Driver.Ast;
using RethinkDb.Driver.Tests.Utils;

namespace RethinkDb.Driver.Tests.ReQL
{
    [TestFixture]
    public class DateAndTimeTests : QueryTestFixture
    {
        [Test]
        public void datetimeoffset_pesudo_type_r_now()
        {
            DateTimeOffset t = R.now().run<DateTimeOffset>(conn);
            //ten minute limit for clock drift.
            var now = DateTimeOffset.Now;
            t.Should().BeCloseTo(now, 600000);
        }

        [Test]
        public void datetime_pesudo_type_r_now()
        {
            DateTime t = R.now().run<DateTime>(conn);
            //rethinkdb returns no timezone with r.now();

            var now = DateTime.UtcNow;

            //ten minute limit for server clock drift.
            t.Should().BeCloseTo(now, 600000);
        }

        [Test]
        public void datetime_expr_localtime()
        {
            var date = DateTime.Now;
            DateTime result = R.expr(date).run<DateTime>(conn);

            result.Should().BeCloseTo(date, 1); // must be within 1ms of each other
        }

        [Test]
        public void datetime_expr_utctime()
        {
            var date = DateTime.UtcNow;
            DateTime result = R.expr(date).run<DateTime>(conn);

            result.Should().BeCloseTo(date, 1);
        }

        [Test]
        public void unspecified_date_time()
        {
            var date = new DateTime(2015, 11, 14, 1, 2, 3, DateTimeKind.Unspecified);

            //ISO 8601 string has no time zone, and no default time zone was provided.

            Action action = () =>
                {
                    DateTime result = R.expr(date).run<DateTime>(conn);
                };

            action.ShouldThrow<ReqlQueryLogicError>("DateTime unspecified timezone should not be ISO8601 valid.");
        }

        [Test]
        public void unspecified_date_time_with_default_timezone()
        {
            var date = new DateTime(2015, 11, 14, 1, 2, 3, DateTimeKind.Unspecified);

            var default_timezone = new {default_timezone = "-09:00"};

            DateTime result = (R.expr(date) as Iso8601)[default_timezone].run<DateTime>(conn);

            var dateTimeUtc = new DateTime(2015, 11, 14, 1, 2, 3) + TimeSpan.FromHours(9);

            result.ToUniversalTime().Should().BeCloseTo(dateTimeUtc, 1);

            var withoutTimezone = date.ToString("o");
            DateTime result2 = R.iso8601(withoutTimezone)[default_timezone].run<DateTime>(conn);

            result2.ToUniversalTime().Should().BeCloseTo(dateTimeUtc, 1);

        }

        [Test]
        public void use_raw_object()
        {
            JObject result = R.now().run<JObject>(conn);
            //ten minute limit for clock drift.

            result["$reql_type$"].ToString().Should().Be("TIME");

            result.Dump();
        }

        [Test]
        public void can_serdes_flight()
        {
            var departure = new DateTime(2011, 11, 14, 1, 33, 22, DateTimeKind.Utc);
            var flight = new Flight
                {
                    id = "lax",
                    Destination = "LAX",
                    DepartureLocal = departure.ToLocalTime(),
                    DepartureUtc = departure.ToUniversalTime()
                };

            var result = R.db(DbName).table(TableName)
                .insert(flight).run(conn);

            Flight f = R.db(DbName).table(TableName)
                .get("lax").run<Flight>(conn);

            f.Should().NotBeNull();
        }
    }

    public class Flight
    {
        public string id { get; set; }
        public string Destination { get; set; }
        public DateTime DepartureLocal { get; set; }
        public DateTime DepartureUtc { get; set; }
    }

}