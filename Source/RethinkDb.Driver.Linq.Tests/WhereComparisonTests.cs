using System;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using RethinkDb.Driver.Linq.Attributes;


namespace RethinkDb.Driver.Linq.Tests
{
    public class WhereComparisonTests : BaseLinqTest
    {
        [Test]
        public void ForSimpleToList_DoesNotCallAnyReqlMethods()
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

            var expected = RethinkDB.R.Table( TableName );

            var result = GetQueryable<TestObject>( TableName, expected ).ToList();

            Assert.AreEqual( 2, result.Count );
            foreach( var testObject in data )
                Assert.True( result.Any( x => x.Name == testObject.Name ) );
        }

        [Test]
        public void ForSimpleEqualOnNonIndex_GeneratesCorrectReql()
        {
            var data = new List<TestObject>
            {
                new TestObject
                {
                    Name = "Hello"
                },
                new TestObject
                {
                    Name = "TestObject1"
                }
            };

            SpawnData( data );

            var expected = RethinkDB.R.Table( TableName ).Filter( x => x["Name"].Eq( "Hello" ) );

            var queryable = GetQueryable<TestObject>( TableName, expected );

            var result = queryable
                .Where( x => x.Name == "Hello" )
                .ToList();

            Assert.AreEqual( 1, result.Count );
            Assert.AreEqual( data[0].Name, result[0].Name );
        }

        [Test]
        public void ForSimpleNotEqualOnNonIndex_GeneratesCorrectReql()
        {
            var data = new List<TestObject>
            {
                new TestObject
                {
                    Name = "Hello"
                },
                new TestObject
                {
                    Name = "TestObject1"
                }
            };

            SpawnData( data );

            var expected = RethinkDB.R.Table( TableName ).Filter( x => x["Name"].Eq( "Hello" ).Not() );

            var queryable = GetQueryable<TestObject>( TableName, expected );

            var result = queryable
                .Where( x => x.Name != "Hello" )
                .ToList();

            Assert.AreEqual( 1, result.Count );
            Assert.AreEqual( data[1].Name, result[0].Name );
        }

        [Test]
        public void ForSimpleEqualOnPrimaryIndex_GeneratesCorrectReql()
        {
            var data = new List<TestObject>
            {
                new TestObject
                {
                    Id = Guid.NewGuid(),
                    Name = "TestObject1"
                },
                new TestObject
                {
                    Id = Guid.NewGuid(),
                    Name = "TestObject2"
                }
            };

            SpawnData( data );

            var expected = RethinkDB.R.Table( TableName ).Get( data[0].Id );

            var queryable = GetQueryable<TestObject>( TableName, expected );

            var result = queryable
                .Where( x => x.Id == data[0].Id )
                .ToList();

            Assert.AreEqual( 1, result.Count );
            Assert.AreEqual( data[0].Id, result[0].Id );
            Assert.AreEqual( data[0].Name, result[0].Name );
        }

        [Test]
        public void ForSimpleEqualOnPrimaryIndexWhenIndexIsOnRight_GeneratesCorrectReql()
        {
            var data = new List<TestObject>
            {
                new TestObject
                {
                    Id = Guid.NewGuid(),
                    Name = "TestObject1"
                },
                new TestObject
                {
                    Id = Guid.NewGuid(),
                    Name = "TestObject2"
                }
            };

            SpawnData( data );

            var expected = RethinkDB.R.Table( TableName ).Get( data[0].Id );

            var queryable = GetQueryable<TestObject>( TableName, expected );

            var result = queryable
                .Where( x => data[0].Id == x.Id )
                .ToList();

            Assert.AreEqual( 1, result.Count );
            Assert.AreEqual( data[0].Id, result[0].Id );
            Assert.AreEqual( data[0].Name, result[0].Name );
        }

        [Test]
        public void ForSimpleEqualOnSecondaryIndex_GeneratesCorrectReql()
        {
            var data = new List<TestObject>
            {
                new TestObject
                {
                    Name = "TestObject1",
                    Location = "LocationA"
                },
                new TestObject
                {
                    Name = "TestObject2",
                    Location = "LocationB"
                }
            };

            SpawnData( data );

            var expected = RethinkDB.R.Table( TableName ).GetAll( data[0].Location ).OptArg( "index", "Location" );

            var queryable = GetQueryable<TestObject>( TableName, expected );

            var result = queryable
                .Where( x => x.Location == data[0].Location )
                .ToList();

            Assert.AreEqual( 1, result.Count );
            Assert.AreEqual( data[0].Id, result[0].Id );
            Assert.AreEqual( data[0].Name, result[0].Name );
        }

