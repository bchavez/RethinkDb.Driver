using System;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using RethinkDb.Driver.Net;

namespace RethinkDb.Driver.Utils
{
    internal static class TaskHelper
    {
        // Proper Library Sync usage
        // https://channel9.msdn.com/Series/Three-Essential-Tips-for-Async/Async-library-methods-should-consider-using-Task-ConfigureAwait-false-
        // http://blogs.msdn.com/b/lucian/archive/2013/11/23/talk-mvp-summit-async-best-practices.aspx
        public static T WaitSync<T>(this Task<T> task)
        {
            task.Wait();
            return task.Result;
        }

        public static void WaitSync(this Task task)
        {
            task.Wait();
        }


        static TaskHelper()
        {
            CompletedTask = Task.FromResult(true);
            CompletedResponse = Task.FromResult<Response>(null);
        }

        public static Task CompletedTask { get; private set; }
        public static Task<Response> CompletedResponse { get; private set; }
    }
}