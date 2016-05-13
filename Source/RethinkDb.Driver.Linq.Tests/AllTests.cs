using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;

namespace RethinkDb.Driver.Linq.Tests
{
    public class AllTests : BaseLinqTest
    {
        [Test]
        public void ForAll_GeneratesCorrectReql()
        {
            var data = new List<TestObject>
            {
                new TestObject
                {
                    Name = "TestObject1"
                },
                new TestObject
                {
                    Name = "TestObject1"
                }
            };

            SpawnData( data );

            var expected = RethinkDB.R.Table( TableName ).Filter( x => x["Name"].Eq( "TestObject1" ).Not() ).Count();

            var result = GetQueryable<TestObject>( TableName, expected ).All( x => x.Name == "TestObject1" );

            Assert.True( result );
        }

        [Test]
        public void ForAllOnSubProperty_GeneratesCorrectReql()
        {
            var data = new List<TestObject>
            {
                new TestObject
                {
                    Information = new Information
                    {
                        Name = "TestObject1"
                    }
                },
                new TestObject
                {
                    Information = new Information
                    {
                        Name = "TestObject1"
                    }
                }
            };

            SpawnData( data );

            var expected = RethinkDB.R.Table( TableName ).Filter( x => x["Information"]["Name"].Eq( "TestObject1" ).Not() ).Count();

            var result = GetQueryable<TestObject>( TableName, expected ).All( x => x.Information.Name == "TestObject1" );

            Assert.True( result );
        }

        [Test]
        public void WhenIsFilteredByAllClause_GeneratesCorrectReql()
        {
            var data = new List<TestObject>
            {
                new TestObject
                {
                    Name = "TestObject1",
                    Locations = new List<string> {"Hello" }
                },
                new TestObject
                {
                    Name = "TestObject2",
                    Locations = new List<string> { "Hello2" }
                }
            };

            SpawnData( data );

            var expected = RethinkDB.R.Table( TableName )
                .Filter( x => x["Locations"].Filter( l => l.Eq( "Hello" ).Not() ).Count().Eq( 0 ) );

            var queryable = GetQueryable<TestObject>( TableName, expected );

            var result = queryable
                .Where( x => x.Locations.All( l => l == "Hello" ) )
                .ToList();

            Assert.AreEqual( 1, result.Count );
        }

        [Test]
        public void WhenIsFilteredByAllClauseNot_GeneratesCorrectReql()
        {
            var data = new List<TestObject>
            {
                new TestObject
                {
                    Name = "TestObject1",
                    Locations = new List<string> {"Hello" }
                },
                new TestObject
                {
                    Name = "TestObject2",
                    Locations = new List<string> { "Hello2" }
                }
            };

            SpawnData( data );

            var expected = RethinkDB.R.Table( TableName )
                .Filter( x => x["Locations"].Filter( l => l.Eq( "Hello" ).Not() ).Count().Eq( 0 ).Not() );

            var queryable = GetQueryable<TestObject>( TableName, expected );

            var result = queryable
                .Where( x => !x.Locations.All( l => l == "Hello" ) )
                .ToList();

            Assert.AreEqual( 1, result.Count );
        }

        public class TestObject
        {
            public string Name { get; set; }
            public Information Information { get; set; }
            public List<string> Locations { get; set; }
        }
    }

    public class Information
    {
        public string Name { get; set; }
    }
}
