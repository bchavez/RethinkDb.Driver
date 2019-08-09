#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member

using System;
using System.Runtime.ExceptionServices;
using System.Threading;
using System.Threading.Tasks;
using RethinkDb.Driver.Net;

namespace RethinkDb.Driver.Utils
{
    public static class TaskHelper
    {
        // Proper Library Sync usage
        // https://channel9.msdn.com/Series/Three-Essential-Tips-for-Async/Async-library-methods-should-consider-using-Task-ConfigureAwait-false-
        // http://blogs.msdn.com/b/lucian/archive/2013/11/23/talk-mvp-summit-async-best-practices.aspx
        public static T WaitSync<T>(this Task<T> task)
        {
            try
            {
                task.Wait();
            }
            catch( AggregateException ae )
            {
                ExceptionDispatchInfo.Capture(ae.InnerException).Throw();
            }
            return task.Result;
        }

        public static void WaitSync(this Task task)
        {
            try
            {
                task.Wait();
            }
            catch( AggregateException ae )
            {
                ExceptionDispatchInfo.Capture(ae.InnerException).Throw();
            }
        }


        static TaskHelper()
        {
            CompletedTask = Task.FromResult(true);
            CompletedTaskTrue = Task.FromResult(true);
            CompletedTaskFalse = Task.FromResult(false);
            CompletedResponse = Task.FromResult<Response>(null);
        }

        public static Task CompletedTask { get; }
        public static Task<bool> CompletedTaskTrue { get; }
        public static Task<bool> CompletedTaskFalse { get; }
        public static Task<Response> CompletedResponse { get; }

        //Loosely based on: https://stackoverflow.com/a/22078975/9275
        public static async Task<bool> WaitOrTimeout<TResult>(this Task<TResult> task, TimeSpan timeout, CancellationToken cancellationToken = default)
        {
            var completedTask = await Task.WhenAny(task, Task.Delay(timeout, cancellationToken))
                .ConfigureAwait(false);

            if (completedTask == task)
            {
                await task;  // Very important in order to propagate exceptions
                return true;
            }

            return false;
        }
    }


}