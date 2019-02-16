using System;
using System.Diagnostics;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;
using NUnit.Framework;
using RethinkDb.Driver.Net;
using RethinkDb.Driver.Tests.Utils;

namespace RethinkDb.Driver.Tests.GitHubIssues
{
    [TestFixture]
    public class Issue138
    {
        [Test]
        [Explicit]
        public async Task explore_exception()
        {
            var r = RethinkDB.R;

            var conn = r.Connection()
                .Hostname("192.168.0.131")
                .Connect();


            var feed = r.Db("query").Table("test").Changes().RunChanges<JObject>(conn);

            while(await feed.MoveNextAsync())
            {
                var s = feed.Current.DumpString();
                Debug.Write(s);
                feed.Current.Dump();
            }
        }

    }
}