using System;
using NUnit.Framework;
using RethinkDb.Driver.Tests.Utils;

namespace RethinkDb.Driver.Tests.ReQL
{
    [TestFixture]
    public class OtherTests : QueryTestFixture
    {
        [Test]
        public void test_timezone()
        {
            var val = r.epochTime(1.4444445).toIso8601().run(conn);
            Console.WriteLine(val.GetType());
        }

        [Test]
        public void test_date_time_conversion()
        {
            var dt = TestingCommon.datetime.fromtimestamp(896571240L, TestingCommon.ast.rqlTzinfo("00:00"));
            dt.Dump();

            var dt2 = TestingCommon.datetime.fromtimestamp(1375147296.681, TestingCommon.ast.rqlTzinfo("-07:00"));
            dt2.Dump();
        }
    }
}