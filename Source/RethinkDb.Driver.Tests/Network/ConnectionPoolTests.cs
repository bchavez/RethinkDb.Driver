using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using Humanizer;
using NUnit.Framework;
using RethinkDb.Driver.Net.Clustering;
using RethinkDb.Driver.Tests.Utils;
using Z.ExtensionMethods;

namespace RethinkDb.Driver.Tests.Network
{
    [TestFixture]
    public class ConnectionPoolTests
    {
        [Test]
        public void roundrobin_test()
        {
            var sw = Stopwatch.StartNew();

            var p = new RoundRobinHostPool();
            p.AddHost("a", null);
            p.AddHost("b", null);
            p.AddHost("c", null);

            //initially hosts are not dead, for testing of course.
            foreach( var h in p.HostList )
            {
                h.Dead = false;
            }

            p.GetRoundRobin().Host.Should().Be("a");
            p.GetRoundRobin().Host.Should().Be("b");
            p.GetRoundRobin().Host.Should().Be("c");
            var respA = p.GetRoundRobin();
            respA.Host.Should().Be("a");

            respA.MarkFailed();
            var respB = p.GetRoundRobin();
            respB.MarkFailed();
            var respC = p.GetRoundRobin();
            respC.Host.Should().Be("c");

            // get again, and verify that it's still c
            p.GetRoundRobin().Host.Should().Be("c");
            p.GetRoundRobin().Host.Should().Be("c");
            p.GetRoundRobin().Host.Should().Be("c");

            var resp = p.GetRoundRobin();
            resp.Should().NotBeNull();
            sw.Stop();
            //TOTAL TIME: 17 milliseconds
            Console.WriteLine($"TOTAL TIME: {sw.Elapsed.Humanize()}");
        }


        [Test]
        [Explicit            ]
        public void epsilon_test()
        {
            var sw = Stopwatch.StartNew();
            EpsilonGreedyHostPool.Random = new Random(10);

            var p = new EpsilonGreedyHostPool(null, EpsilonCalculator.Linear(), autoStartDecayTimer:false);
            p.AddHost("a", null);
            p.AddHost("b", null);

            //initially hosts are not dead, for testing of course.
            foreach (var h in p.HostList)
            {
                h.Dead = false;
            }

            //Initially, A is faster than B;
            var timings = new Dictionary<string, long>()
                {
                    {"a", 200},
                    {"b", 300}
                };

            var hitCounts = new Dictionary<string, long>()
                {
                    {"a", 0},
                    {"b", 0}
                };

            var iterations = 12000;

            for( var i = 0; i < iterations; i++ )
            {
                if( (i != 0) && (i % 100) == 0 )
                {
                    p.PerformEpsilonGreedyDecay(null);
                }
                var hostR = p.GetEpsilonGreedy();// as EpsilonHostPoolResponse;
                var host = hostR.Host;
                hitCounts[host]++;
                var timing = timings[host];
                p.MarkSuccess(hostR, 0, TimeSpan.FromMilliseconds(timing).Ticks);
            }

            foreach( var host in hitCounts )
            {
                Console.WriteLine($"Host {host.Key} hit {host.Value} times {((double)host.Value / iterations):P}");
            }


            // 60-40
            //Host a hit 7134 times 59.45 %
            //Host b hit 4866 times 40.55 %
            hitCounts["a"].Should().BeGreaterThan(hitCounts["b"]);
            //hitCounts["a"].Should().Be(7134);
            //hitCounts["b"].Should().Be(4866);

            

            //===========================================
            //timings change, now B is faster than A
            timings["a"] = 500;
            timings["b"] = 100;
            hitCounts["a"] = 0;
            hitCounts["b"] = 0;

            for (var i = 0; i < iterations; i++)
            {
                if ((i != 0) && (i % 100) == 0)
                {
                    p.PerformEpsilonGreedyDecay(null);
                }
                var hostR = p.GetEpsilonGreedy();// as EpsilonHostPoolResponse;
                var host = hostR.Host;
                hitCounts[host]++;
                var timing = timings[host];
                p.MarkSuccess(hostR, 0, TimeSpan.FromMilliseconds(timing).Ticks);
            }

            //TOTAL TIME: 177 milliseconds
            sw.Stop();
            Console.WriteLine($"TOTAL TIME: {sw.Elapsed.Humanize()}");

            foreach (var host in hitCounts)
            {
                Console.WriteLine($"Host {host.Key} hit {host.Value} times {((double)host.Value / iterations):P}");
            }

            // 70-30
            //Host a hit 3562 times 29.68 %
            //Host b hit 8438 times 70.32 %
            hitCounts["b"].Should().BeGreaterThan(hitCounts["a"]);
            //hitCounts["a"].Should().Be(3562);
            //hitCounts["b"].Should().Be(8438);
            
        }

