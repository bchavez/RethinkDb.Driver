using System;
using FluentAssertions;
using NUnit.Framework;
using RethinkDb.Driver.Net;
using RethinkDb.Driver.Proto;

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
    }

}