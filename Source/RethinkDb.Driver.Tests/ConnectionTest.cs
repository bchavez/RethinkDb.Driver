using System;
using FluentAssertions;
using Newtonsoft.Json.Linq;
using NUnit.Framework;
using RethinkDb.Driver.Net;

namespace RethinkDb.Driver.Tests
{
    [TestFixture]
    public class ConnectionTest
    {
        private const string DbName = "CSharpDriverTests";
        private const string TableName = "ATable";

        public static RethinkDB r = RethinkDB.r;

        private Connection conn;

        private void EnsureConnection()
        {
            if( conn == null )
            {
                this.conn = r.connection()
                    .hostname("192.168.0.11")
                    .port(RethinkDBConstants.DEFAULT_PORT)
                    .connect();
            }
        }

        [TestFixtureSetUp]
        public void BeforeRunningTestSession()
        {

        }

        [Test]
        [Explicit]
        public void test_setup()
        {
            EnsureConnection();

            r.dbCreate(DbName).run(conn);

            r.db(DbName).wait_().run(conn);
            r.db(DbName).tableCreate(TableName).run(conn);
            r.db(DbName).table(TableName).wait_().run(conn);
        }

        [Test]
        [Explicit]
        public void test_check_teardown()
        {
            EnsureConnection();

            r.db(DbName).tableDrop(TableName).run(conn);
            r.dbDrop(DbName).run(conn);
            conn.close();
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
            result.Should().BeGreaterOrEqualTo(2).And.BeLessThan(18);
        }
        
    }

}