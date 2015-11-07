using FluentAssertions;
using NUnit.Framework;
using RethinkDb.Driver.Net;

namespace RethinkDb.Driver.Tests
{
    [TestFixture]
    public class ConnectionTest
    {
        public static RethinkDB r = RethinkDB.r;

        private Connection conn;
        
        [Test]
        public void can_connect()
        {
            var c = r.connection()
                .hostname(AppSettings.TestHost)
                .port(AppSettings.TestPort)
                .timeout(60)
                .connect();

            int result = r.random(1, 9).add(r.random(1, 9)).run<int>(c);
            result.Should().BeGreaterOrEqualTo(2).And.BeLessThan(18);
        }
    }
}