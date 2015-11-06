using System;
using System.Linq;
using System.Net.NetworkInformation;
using FluentBuild;
using FluentBuild.BuildExe;
using NUnit.Framework;
using Templates.Utils;

namespace Builder
{
    [TestFixture]
    public class Testing
    {
        [TestFixtureSetUp]
        public void BeforeRunningTestSession()
        {
            Defaults.Logger.Verbosity = VerbosityLevel.Full;

            System.IO.Directory.SetCurrentDirectory(@"..\..\..\..");
        }

        [Test]
        [Explicit]
        public void Test()
        {
            BauBuild.Main(new [] { "citest"});
        }
    }
}