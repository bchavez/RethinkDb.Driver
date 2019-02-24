using FluentAssertions;
using Newtonsoft.Json.Linq;
using NUnit.Framework;
using RethinkDb.Driver.Model;
using RethinkDb.Driver.Net;
using RethinkDb.Driver.Tests.Utils;

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
            JObject reqlType = R.binary(new byte[] { 1, 2, 3 }).Run<JObject>(conn, new {binary_format = "raw"});
            reqlType[Converter.PseudoTypeKey].ToString().Should().Be("BINARY");
        }

        public class TestObj1
        {
            public string Name { get; set; }
            public byte[] Bytes { get; set; } = null;
        }

        [Test]
        public void can_serdez_null_byte_array()
        {
            var arr = new TestObj1();
            arr.Name = "SomeTest";
            arr.Bytes = null;


            var result = R.Db(DbName).Table(TableName)
                .Insert(arr)
                .RunWrite(conn);

            result.AssertInserted(1);

            var id = result.GeneratedKeys[0];

            var fromdb = R.Db(DbName).Table(TableName)
                .Get(id)
                .RunResult<TestObj1>(conn);

            fromdb.Dump();

            fromdb.Name.Should().Be(arr.Name);
            fromdb.Bytes.Should().BeNull();
        }
    }
}