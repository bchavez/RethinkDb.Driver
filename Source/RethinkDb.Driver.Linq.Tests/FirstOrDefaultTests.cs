using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;

namespace RethinkDb.Driver.Linq.Tests
{
    public class FirstOrDefaultTests : BaseLinqTest
    {
        [Test]
        public void FirstOrDefaultWithNoFilter_GeneratesCorrectQuery()
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

            var result = GetQueryable<TestObject>( TableName, expected ).FirstOrDefault();
        }

        [Test]
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

            var result = GetQueryable<TestObject>( TableName, expected ).FirstOrDefault( x => x.Name == "TestObject2" );

            Assert.AreEqual( "TestObject2", result.Name );
        }


        [Test]
        public void WhenIsFilteredByFirstOrDefaultClause_GeneratesCorrectReql()
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
                            Locations = new List<Location>
                            {
                                new Location()
                            }
                        }
                    }
                }
            };

            SpawnData( data );

            var expected = RethinkDB.R.Table( TableName )
                .Filter( x => x["Resources"].Nth( 0 ).Default_( (object)null ).Eq( null ).Not() );

            var queryable = GetQueryable<TestObject>( TableName, expected );

            var result = queryable
                .Where( x => x.Resources.FirstOrDefault() != null )
                .ToList();

            Assert.AreEqual( 1, result.Count );
        }

        [Test]
        public void FirstWithFilterAndNoMatches_ReturnsNull()
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

            var result = GetQueryable<TestObject>( TableName, expected ).FirstOrDefault( x => x.Name == "TestObject3" );

            Assert.Null( result );
        }

        [Test]
        public void WhenIsFilteredByFirstClause_GeneratesCorrectReql()
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
                    Locations = new List<string> {"Hello2"}
                }
            };

            SpawnData( data );

            var expected = RethinkDB.R.Table( TableName )
                .Filter( x => x["Locations"].Nth( 0 ).Eq( "Hello" ) );

            var queryable = GetQueryable<TestObject>( TableName, expected );

            var result = queryable
                .Where( x => x.Locations.First() == "Hello" )
                .ToList();

            Assert.AreEqual( 1, result.Count );
            Assert.AreEqual( 1, result[0].Locations.Count );
        }

        [Test]
        public void WhenIsFilteredByFirstClauseAndThenUsingSubProperty_GeneratesCorrectReql()
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
                            Locations = new List<Location>
                            {
                                new Location()
                            }
                        }
                    }
                }
            };

            SpawnData( data );

            var expected = RethinkDB.R.Table( TableName )
                .Filter( x => x["Resources"].Nth( 0 )["Locations"].Count().Gt( 0 ) );

            var queryable = GetQueryable<TestObject>( TableName, expected );

            var result = queryable
                .Where( x => x.Resources.First().Locations.Any() )
                .ToList();

            Assert.AreEqual( 1, result.Count );
        }

        [Test]
        public void WhenIsFilteredByFirstClauseAndThenUsingSubPropertyComplex_GeneratesCorrectReql()
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
                .Filter( x => x["Resources"].Nth( 0 )["Locations"].Nth( 0 )["Usages"].Filter( u => u.Eq( "Main" ) ).Count().Gt( 0 ) );

            var queryable = GetQueryable<TestObject>( TableName, expected );

            var result = queryable
                .Where( x => x.Resources.First().Locations.First().Usages.Any( u => u == "Main" ) )
                .ToList();

            Assert.AreEqual( 1, result.Count );
        }

        public class TestObject
        {
            public string Name { get; set; }
            public List<Resource> Resources { get; set; }
            public List<string> Locations { get; set; }
        }
    }
}
