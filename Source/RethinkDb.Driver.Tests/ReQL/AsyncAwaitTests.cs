using FluentAssertions;
using NUnit.Framework;

namespace RethinkDb.Driver.Tests.ReQL
{
    [TestFixture]
    public class AsyncAwaitTests : QueryTestFixture
    {
        [Test]
        public async void basic_test()
        {
            bool b = await r.expr(true).runAsync<bool>(conn);

            b.Should().Be(true);
        }

        [Test]
        public async void async_insert()
        {
            
        }

        [Test]
        public void asnync_()
        {
            
        }
    }


    [TestFixture]
    public class ChangeFeedTests : QueryTestFixture
    {  
        [Test]
        public void Test()
        {
            
        }
    }

}