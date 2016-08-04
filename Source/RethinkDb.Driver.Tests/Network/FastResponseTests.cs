using System;
using FluentAssertions;
using NUnit.Framework;
using RethinkDb.Driver.Net;
using RethinkDb.Driver.Proto;
using RethinkDb.Driver.Tests.Utils;

namespace RethinkDb.Driver.Tests.Network
{
    [TestFixture]
    public class FastResponseTests
    {
        [Test]
        public void ensure_correct_number_of_fast_response_types()
        {
            Enum.GetValues(typeof(ResponseType))
                .Length.Should().Be(ResponseTypeLong.Total);
        }

        [Test]
        public void ensure_distance_between_request_types()
        {
            var diff = ResponseTypeLong.SUCCESS_ATOM - ResponseTypeLong.SUCCESS_SEQUENCE;

            diff.Dump();

        }
    }

}