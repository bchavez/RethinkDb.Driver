using System;
using FluentAssertions;
using NUnit.Framework;
using RethinkDb.Driver.Ast;
using RethinkDb.Driver.Net;

namespace RethinkDb.Driver.Tests
{
    [TestFixture]
    public abstract class QueryTestFixture
    {
        public const string DbName = "query";
        public const string TableName = "test";

        public static RethinkDB R = RethinkDB.R;

        public static Table table = R.Db(DbName).Table(TableName);

        protected Connection conn;
        private void EnsureConnection()
        {
            if (conn == null || !conn.Open)
            {
                this.conn = DefaultConnectionBuilder()
                    .Connect();
            }
        }

        public static Connection.Builder DefaultConnectionBuilder()
        {
            return R.Connection()
                .Hostname(AppSettings.TestHost)
                .Port(AppSettings.TestPort);
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

        [OneTimeSetUp]
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
        
        [OneTimeTearDown]
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
            R.Db("rethinkdb").Table("_debug_scratch").Delete().Run(conn);
            conn.Close(false);
        }


        protected void ClearDefaultTable()
        {
            R.Db(DbName).Table(TableName).Delete().Run(conn);
        }

        protected void ClearTable(string dbName, string tableName)
        {
            DropTable(dbName, tableName);
            CreateTable(dbName, tableName);
        }

        protected void CreateDb(string dbName)
        {
            R.DbCreate(dbName).Run(conn);
            R.Db(dbName).Wait_().Run(conn);
        }

        protected void DropDb(string dbName)
        {
            R.DbDrop(dbName).Run(conn);
        }

        protected void DropTable(string dbName, string tableName)
        {
            R.Db(dbName).TableDrop(tableName).Run(conn);
        }

        protected void CreateTable(string dbName, string tableName)
        {
            R.Db(dbName).TableCreate(tableName).Run(conn);
            R.Db(dbName).Table(tableName).Wait_().Run(conn);
        }
    }
}