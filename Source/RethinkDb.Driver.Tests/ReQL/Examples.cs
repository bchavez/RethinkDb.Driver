using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml.XPath;
using FluentAssertions;
using Newtonsoft.Json;
using NUnit.Framework;
using RethinkDb.Driver.Model;
using RethinkDb.Driver.Net;
using RethinkDb.Driver.Tests.Utils;
using RethinkDb.Driver.Ast;

namespace RethinkDb.Driver.Tests.ReQL
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
    public class Examples : QueryTestFixture
    {
        private void InsertSomePocos()
        {
            var arr = new[]
                {
                    new Foo {id = "a", Baz = 1, Bar = 1},
                    new Foo {id = "b", Baz = 2, Bar = 2},
                    new Foo {id = "c", Baz = 3, Bar = 3}
                };
            Result result = R.Db(DbName).Table(TableName).Insert(arr).Run<Result>(conn);
            result.AssertInserted(3);
        }

        public override void BeforeEachTest()
        {
            base.BeforeEachTest();
            ClearTable();
        }

        private void ClearTable()
        {
            ClearTable(DbName, TableName);
        }

        [Test]
        public void test_booleans()
        {
            bool t = R.Expr(true).Run<bool>(conn);
            t.Should().Be(true);
        }

        [Test]
        public void insert_test_without_id()
        {
            var obj = new Foo { Bar = 1, Baz = 2, Tim = DateTimeOffset.Now };
            Result result = R.Db(DbName).Table(TableName).Insert(obj).Run<Result>(conn);
            result.Dump();
        }

        [Test]
        public void insert_an_array_of_pocos()
        {
            InsertSomePocos();
        }

        [Test]
        public void get_test()
        {
            InsertSomePocos();
            Foo foo = R.Db(DbName).Table(TableName).Get("a").Run<Foo>(conn);
            foo.Bar.Should().Be(1);
            foo.Baz.Should().Be(1);
        }


        [Test]
        public void dynamic_getall_test()
        {
            InsertSomePocos();
            Cursor<Foo> all = R.Db(DbName).Table(TableName).GetAll("a", "b", "c").Run<Foo>(conn);

            var items = all.ToList();
            items.Count.Should().Be(3);

            foreach (var foo in items)
            {
                Console.WriteLine($"Printing: {foo.id}!");
                foo.Dump();
            }
        }

        [Test]
        public void runcursor_use_a_cursor_to_get_items()
        {
            InsertSomePocos();
            Cursor<Foo> all = R.Db(DbName).Table(TableName).GetAll("a", "b", "c").RunCursor<Foo>(conn);

            var items = new List<Foo>();
            foreach (var foo in all)
            {
                Console.WriteLine($"Printing: {foo.id}!");
                foo.Dump();
                items.Add(foo);
            }
            items.Select(f => f.id).ToArray().Should().BeEquivalentTo("a", "b", "c");
        }

        [Test]
        public void runcursor_getall_and_linq()
        {
            InsertSomePocos();
            Cursor<Foo> all = R.Db(DbName).Table(TableName).GetAll("a", "b", "c").RunCursor<Foo>(conn);

            var bazInOrder = all.OrderByDescending(f => f.Baz)
                .Select(f => f.Baz);

            var items = new List<int>();
            foreach (var baz in bazInOrder)
            {
                Console.WriteLine(baz);
                items.Add(baz);
            }

            items.Should().BeInDescendingOrder().And.BeEquivalentTo(3,2,1);
        }
        
        [Test]
        public void getall_using_an_index_with_optarg_indexer()
        {
            const string IndexName = "Idx";

            R.Db(DbName)
                .Table(TableName)
                .IndexCreate(IndexName).Run(conn);

            R.Db(DbName)
                .Table(TableName)
                .IndexWait(IndexName).Run(conn);

            var foos = new[]
                {
                    new Foo {id = "a", Baz = 1, Bar = 1, Idx = "qux"},
                    new Foo {id = "b", Baz = 2, Bar = 2, Idx = "bub"},
                    new Foo {id = "c", Baz = 3, Bar = 3, Idx = "qux"}
                };

            R.Db(DbName).Table(TableName).Insert(foos).Run(conn);

            Cursor<Foo> all = R.Db(DbName).Table(TableName)
                .GetAll("qux")[new { index = "Idx" }]
                .Run<Foo>(conn);

            var results = all.ToArray();

            var onlyQux = foos.Where(f => f.Idx == "qux");

            results.ShouldAllBeEquivalentTo(onlyQux);
        }

        [Test]
        public void getfield_expression_test()
        {
            R.Db(DbName).Table(TableName).Delete().Run(conn);
            var arr = new[]
                {
                    new Foo {id = "a", Baz = 1, Bar = 1, Tim = DateTimeOffset.Now},
                    new Foo {id = "b", Baz = 2, Bar = 2, Tim = DateTimeOffset.Now},
                    new Foo {id = "c", Baz = 3, Bar = 3, Tim = DateTimeOffset.Now}
                };
            Result result = R.Db(DbName).Table(TableName).Insert(arr).Run<Result>(conn);
            result.Dump();
            result.Inserted.Should().Be(3);

            long bazInFooC = R.Db(DbName).Table(TableName).Get("c")["Baz"].Run(conn);
            bazInFooC.Should().Be(3);
        }

        [Test]
        public void test_overloading()
        {
            R.Db(DbName).Table(TableName).Delete().Run(conn);
            var arr = new[]
                {
                    new Foo {id = "a", Baz = 1, Bar = 1, Tim = DateTimeOffset.Now},
                    new Foo {id = "b", Baz = 2, Bar = 2, Tim = DateTimeOffset.Now},
                    new Foo {id = "c", Baz = 3, Bar = 3, Tim = DateTimeOffset.Now}
                };
            Result result = R.Db(DbName).Table(TableName).Insert(arr).Run<Result>(conn);
            result.Dump();
            result.Inserted.Should().Be(3);

            var expA = R.Db(DbName).Table(TableName).Get("a")["Baz"];
            var expB = R.Db(DbName).Table(TableName).Get("b")["Bar"];

            int add = (expA + expB + 1).Run<int>(conn);
            add.Should().Be(4);
        }

        [Test]
        public void test_implicit_operator_overload()
        {
            long x = (R.Expr(1) + 1).Run(conn); //everything between () actually gets executed on the server
            x.Should().Be(2);
        }

        [Test]
        public void test_loop()
        {
            Cursor<int> result = R.Range(1, 4).RunCursor<int>(conn);

            foreach (var i in result)
            {
                Console.WriteLine(i);
            }
        }

        public class Avatar
        {
            [JsonProperty("id")]
            public string Id { get; set; }
            public byte[] ImageData { get; set; }
        }

        [Test]
        public void insert_some_binary_data()
        {
            var data = Enumerable.Range(0, 256)
                .Select(Convert.ToByte)
                .ToArray();

            var avatar = new Avatar
                {
                    Id = "myavatar",
                    ImageData = data,
                };


            R.Db(DbName).Table(TableName).Delete().Run(conn);

            R.Db(DbName).Table(TableName)
                .Insert(avatar).Run(conn);


            Avatar fromDb = R.Db(DbName).Table(TableName)
                .Get("myavatar").Run<Avatar>(conn);


            fromDb.Id.Should().Be(avatar.Id);
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
                    {"the_data", R.Binary(data)}
                };

            R.Db(DbName).Table(TableName)
                .Insert(myObject).Run(conn);

            var result = R.Db(DbName).Table(TableName)
                .Get("javabin").Run(conn);

            ExtensionsForTesting.Dump(result.the_data);
        }

        [Test]
        public void server_info()
        {
            var server = conn.Server();

            server.Id.Should().NotBeNullOrWhiteSpace();
            if( server.Proxy )
            {
                server.Name.Should().BeNull();
            }
            else
            {
                server.Name.Should().NotBeNullOrWhiteSpace();
            }
        }

        [Test]
        public void check_if_table_exists()
        {
            var result = R.Db(DbName).TableList().RunAtom<List<string>>(conn);

            if( result.Contains("test") )
            {
                //exists
            }
            else
            {
                //doesnt exist
            }

            var newTableResult = R.Db(DbName).TableList().Contains("newTable")
                .Do_(tableExists =>
                    {
                        return R.Branch(
                            tableExists, /* The test */
                            new {tables_created = 0}, /* If False */
                            R.Db(DbName).TableCreate("newTable") /* If true */
                            );
                    }).RunWrite(conn);

            newTableResult.Dump();
        }

        [Test]
        public void test_dynamically_generated_query()
        {
            // The query should only return documents with any of the following colors
            var colorsToSearchFor = new[] { "0BLK", "1WHT" };

            // Our fake data set with 2 matching docs and 2 non-matching docs      
            var results = R.Expr(new[]
                {
                    new Product() {ColorCode = "4NAV"},
                    new Product() {ColorCode = "1WHT"},
                    new Product() {ColorCode = "0BLK"},
                    new Product() {ColorCode = "3PRP"}
                })
                .Filter(doc => BuildDynamicFilter(doc, colorsToSearchFor))
                .RunAtom<List<Product>>(conn);

            // Results should be ["0BLK", "1WHT"]
            results.Count.Should().Be(2);
            results.Select(x => x.ColorCode).Should().NotContain("4NAV");
            results.Select(x => x.ColorCode).Should().NotContain("3PRP");
            results.Dump();
        }

        private ReqlExpr BuildDynamicFilter(ReqlExpr expr, string[] colorSearch)
        {
            var statement = R.Or();
            foreach( var color in colorSearch )
            {
                statement = statement.Or(expr[nameof(Product.ColorCode)].Eq(color));
            }
            return statement;
        }

        private class Product
        {
            public string ColorCode;
        }

    }


}
