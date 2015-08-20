using System;
using System.Collections.Generic;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using NUnit.Framework;
using Templates.Metadata;

namespace Templates
{
    [TestFixture]
    public class EnumTests : TemplateTest
    {
        [Test]
        public void VersionTest()
        {
            var versions = MetaDb.Protocol.SelectToken("VersionDummy.Version").ToObject<Dictionary<string, object>>();

            var tmpl = new Enum
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

            var tmpl = new Enum
            {
                EnumName = "Protocol",
                Enums = versions
            };
            Console.WriteLine(tmpl.TransformText());
        }
    }
}