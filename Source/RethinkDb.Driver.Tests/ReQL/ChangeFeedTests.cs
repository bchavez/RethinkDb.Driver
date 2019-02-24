using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using Newtonsoft.Json.Linq;
using NUnit.Framework;
using RethinkDb.Driver.Model;
using RethinkDb.Driver.Tests.Utils;

namespace RethinkDb.Driver.Tests.ReQL
{
    [TestFixture]
    public class ChangeFeedTests : QueryTestFixture
    {  
        [Test]
        [Explicit]
        public void change_feeds_without_rx()
        {
            var result = R.Db(DbName).Table(TableName)
                .Delete()[new { return_changes = true }]
                .RunWrite(conn)
                .AssertNoErrors();

            var changes = R.Db(DbName).Table(TableName)
                .Changes()[new { include_states = false }]
                .RunChanges<JObject>(conn);


            
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
                R.Db(DbName).Table(TableName)
                    .Insert(new { foo = "bar" })
                    .Run(conn);
            });

            Thread.Sleep(3000);

            Task.Run(() =>
            {
                R.Db(DbName).Table(TableName)
                    .Insert(new { foo = "bar" })
                    .Run(conn);
            });

            Thread.Sleep(3000);

            Task.Run(() =>
            {
                R.Db(DbName).Table(TableName)
                    .Insert(new { foo = "bar" })
                    .Run(conn);
            });

            Thread.Sleep(3000);

            changes.Close();

            task.Result.Should().Be(3);
        }

        [Test]
        [Explicit]
        public void can_enumerate_though_change_feed_manually()
        {
            var result = R.Db(DbName).Table(TableName)
                .Delete()[new { return_changes = true }]
                .RunWrite(conn)
                .AssertNoErrors();

            var changes = R.Db(DbName).Table(TableName)
                .Changes()[new { include_states = false }]
                .RunChanges<JObject>(conn);

            var task = Task.Run(async () =>
            {
                var count = 0;
                while (await changes.MoveNextAsync())
                {
                    changes.Current.Dump();
                    count++;
                }
                return count;
            });

            Thread.Sleep(3000);

            Task.Run(() =>
            {
                R.Db(DbName).Table(TableName)
                    .Insert(new { foo = "bar" })
                    .Run(conn);
            });

            Thread.Sleep(3000);

            Task.Run(() =>
            {
                R.Db(DbName).Table(TableName)
                    .Insert(new { foo = "bar" })
                    .Run(conn);
            });

            Thread.Sleep(3000);

            Task.Run(() =>
            {
                R.Db(DbName).Table(TableName)
                    .Insert(new { foo = "bar" })
                    .Run(conn);
            });

            Thread.Sleep(3000);

            changes.Close();

            Task.WaitAll(task);
            task.Result.Should().Be(3);
        }


        [Test]
        public void can_get_change_type()
        {
            var result = R.Db(DbName).Table(TableName)
                .Delete()[new { return_changes = true }]
                .RunWrite(conn)
                .AssertNoErrors();

            var changes = R.Db(DbName).Table(TableName)
                .Changes()[new { include_states = false, include_types = true }]
                .RunChanges<JObject>(conn);

            var task = Task.Run(async () =>
            {
                var count = 0;
                while (await changes.MoveNextAsync())
                {
                    changes.Current.Type.Should().Be(ChangeType.Add);
                    changes.Current.Dump();
                    count++;
                }
                return count;
            });

            Thread.Sleep(3000);

            Task.Run(() =>
            {
                R.Db(DbName).Table(TableName)
                    .Insert(new { foo = "bar" })
                    .Run(conn);
            });

            Thread.Sleep(3000);

            Task.Run(() =>
            {
                R.Db(DbName).Table(TableName)
                    .Insert(new { foo = "bar" })
                    .Run(conn);
            });

            Thread.Sleep(3000);

            Task.Run(() =>
            {
                R.Db(DbName).Table(TableName)
                    .Insert(new { foo = "bar" })
                    .Run(conn);
            });

            Thread.Sleep(3000);

            changes.Close();

            Task.WaitAll(task);
            task.Result.Should().Be(3);
        }
    }
}