        [Test]
        [Explicit]
        public void benchmark_epsilon()
        {
            EpsilonGreedyHostPool.Random = new Random(10);

            var p = new EpsilonGreedyHostPool(null, new LinearEpsilonValueCalculator(), autoStartDecayTimer:false);
            p.AddHost("a", null);
            p.AddHost("b", null);

            //initially hosts are not dead, for testing of course.
            foreach (var h in p.HostList)
            {
                h.Dead = false;
            }

            //var hitA = 0;
            //var hitB = 0;

            var iterations = 120000;
            var changeTimingsAt = 60000;

            var threads = 5;


            var hitA = new int[threads];
            var hitB = new int[threads];

            var total = 0;

            var locker = 0;

            Action<int> maybeReset = (iTotal) =>
                {
                    if( (iTotal != 0) && (iTotal % 100) == 0 &&
                    Interlocked.CompareExchange(ref locker, 1, 0) == 0)
                    {
                        p.PerformEpsilonGreedyDecay(null);
                        Interlocked.Decrement(ref locker);
                    }
                };

            var sw = Stopwatch.StartNew();
            var tasks = Enumerable.Range(1, threads).Select(t =>
                {
                    return Task.Run(() =>
                        {
                            //Initially, A is faster than B;
                            var timingA = 200; //60% = 1 - 2/5
                            var timingB = 300; //40% = 1 - 3/5

                            for( var i = 0; i < iterations; i++ )
                            {
                                var at = Interlocked.Increment(ref total);
                                maybeReset(at);

                                HostEntry hostR;

                                hostR = p.GetEpsilonGreedy(); // as EpsilonHostPoolResponse;

                                var host = hostR.Host;
                                var timing = 0;
                                if( host == "a" )
                                {
                                    Interlocked.Increment(ref hitA[t - 1]);
                                    timing = timingA;
                                }
                                else if( host == "b" )
                                {
                                    Interlocked.Increment(ref hitB[t - 1]);
                                    timing = timingB;
                                }
                                
                                p.MarkSuccess(hostR, 0, TimeSpan.FromMilliseconds(timing).Ticks);
                                //if( changeTimingsAt == i )
                                //{
                                //    //Half way, B is faster than A;
                                //    timingA = 500;
                                //    timingB = 100;
                                //}
                            }
                        });
                });
            Task.WaitAll(tasks.ToArray());
            sw.Stop();


            //TOTAL TIME: 864 milliseconds
            Console.WriteLine($"TOTAL TIME: {sw.Elapsed.Humanize()}");


            for( int t = 0; t < threads; t++ )
            {
                var totalHits = hitA[t] + hitB[t];
                Console.WriteLine($"Thread {t} HitA: {hitA[t]} ({hitA[t] / (float)totalHits:P}), HitB: {hitB[t]} ({hitB[t] / (float)totalHits:p}), Total: {totalHits}");
            }
            

            Console.WriteLine($"Global Total: {total}");
        }

