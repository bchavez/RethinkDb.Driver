using System;
using System.CodeDom.Compiler;
using System.Reactive.Linq;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using NUnit.Framework;
using RethinkDb.Driver.Model;
using RethinkDb.Driver.Tests.Utils;

namespace RethinkDb.Driver.Tests.ReQL
{
    [TestFixture]
    [Explicit]
    public class RxReactiveExtensionTests : QueryTestFixture
    {
        [Test]
        public void basic_change_feed_with_reactive_extensions()
        {
            var result = r.db(DbName).table(TableName)
                .delete()[new {return_changes = true}]
                .runResult(conn)
                .EnsureSuccess();

            result.Dump();

            result.ChangesAs<JObject>().Dump();

            var changes = r.db(DbName).table(TableName)
                .changes()[new {include_states = true, include_initial = true}]
                //.runCursor<Change<JObject>>(conn);
                .runChanges<JObject>(conn);

            var observable = changes.ToObservable();

            observable.Subscribe(OnNext, OnError, OnCompleted);
        }

        private void OnCompleted()
        {
            Console.WriteLine("On Completed.");
        }

        private void OnError(Exception obj)
        {
            Console.WriteLine("On Error");
            Console.WriteLine(obj.Message);
        }

        private void OnNext(Change<JObject> obj)
        {
            Console.WriteLine("On Next");
            obj.Dump();
        }
    }
}