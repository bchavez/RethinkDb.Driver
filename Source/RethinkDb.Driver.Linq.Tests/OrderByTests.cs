using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using RethinkDb.Driver.Linq.Attributes;

namespace RethinkDb.Driver.Linq.Tests
{
    public class OrderByTests : BaseLinqTest
    {
        [Test]
        public void For1OrderBy_GeneratesCorrectReql()
        {
            var data = new List<TestObject>
            {
                new TestObject
                {
                    Name = "TestObject1",
                    Name2 = "1"
                },
                new TestObject
                {
                    Name = "TestObject2",
                    Name2 = "2"
                }
            };

            SpawnData( data );

            var expected = RethinkDB.R.Table( TableName ).OrderBy( "Name" );

            var queryable = GetQueryable<TestObject>( TableName, expected );

            var result = queryable
                .OrderBy( x => x.Name )
                .ToList();

            Assert.AreEqual( 2, result.Count );
            Assert.AreEqual( data[0].Name, result[0].Name );
            Assert.AreEqual( data[1].Name, result[1].Name );
        }

        [Test]
        public void For2OrderByDescending_GeneratesCorrectReql()
        {
            var data = new List<TestObject>
            {
                new TestObject
                {
                    Name = "TestObject1",
                    Name2 = "Name1"
                },
                new TestObject
                {
                    Name = "TestObject2",
                    Name2 = "Name2"
                }
            };

            SpawnData( data );

            var expected = RethinkDB.R.Table( TableName ).OrderBy( RethinkDB.R.Desc("Name") );

            var queryable = GetQueryable<TestObject>( TableName, expected );

            var result = queryable
                .OrderByDescending( x => x.Name )
                .ToList();

            Assert.AreEqual( 2, result.Count );
            Assert.AreEqual( data[1].Name, result[0].Name );
            Assert.AreEqual( data[0].Name, result[1].Name );
        }

        [Test]
        public void ForOrderByOnPrimaryIndex_GeneratesCorrectReql()
        {
            var data = new List<TestObject>
            {
                new TestObject
                {
                    Name2 = "TestObject1"
                },
                new TestObject
                {
                    Name2 = "TestObject2"
                }
            };

            SpawnData( data );

            var expected = RethinkDB.R.Table( TableName ).OrderBy( "Name2" ).OptArg( "index", "Name2" );

            var queryable = GetQueryable<TestObject>( TableName, expected );

            var result = queryable
                .OrderBy( x => x.Name2 )
                .ToList();

            Assert.AreEqual( 2, result.Count );
            Assert.AreEqual( data[0].Name2, result[0].Name2 );
            Assert.AreEqual( data[1].Name2, result[1].Name2 );
        }

        [Test]
        public void ForOrderByOnSecondaryIndex_GeneratesCorrectReql()
        {
            var data = new List<TestObject>
            {
                new TestObject
                {
                    Name3 = "TestObject1",
                    Name2 = "1"
                },
                new TestObject
                {
                    Name3 = "TestObject2",
                    Name2 = "2"
                }
            };

            SpawnData( data );

            var expected = RethinkDB.R.Table( TableName ).OrderBy( "Name3" ).OptArg( "index", "Name3" );

            var queryable = GetQueryable<TestObject>( TableName, expected );

            var result = queryable
                .OrderBy( x => x.Name3 )
                .ToList();

            Assert.AreEqual( 2, result.Count );
            Assert.AreEqual( data[0].Name3, result[0].Name3 );
            Assert.AreEqual( data[1].Name3, result[1].Name3 );
        }

        public class TestObject
        {
            public string Name { get; set; }

            [PrimaryIndex]
            public string Name2 { get; set; }

            [SecondaryIndex]
            public string Name3 { get; set; }
        }
    }
}
