using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NUnit.Framework;

namespace RethinkDb.Driver.Linq.Tests
{
    public class MathematicalTests : BaseLinqTest
    {
        [Test]
        public void WhenSelectIsAddingToMembers_ReturnsSum()
        {
            SpawnData( new List<MathematicalTestObject>
            {
                new MathematicalTestObject
                {
                    Name = "TestObject1",
                    Value1 = 1,
                    Value2 = 2
                }
            } );

            var expected = RethinkDB.R.Table( TableName ).Map( x => x["Value1"].Add( x["Value2"] ) ).Nth( 0 );

            var result = GetQueryable<MathematicalTestObject>( TableName, expected ).Select( x => x.Value1 + x.Value2 )
                .First();

            Assert.AreEqual( 3, result );
        }

        [Test]
        public void WhenWhereIsAddingAndCheckingResult_ReturnsCorrectResults()
        {
            SpawnData( new List<MathematicalTestObject>
            {
                new MathematicalTestObject
                {
                    Name = "TestObject1",
                    Value1 = 1,
                    Value2 = 2
                },
                new MathematicalTestObject
                {
                    Name = "TestObject2",
                    Value1 = 3,
                    Value2 = 4
                }
            } );

            var expected = RethinkDB.R.Table( TableName ).Filter( x => x["Value1"].Add( x["Value2"] ).Eq( 3 ) );

            var result = GetQueryable<MathematicalTestObject>( TableName, expected )
                .Where( x => x.Value1 + x.Value2 == 3 ).ToList();

            Assert.AreEqual( 1, result.Count );
            Assert.AreEqual( "TestObject1", result[0].Name );
        }

        [Test]
        public void WhenSelectIsSubtractingToMembers_ReturnsSum()
        {
            SpawnData( new List<MathematicalTestObject>
            {
                new MathematicalTestObject
                {
                    Name = "TestObject1",
                    Value1 = 1,
                    Value2 = 2
                }
            } );

            var expected = RethinkDB.R.Table( TableName ).Map( x => x["Value2"].Sub( x["Value1"] ) ).Nth( 0 );

            var result = GetQueryable<MathematicalTestObject>( TableName, expected ).Select( x => x.Value2 - x.Value1 )
                .First();

            Assert.AreEqual( 1, result );
        }

        [Test]
        public void WhenWhereIsSubtractingAndCheckingResult_ReturnsCorrectResults()
        {
            SpawnData( new List<MathematicalTestObject>
            {
                new MathematicalTestObject
                {
                    Name = "TestObject1",
                    Value1 = 1,
                    Value2 = 2
                },
                new MathematicalTestObject
                {
                    Name = "TestObject2",
                    Value1 = 3,
                    Value2 = 5
                }
            } );

            var expected = RethinkDB.R.Table( TableName ).Filter( x => x["Value2"].Sub( x["Value1"] ).Eq( 1 ) );

            var result = GetQueryable<MathematicalTestObject>( TableName, expected )
                .Where( x => x.Value2 - x.Value1 == 1 ).ToList();

            Assert.AreEqual( 1, result.Count );
            Assert.AreEqual( "TestObject1", result[0].Name );
        }

        [Test]
        public void WhenSelectIsMultiplyingToMembers_ReturnsSum()
        {
            SpawnData( new List<MathematicalTestObject>
            {
                new MathematicalTestObject
                {
                    Name = "TestObject1",
                    Value1 = 2,
                    Value2 = 2
                }
            } );

            var expected = RethinkDB.R.Table( TableName ).Map( x => x["Value1"].Mul( x["Value2"] ) ).Nth( 0 );

            var result = GetQueryable<MathematicalTestObject>( TableName, expected ).Select( x => x.Value1 * x.Value2 )
                .First();

            Assert.AreEqual( 4, result );
        }

        [Test]
        public void WhenWhereIsMultiplyingAndCheckingResult_ReturnsCorrectResults()
        {
            SpawnData( new List<MathematicalTestObject>
            {
                new MathematicalTestObject
                {
                    Name = "TestObject1",
                    Value1 = 2,
                    Value2 = 2
                },
                new MathematicalTestObject
                {
                    Name = "TestObject2",
                    Value1 = 3,
                    Value2 = 5
                }
            } );

            var expected = RethinkDB.R.Table( TableName ).Filter( x => x["Value1"].Mul( x["Value2"] ).Eq( 4 ) );

            var result = GetQueryable<MathematicalTestObject>( TableName, expected )
                .Where( x => x.Value1 * x.Value2 == 4 ).ToList();

            Assert.AreEqual( 1, result.Count );
            Assert.AreEqual( "TestObject1", result[0].Name );
        }

