using System;
using System.Reactive.Concurrency;
using System.Reactive.Linq;
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
    public class RxReactiveExtensionTests : QueryTestFixture
    {
        [Test]
        public void basic_change_feed_with_reactive_extensions()
        {
            var onCompleted = 0;
            var onError = 0;
            var onNext = 0;

            var result = R.Db(DbName).Table(TableName)
                    .Delete()[new { return_changes = true }]
                    .RunWrite(conn)
                    .AssertNoErrors();

            result.ChangesAs<JObject>().Dump();

            var changes = R.Db(DbName).Table(TableName)
                //.changes()[new {include_states = true, include_initial = true}]
                .Changes()
                .RunChanges<JObject>(conn);

            changes.IsFeed.Should().BeTrue();
            
            var observable = changes.ToObservable();

            //use a new thread if you want to continue,
            //otherwise, subscription will block.
            observable.SubscribeOn(NewThreadScheduler.Default)
                .Subscribe(
                    x => OnNext(x, ref onNext),
                    e => OnError(e, ref onError),
                    () => OnCompleted(ref onCompleted)
                );


            //Next simulate 3 inserts into the table.
            Thread.Sleep(3000);

            Task.Run(() =>
                {
                    R.Db(DbName).Table(TableName)
                        .Insert(new { foo = "change1" })
                        .Run(conn);
                });

            Thread.Sleep(3000);

            Task.Run(() =>
            {
                R.Db(DbName).Table(TableName)
                    .Insert(new { foo = "change2" })
                    .Run(conn);
            });

            Thread.Sleep(3000);

            Task.Run(() =>
                {
                    R.Db(DbName).Table(TableName)
                        .Insert(new { foo = "change3" })
                        .Run(conn);
                });

            Thread.Sleep(3000);

            changes.Close();

            Thread.Sleep(3000);

            onCompleted.Should().Be(1);
            onNext.Should().Be(3);
            onError.Should().Be(0);
        }

        private void OnCompleted(ref int onCompleted)
        {
            Console.WriteLine("On Completed.");
            onCompleted++;
        }

        private void OnError(Exception obj, ref int onError)
        {
            Console.WriteLine("On Error");
            Console.WriteLine(obj.Message);
            onError++;
        }

        private void OnNext(Change<JObject> obj, ref int onNext)
        {
            Console.WriteLine("On Next");
            obj.Dump();
            onNext++;
        }

    
    }
}
