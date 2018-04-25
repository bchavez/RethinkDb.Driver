using System;
using System.Diagnostics;
using System.Threading;
using FluentAssertions;
using NUnit.Framework;
using RethinkDb.Driver.Ast;
using RethinkDb.Driver.Tests.Utils;

namespace RethinkDb.Driver.Tests.GitHubIssues
{
    [TestFixture]
    [Explicit]
    public class Issue128
    {
        [Test]
        public void should_not_get_nullreference_after_reconnect()
        {
            var r = RethinkDB.R;

            var conn = r.Connection()
                .Hostname("localhost")
                .Connect();

            //stop the rethinkdb server
            Thread.Sleep(5000);
            var ps = Process.GetProcessesByName("rethinkdb");
            foreach( var process in ps )
            {
                process.Kill();
            }
            Thread.Sleep(5000);

            try
            {
                conn.Reconnect();
            }
            catch { }

            Action act = () => r.Now().RunAtom<DateTime>(conn);
            act.ShouldThrow<ReqlDriverError>();
        }
    }

}