using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using FluentAssertions;
using FluentAssertions.Common;
using Newtonsoft.Json.Linq;
using NLog;
using NLog.Internal;
using NUnit.Framework;
using RethinkDb.Driver.Ast;
using RethinkDb.Driver.Model;
using RethinkDb.Driver.Net;
using Z.ExtensionMethods;

namespace RethinkDb.Driver.Tests
{
    [TestFixture]
    public class GeneratedTest
    {
        protected static int TestCounter = 0;
        protected const string DbName = "test";

        protected static RethinkDB r = RethinkDB.r;
        protected Connection conn;

        protected List<string> tableVars = new List<string>();

        public string GetTestHost()
        {
            if( Environment.GetEnvironmentVariable("CI").IsNotNullOrWhiteSpace() )
            {
                //CI is testing.
                return "localhost";
            }
            return System.Configuration.ConfigurationManager.AppSettings["TestServer"];
        }

        public int GetTestPort()
        {
            if (Environment.GetEnvironmentVariable("CI").IsNotNullOrWhiteSpace())
            {
                //CI is testing.
                return 28015;
            }
            var port = System.Configuration.ConfigurationManager.AppSettings["TestPort"];
            return int.Parse(port);
        }


        [TestFixtureSetUp]
        public void BeforeRunningTestSession()
        {
        }

        [TestFixtureTearDown]
        public void AfterRunningTestSession()
        {
            Console.WriteLine($"TOTAL TESTS: {TestCounter}");
        }

        private ManualResetEvent FixtureWaitHandle = new ManualResetEvent(true);

        [SetUp]
        public void BeforeEachTest()
        {
            Log.Trace(">>>>>>>>>>>>>> SET UP");
            FixtureWaitHandle.WaitOne();

            conn = r.connection()
                .hostname(GetTestHost())
                .port(GetTestPort())
                .connect();

            try
            {
                r.dbCreate(DbName).run(conn);
                r.db(DbName).wait_().run(conn);
            }
            catch
            {
            }

            foreach( var tableName in tableVars )
            {
                try
                {
                    r.db(DbName).tableCreate(tableName).run(conn);
                    r.db(DbName).table(tableName).wait_().run(conn);
                }
                catch
                {
                }
            }
        }

        [TearDown]
        public void AfterEachTest()
        {
            Log.Trace(">>>>>>>>>>>>>> TARE DOWN");

            r.db("rethinkdb").table("_debug_scratch").delete().run(conn);
            if( !conn.Open )
            {
                conn.close();
                conn.reconnect();
            }

            foreach( var tableName in tableVars )
            {
                try
                {
                    r.db(DbName).tableDrop(tableName).run(conn);
                }
                catch
                {
                }
            }
            try
            {
                r.dbDrop(DbName).run(conn);
            }
            catch
            {
            }
            conn.close(false);
            FixtureWaitHandle.Set();
        }
    }
}