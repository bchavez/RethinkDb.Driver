using FluentBuild;
using NUnit.Framework;

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
        }
    }
}