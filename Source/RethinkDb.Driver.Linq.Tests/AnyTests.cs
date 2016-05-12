using System.Collections.Generic;
using System.Linq;
using Xunit;

namespace RethinkDb.Driver.Linq.Tests
{
    public class AnyTests : BaseLinqTest
    {
        [Fact]
        public void ForSimpleAnyQuery_GeneratesCorrectReql()
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

            var expected = RethinkDB.R.Table( TableName ).Count();

            var result = GetQueryable<TestObject>( TableName, expected ).Any();

            Assert.True( result );
        }

        [Fact]
        public void ForSimpleAnyQueryWithCondition_GeneratesCorrectReql()
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

            var expected = RethinkDB.R.Table( TableName ).Filter( x => x["Name"].Eq( "TestObject1" ) ).Count();

            var result = GetQueryable<TestObject>( TableName, expected ).Any( x => x.Name == "TestObject1" );

            Assert.True( result );
        }

        [Fact]
        public void WhenIsFilteredByAnyClauseAndNot_GeneratesCorrectReql()
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
                    Locations = new List<string>()
                }
            };

            SpawnData( data );

            var expected = RethinkDB.R.Table( TableName )
                .Filter( x => x["Locations"].Count().Gt( 0 ).Not() );

            var queryable = GetQueryable<TestObject>( TableName, expected );

            var result = queryable
                .Where( x => !x.Locations.Any() )
                .ToList();

            Assert.Equal( 1, result.Count );
        }

        [Fact]
        public void WhenIsFilteredByAnyClauseWithSubAny_GeneratesCorrectReql()
        {
            var data = new List<TestObject>
            {
                new TestObject
                {
                    Name = "TestObject1",
                    Locations = new List<string> {"Hello"}
                },
                new TestObject
                {
                    Name = "TestObject2",
                    Resources = new List<Resource>
                    {
                        new Resource
                        {
                            Locations = new List<Location>
                            {
                                new Location
                                {
                                    Usages = new List<string>
                                    {
                                        "First usage"
                                    }
                                }
                            }
                        }
                    }
                }
            };

            SpawnData( data );

            var expected = RethinkDB.R.Table( TableName )
                .Filter( x => x["Resources"].Filter( resource => resource["Locations"].Filter( location => location["Usages"].Count().Gt( 0 ) ).Count().Gt( 0 ) ).Count().Gt( 0 ) );

            var queryable = GetQueryable<TestObject>( TableName, expected );

            var result = queryable
                .Where( x => x.Resources.Any( resource => resource.Locations.Any( l => l.Usages.Any() ) ) )
                .ToList();

            Assert.Equal( 1, result.Count );
        }

        [Fact]
        public void WhenIsFilteredByAnyClauseAndEqualsFalse_GeneratesCorrectReql()
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
                    Locations = new List<string>()
                }
            };

            SpawnData( data );

            var expected = RethinkDB.R.Table( TableName )
                .Filter( x => x["Locations"].Count().Gt( 0 ).Eq( false ) );

            var queryable = GetQueryable<TestObject>( TableName, expected );

            var result = queryable
                .Where( x => x.Locations.Any() == false )
                .ToList();

            Assert.Equal( 1, result.Count );
            Assert.Equal( 0, result[0].Locations.Count );
        }


        [Fact]
        public void WhenIsFilteredByAnyClause_GeneratesCorrectReql()
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
                    Locations = new List<string>()
                }
            };

            SpawnData( data );

            var expected = RethinkDB.R.Table( TableName )
                .Filter( x => x["Locations"].Count().Gt( 0 ) );

            var queryable = GetQueryable<TestObject>( TableName, expected );

            var result = queryable
                .Where( x => x.Locations.Any() )
                .ToList();

            Assert.Equal( 1, result.Count );
        }

        [Fact]
        public void WhenIsFilteredByAnyClauseWithFilter_GeneratesCorrectReql()
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
                    Locations = new List<string> {"Hello1" }
                }
            };

            SpawnData( data );

            var expected = RethinkDB.R.Table( TableName )
                .Filter( x => x["Locations"].Filter( l => l.Eq( "Hello" ) ).Count().Gt( 0 ) );

            var queryable = GetQueryable<TestObject>( TableName, expected );

            var result = queryable
                .Where( x => x.Locations.Any( l => l == "Hello" ) )
                .ToList();

            Assert.Equal( 1, result.Count );
        }

        public class TestObject
        {
            public string Name { get; set; }
            public List<string> Locations { get; set; }
            public List<Resource> Resources { get; set; }
        }
    }
}
