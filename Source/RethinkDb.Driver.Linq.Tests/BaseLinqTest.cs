using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using NUnit.Framework;
using Remotion.Linq;
using Remotion.Linq.Parsing.Structure;
using RethinkDb.Driver.Ast;
using RethinkDb.Driver.Linq.Attributes;
using RethinkDb.Driver.Net;
using RethinkDb.Driver.Proto;
using RethinkDb.Driver.Tests;

namespace RethinkDb.Driver.Linq.Tests
{
    public abstract class BaseLinqTest : QueryTestFixture
    {
        protected string TableName;

        protected Connection Connection;

        [TestFixtureSetUp]
        public void BeforeRunningTestSession()
        {
            Connection = SetupConnection();
        }

        [TestFixtureTearDown]
        public void AfterRunningTestSession()
        {
            Connection.Close();
        }

        [SetUp]
        public void BeforeEachTest()
        {
            TableName = Guid.NewGuid().ToString().Replace( "-", "" );
        }

        [TearDown]
        public void AfterEachTest()
        {
            RethinkDB.R.TableDrop( TableName ).Run( Connection );
        }

        private void QueriesAreTheSame( ReqlAst expected, ReqlAst actual )
        {
            var buildMethod = typeof( ReqlAst ).GetMethod( "Build", BindingFlags.Instance | BindingFlags.NonPublic );

            var expectedJson = ( (JArray)buildMethod.Invoke( expected, new object[0] ) ).ToString( Formatting.None );
            var actualJson = ( (JArray)buildMethod.Invoke( actual, new object[0] ) ).ToString( Formatting.None );

            var removeIdsRegex = new Regex( @"\[\d+\]" );

            var expectedParsed = removeIdsRegex.Replace( ParseReql( expectedJson ), "" );
            var actualParsed = removeIdsRegex.Replace( ParseReql( actualJson ), "" );
            
            Assert.AreEqual( expectedParsed, actualParsed );
        }

        private static string ParseReql( string expectedJson )
        {
            var replacer = new Regex( @"(?<=\[)\d+(?!\d*])" );
            return replacer.Replace( expectedJson, m =>
            {
                TermType termType;
                if( Enum.TryParse( m.Value, out termType ) )
                {
                    return m.Result( termType.ToString() );
                }
                return m.Value;
            } );
        }

        protected RethinkQueryable<T> GetQueryable<T>( string table,ReqlAst expected )
        {
            var executor = new TestRethinkQueryExecutor( RethinkDB.R.Table( table ), Connection, reql =>
            {
                QueriesAreTheSame( expected, reql );
            } );
            return new RethinkQueryable<T>(
                new DefaultQueryProvider(
                    typeof( RethinkQueryable<> ),
                    QueryParser.CreateDefault(),
                    executor )
                );
        }

        protected void SpawnData<T>( List<T> data )
        {
            var reql = RethinkDB.R.TableCreate( TableName );

            var primaryIndex = typeof( T ).GetProperties().FirstOrDefault( x => x.CustomAttributes.Any( a => a.AttributeType == typeof( PrimaryIndexAttribute ) ) );
            if( primaryIndex != null )
                reql = reql.OptArg( "primary_key", primaryIndex.Name );
            reql.Run( Connection );

            var secondaryIndexes = typeof( T ).GetProperties().Where( x => x.CustomAttributes.Any( a => a.AttributeType == typeof( SecondaryIndexAttribute ) ) );
            foreach( var secondaryIndex in secondaryIndexes )
            {
                RethinkDB.R.Table( TableName ).IndexCreate( secondaryIndex.Name ).Run( Connection );
            }
            RethinkDB.R.Table( TableName ).IndexWait().Run( Connection );


            foreach( var testObject in data )
                RethinkDB.R.Table( TableName ).Insert( testObject ).Run( Connection );
        }

        private static Connection SetupConnection()
        {
            return RethinkDB.R.Connection()
                .Db(DbName)
                .Hostname(RethinkDBConstants.DefaultHostname)
                .Port(RethinkDBConstants.DefaultPort)
                .Timeout(RethinkDBConstants.DefaultTimeout)
                .Connect();
        }
    }
}