using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using NUnit.Framework;
using RethinkDb.Driver.Tests.Utils;
using RethinkDb.Driver.Utils;

namespace RethinkDb.Driver.ReGrid.Tests
{
    [TestFixture]
    public class UploadTests : BucketTest
    {
        [Test]
        public void test_upload()
        {
            bucket.Purge();
            bucket.Mount();

            var fileId = bucket.Upload(testFile, TestBytes.OneHalfChunk);

            fileId.Should().NotBeEmpty();

            var info = bucket.GetFileInfoByNameAsync(testFile, -1).WaitSync();

            info.Dump();

            info.Id.Should().Be(fileId);
        }


        [Test]
        public void uploadfile_with_no_bytes_should_have_no_chunks()
        {
            
        }

        [Test]
        public void test_upload_stream()
        {
            bucket.Purge();
            bucket.Mount();

            Guid uploadId;
            using( var fileStream = new MemoryStream(TestBytes.OneHalfChunk))
            using( var uploadStream = bucket.OpenUploadStream(testFile) )
            {
                uploadId = uploadStream.FileInfo.Id;
                fileStream.CopyTo(uploadStream);
            }
            
            var upload = bucket.DownloadAsBytesByName(testFile);

            upload.Should().Equal(TestBytes.OneHalfChunk);
        }
    }
}