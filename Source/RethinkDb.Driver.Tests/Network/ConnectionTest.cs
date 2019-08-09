using System;
using System.Net.Security;
using System.Net.Sockets;
using System.Security.Authentication;
using System.Security.Cryptography.X509Certificates;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using NUnit.Framework;
using NUnit.Framework.Compatibility;
using RethinkDb.Driver.Net;
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
        public async Task no_connection_to_a_pool_with_timeout_throws_async()
        {
            const int Timeout = 2;
            Func<Task<ConnectionPool>> func = async () =>
                await R.ConnectionPool()
                    .PoolingStrategy(new RoundRobinHostPool())
                    .Seed("127.0.0.2:2801")
                    .InitialTimeout(Timeout)
                    .ConnectAsync();

            var sw = Stopwatch.StartNew();
            func.ShouldThrow<ReqlDriverError>();
            sw.Stop();

            sw.Elapsed.Should().BeCloseTo(TimeSpan.FromSeconds(Timeout), 700);
        }

        [Test]
        public async Task no_connection_to_a_pool_with_canceltoken_stops_async()
        {
            var cts = new CancellationTokenSource();
            Func<Task<ConnectionPool>> func = async () =>
                await R.ConnectionPool()
                    .PoolingStrategy(new RoundRobinHostPool())
                    .Seed("127.0.0.2:2801")
                    .ConnectAsync(cts.Token);

            const int CancellationDelay = 1200;
            var sw = Stopwatch.StartNew();
            cts.CancelAfter(CancellationDelay);
            func.ShouldThrow<ReqlDriverError>();
            sw.Stop();

            sw.Elapsed.Should().BeCloseTo(TimeSpan.FromMilliseconds(CancellationDelay), 700);
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


        [Test]
        public void we_should_fail_with_something_if_we_cant_connect_with_good_creds()
        {
            var c = R.Connection()
                .User("Foo", "barb")
                .Hostname(AppSettings.TestHost)
                .Port(AppSettings.TestPort);

            Action act = ()=> c.Connect();

            act.ShouldThrow<ReqlAuthError>();


        }

        [Test]
        [Explicit]
        public void can_conenct_to_cluster_via_tls()
        {
            var conn = R.ConnectionPool()
                .Seed("192.168.0.140")
                .PoolingStrategy(new RoundRobinHostPool())
                .EnableSsl(new SslContext
                        {
                            ServerCertificateValidationCallback = ValidationCallback,
                            EnabledProtocols = SslProtocols.Tls12
                        },
                    licenseTo: "Brian Chavez",
                    licenseKey: "stuff")
                .Connect();

            var val = R.Expr(1).Add(1).RunAtom<int>(conn);
            val.Should().Be(2);
        }

        [Test]
        [Explicit]
        public void can_connect_over_ssl()
        {
            var conn = R.Connection()
                .Hostname("192.168.0.140")
                .EnableSsl(
                    new SslContext
                        {
                            ServerCertificateValidationCallback = ValidationCallback,
                            EnabledProtocols = SslProtocols.Tls12
                        },
                    licenseTo: "bchavez@gmail.com",
                    licenseKey: "stuff"
                )
                .Connect();

            var val = R.Expr(1).Add(1).RunAtom<int>(conn);
            val.Should().Be(2);
        }
        private X509Certificate2 x509cert;
        private bool ValidationCallback(object sender, X509Certificate cert, X509Chain chain, SslPolicyErrors sslPolicyErrors)
        {
            //var truth = chain.ChainElements[0].Certificate.Thumbprint == x509cert.Thumbprint;

         //   return truth;
            return true;
        }
    }
}