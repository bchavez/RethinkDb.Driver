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

        protected static RethinkDB r = RethinkDB.r;
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

        public static YamlTestContext Context
        {
            get
            {
                return CallContext.GetData("YamlTestContext") as YamlTestContext;
            }
            set { CallContext.SetData("YamlTestContext", value); }
        }

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
            
            conn = r.connection()
                .hostname(AppSettings.TestHost)
                .port(AppSettings.TestPort)
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