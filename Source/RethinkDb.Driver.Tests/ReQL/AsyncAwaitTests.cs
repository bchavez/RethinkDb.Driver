using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using NUnit.Framework;
using RethinkDb.Driver.Model;
using RethinkDb.Driver.Tests.Utils;

namespace RethinkDb.Driver.Tests.ReQL
{
    [TestFixture]
    public class AsyncAwaitTests : QueryTestFixture
    {
        [Test]
        public async Task basic_test()
        {
            bool b = await R.Expr(true).RunAsync<bool>(conn);

            b.Should().Be(true);
        }

        [Test]
        public async Task async_insert()
        {
            //ClearDefaultTable();
            R.Db(DbName).Table(TableName).Delete().Run(conn);

            var games = new[]
                {
                    new Game {id = 2, player = "Bob", points = 15, type = "ranked"},
                    new Game {id = 5, player = "Alice", points = 7, type = "free"},
                    new Game {id = 11, player = "Bob", points = 10, type = "free"},
                    new Game {id = 12, player = "Alice", points = 2, type = "free"},
                };

            var result = await R.Db(DbName).Table(TableName)
                                .Insert(games)
                                .RunWriteAsync(conn);

            result.AssertInserted(4);
        }

        const string TimeoutFunction = "while(true){}";

        [Test]
        public void canceltoken_directquery()
        {
            var query = R.Js(TimeoutFunction)[new {timeout = 10}];

            using(var cancelSource = new CancellationTokenSource(TimeSpan.FromSeconds(1.5)))
            {
                var token = cancelSource.Token;
                Func<Task> action = async () => await query.RunAsync(conn, token);
                action.ShouldThrow<TaskCanceledException>();
            }
        }


        [Test]
        public void immedately_canceled_cursor_shouldnt_disrupt_current()
        {
            var cursor = R.range(1, 9999)
                .RunCursor<int>(conn);

            using(var cts = new CancellationTokenSource())
            {
                cts.Cancel();
                //preset cancel shouldn't disrupt buffered items.
                var token = cts.Token;
                Func<Task> action = async () => await cursor.MoveNextAsync(token);
                action.ShouldThrow<TaskCanceledException>();

                cursor.Current.Should().Be(0);
            }

        }

        [Test]
        public async Task cancled_token_midway_during_enumeration_shouldnt_distrupt_order()
        {
            var cursor = R.range(1, 9999)
                .RunCursor<int>(conn);

            var consumed = new List<int>();
            using (var cts = new CancellationTokenSource())
            {
                //preset cancel shouldn't disrupt buffered items.
                var token = cts.Token;

                for(int i = 0; i < 25; i++)
                {
                    await cursor.MoveNextAsync(token);
                    consumed.Add(cursor.Current);
                }

                //mid way, we have a problem and cancel.
                cts.Cancel();

                Func<Task> action = async () => await cursor.MoveNextAsync(token);
                action.ShouldThrow<TaskCanceledException>();


                //continue consuming...
                for (int i = 0; i < 25; i++)
                {
                    await cursor.MoveNextAsync(CancellationToken.None);
                    consumed.Add(cursor.Current);
                }
            }

            consumed.Should().Equal(Enumerable.Range(1, 50));

        }
    }
}