using System;
using System.Collections.Generic;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.Remoting.Contexts;
using System.Threading;
using System.Threading.Tasks;
using RethinkDb.Driver.Ast;
using RethinkDb.Driver.Net;

namespace RethinkDb.Driver.Utils
{
    internal static class ExtensionsForType
    {
#if DNX
        public static bool IsSubclassOf(this Type type, Type other)
        {
            return type.GetTypeInfo().IsSubclassOf(other);
        }
#endif

        public static bool IsGenericType(this Type type)
        {
#if DNX
            return type.GetTypeInfo().IsGenericType;
#else
            return type.IsGenericType;
#endif
        }
    }

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

        public static ConfiguredTaskAwaitable<T> UseInternalAwait<T>(this Task<T> task)
        {
            return task.ConfigureAwait(false);
        }

        public static Task CompletedTask => Task.FromResult(true);
        public static Task<Response> CompletedResponse => Task.FromResult<Response>(null);
    }
}