using System;
using FluentAssertions;
using Newtonsoft.Json.Linq;
using NUnit.Framework;
using RethinkDb.Driver.Net;
using System.Text;
using RethinkDb.Driver.Ast;
using Templates.Utils;
using Z.ExtensionMethods;

namespace RethinkDb.Driver.Tests
{
    [TestFixture]
    public class ConnectionTest
    {
        private const string DbName = "CSharpDriverTests";
        private const string TableName = "TableA";

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
            EnsureConnection();
            //try
            //{
            //    r.dbCreate(DbName).run(conn);

            //    r.db(DbName).wait_().run(conn);
            //    r.db(DbName).tableCreate(TableName).run(conn);
            //    r.db(DbName).table(TableName).wait_().run(conn);
            //}
            //catch
            //{
            //}
        }

        [TestFixtureTearDown]
        public void AfterRunningTestSession()
        {
            //try
            //{
            //    r.db(DbName).tableDrop(TableName).run(conn);
            //    r.dbDrop(DbName).run(conn);
            //    conn.close();
            //}
            //catch { }
        }

        [Test]
        [Explicit]
        public void create_table()
        {
            r.dbCreate(DbName).run(conn);

            r.db(DbName).wait_().run(conn);
            r.db(DbName).tableCreate(TableName).run(conn);
            r.db(DbName).table(TableName).wait_().run(conn);
        }

        [Test]
        [Explicit]
        public void delete_table()
        {
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

            var result = r.random(1, 9).add(r.random(1, 9)).run<int>(c);
            Console.WriteLine(result);
            result.Should().BeGreaterOrEqualTo(2).And.BeLessThan(18);
        }

        [Test]
        public void test_booleans()
        {
            bool t = r.expr(true).run<bool>(conn);
            t.Should().Be(true);
        }

        [Test]
        public void test_time_pesudo_type()
        {
            var t = r.now().run<DateTimeOffset>(conn);
            //ten minute limit for clock drift.
            t.Should().BeCloseTo(DateTimeOffset.UtcNow, 600000);
        }

        [Test]
        public void test_datetime()
        {
            var date = DateTime.Now;
            var result = r.expr(date).run<DateTime>(conn);
            //(date - result).Dump();
            //result.Should().Be(date);
            result.Should().BeCloseTo(date, 1); // must be within 1ms of each other
        }

        [Test]
        public void test_jvalue()
        {
            var t = r.now().run<JValue>(conn);
            //ten minute limit for clock drift.
            t.Dump();
        }

        [Test]
        public void test_date_time_conversion()
        {
            var dt = GeneratedTest.datetime.fromtimestamp(896571240L, GeneratedTest.ast.rqlTzinfo("00:00"));
            dt.Dump();

            var dt2 = GeneratedTest.datetime.fromtimestamp(1375147296.681, GeneratedTest.ast.rqlTzinfo("-07:00"));
            dt2.Dump();
        }

        [Test]
        public void insert_test()
        {
            var obj = new Foo { id = "abc", Bar = 1, Jam = 2};
            var result = r.db(DbName).table(TableName).insert(obj).run(conn);
            result.Dump();
        }

        [Test]
        public void get_test()
        {
            var foo = r.db(DbName).table(TableName).get("abc").run<Foo>(conn);
            foo.Dump();
        }
        
    }

    public class Foo
    {
        public string id { get; set; }
        public int Bar { get; set; }
        public int Jam { get; set; }
    }

}