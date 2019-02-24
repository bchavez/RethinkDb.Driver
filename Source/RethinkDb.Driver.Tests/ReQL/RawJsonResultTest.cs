using NUnit.Framework;
using System.Collections.Generic;

namespace RethinkDb.Driver.Tests.ReQL {

    [TestFixture]
    public class RawJsonResultTest : QueryTestFixture {

        [Test]
        public void RawJson() {
            IList<long> ids = new List<long>() { 29046937, 27697936 };

            string result = R.Db(DbName).Table(TableName)
                                               .GetAll(R.Args(ids))
                                               .RunAsRawJson(conn);

            Assert.True(true);
        }
    }
}