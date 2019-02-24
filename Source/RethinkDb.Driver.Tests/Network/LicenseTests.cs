using System;
using FluentAssertions;
using NUnit.Framework;
using RethinkDb.Driver.Net;

namespace RethinkDb.Driver.Tests.Network
{
    [TestFixture]
    public class LicenseTests
    {
        [Test]
        public void keys_should_throw()
        {
            Action a = () => LicenseVerifier.AssertKeyIsNotBanned("fff");

            a.ShouldNotThrow();

            Action b = () => LicenseVerifier.AssertKeyIsNotBanned(
                "fuIIq8Pre2NzXVi0otn54PCx22NNAbNReAsk/ylDIV/ZrWeC60B+C76oj3/Ptb8b02vxPYdN6nR2nz3IgYG/O6Zy5TKoYl2UnR2aNq8sKxjv9siwsjMS82EZB8pxs0UwPoz+xmrKY40sqiIz+thDI2EH1MlGoZd+KfJImJp7fvI=");

            b.ShouldThrow<UnauthorizedAccessException>();

            Action c = () => LicenseVerifier.AssertKeyIsNotBanned("tE4z+qpOuKWP4XfmAbnyepzI6m/qx2DaI+aDkMes94ujERmA7O6bb0100+LiClLymVLXYXNvkRBg7ot6NGlfyli/8x1h3IgL+HD8gFoWdTAN4oG8wE8ZyrFugnqmAHUDAy4h/KrOqB8VUXwGQh8Y/0ZxOBQb0KOaZJC/MUMbve8=");

            c.ShouldThrow<UnauthorizedAccessException>();
        }
    }

}