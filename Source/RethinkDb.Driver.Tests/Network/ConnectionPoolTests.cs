using System;
using System.Collections.Generic;
using System.Diagnostics;
using FluentAssertions;
using Humanizer;
using NUnit.Framework;
using RethinkDb.Driver.Net.Clustering;
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
            var dummyErr = new Exception("dummy error");

            var p = new RoundRobinHostPool(new[] {"a", "b", "c"});

            p.Get().Host.Should().Be("a");
            p.Get().Host.Should().Be("b");
            p.Get().Host.Should().Be("c");
            var respA = p.Get();
            respA.Host.Should().Be("a");


            respA.Mark(dummyErr);
            var respB = p.Get();
            respB.Mark(dummyErr);
            var respC = p.Get();
            respC.Host.Should().Be("c");
            respC.Mark(null);

            // get again, and verify that it's still c
            p.Get().Host.Should().Be("c");

            // now try to mark b as success; should fail because already marked
            respB.Mark(null);

            p.Get().Host.Should().Be("c");

            respA = new HostPoolResponse {Host = "a", HostPool = p};
            respA.Mark(null);

            p.Get().Host.Should().Be("a");
            p.Get().Host.Should().Be("c");

            new[] {"a", "b", "c"}.ForEach(host =>
                {
                    var response = new HostPoolResponse {Host = host, HostPool = p};
                    response.Mark(dummyErr);
                });

            var resp = p.Get();
            resp.Should().NotBeNull();
            sw.Stop();
            //TOTAL TIME: 17 milliseconds
            Console.WriteLine($"TOTAL TIME: {sw.Elapsed.Humanize()}");
        }


        [Test]
        public void epsilon_test()
        {
            var sw = Stopwatch.StartNew();
            EpsilonGreedy.Random = new Random(10);

            var p = new EpsilonGreedy(new [] { "a", "b"}, null, new LinearEpsilonValueCalculator());

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
                var hostR = p.Get() as EpsilonHostPoolResponse;
                var host = hostR.Host;
                hitCounts[host]++;
                var timing = timings[host];
                hostR.Ended = hostR.Started.Add(TimeSpan.FromMilliseconds(timing));
                hostR.Mark(null);
            }

            foreach( var host in hitCounts )
            {
                Console.WriteLine($"Host {host.Key} hit {host.Value} times {((double)host.Value / iterations):P}");
            }


            // 60-40
            //Host a hit 7134 times 59.45 %
            //Host b hit 4866 times 40.55 %
            hitCounts["a"].Should().BeGreaterThan(hitCounts["b"]);
            hitCounts["a"].Should().Be(7134);
            hitCounts["b"].Should().Be(4866);

            

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
                var hostR = p.Get() as EpsilonHostPoolResponse;
                var host = hostR.Host;
                hitCounts[host]++;
                var timing = timings[host];
                hostR.Ended = hostR.Started.Add(TimeSpan.FromMilliseconds(timing));
                hostR.Mark(null);
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
            hitCounts["a"].Should().Be(3562);
            hitCounts["b"].Should().Be(8438);
            
        }

        [Test]
        public void benchmark_epsilon()
        {
            EpsilonGreedy.Random = new Random(10);

            var p = new EpsilonGreedy(new[] { "a", "b" }, null, new LinearEpsilonValueCalculator());

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
            var changeTimingsAt = 6000;

            var sw = Stopwatch.StartNew();
            for (var i = 0; i < iterations; i++)
            {
                if ((i != 0) && (i % 100) == 0)
                {
                    p.PerformEpsilonGreedyDecay(null);
                }
                var hostR = p.Get() as EpsilonHostPoolResponse;
                var host = hostR.Host;
                hitCounts[host]++;
                var timing = timings[host];
                hostR.Ended = hostR.Started.Add(TimeSpan.FromMilliseconds(timing));
                hostR.Mark(null);

                if( changeTimingsAt == i )
                {
                    timings["a"] = 500;
                    timings["b"] = 100;
                }
            }
            sw.Stop();

            //TOTAL TIME: 85 milliseconds
            Console.WriteLine($"TOTAL TIME: {sw.Elapsed.Humanize()}");

            foreach (var host in hitCounts)
            {
                Console.WriteLine($"Host {host.Key} hit {host.Value} times {((double)host.Value / iterations):P}");
            }

        }

        [Test]
        public void bechmark_round_robin()
        {
            
            var p = new RoundRobinHostPool(new[] { "a", "b", "c" });

            var hitCounts = new Dictionary<string, long>()
                {
                    {"a", 0},
                    {"b", 0},
                    {"c", 0}
                };

            var iterations = 12000;

            var sw = Stopwatch.StartNew();
            for ( var x = 0; x < iterations; x++ )
            {
                var h = p.Get();
                hitCounts[h.Host]++;
                h.Mark(null);
            }
            sw.Stop();

            foreach (var host in hitCounts)
            {
                Console.WriteLine($"Host {host.Key} hit {host.Value} times {((double)host.Value / iterations):P}");
            }

            //TOTAL TIME: 17 milliseconds
            Console.WriteLine($"TOTAL TIME: {sw.Elapsed.Humanize()}");

        }
    }

}