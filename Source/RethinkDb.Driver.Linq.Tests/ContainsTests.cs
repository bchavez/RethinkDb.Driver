using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;

namespace RethinkDb.Driver.Linq.Tests
{
    public class ContainsTests : BaseLinqTest
    {
        [Test]
        public void ContainsForProperty_ReturnsCorrectReql()
        {
            var strings = new[]
            {
                "Hello"
            };

            var data = new List<TestObject>
            {
                new TestObject
                {
                    Name = "Hello"
                }
            };
            ;

            SpawnData( data );

            var expected = RethinkDB.R.Table( TableName )
                .Filter( x => RethinkDB.R.Expr( RethinkDB.R.Array( strings ) ).Contains( x["Name"] ) );

            var queryable = GetQueryable<TestObject>( TableName, expected );

            var result = queryable
                .Where( x => strings.Contains( x.Name ) )
                .ToList();

            Assert.AreEqual( 1, result.Count );
        }

        class TestObject
        {
            public string Name { get; set; }
        }
    }
}
