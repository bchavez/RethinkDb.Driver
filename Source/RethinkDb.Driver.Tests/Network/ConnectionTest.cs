using FluentAssertions;
using NUnit.Framework;
using RethinkDb.Driver.Net;

namespace RethinkDb.Driver.Tests.Network
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

        [Test]
        public void can_connect_to_cluster()
        {
            var c = r.hostpool()
            .seed(new { "192.168.0.11:28015", "192.168.0.12:28015" })
            .discover()
            .connect();
        }
    }
}