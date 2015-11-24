using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using Newtonsoft.Json.Linq;
using NUnit.Framework;
using RethinkDb.Driver.Model;

namespace RethinkDb.Driver.Tests.ReQL
{
    [TestFixture]
    public class ChangeFeedTests : QueryTestFixture
    {  
        [Test]
        [Explicit]
        public void change_feeds_without_rx()
        {
            var result = r.db(DbName).table(TableName)
                .delete()[new { return_changes = true }]
                .runResult(conn)
                .AssertNoErrors();

            var changes = r.db(DbName).table(TableName)
                .changes()[new { include_states = true }]
                .runChanges<JObject>(conn);


            
            var task = Task.Run(() =>
                {
                    var count = 0;
                    while( changes.MoveNext() )
                    {
                        count++;
                    }
                    return count;
                });

            Thread.Sleep(3000);

            Task.Run(() =>
            {
                r.db(DbName).table(TableName)
                    .insert(new { foo = "bar" })
                    .run(conn);
            });

            Thread.Sleep(3000);

            Task.Run(() =>
            {
                r.db(DbName).table(TableName)
                    .insert(new { foo = "bar" })
                    .run(conn);
            });

            Thread.Sleep(3000);

            Task.Run(() =>
            {
                r.db(DbName).table(TableName)
                    .insert(new { foo = "bar" })
                    .run(conn);
            });

            Thread.Sleep(3000);

            changes.close();

            task.Result.Should().Be(3);
        }
    }
}