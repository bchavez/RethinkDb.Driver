using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NUnit.Framework;

namespace RethinkDb.Driver.Linq.Tests
{
    public class SelectTests : BaseLinqTest
    {
        private void SpawnTestData()
        {
            var data = new List<Place>
            {
                new Place
                {
                    Name = "Name1",
                    Area = "Area1",
                    Size = 10
                },
                new Place
                {
                    Name = "Name2",
                    Area = "Area1",
                    Size = 20
                },
                new Place
                {
                    Name = "Name3",
                    Area = "Area2",
                    Size = 30
                }
            };

            SpawnData( data );
        }

        [Test]
        public void WhenSelectingPrimitiveType_ReturnsValues()
        {
            SpawnTestData();

            var expected = RethinkDB.R.Table( TableName ).Map( x => x["Name"] );

            var queryable = GetQueryable<Place>( TableName, expected );

            var result = queryable
                .Select( x => x.Name )
                .ToList();

            Assert.AreEqual( 3, result.Count );
            Assert.IsTrue( result.Contains( "Name1" ) );
            Assert.IsTrue( result.Contains( "Name2" ) );
            Assert.IsTrue( result.Contains( "Name3" ) );
        }

        [Test]
        public void WhenSelectingAnotherType_ReturnsValues()
        {
            SpawnTestData();

            var expected = RethinkDB.R.Table( TableName ).Map( x => RethinkDB.R.HashMap( "Name", x["Name"] ) );

            var queryable = GetQueryable<Place>( TableName, expected );

            var result = queryable
                .Select( x => new SelectResult
                {
                    Name = x.Name
                } )
                .ToList();

            Assert.AreEqual( 3, result.Count );
            Assert.IsTrue( result.Any( x => x.Name == "Name1" ) );
            Assert.IsTrue( result.Any( x => x.Name == "Name2" ) );
            Assert.IsTrue( result.Any( x => x.Name == "Name3" ) );
        }

        [Test]
        public void WhenSelectingKeyFromGroupBy_ReturnsValues()
        {
            SpawnTestData();

            var expected = RethinkDB.R.Table( TableName )
                .Group( "Area" )
                .Ungroup()
                .Map( x => RethinkDB.R.HashMap( "Area", x["group"] ) );

            var queryable = GetQueryable<Place>( TableName, expected );

            var result = queryable
                .GroupBy( x => x.Area )
                .Select( x => new GroupByResult
                {
                    Area = x.Key
                } )
                .ToList();

            Assert.AreEqual( 2, result.Count );
            Assert.IsTrue( result.Any( x => x.Area == "Area1" ) );
            Assert.IsTrue( result.Any( x => x.Area == "Area2" ) );
        }

        [Test]
        public void WhenSelectingKeyAndCountFromGroupBy_ReturnsValues()
        {
            SpawnTestData();

            var expected = RethinkDB.R.Table( TableName )
                .Group( "Area" )
                .Ungroup()
                .Map( x => RethinkDB.R.HashMap( "Area", x["group"] ).With( "PlacesCount", x["reduction"].Count() ) );

            var queryable = GetQueryable<Place>( TableName, expected );

            var result = queryable
                .GroupBy( x => x.Area )
                .Select( x => new GroupByResult
                {
                    Area = x.Key,
                    PlacesCount = x.Count()
                } )
                .ToList();

            Assert.AreEqual( 2, result.Count );
            Assert.IsTrue( result.Any( x => x.Area == "Area1" && x.PlacesCount == 2 ) );
            Assert.IsTrue( result.Any( x => x.Area == "Area2" && x.PlacesCount == 1 ) );
        }

        [Test]
        public void WhenSelectingKeyAndFilteredValuesFromGroupBy_ReturnsValues()
        {
            SpawnTestData();

            var expected = RethinkDB.R.Table( TableName )
                .Group( "Area" )
                .Ungroup()
                .Map( x => RethinkDB.R.HashMap( "Area", x["group"] )
                    .With( "Places", x["reduction"].Filter( p => p["Size"].Gt( 9 ) ).OrderBy( "Size" ) ) );

            var queryable = GetQueryable<Place>( TableName, expected );

            var result = queryable
                .GroupBy( x => x.Area )
                .Select( x => new GroupByResult
                {
                    Area = x.Key,
                    Places = x.Where( p => p.Size > 9 ).OrderBy( p => p.Size ).ToList()
                } )
                .ToList();

            Assert.AreEqual( 2, result.Count );
            var firstResult = result.First();
            Assert.AreEqual( "Area1", firstResult.Area );

            Assert.IsTrue( firstResult.Places.Any( x => x.Name == "Name1" && x.Size == 10 ) );
            Assert.IsTrue( firstResult.Places.Any( x => x.Name == "Name2" && x.Size == 20 ) );
        }

        [Test]
        public void WhenSelectingKeyAndCountFromGroupByUsingAnonymous_ReturnsValues()
        {
            SpawnTestData();

            var expected = RethinkDB.R.Table( TableName )
                .Group( "Area" )
                .Ungroup()
                .Map( x => RethinkDB.R.HashMap( "Area", x["group"] ).With( "PlacesCount", x["reduction"].Count() ) );

            var queryable = GetQueryable<Place>( TableName, expected );

            var result = queryable
                .GroupBy( x => x.Area )
                .Select( x => new
                {
                    Area = x.Key,
                    PlacesCount = x.Count()
                } )
                .ToList();

            Assert.AreEqual( 2, result.Count );
            Assert.IsTrue( result.Any( x => x.Area == "Area1" && x.PlacesCount == 2 ) );
            Assert.IsTrue( result.Any( x => x.Area == "Area2" && x.PlacesCount == 1 ) );
        }

        [Test]
        public void WhenSelectingKeyAndFirstFromGroupByUsingAnonymous_ReturnsValues()
        {
            SpawnTestData();

            var expected = RethinkDB.R.Table( TableName )
                .Group( "Area" )
                .Ungroup()
                .Map( x => RethinkDB.R.HashMap( "Area", x["group"] )
                    .With( "FirstPlace", x["reduction"].OrderBy( "Name" ).Nth( 0 ) ) );

            var queryable = GetQueryable<Place>( TableName, expected );

            var result = queryable
                .GroupBy( x => x.Area )
                .Select( x => new
                {
                    Area = x.Key,
                    FirstPlace = x.OrderBy( p => p.Name ).First()
                } )
                .ToList();

            Assert.AreEqual( 2, result.Count );
            Assert.IsTrue( result.Any( x => x.Area == "Area1" && x.FirstPlace.Name == "Name1" ) );
        }

        public class GroupByResult
        {
            public string Area { get; set; }
            public int PlacesCount { get; set; }
            public List<Place> Places { get; set; }
        }

        public class SelectResult
        {
            public string Name { get; set; }
        }

        public class Place
        {
            public string Area { get; set; }
            public string Name { get; set; }
            public int Size { get; set; }
        }
    }
}