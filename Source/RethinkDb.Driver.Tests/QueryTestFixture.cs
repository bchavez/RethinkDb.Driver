using System;
using NUnit.Framework;
using RethinkDb.Driver.Net;

namespace RethinkDb.Driver.Tests
{
    [TestFixture]
    public abstract class QueryTestFixture
    {
        protected const string DbName = "query";
        protected const string TableName = "test";

        public static RethinkDB R = RethinkDB.R;

        protected Connection conn;
        private void EnsureConnection()
        {
            if (conn == null || !conn.Open)
            {
                this.conn = R.Connection()
                    .Hostname(AppSettings.TestHost)
                    .Port(AppSettings.TestPort)
                    .Connect();
            }
        }


        private void SetupFluentAssertion()
        {
            //Hook into FluentAssertion so we see very useful 
            //YamlTest context information. TestLine, expected, got, etc...
            FluentAssertions.Common.Services.ResetToDefaults();
            var hook = FluentAssertions.Common.Services.ThrowException;
            FluentAssertions.Common.Services.ThrowException = s =>
                {
                    var logContext = TestLogContext.Context.ToString();
                    hook(logContext + s);
                };
        }

        [TestFixtureSetUp]
        public void BeforeRunningTestSession()
        {
            SetupFluentAssertion();
            EnsureConnection();

            try
            {
                CreateDb(DbName);
            }
            catch
            {
            }
        }
        
        [TestFixtureTearDown]
        public void AfterRunningTestSession()
        {
            try
            {
                DropDb(DbName);
            }
            catch
            {
            }

            try
            {
                conn.Close(false);
            }
            catch
            {
            }
        }

        [SetUp]
        public  virtual void BeforeEachTest()
        {
            EnsureConnection();

            try
            {
                CreateTable(DbName, TableName);
            }
            catch
            {
            }
            TestLogContext.ResetContext();
        }

        [TearDown]
        public void AfterEachTest()
        {
            R.db("rethinkdb").table("_debug_scratch").delete().Run(conn);
            conn.Close(false);
        }


        protected void ClearDefaultTable()
        {
            R.db(DbName).table(TableName).delete().Run(conn);
        }

        protected void ClearTable(string dbName, string tableName)
        {
            DropTable(dbName, tableName);
            CreateTable(dbName, tableName);
        }

        protected void CreateDb(string dbName)
        {
            R.dbCreate(dbName).Run(conn);
            R.db(dbName).wait_().Run(conn);
        }

        protected void DropDb(string dbName)
        {
            R.dbDrop(dbName).Run(conn);
        }

        protected void DropTable(string dbName, string tableName)
        {
            R.db(dbName).tableDrop(tableName).Run(conn);
        }

        protected void CreateTable(string dbName, string tableName)
        {
            R.db(dbName).tableCreate(tableName).Run(conn);
            R.db(dbName).table(tableName).wait_().Run(conn);
        }
    }
}