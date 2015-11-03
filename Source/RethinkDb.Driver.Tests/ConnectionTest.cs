using System;
using System.Collections.Generic;
using System.Linq;
using FluentAssertions;
using Newtonsoft.Json.Linq;
using NUnit.Framework;
using RethinkDb.Driver.Net;
using System.Text;
using Newtonsoft.Json;
using RethinkDb.Driver.Ast;
using RethinkDb.Driver.Model;
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

            int result = r.random(1, 9).add(r.random(1, 9)).run<int>(c);
            Console.WriteLine(result);
            result.Should().BeGreaterOrEqualTo(2).And.BeLessThan(18);
        }

        [Test]
        public void test_timezone()
        {
            var val = r.epochTime(1.4444445).toIso8601().run(conn);
            Console.WriteLine(val.GetType());
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
            var dt = TestingCommon.datetime.fromtimestamp(896571240L, TestingCommon.ast.rqlTzinfo("00:00"));
            dt.Dump();

            var dt2 = TestingCommon.datetime.fromtimestamp(1375147296.681, TestingCommon.ast.rqlTzinfo("-07:00"));
            dt2.Dump();
        }

        [Test]
        public void insert_test_without_id()
        {
            var obj = new Foo {Bar = 1, Baz = 2, Tim = DateTimeOffset.Now};
            Result result = r.db(DbName).table(TableName).insert(obj).run(conn);
            result.Dump();
        }

        [Test]
        public void insert_an_array_of_pocos()
        {
            var arr = new[]
                {
                    new Foo {id = "a", Baz = 1, Bar = 1},
                    new Foo {id = "b", Baz = 2, Bar = 2},
                    new Foo {id = "c", Baz = 3, Bar = 3}
                };
            var result = r.db(DbName).table(TableName).insert(arr).run(conn);
            result.Dump();
        }

        [Test]
        public void get_test()
        {
            var foo = r.db(DbName).table(TableName).get("a").run<Foo>(conn);
            foo.Dump();
        }

        [Test]
        public void get_with_time()
        {
            Foo foo = r.db(DbName).table(TableName).get("4d4ba69e-048c-43b7-b842-c7b49dc6691c")
                .run<Foo>(conn);

            foo.Tim.Dump();
        }

        [Test]
        public void getall_test()
        {
            Cursor<Foo> all = r.db(DbName).table(TableName).getAll("a", "b", "c").run<Foo>(conn);

            all.BufferedItems.Dump();

            foreach( var foo in all )
            {
                Console.WriteLine($"Printing: {foo.id}!");
                foo.Dump();
            }
        }

        [Test]
        public void use_a_cursor_to_get_items()
        {
            Cursor<Foo> all = r.db(DbName).table(TableName).getAll("a", "b", "c").runCursor<Foo>(conn);

            foreach( var foo in all )
            {
                Console.WriteLine($"Printing: {foo.id}!");
                foo.Dump();
            }
        }

        [Test]
        public void getall_with_linq()
        {
            Cursor<Foo> all = r.db(DbName).table(TableName).getAll("a", "b", "c").runCursor<Foo>(conn);

            var bazInOrder = all.OrderByDescending(f => f.Baz)
                .Select(f => f.Baz);
            foreach( var baz in bazInOrder )
            {
                Console.WriteLine(baz);
            }
        }

        [Test]
        public void getall_indexer_opt_args()
        {
            var all = r.db(DbName).table(TableName)
                .getAll("foo")[new {index = "foo"}]
                .run<Foo>(conn);
        }
    }

    public class Foo
    {
        [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
        public string id { get; set; }

        public int Bar { get; set; }
        public int Baz { get; set; }
        public DateTimeOffset Tim { get; set; }
    }
}