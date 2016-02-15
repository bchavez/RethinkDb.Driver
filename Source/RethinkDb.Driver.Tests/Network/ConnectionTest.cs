using System.Threading.Tasks;
using FluentAssertions;
using NUnit.Framework;
using RethinkDb.Driver.Net.Clustering;

namespace RethinkDb.Driver.Tests.Network
{
    [TestFixture]
    public class ConnectionTest
    {
        public static RethinkDB R = RethinkDB.R;

        //private Connection conn;
        
        [Test]
        public void can_connect()
        {
            var c = R.Connection()
                .Hostname(AppSettings.TestHost)
                .Port(AppSettings.TestPort)
                .Timeout(60)
                .Connect();
            
            int result = R.Random(1, 9).Add(R.Random(1, 9)).Run<int>(c);
            result.Should().BeGreaterOrEqualTo(2).And.BeLessThan(18);
        }

        [Test]
        public async Task can_connect_async_test()
        {
            var c = await R.Connection()
                .Hostname(AppSettings.TestHost)
                .Port(AppSettings.TestPort)
                .Timeout(60)
                .ConnectAsync();
            
            int result = R.Random(1, 9).Add(R.Random(1, 9)).Run<int>(c);
            result.Should().BeGreaterOrEqualTo(2).And.BeLessThan(18);
        }

        [Test]
        public void can_connect_to_a_cluster()
        {
            var c = R.ConnectionPool()
                .PoolingStrategy(new RoundRobinHostPool())
                .Seed(new[] {$"{AppSettings.TestHost}:{AppSettings.TestPort}"})
                .Connect();


            int result = R.Random(1, 9).Add(R.Random(1, 9)).Run<int>(c);
            result.Should().BeGreaterOrEqualTo(2).And.BeLessThan(18);
        }

    }
}