        [Test]
        public void ForEqualWith1AndOnSecondaryIndex_GeneratesCorrectReql()
        {
            var data = new List<TestObject>
            {
                new TestObject
                {
                    Name = "TestObject1",
                    Location = "LocationA"
                },
                new TestObject
                {
                    Name = "TestObject2",
                    Location = "LocationA"
                },
                new TestObject
                {
                    Name = "TestObject3",
                    Location = "LocationB"
                }
            };

            SpawnData( data );

            var expected = RethinkDB.R.Table( TableName )
                .GetAll( data[0].Location )
                .OptArg( "index", "Location" )
                .Filter( x => x["Name"].Eq( data[0].Name ) );

            var queryable = GetQueryable<TestObject>( TableName, expected );

            var result = queryable
                .Where( x => x.Location == data[0].Location && x.Name == data[0].Name )
                .ToList();

            Assert.AreEqual( 1, result.Count );
            Assert.AreEqual( data[0].Id, result[0].Id );
            Assert.AreEqual( data[0].Name, result[0].Name );
        }

        [Test]
        public void ForEqualWith2AndOnSecondaryIndex_GeneratesCorrectReql()
        {
            var data = new List<TestObject>
            {
                new TestObject
                {
                    Name = "TestObject1",
                    Location = "LocationA"
                },
                new TestObject
                {
                    Name = "TestObject2",
                    Location = "LocationA"
                },
                new TestObject
                {
                    Name = "TestObject3",
                    Location = "LocationB"
                }
            };

            SpawnData( data );

            var expected = RethinkDB.R.Table( TableName )
                .GetAll( data[0].Location )
                .OptArg( "index", "Location" )
                .Filter( x => x["Name"].Eq( data[0].Name ) );
            expected = expected.Filter( x => x["Name2"].Eq( data[1].Name2 ) );

            var queryable = GetQueryable<TestObject>( TableName, expected );

            var result = queryable
                .Where( x =>
                    x.Location == data[0].Location &&
                    x.Name == data[0].Name &&
                    x.Name2 == data[0].Name2 )
                .ToList();

            Assert.AreEqual( 1, result.Count );
            Assert.AreEqual( data[0].Id, result[0].Id );
            Assert.AreEqual( data[0].Name, result[0].Name );
        }

        [Test]
        public void WhenIsFilteredByOnceColumnThenByIndex_DoesNotUseIndex()
        {
            var data = new List<TestObject>
            {
                new TestObject
                {
                    Name = "TestObject1",
                    Location = "LocationA"
                },
                new TestObject
                {
                    Name = "TestObject2",
                    Location = "LocationB"
                }
            };

            SpawnData( data );

            var expected = RethinkDB.R.Table( TableName )
                .Filter( x => x["Name"].Eq( "TestObject1" ) )
                .Filter( x => x["Location"].Eq( data[0].Location ) );

            var queryable = GetQueryable<TestObject>( TableName, expected );

            var result = queryable
                .Where( x => x.Name == "TestObject1" )
                .Where( x => x.Location == data[0].Location )
                .ToList();

            Assert.AreEqual( 1, result.Count );
            Assert.AreEqual( data[0].Id, result[0].Id );
            Assert.AreEqual( data[0].Name, result[0].Name );
        }

        [Test]
        public void WhenFilterIsLessThanAndMoreThanOnIndexedColumn_UsesIndex()
        {
            var data = new List<TestObject>
            {
                new TestObject
                {
                    Name = "TestObject1",
                    Score = 1
                },
                new TestObject
                {
                    Name = "TestObject2",
                    Score = 2
                },
                new TestObject
                {
                    Name = "TestObject3",
                    Score = 3
                },
                new TestObject
                {
                    Name = "TestObject4",
                    Score = 4
                }
            };

            SpawnData( data );

            var expected = RethinkDB.R.Table( TableName )
                .Between( 1, 4 ).OptArg( "index", "Score" ).OptArg( "left_bound", "open" );

            var queryable = GetQueryable<TestObject>( TableName, expected );

            var result = queryable
                .Where( x => x.Score < 4 && x.Score > 1 )
                .ToList();

            var expectedNames = new[]
            {
                "TestObject2",
                "TestObject3"
            };
            Assert.AreEqual( 2, result.Count );
            Assert.IsTrue( expectedNames.All( n => result.Any( x => x.Name == n ) ) );
        }

        [Test]
        public void WhenFilterIsLessThanAndMoreThanOrEqualToOnIndexedColumn_UsesIndex()
        {
            var data = new List<TestObject>
            {
                new TestObject
                {
                    Name = "TestObject1",
                    Score = 1
                },
                new TestObject
                {
                    Name = "TestObject2",
                    Score = 2
                },
                new TestObject
                {
                    Name = "TestObject3",
                    Score = 3
                },
                new TestObject
                {
                    Name = "TestObject4",
                    Score = 4
                }
            };

            SpawnData( data );

            var expected = RethinkDB.R.Table( TableName )
                .Between( 1, 4 ).OptArg( "index", "Score" );

            var queryable = GetQueryable<TestObject>( TableName, expected );

            var result = queryable
                .Where( x => x.Score < 4 && x.Score >= 1 )
                .ToList();


            var expectedNames = new[]
            {
                "TestObject1",
                "TestObject2",
                "TestObject3"
            };
            Assert.AreEqual( 3, result.Count );
            Assert.IsTrue( expectedNames.All( n => result.Any( x => x.Name == n ) ) );
        }

