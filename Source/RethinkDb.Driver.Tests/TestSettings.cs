using System;
using Z.ExtensionMethods;

namespace RethinkDb.Driver.Tests
{
    public static class TestSettings
    {
        public static string TestHost
        {
            get
            {
                if (Environment.GetEnvironmentVariable("CI").IsNotNullOrWhiteSpace())
                {
                    //CI is testing.
                    return "localhost";
                }
                return System.Configuration.ConfigurationManager.AppSettings["TestServer"];
            }
        }

        public static int TestPort
        {
            get
            {
                if (Environment.GetEnvironmentVariable("CI").IsNotNullOrWhiteSpace())
                {
                    //CI is testing.
                    return 28015;
                }
                var port = System.Configuration.ConfigurationManager.AppSettings["TestPort"];
                return int.Parse(port);
            }
        }
    }
}