        [Test]
        public void WhenSelectIsDividingToMembers_ReturnsSum()
        {
            SpawnData( new List<MathematicalTestObject>
            {
                new MathematicalTestObject
                {
                    Name = "TestObject1",
                    Value1 = 6,
                    Value2 = 2
                }
            } );

            var expected = RethinkDB.R.Table( TableName ).Map( x => x["Value1"].Div( x["Value2"] ) ).Nth( 0 );

            var result = GetQueryable<MathematicalTestObject>( TableName, expected ).Select( x => x.Value1 / x.Value2 )
                .First();

            Assert.AreEqual( 3, result );
        }

        [Test]
        public void WhenWhereIsDividingAndCheckingResult_ReturnsCorrectResults()
        {
            SpawnData( new List<MathematicalTestObject>
            {
                new MathematicalTestObject
                {
                    Name = "TestObject1",
                    Value1 = 6,
                    Value2 = 2
                },
                new MathematicalTestObject
                {
                    Name = "TestObject2",
                    Value1 = 3,
                    Value2 = 5
                }
            } );

            var expected = RethinkDB.R.Table( TableName ).Filter( x => x["Value1"].Div( x["Value2"] ).Eq( 3 ) );

            var result = GetQueryable<MathematicalTestObject>( TableName, expected )
                .Where( x => x.Value1 / x.Value2 == 3 ).ToList();

            Assert.AreEqual( 1, result.Count );
            Assert.AreEqual( "TestObject1", result[0].Name );
        }

        [Test]
        public void WhenSelectIsModulusToMembers_ReturnsSum()
        {
            SpawnData( new List<MathematicalTestObject>
            {
                new MathematicalTestObject
                {
                    Name = "TestObject1",
                    Value1 = 4,
                    Value2 = 2
                }
            } );

            var expected = RethinkDB.R.Table( TableName ).Map( x => x["Value1"].Mod( x["Value2"] ) ).Nth( 0 );

            var result = GetQueryable<MathematicalTestObject>( TableName, expected ).Select( x => x.Value1 % x.Value2 )
                .First();

            Assert.AreEqual( 0, result );
        }

        [Test]
        public void WhenWhereIsModulusAndCheckingResult_ReturnsCorrectResults()
        {
            SpawnData( new List<MathematicalTestObject>
            {
                new MathematicalTestObject
                {
                    Name = "TestObject1",
                    Value1 = 4,
                    Value2 = 2
                },
                new MathematicalTestObject
                {
                    Name = "TestObject2",
                    Value1 = 3,
                    Value2 = 5
                }
            } );

            var expected = RethinkDB.R.Table( TableName ).Filter( x => x["Value1"].Mod( x["Value2"] ).Eq( 0 ) );

            var result = GetQueryable<MathematicalTestObject>( TableName, expected )
                .Where( x => x.Value1 % x.Value2 == 0 ).ToList();

            Assert.AreEqual( 1, result.Count );
            Assert.AreEqual( "TestObject1", result[0].Name );
        }

        [Test]
        public void WhenSelectIsPlusAndTimesToMembers_ReturnsSum()
        {
            SpawnData( new List<MathematicalTestObject>
            {
                new MathematicalTestObject
                {
                    Name = "TestObject1",
                    Value1 = 4,
                    Value2 = 2
                }
            } );

            var expected = RethinkDB.R.Table( TableName )
                .Map( x => x["Value1"].Add( x["Value2"].Mul( x["Value2"] ) ) )
                .Nth( 0 );

            var result = GetQueryable<MathematicalTestObject>( TableName, expected )
                .Select( x => x.Value1 + x.Value2 * x.Value2 )
                .First();

            Assert.AreEqual( 8, result );
        }

        [Test]
        public void WhenSelectIsPlusThenTimesToMembers_ReturnsSum()
        {
            SpawnData(new List<MathematicalTestObject>
            {
                new MathematicalTestObject
                {
                    Name = "TestObject1",
                    Value1 = 4,
                    Value2 = 2
                }
            });

            var expected = RethinkDB.R.Table( TableName )
                .Map( x => x["Value1"].Add( x["Value2"] ).Mul( x["Value2"] ) )
                .Nth( 0 );

            var result = GetQueryable<MathematicalTestObject>( TableName, expected )
                .Select( x => ( x.Value1 + x.Value2 ) * x.Value2 )
                .First();

            Assert.AreEqual(12, result);
        }
    }

    public class MathematicalTestObject
    {
        public string Name { get; set; }
        public int Value1 { get; set; }
        public int Value2 { get; set; }
    }
}