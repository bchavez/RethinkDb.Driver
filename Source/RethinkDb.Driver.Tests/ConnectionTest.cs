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
                .connect();

            var res = r.random(1, 2).add(r.random(1, 2)).run(c);
        }
        
    }

}