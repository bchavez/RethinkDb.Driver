using System;
using System.Collections.Generic;
using System.Runtime.Remoting.Messaging;
using System.Threading;

using Newtonsoft.Json;
using NUnit.Framework;
using RethinkDb.Driver.Net;
using Z.ExtensionMethods;

namespace RethinkDb.Driver.Tests
{
    [TestFixture]
    public abstract class YamlTestFixture
    {
        protected static int TestCounter = 0;
        protected const string DbName = "test";

        protected static RethinkDB r = RethinkDB.R;
        protected Connection conn;

        protected List<string> tableVars = new List<string>();

        protected void SetContext( string testContext )
        {
            TestLogContext.ResetContext();
            var json = testContext.DecodeBase64();
            var ctx = JsonConvert.DeserializeObject<YamlTestContext>(json);

            TestCounter++;

            Context = ctx;
        }

        public static YamlTestContext Context { get; set; }

        private void SetupFluentAssertion()
        {
            //Hook into FluentAssertion so we see very useful 
            //YamlTest context information. TestLine, expected, got, etc...
            FluentAssertions.Common.Services.ResetToDefaults();
            var hook = FluentAssertions.Common.Services.ThrowException;
            FluentAssertions.Common.Services.ThrowException = s =>
                {
                    var context = Context.ToString();
                    hook(context + s);
                };
        }

        [TestFixtureSetUp]
        public void BeforeRunningTestSession()
        {
            SetupFluentAssertion();
        }

        [TestFixtureTearDown]
        public void AfterRunningTestSession()
        {
            Console.WriteLine($"YAML TEST COUNTER: {TestCounter}");
        }

        private ManualResetEvent FixtureWaitHandle = new ManualResetEvent(true);

        [SetUp]
        public void BeforeEachTest()
        {
            FixtureWaitHandle.WaitOne();
            
            conn = r.Connection()
                .Hostname(AppSettings.TestHost)
                .Port(AppSettings.TestPort)
                .Connect();

            try
            {
                r.dbCreate(DbName).Run(conn);
                r.db(DbName).wait_().Run(conn);
            }
            catch
            {
            }

            foreach( var tableName in tableVars )
            {
                try
                {
                    r.db(DbName).tableCreate(tableName).Run(conn);
                    r.db(DbName).table(tableName).wait_().Run(conn);
                }
                catch
                {
                }
            }
        }

        [TearDown]
        public void AfterEachTest()
        {
            r.db("rethinkdb").table("_debug_scratch").delete().Run(conn);
            if( !conn.Open )
            {
                conn.Close();
                conn.Reconnect();
            }

            foreach( var tableName in tableVars )
            {
                try
                {
                    r.db(DbName).tableDrop(tableName).Run(conn);
                }
                catch
                {
                }
            }
            try
            {
                r.dbDrop(DbName).Run(conn);
            }
            catch
            {
            }
            conn.Close(false);
            FixtureWaitHandle.Set();
        }
    }
}