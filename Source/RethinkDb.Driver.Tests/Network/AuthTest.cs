using System;
using System.Text;
using FluentAssertions;
using NUnit.Framework;
using RethinkDb.Driver.Net;
using RethinkDb.Driver.Tests.Utils;

namespace RethinkDb.Driver.Tests.Network
{
    [TestFixture]
    public class AuthTest
    {
        private string bogusUsername = "bogus_guy";
        private string bogusPassword = "bogus_man+=";

        public static RethinkDB R = RethinkDB.R;

        [OneTimeSetUp]
        public void BeforeRunningTestSession()
        {
            var adminConn = QueryTestFixture.DefaultConnectionBuilder()
                .Connect();

            R.Db("rethinkdb").Table("users").Insert(
                new {id = bogusUsername, password = bogusPassword}
                ).Run(adminConn);

            adminConn.Close();
        }

        [OneTimeTearDown]
        public void AfterRunningTestSession()
        {
            var adminConn = QueryTestFixture.DefaultConnectionBuilder()
                .Connect();

            R.Db("rethinkdb").Table("users").Get(bogusUsername)
                .Delete().RunWrite(adminConn);

            adminConn.Close();
        }

        [Test]
        public void test_pbkdf2()
        {
            var pass = Encoding.UTF8.GetBytes("pencil");
            var salt = Convert.FromBase64String("W22ZaJ0SNY7soEsUEjb6gQ==");

            var cooked = Convert.ToBase64String(Crypto.Pbkdf2(pass, salt));

            cooked.Dump();

            cooked.Should().Be("xKSVEDI6tPlSysH6mUQZOeeOp01r6B3fcJbodRPcYV0=");
        }

        [Test]
        public void test_connect_with_non_admin_user()
        {
            var bogusConn = QueryTestFixture.DefaultConnectionBuilder()
                .User(bogusUsername, bogusPassword)
                .Connect();

            R.Expr("HELLO FROM THE C# DRIVER! :D YEE HAAAW!  HELLO FROM THE C# DRIVER! :D YEE HAAAW!"+
                "  HELLO FROM THE C# DRIVER! :D YEE HAAAW!  HELLO FROM THE C# DRIVER! :D YEE HAAAW!  ")
                .Run(bogusConn);

            bogusConn.Close();
        }

        [Test]
        public void test_connect_with_bogus_authkey_and_username()
        {
            Action act = () => QueryTestFixture.DefaultConnectionBuilder()
                .User(bogusUsername, bogusPassword).AuthKey("test")
                .Connect();

            act.ShouldThrow<ReqlDriverError>();
        }
    }
}