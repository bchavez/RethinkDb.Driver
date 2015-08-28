using System;
using System.Collections.Generic;
using NUnit.Framework;
using Templates.CodeGen;
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

        [Test]
        public void VersionTest()
        {
            var versions = MetaDb.Protocol.SelectToken("VersionDummy.Version").ToObject<Dictionary<string, object>>();

            var tmpl = new EnumTemplate
            {
                EnumName = "Version",
                Enums = versions
            };
            Console.WriteLine(tmpl.TransformText());
        }

        [Test]
        public void ProtocolTest()
        {
            var versions = MetaDb.Protocol.SelectToken("VersionDummy.Protocol").ToObject<Dictionary<string, object>>();

            var tmpl = new EnumTemplate
            {
                EnumName = "Protocol",
                Enums = versions
            };
            Console.WriteLine(tmpl.TransformText());
        }
    }
}