using FluentAssertions;
using NUnit.Framework;
using RethinkDb.Driver.Net;

namespace RethinkDb.Driver.Tests.Network
{
    [TestFixture]
    public class ConnectionTest
    {
        public static RethinkDB r = RethinkDB.R;

        private Connection conn;
        
        [Test]
        public void can_connect()
        {
            var c = r.Connection()
                .Hostname(AppSettings.TestHost)
                .Port(AppSettings.TestPort)
                .Timeout(60)
                .Connect();
            
            int result = r.random(1, 9).add(r.random(1, 9)).Run<int>(c);
            result.Should().BeGreaterOrEqualTo(2).And.BeLessThan(18);
        }


    }
}