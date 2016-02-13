using FluentAssertions;
using Newtonsoft.Json.Linq;
using NUnit.Framework;
using RethinkDb.Driver.Net;

namespace RethinkDb.Driver.Tests.ReQL
{
    [TestFixture]
    public class BinaryTests : QueryTestFixture
    {
        [Test]
        public void binary_echo()
        {
            byte[] data = R.Binary(new byte[] {1, 2, 3}).Run<byte[]>(conn);
            data.Should().Equal(1, 2, 3);
        }


        [Test]
        public void can_get_raw_binary_type()
        {
            JObject reqlType = R.binary(new byte[] { 1, 2, 3 }).Run<JObject>(conn);
            reqlType[Converter.PseudoTypeKey].ToString().Should().Be("BINARY");
        }
    }
}