using FluentAssertions;
using NUnit.Framework;
using RethinkDb.Driver.Tests;
using RethinkDb.Driver.Tests.Utils;

namespace RethinkDb.Driver.ReGrid.Tests
{
    public class GithubIssues : QueryTestFixture
    {
        [Test]
        public void issue_28_should_be_able_to_download_on_windows_preview()
        {
            var dbname = "query";
            Bucket bucket = new Bucket(conn, "query", "file");
            bucket.Purge();
            bucket.Mount();

            var uploadId = bucket.Upload("MyNameIsBob.jpg", TestBytes.TwoMB);
            uploadId.Dump();

            byte[] bytes = bucket.DownloadBytes(uploadId, new DownloadOptions());

            bytes.Should().Equal(TestBytes.TwoMB);
        }
    }
}