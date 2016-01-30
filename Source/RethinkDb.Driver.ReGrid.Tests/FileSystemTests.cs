using System;
using System.Threading.Tasks;
using FluentAssertions;
using NUnit.Framework;
using RethinkDb.Driver.Tests.Utils;
using RethinkDb.Driver.Utils;

namespace RethinkDb.Driver.ReGrid.Tests
{
    [TestFixture]
    public class FileSystemTests : BucketTest
    {
    
        [Test]
        public void thow_exception_when_file_isnt_found()
        {
            bucket.Mount();

            Action act = () => bucket.GetFileInfoByNameAsync("foooooooobar.mp3", -99)
                .WaitSync();

            act.ShouldThrow<FileNotFoundException>();
        }


        [Test]
        [Explicit]
        public async Task path_ix_list_history()
        {
            //var files = this.fileTable
            //    .between(
            //        r.array(Status.Completed, "foobar.mp3", r.minval()),
            //        r.array(Status.Completed, "foobar.mp3", r.maxval())
            //        )[new {index = "path_ix"}]
            //    .orderBy()[new { index = r.desc("path_ix") }]
            //    .runCursor<FileInfo>(conn);

            var fileInfo = await bucket.GetFileInfoByNameAsync(testFile, -1);

            fileInfo.Dump();

        }

        [Test]
        public async Task path_ix_file_revision()
        {
            CreateBucketWithTwoFileRevisions();

            Console.WriteLine(">>>> LATEST ");

            var latest = await bucket.GetFileInfoByNameAsync(testFile, -1);

            latest.Dump();

            Console.WriteLine(">>>> ORIGINAL ");

            var original = await bucket.GetFileInfoByNameAsync(testFile, 0);

            original.Dump();

            original.FinishedAtDate.Should().BeBefore(latest.FinishedAtDate.Value);
        }



    }
}