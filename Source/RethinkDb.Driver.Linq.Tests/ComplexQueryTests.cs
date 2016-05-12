using System;
using System.Collections.Generic;
using System.Linq;
using RethinkDb.Driver.Linq.Attributes;
using Xunit;

namespace RethinkDb.Driver.Linq.Tests
{
    public class ComplexQueryTests : BaseLinqTest
    {
        [Fact]
        public void ComplexQuery()
        {
            var data = new List<ComplexObject>
            {
                new ComplexObject
                {
                    Name = "My First Name",
                    Length = 10,
                    CreatedDate = new DateTime( 2016, 1, 2 )
                },
                new ComplexObject
                {
                    Name = "My First Name",
                    Length = 10,
                    CreatedDate = new DateTime( 2016, 1, 1 )
                },
                new ComplexObject
                {
                    Name = "My First Name",
                    Length = 5,
                    CreatedDate = new DateTime( 2016, 1, 1 )
                }
            };

            SpawnData( data );

            var expected = RethinkDB.R.Table( TableName )
                .GetAll( 10 )
                .OptArg( "index", "Length" )
                .Filter( x => x["Name"].Eq( "My First Name" ) )
                .Filter( x => x["Length"].Gt( 10 ) )
                .OrderBy( "CreatedDate" );

            var queryable = GetQueryable<ComplexObject>( TableName, expected );

            var result = ( from complexObject in queryable
                where complexObject.Length == 10
                where complexObject.Name == "My First Name"
                where complexObject.Length > 10
                orderby complexObject.CreatedDate
                select complexObject ).ToList();

            Assert.NotNull( result );
        }

        public class ComplexObject
        {
            public string Name { get; set; }

            [SecondaryIndex]
            public int Length { get; set; }

            [SecondaryIndex]
            public DateTime CreatedDate { get; set; }
        }
    }
}
