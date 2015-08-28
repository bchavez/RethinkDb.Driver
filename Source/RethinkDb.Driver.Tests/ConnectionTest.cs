using System;
using FluentAssertions;
using Newtonsoft.Json.Linq;
using NUnit.Framework;

namespace RethinkDb.Driver.Tests
{
    [TestFixture]
    public class ConnectionTest
    {
        public static RethinkDB r = RethinkDB.r;

        [TestFixtureSetUp]
        public void BeforeRunningTestSession()
        {

        }

        [Test]
        public void can_connect()
        {
            var c = r.connection()
                .hostname("192.168.0.11")
                .port(RethinkDBConstants.DEFAULT_PORT)
                .timeout(60)
                .connect();

            var result = r.random(1, 9).add(r.random(1, 9)).run<JValue>(c).ToObject<int>();
            Console.WriteLine(result);
            result.Should().BeGreaterThan(2).And.BeLessThan(18);
        }
        
    }

}