        [Test]
        public void WhenFilterIsLessThanOrEqualToAndMoreThanOnIndexedColumn_UsesIndex()
        {
            var data = new List<TestObject>
            {
                new TestObject
                {
                    Name = "TestObject1",
                    Score = 1
                },
                new TestObject
                {
                    Name = "TestObject2",
                    Score = 2
                },
                new TestObject
                {
                    Name = "TestObject3",
                    Score = 3
                },
                new TestObject
                {
                    Name = "TestObject4",
                    Score = 4
                }
            };

            SpawnData( data );

            var expected = RethinkDB.R.Table( TableName )
                .Between( 1, 4 )
                .OptArg( "index", "Score" )
                .OptArg( "left_bound", "open" )
                .OptArg( "right_bound", "closed" );

            var queryable = GetQueryable<TestObject>( TableName, expected );

            var result = queryable
                .Where( x => x.Score <= 4 && x.Score > 1 )
                .ToList();

            var expectedNames = new[]
            {
                "TestObject2",
                "TestObject3",
                "TestObject4"
            };
            Assert.AreEqual( 3, result.Count );
            Assert.IsTrue( expectedNames.All( n => result.Any( x => x.Name == n ) ) );
        }

        [Test]
        public void WhenFilterIsLessThanOrEqualToAndMoreThanOrEqualToOnIndexedColumn_UsesIndex()
        {
            var data = new List<TestObject>
            {
                new TestObject
                {
                    Name = "TestObject1",
                    Score = 1
                },
                new TestObject
                {
                    Name = "TestObject2",
                    Score = 2
                },
                new TestObject
                {
                    Name = "TestObject3",
                    Score = 3
                },
                new TestObject
                {
                    Name = "TestObject4",
                    Score = 4
                }
            };

            SpawnData( data );

            var expected = RethinkDB.R.Table( TableName )
                .Between( 1, 4 )
                .OptArg( "index", "Score" )
                .OptArg( "right_bound", "closed" );

            var queryable = GetQueryable<TestObject>( TableName, expected );

            var result = queryable
                .Where( x => x.Score <= 4 && x.Score >= 1 )
                .ToList();

            var expectedNames = new[]
            {
                "TestObject1",
                "TestObject2",
                "TestObject3",
                "TestObject4"
            };
            Assert.AreEqual( 4, result.Count );
            Assert.IsTrue( expectedNames.All( n => result.Any( x => x.Name == n ) ) );
        }

        [Test]
        public void WhenFilterIsLessThanOrMoreThanOnIndexedColumn_DoesNotUseIndex()
        {
            var data = new List<TestObject>
            {
                new TestObject
                {
                    Name = "TestObject1",
                    Score = 1
                },
                new TestObject
                {
                    Name = "TestObject2",
                    Score = 2
                },
                new TestObject
                {
                    Name = "TestObject3",
                    Score = 3
                },
                new TestObject
                {
                    Name = "TestObject4",
                    Score = 4
                }
            };

            SpawnData( data );

            var expected = RethinkDB.R.Table( TableName )
                .Filter( x => x["Score"].Lt( 4 ).Or( x["Score"].Gt( 1 ) ) );

            var queryable = GetQueryable<TestObject>( TableName, expected );

            var result = queryable
                .Where( x => x.Score < 4 || x.Score > 1 )
                .ToList();

            var expectedNames = new[]
            {
                "TestObject1",
                "TestObject2",
                "TestObject3",
                "TestObject4"
            };
            Assert.AreEqual( 4, result.Count );
            Assert.IsTrue( expectedNames.All( n => result.Any( x => x.Name == n ) ) );
        }












        public class TestObject
        {
            public TestObject()
            {
                Id = Guid.NewGuid();
            }

            [PrimaryIndex]
            public Guid Id { get; set; }

            public string Name { get; set; }
            public string Name2 { get; set; }

            [SecondaryIndex]
            public string Location { get; set; }
            [SecondaryIndex]
            public int Score { get; set; }

            public List<string> Locations { get; set; }
            public List<Resource> Resources { get; set; }
        }
    }

    public class Location
    {
        public List<string> Usages { get; set; }
    }

    public class Resource
    {
        public List<Location> Locations { get; set; }
        public string Name { get; set; }
    }
}