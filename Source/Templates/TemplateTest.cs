using NUnit.Framework;
using Templates.Metadata;

namespace Templates
{
    [TestFixture]
    public class TemplateTest
    {
        [TestFixtureSetUp]
        public void BeforeRunningTestSession()
        {
            MetaDb.Initialize(@"..\..\Metadata");
        }
    }
}