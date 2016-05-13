using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;

namespace RethinkDb.Driver.Linq.Tests
{
    public class AverageTests : BaseLinqTest
    {
        [Test]
        public void ForSimpleAverage_GeneratesCorrectQuery()
        {
            var data = new List<TestObject>
            {
                new TestObject
                {
                    Size = 1
                },
                new TestObject
                {
                    Size = 3
                }
            };

            SpawnData( data );

            var expected = RethinkDB.R.Table( TableName ).Avg( x => x["Size"] );

            var result = GetQueryable<TestObject>( TableName, expected ).Average( x => x.Size );

            Assert.AreEqual( 2, result );
        }

        [Test]
        public void ForSimpleAverageForSubProperty_GeneratesCorrectQuery()
        {
            var data = new List<TestObject>
            {
                new TestObject
                {
                    Information = new Information
                    {
                        Size = 1
                    }
                },
                new TestObject
                {
                    Information = new Information
                    {
                        Size = 3
                    }
                }
            };

            SpawnData( data );

            var expected = RethinkDB.R.Table( TableName ).Avg( x => x["Information"]["Size"] );

            var result = GetQueryable<TestObject>( TableName, expected ).Average( x => x.Information.Size );

            Assert.AreEqual( 2, result );
        }

        [Test]
        public void ForSimpleAverageForSubSubProperty_GeneratesCorrectQuery()
        {
            var data = new List<TestObject>
            {
                new TestObject
                {
                    MainInformation = new MainInformation
                    {
                        Information = new Information
                        {
                            Size = 1
                        }
                    }
                },
                new TestObject
                {
                    MainInformation = new MainInformation
                    {
                        Information = new Information
                        {
                            Size = 3
                        }
                    }
                }
            };

            SpawnData( data );

            var expected = RethinkDB.R.Table( TableName ).Avg( x => x["MainInformation"]["Information"]["Size"] );

            var result = GetQueryable<TestObject>( TableName, expected ).Average( x => x.MainInformation.Information.Size );

            Assert.AreEqual( 2, result );
        }

        public class MainInformation
        {
            public Information Information { get; set; }
        }

        public class Information
        {
            public int Size { get; set; }
        }

        public class TestObject
        {
            public int Size { get; set; }
            public Information Information { get; set; }
            public MainInformation MainInformation { get; set; }
        }
    }
}
