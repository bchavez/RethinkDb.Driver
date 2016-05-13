using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;

namespace RethinkDb.Driver.Linq.Tests
{
    public class GroupByTests : BaseLinqTest
    {
        [Test]
        public void WhenGroupingOnPropertyGroupsData()
        {
            var data = new List<GroupObject>
            {
                new GroupObject
                {
                    Name = "Name1",
                    Area = "Area1"
                },
                new GroupObject
                {
                    Name = "Name2",
                    Area = "Area1"
                },
                new GroupObject
                {
                    Name = "Name3",
                    Area = "Area2"
                }
            };

            SpawnData( data );

            var expected = RethinkDB.R.Table( TableName ).Group( "Area" ).Ungroup();

            var queryable = GetQueryable<GroupObject>( TableName, expected );

            var result = queryable
                .GroupBy( x => x.Area )
                .ToList();
        }

        [Test]
        public void WhenGroupingOnPropertyWithSelectorGroupsData()
        {
            var data = new List<GroupObject>
            {
                new GroupObject
                {
                    Name = "Name1",
                    Area = "Area1"
                },
                new GroupObject
                {
                    Name = "Name2",
                    Area = "Area1"
                },
                new GroupObject
                {
                    Name = "Name3",
                    Area = "Area2"
                }
            };

            SpawnData( data );

            var expected = RethinkDB.R.Table( TableName ).Group( "Area" ).GetField( "Name" ).Ungroup();

            var queryable = GetQueryable<GroupObject>( TableName, expected );

            var result = queryable
                .GroupBy( x => x.Area, x => x.Name )
                .ToList();
        }

        [Test]
        public void WhenGroupingOnPropertyAndThenFilteringOnKeyGroupsData()
        {
            var data = new List<GroupObject>
            {
                new GroupObject
                {
                    Name = "Name1",
                    Area = "Area1"
                },
                new GroupObject
                {
                    Name = "Name2",
                    Area = "Area1"
                },
                new GroupObject
                {
                    Name = "Name3",
                    Area = "Area2"
                }
            };

            SpawnData( data );

            var expected = RethinkDB.R.Table( TableName ).Group( "Area" ).Ungroup().Filter( x=>x["group"].Eq( "Area2" ) );

            var queryable = GetQueryable<GroupObject>( TableName, expected );

            var result = queryable
                .GroupBy( x => x.Area )
                .Where( x => x.Key == "Area2" )
                .ToList();
        }

        [Test]
        public void WhenGroupingOnPropertyAndFilteringOnGroupList_GeneratesCorrectReql()
        {
            var data = new List<GroupObject>
            {
                new GroupObject
                {
                    Name = "Name1",
                    Area = "Area1"
                },
                new GroupObject
                {
                    Name = "Name2",
                    Area = "Area1"
                },
                new GroupObject
                {
                    Name = "Name3",
                    Area = "Area2"
                }
            };

            SpawnData( data );

            var expected = RethinkDB.R.Table( TableName )
                .Group( "Area" )
                .Ungroup()
                .Filter( x => x["reduction"].Contains( obj => obj["Name"].Eq( "Name1" ) ) );

            var queryable = GetQueryable<GroupObject>( TableName, expected );

            var result = queryable
                .GroupBy( x => x.Area )
                .Where( x => x.Any( g => g.Name == "Name1" ) )
                .ToList();
        }

        public class GroupObject
        {
            public string Area { get; set; }
            public string Name { get; set; }
        }
    }
}
