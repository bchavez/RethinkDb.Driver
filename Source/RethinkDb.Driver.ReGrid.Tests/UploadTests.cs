using System;
using System.IO;
using System.Linq;
using FluentAssertions;
using NUnit.Framework;
using RethinkDb.Driver.Tests.Utils;
using RethinkDb.Driver.Utils;

namespace RethinkDb.Driver.ReGrid.Tests
{
    [TestFixture]
    public class UploadTests : BucketTest
    {
        [SetUp]
        public void BeforeEachTest()
        {
            //make sure we get a new conn on each test.
            bucket.Purge();
            bucket.Mount(); // ensure we have a clear table on each upload.
        }

        [Test]
        public void test_upload()
        {
            var fileId = bucket.Upload(testFile, TestBytes.OneHalfChunk);

            fileId.Should().NotBeEmpty();

            var info = bucket.GetFileInfoByNameAsync(testFile, -1).WaitSync();

            info.Dump();

            info.Id.Should().Be(fileId);
        }


        [Test]
        public void uploadfile_with_no_bytes_should_have_no_chunks()
        {
            var fileId = bucket.Upload(testFile, TestBytes.NoChunks);

            var chunks = GridUtility.EnumerateChunks(bucket, fileId).ToList();
            chunks.Count.Should().Be(0);
        }

        [Test]
        public void uploadoptions_with_different_chunksize()
        {
            var opts = new UploadOptions
                {
                    ChunkSizeBytes = 1024
                };

            var data = TestBytes.Generate(1024 * 2);

            var fileId = bucket.Upload(testFile, data, opts);

            fileId.Should().NotBeEmpty();

            var info = bucket.GetFileInfoByNameAsync(testFile, -1).WaitSync();

            info.ChunkSizeBytes.Should().Be(1024);

            //verify chunks
            var chunks = GridUtility.EnumerateChunks(bucket, info.Id).ToList();

            chunks.Count.Should().Be(2);

            foreach( var chunk in chunks )
            {
                chunk.Data.Length.Should().Be(1024);
            }
        }

        [Test]
        public void test_upload_stream()
        {
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