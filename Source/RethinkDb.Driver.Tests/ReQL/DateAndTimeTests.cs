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
            DateTimeOffset t = R.Now().Run<DateTimeOffset>(conn);
            //ten minute limit for clock drift.
            var now = DateTimeOffset.Now;
            t.Should().BeCloseTo(now, 600000);
        }

        [Test]
        public void datetime_pesudo_type_r_now()
        {
            DateTime t = R.Now().Run<DateTime>(conn);
            //rethinkdb returns no timezone with r.now();

            var now = DateTime.UtcNow;

            //ten minute limit for server clock drift.
            t.Should().BeCloseTo(now, 600000);
        }

        [Test]
        public void datetime_expr_localtime()
        {
            var date = DateTime.Now;
            DateTime result = R.Expr(date).Run<DateTime>(conn);

            result.Should().BeCloseTo(date, 1); // must be within 1ms of each other
        }

        [Test]
        public void datetime_expr_utctime()
        {
            var date = DateTime.UtcNow;
            DateTime result = R.Expr(date).Run<DateTime>(conn);

            result.Should().BeCloseTo(date, 1);
        }

        [Test]
        public void unspecified_date_time_should_come_back_local_by_default()
        {
            var date = new DateTime(2015, 11, 14, 1, 2, 3, DateTimeKind.Unspecified);

            var result = R.Expr(date).RunResult<DateTime>(conn);

            var expected = new DateTime(2015, 11, 14, 1, 2, 3, DateTimeKind.Local);

            result.Should().BeCloseTo(expected, (int)TimeSpan.FromMinutes(30).TotalMilliseconds);
        }

        [Test]
        public void should_be_able_to_use_datetime_with_iso8601()
        {
            var date = new DateTime(2015, 11, 14, 1, 2, 3, DateTimeKind.Unspecified);

            //ISO 8601 string has no time zone, and no default time zone was provided.
            Action action = () =>
            {
                DateTime result = R.Iso8601(date).Run<DateTime>(conn);
            };

            action.ShouldThrow<ReqlQueryLogicError>("DateTime unspecified timezone should not be ISO8601 valid.");
        }

        [Test]
        public void can_convert_utc_datetime_to_epochtime()
        {
            var date = new DateTime(2015, 11, 14, 1, 2, 3, DateTimeKind.Utc);

            var result = R.EpochTime(date).RunResult<DateTime>(conn);

            result.Should().BeCloseTo(date, (int)TimeSpan.FromMinutes(30).TotalMilliseconds);
        }

        [Test]
        public void can_convert_local_datetime_to_epochtime()
        {
            var date = new DateTime(2015, 11, 14, 1, 2, 3, DateTimeKind.Local);

            var result = R.EpochTime(date).RunResult<DateTime>(conn);

            result.Should().BeCloseTo(date.ToUniversalTime(), (int)TimeSpan.FromMinutes(30).TotalMilliseconds);
        }

        [Test]
        public void unspecified_date_time_with_default_timezone()
        {
            var date = new DateTime(2015, 11, 14, 1, 2, 3, DateTimeKind.Unspecified);

            var datestr = date.ToString("o");

            var default_timezone = new {default_timezone = "-09:00"};

            DateTime result = R.Iso8601(datestr)[default_timezone].Run<DateTime>(conn);

            var dateTimeUtc = new DateTime(2015, 11, 14, 1, 2, 3) + TimeSpan.FromHours(9);

            result.ToUniversalTime().Should().BeCloseTo(dateTimeUtc, 1);

            var withoutTimezone = date.ToString("o");
            DateTime result2 = R.Iso8601(withoutTimezone)[default_timezone].Run<DateTime>(conn);

            result2.ToUniversalTime().Should().BeCloseTo(dateTimeUtc, 1);
        }

        [Test]
        public void use_raw_object()
        {
            JObject result = R.Now().Run<JObject>(conn, new {time_format="raw"});
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

            var result = R.Db(DbName).Table(TableName)
                .Insert(flight).Run(conn);

            Flight f = R.Db(DbName).Table(TableName)
                .Get("lax").Run<Flight>(conn);

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