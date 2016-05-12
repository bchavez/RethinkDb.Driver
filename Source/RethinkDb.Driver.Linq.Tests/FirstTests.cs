using System;
using System.Collections.Generic;
using System.Linq;
using Xunit;

namespace RethinkDb.Driver.Linq.Tests
{
    public class FirstTests : BaseLinqTest
    {
        [Fact]
        public void FirstWithNoFilter_GeneratesCorrectQuery()
        {
            var data = new List<TestObject>
            {
                new TestObject
                {
                    Name = "TestObject1"
                },
                new TestObject
                {
                    Name = "TestObject2"
                }
            };

            SpawnData( data );

            var expected = RethinkDB.R.Table( TableName ).Nth( 0 );

            var result = GetQueryable<TestObject>( TableName, expected ).First();
        }

        [Fact]
        public void FirstWithFilter_GeneratesCorrectQuery()
        {
            var data = new List<TestObject>
            {
                new TestObject
                {
                    Name = "TestObject1"
                },
                new TestObject
                {
                    Name = "TestObject2"
                }
            };

            SpawnData( data );

            var expected = RethinkDB.R.Table( TableName ).Filter( x => x["Name"].Eq( "TestObject2" ) ).Nth( 0 );

            var result = GetQueryable<TestObject>( TableName, expected ).First( x => x.Name == "TestObject2" );

            Assert.Equal( "TestObject2", result.Name );
        }

        [Fact]
        public void FirstWithFilterAndNoMatches_ThrowExecpetion()
        {
            var data = new List<TestObject>
            {
                new TestObject
                {
                    Name = "TestObject1"
                },
                new TestObject
                {
                    Name = "TestObject2"
                }
            };

            SpawnData( data );

            var expected = RethinkDB.R.Table( TableName ).Filter( x => x["Name"].Eq( "TestObject3" ) ).Nth( 0 );

            Assert.Throws<InvalidOperationException>( () =>
            {
                var result = GetQueryable<TestObject>( TableName, expected ).First( x => x.Name == "TestObject3" );
            } );
        }
        [Fact]
        public void WhenIsFilteredByFirstWithFilter_GeneratesCorrectReql()
        {
            var data = new List<TestObject>
            {
                new TestObject
                {
                    Name = "TestObject2",
                    Resources = new List<Resource>
                    {
                        new Resource
                        {
                            Name = "First",
                            Locations = new List<Location>
                            {
                                new Location
                                {
                                    Usages = new List<string>
                                    {
                                        "Main"
                                    }
                                }
                            }
                        }
                    }
                }
            };

            SpawnData( data );

            var expected = RethinkDB.R.Table( TableName )
                .Filter( x => x["Resources"].Filter( r => r["Locations"].Filter( l => l["Usages"].Filter( u => u.Eq( "Main" ) ).Nth( 0 ).Default_( (object)null ).Eq( null ).Not() ).Count().Gt( 0 ) ).Nth( 0 )["Name"].Eq( "First" ) );

            var queryable = GetQueryable<TestObject>( TableName, expected );

            var result = queryable
                .Where( x => x.Resources.First( r => r.Locations.Any( l => l.Usages.FirstOrDefault( u => u == "Main" ) != null ) ).Name == "First" )
                .ToList();

            Assert.Equal( 1, result.Count );
        }

        public class TestObject
        {
            public string Name { get; set; }
            public List<Resource> Resources { get; set; }
        }
    }
}
