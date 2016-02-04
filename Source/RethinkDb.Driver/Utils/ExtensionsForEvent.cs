using System;

namespace RethinkDb.Driver.Utils
{
    public static class ExtensionsForEvent
    {
        public static void FireEvent<T>(this EventHandler<T> eve, object sender, T arg)
        {
            var handlers = eve?.GetInvocationList();
            if (handlers != null)
            {
                foreach (var del in handlers)
                {
                    var callback = (EventHandler<T>)del;
                    try
                    {
                        callback(sender, arg);
                    }
                    catch
                    {
                    }
                }
            }
        }
    }
}