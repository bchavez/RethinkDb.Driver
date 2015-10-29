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
        protected const string DbName = "CSharpDriverTests";
        protected const string Hostname = "192.168.0.11";
        protected const int Port = RethinkDb.Driver.RethinkDBConstants.DEFAULT_PORT;

        protected static RethinkDB r = RethinkDB.r;
        protected Connection conn;

        protected List<string> tableVars = new List<string>();


        [TestFixtureSetUp]
        public void BeforeRunningTestSession()
        {

        }

        [TestFixtureTearDown]
        public void AfterRunningTestSession()
        {
            Console.WriteLine($"TOTAL TESTS: {TestCounter}");
        }


        [SetUp]
        public void BeforeEachTest()
        {
            conn = r.connection()
                .hostname(Hostname)
                .port(Port)
                .connect();

            try
            {
                r.dbCreate(DbName).run(conn);
                r.db(DbName).wait_().run(conn);
            }
            catch { }

            foreach( var tableName in tableVars )
            {
                try
                {
                    r.db(DbName).tableCreate(tableName).run(conn);
                    r.db(DbName).table(tableName).wait_().run(conn);
                }
                catch{}
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
                r.db(DbName).tableDrop(tableName).run(conn);
            }
            r.dbDrop(DbName).run(conn);
            conn.close(false);
        }
     
    }

    
}
