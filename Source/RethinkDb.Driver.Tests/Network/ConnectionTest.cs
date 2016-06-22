using System;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using NUnit.Framework;
using RethinkDb.Driver.Net.Clustering;
using RethinkDb.Driver.Tests.Utils;

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
                .Timeout(10)
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
                .InitialTimeout(10)
                .Connect();

            int result = R.Random(1, 9).Add(R.Random(1, 9)).Run<int>(c);
            result.Should().BeGreaterOrEqualTo(2).And.BeLessThan(18);
        }

        [Test]
        public void no_connection_to_a_pool_with_timeout_throws()
        {
            Action act = () => R.ConnectionPool()
                .PoolingStrategy(new RoundRobinHostPool())
                .Seed("127.0.0.2:2801")
                .InitialTimeout(10)
                .Connect();

            act.ShouldThrow<ReqlDriverError>();
        }

        [Test]
        public void invalid_IP_throws_socket_exception_not_aggregate_exception()
        {
            Action invalidConnect = () => R.Connection()
                        .Hostname("127.0.0.2")
                        .Port(5555)
                        .Connect();

            invalidConnect.ShouldThrow<SocketException>();
        }

        [Test]
        public void can_reconnect_after_close()
        {
            var c = R.Connection()
                .Hostname(AppSettings.TestHost)
                .Port(AppSettings.TestPort)
                .Connect();

            var dt = R.Now().RunAtom<DateTime>(c);
            dt.Dump();
            dt.Should().BeWithin(TimeSpan.FromHours(1));

            c.Close(false);
            c.Reconnect();

            dt = R.Now().RunAtom<DateTime>(c);
            dt.Dump();
            dt.Should().BeWithin(TimeSpan.FromHours(1));

            c.Close(false);
        }

    }
}