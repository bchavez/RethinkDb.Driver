using System;
using System.Collections.Generic;
using System.Linq;
using Xunit;

namespace RethinkDb.Driver.Linq.Tests
{
    public class LastTests : BaseLinqTest
    {
        [Fact]
        public void LastWithNoFilter_GeneratesCorrectQuery()
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

            var expected = RethinkDB.R.Table( TableName ).Nth( -1 );

            var result = GetQueryable<TestObject>( TableName, expected ).Last();
        }

        [Fact]
        public void LastWithFilter_GeneratesCorrectQuery()
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

            var expected = RethinkDB.R.Table( TableName ).Filter( x => x["Name"].Eq( "TestObject2" ) ).Nth( -1 );

            var result = GetQueryable<TestObject>( TableName, expected ).Last( x => x.Name == "TestObject2" );

            Assert.Equal( "TestObject2", result.Name );
        }

        [Fact]
        public void LastWithFilterAndNoMatches_ThrowExecpetion()
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

            var expected = RethinkDB.R.Table( TableName ).Filter( x => x["Name"].Eq( "TestObject3" ) ).Nth( -1 );

            Assert.Throws<InvalidOperationException>( () =>
            {
                var result = GetQueryable<TestObject>( TableName, expected ).Last( x => x.Name == "TestObject3" );
            } );
        }

        public class TestObject
        {
            public string Name { get; set; }
        }
    }
}