        [Test]
        [Explicit]
        public void bechmark_round_robin()
        {
            var p = new RoundRobinHostPool();
            p.AddHost("a", null);
            p.AddHost("b", null);
            p.AddHost("c", null);

            //initially hosts are not dead, for testing of course.
            foreach (var h in p.HostList)
            {
                h.Dead = false;
            }

            var hitA = 0;
            var hitB = 0;
            var hitC = 0;

            var iterations = 120000;
            var threads = 5;

            var sw = Stopwatch.StartNew();
            var tasks = Enumerable.Range(1, threads).Select((i) =>
                {
                    return Task.Run(() =>
                        {
                            for( var x = 0; x < iterations; x++ )
                            {
                                var h = p.GetRoundRobin();

                                if( h.Host == "a" )
                                    Interlocked.Increment(ref hitA);
                                else if( h.Host == "b" )
                                    Interlocked.Increment(ref hitB);
                                else
                                    Interlocked.Increment(ref hitC);
                            }
                        });
                });
            Task.WaitAll(tasks.ToArray());
            sw.Stop();

            var hitCounts = new Dictionary<string, long>()
                {
                    {"a", hitA},
                    {"b", hitB},
                    {"c", hitC}
                };

            foreach( var kvp in hitCounts )
            {
                Console.WriteLine($"Host {kvp.Key} hit {kvp.Value} times {((double)kvp.Value / (iterations * threads)):P}");
            }
            
            //TOTAL TIME: 60 milliseconds
            Console.WriteLine($"TOTAL TIME: {sw.Elapsed.Humanize()}");
        }

        [Test]
        [Explicit]
        public void check_model()
        {
            var r = RethinkDB.r;
            var conn = r.connection()
                .hostname(AppSettings.TestHost)
                .port(AppSettings.TestPort)
                .timeout(60)
                .connect();

            var result = r.db("rethinkdb").table("server_status")
                 .runCursor<Server>(conn);

            var servers = result.ToList();

            //servers.Dump();

            var server = servers[0];

            var realAddresses = server.Network.CanonicalAddress
                .Where(s => // no localhost and no ipv6. for now.
                    !s.Host.StartsWith("127.0.0.1") &&
                    !s.Host.Contains(":"))
                .Select(c => c.Host);

            realAddresses.Dump();


            var now = new[] {"192.168.0.12", "192.168.0.13"};

            realAddresses.Any(ip => now.Any(s => s.Contains(ip))).Dump();
        }

        [Test]
        [Explicit]
        public async void can_connect_to_cluster()
        {
            var r = RethinkDB.r;
            var c = r.hostpool()
                .seed(new[] {"192.168.0.11:28015"})
                .selectionStrategy(new RoundRobinHostPool())
                .discover(true)
                .connect();

            Thread.Sleep(10000);

            int result = r.random(1, 9).add(r.random(1, 9)).run<int>(c);
            result.Should().BeGreaterOrEqualTo(2).And.BeLessThan(18);
        }

        [Test]
        [Explicit]
        public void stay_alive_test()
        {
            var r = RethinkDB.r;
            var c = r.hostpool()
                .seed(new[] { "192.168.0.11:28015" })
                .selectionStrategy(new RoundRobinHostPool())
                .discover(true)
                .connect();

            Thread.Sleep(900000);
        }

        [Test]
        public void failure_should_double_retry()
        {
            var he = new HostEntry("a")
                {
                    RetryDelayMax = TimeSpan.FromSeconds(300),
                    RetryDelayInitial = TimeSpan.FromSeconds(30)
                };

            he.MarkFailed();
            he.Dead.Should().BeTrue();
            he.RetryCount.Should().Be(0);

            he.NextRetry.Should().BeCloseTo(DateTime.Now.AddSeconds(30), precision: 3000);

            he.RetryFailed();

            he.RetryCount.Should().Be(1);
            he.NextRetry.Should().BeCloseTo(DateTime.Now.AddSeconds(30 * 2), precision: 3000);

            he.RetryFailed();
            
            he.RetryCount.Should().Be(2);
            he.NextRetry.Should().BeCloseTo(DateTime.Now.AddSeconds(30 * 2 * 2), precision: 3000);

            //run it past the maximum
            Enumerable.Range(1, 30 * 12)
                .ForEach(i => he.RetryFailed());

            he.RetryFailed();

            he.NextRetry.Should().BeCloseTo(DateTime.Now + he.RetryDelayMax, precision: 1000);


        }
    }

}