using System;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using Newtonsoft.Json.Linq;
using NUnit.Framework;
using RethinkDb.Driver.Net;
using static System.Console;
using static RethinkDb.Driver.RethinkDB;

namespace RethinkDb.Driver.Tests.GitHubIssues
{
    [TestFixture]
    public class Issue146
    {
        private Connection conn;

        [Test]
        public async Task Test()
        {
            Print("Main Thread Start");

            conn = R.Connection().Connect();

            var cts = new CancellationTokenSource();

            RunChanges(cts.Token);

            Print("Starting main thread delay.");
            await Task.Delay(500);

            Print($"Canceling task");
            Action act = () => cts.Cancel();

            act.ShouldNotThrow();

            Print("End of main");
        }

        private async void RunChanges(CancellationToken ct)
        {
            Print("RunChanges: called");
            Cursor<Model.Change<JObject>> changes = null;
            try
            {
                Print("RunChanges: BEFORE Query");
                changes = await R.Db("rethinkdb").Table("jobs")
                    .Changes().OptArg("include_initial", "true")
                    .RunChangesAsync<JObject>(conn, ct);
                Print("RunChanges: Have Cursor, iterating with MoveNextAsync.");
                while (await changes.MoveNextAsync(ct))
                {
                    Print("RunChanges: got a change");
                }
            }
            catch (OperationCanceledException ex)
            {
                Print("RunChanges: op canceled");
            }
            finally
            {
                Print("RunChanges: finally");
                changes?.Close();
                Print("RunChanges: changes cursor closed");
            }
            Print("RunChanges: returning");
        }

        static void Print(string msg)
        {
            WriteLine($">>> (TID:{Thread.CurrentThread.ManagedThreadId}): {msg}");
        }
    }
}