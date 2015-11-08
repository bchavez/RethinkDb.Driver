using System;
using System.Linq;
using FluentAssertions;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using NUnit.Framework;
using RethinkDb.Driver.Model;
using RethinkDb.Driver.Net;
using RethinkDb.Driver.Tests.Utils;

namespace RethinkDb.Driver.Tests
{
    public class Foo
    {
        [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
        public string id { get; set; }

        public int Bar { get; set; }
        public int Baz { get; set; }
        public string Idx { get; set; }
        public DateTimeOffset? Tim { get; set; }
    }

    [TestFixture]
    public class Examples : QueryTest
    {
        [Test]
        public void test_booleans()
        {
            bool t = r.expr(true).run<bool>(conn);
            t.Should().Be(true);
        }

        [Test]
        public void test_time_pesudo_type()
        {
            DateTimeOffset t = r.now().run<DateTimeOffset>(conn);
            //ten minute limit for clock drift.
            t.Should().BeCloseTo(DateTimeOffset.UtcNow, 600000);
        }

        [Test]
        public void test_datetime()
        {
            var date = DateTime.Now;
            DateTime result = r.expr(date).run<DateTime>(conn);
            //(date - result).Dump();
            //result.Should().Be(date);
            result.Should().BeCloseTo(date, 1); // must be within 1ms of each other
        }

        [Test]
        public void test_jvalue()
        {
            JValue t = r.now().run<JValue>(conn);
            //ten minute limit for clock drift.
            t.Dump();
        }

        [Test]
        public void insert_test_without_id()
        {
            var obj = new Foo { Bar = 1, Baz = 2, Tim = DateTimeOffset.Now };
            Result result = r.db(DbName).table(TableName).insert(obj).run<Result>(conn);
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
            Result result = r.db(DbName).table(TableName).insert(arr).run<Result>(conn);
            result.Dump();
        }

        [Test]
        public void get_test()
        {
            Foo foo = r.db(DbName).table(TableName).get("a").run<Foo>(conn);
            foo.Dump();
        }

        [Test]
        public void get_with_time()
        {
            Foo foo = r.db(DbName).table(TableName).get("4d4ba69e-048c-43b7-b842-c7b49dc6691c")
                .run<Foo>(conn);

            foo.Dump();
        }

        [Test]
        public void getall_test()
        {
            Cursor<Foo> all = r.db(DbName).table(TableName).getAll("a", "b", "c").run<Foo>(conn);

            all.BufferedItems.Dump();

            foreach (var foo in all)
            {
                Console.WriteLine($"Printing: {foo.id}!");
                foo.Dump();
            }
        }

        [Test]
        public void use_a_cursor_to_get_items()
        {
            Cursor<Foo> all = r.db(DbName).table(TableName).getAll("a", "b", "c").runCursor<Foo>(conn);

            foreach (var foo in all)
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
            foreach (var baz in bazInOrder)
            {
                Console.WriteLine(baz);
            }
        }

        [Test]
        public void getall_using_an_index_with_optarg_indexer()
        {
            const string IndexName = "Idx";

            DropTable(DbName, TableName);
            CreateTable(DbName, TableName);

            r.db(DbName)
                .table(TableName)
                .indexCreate(IndexName).run(conn);

            r.db(DbName)
                .table(TableName)
                .indexWait(IndexName).run(conn);

            var foos = new[]
                {
                    new Foo {id = "a", Baz = 1, Bar = 1, Idx = "qux"},
                    new Foo {id = "b", Baz = 2, Bar = 2, Idx = "bub"},
                    new Foo {id = "c", Baz = 3, Bar = 3, Idx = "qux"}
                };

            r.db(DbName).table(TableName).insert(foos).run(conn);

            Cursor<Foo> all = r.db(DbName).table(TableName)
                .getAll("qux")[new { index = "Idx" }]
                .run<Foo>(conn);

            var results = all.ToArray();

            var onlyQux = foos.Where(f => f.Idx == "qux");

            results.ShouldAllBeEquivalentTo(onlyQux);
        }

        [Test]
        public void getfield_expression_test()
        {
            r.db(DbName).table(TableName).delete().run(conn);
            var arr = new[]
                {
                    new Foo {id = "a", Baz = 1, Bar = 1, Tim = DateTimeOffset.Now},
                    new Foo {id = "b", Baz = 2, Bar = 2, Tim = DateTimeOffset.Now},
                    new Foo {id = "c", Baz = 3, Bar = 3, Tim = DateTimeOffset.Now}
                };
            Result result = r.db(DbName).table(TableName).insert(arr).run<Result>(conn);
            result.Dump();
            result.Inserted.Should().Be(3);

            long bazInFooC = r.db(DbName).table(TableName).get("c")["Baz"].run(conn);
            bazInFooC.Should().Be(3);
        }

        [Test]
        public void test_overloading()
        {
            r.db(DbName).table(TableName).delete().run(conn);
            var arr = new[]
                {
                    new Foo {id = "a", Baz = 1, Bar = 1, Tim = DateTimeOffset.Now},
                    new Foo {id = "b", Baz = 2, Bar = 2, Tim = DateTimeOffset.Now},
                    new Foo {id = "c", Baz = 3, Bar = 3, Tim = DateTimeOffset.Now}
                };
            Result result = r.db(DbName).table(TableName).insert(arr).run<Result>(conn);
            result.Dump();
            result.Inserted.Should().Be(3);

            var expA = r.db(DbName).table(TableName).get("a")["Baz"];
            var expB = r.db(DbName).table(TableName).get("b")["Bar"];

            int add = (expA + expB + 1).run<int>(conn);
            add.Should().Be(4);
        }

        [Test]
        public void test_implicit_operator_overload()
        {
            long x = (r.expr(1) + 1).run(conn); //everything between () actually gets executed on the server
            x.Should().Be(2);
        }

        [Test]
        public void test_loop()
        {
            Cursor<int> result = r.range(1, 4).runCursor<int>(conn);

            foreach (var i in result)
            {
                Console.WriteLine(i);
            }
        }

        public class Avatar
        {
            public string id { get; set; }
            public byte[] ImageData { get; set; }
        }

        [Test]
        public void insert_some_binary_data()
        {
            var data = Enumerable.Range(0, 256)
                .Select(i => Convert.ToByte(i))
                .ToArray();

            var avatar = new Avatar
                {
                    id = "myavatar",
                    ImageData = data
                };

            r.db(DbName).table(TableName)
                .insert(avatar).run(conn);


            Avatar fromDb = r.db(DbName).table(TableName)
                .get("myavatar").run<Avatar>(conn);


            fromDb.id.Should().Be(avatar.id);
            fromDb.ImageData.Should().Equal(data);
        }

        [Test]
        public void insert_some_binary_the_java_way()
        {
            var data = Enumerable.Range(0, 256)
                .Select(Convert.ToByte)
                .ToArray();

            var myObject = new MapObject()
                {
                    {"id", "javabin"},
                    {"the_data", r.binary(data)}
                };

            r.db(DbName).table(TableName)
                .insert(myObject).run(conn);

            var result = r.db(DbName).table(TableName)
                .get("javabin").run(conn);

            ExtensionsForTesting.Dump(result.the_data);

        }

    }

}