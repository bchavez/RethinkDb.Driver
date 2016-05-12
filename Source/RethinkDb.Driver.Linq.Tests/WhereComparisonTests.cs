using System;
using System.Collections.Generic;
using System.Linq;
using RethinkDb.Driver.Linq.Attributes;
using Xunit;

namespace RethinkDb.Driver.Linq.Tests
{
    public class WhereComparisonTests : BaseLinqTest
    {
        [Fact]
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

            Assert.Equal( 2, result.Count );
            foreach( var testObject in data )
                Assert.True( result.Any( x => x.Name == testObject.Name ) );
        }

        [Fact]
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

            Assert.Equal( 1, result.Count );
            Assert.Equal( data[0].Name, result[0].Name );
        }

        [Fact]
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

            Assert.Equal( 1, result.Count );
            Assert.Equal( data[1].Name, result[0].Name );
        }

        [Fact]
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

            Assert.Equal( 1, result.Count );
            Assert.Equal( data[0].Id, result[0].Id );
            Assert.Equal( data[0].Name, result[0].Name );
        }

        [Fact]
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

            Assert.Equal( 1, result.Count );
            Assert.Equal( data[0].Id, result[0].Id );
            Assert.Equal( data[0].Name, result[0].Name );
        }

        [Fact]
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

            Assert.Equal( 1, result.Count );
            Assert.Equal( data[0].Id, result[0].Id );
            Assert.Equal( data[0].Name, result[0].Name );
        }

        [Fact]
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

            Assert.Equal( 1, result.Count );
            Assert.Equal( data[0].Id, result[0].Id );
            Assert.Equal( data[0].Name, result[0].Name );
        }

        [Fact]
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

            Assert.Equal( 1, result.Count );
            Assert.Equal( data[0].Id, result[0].Id );
            Assert.Equal( data[0].Name, result[0].Name );
        }

        [Fact]
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

            Assert.Equal( 1, result.Count );
            Assert.Equal( data[0].Id, result[0].Id );
            Assert.Equal( data[0].Name, result[0].Name );
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