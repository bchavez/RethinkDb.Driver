using System;
using System.IO;
using System.Linq;
using FluentAssertions;
using Newtonsoft.Json.Linq;
using NUnit.Framework;
using RethinkDb.Driver.Net;
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
            var fileId = bucket.Upload(testfile, TestBytes.OneHalfChunk);

            fileId.Should().NotBeEmpty();

            var info = bucket.GetFileInfoByNameAsync(testfile, -1).WaitSync();

            info.Dump();

            info.Id.Should().Be(fileId);
        }


        [Test]
        public void uploadfile_with_no_bytes_should_have_no_chunks()
        {
            var fileId = bucket.Upload(testfile, TestBytes.NoChunks);

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

            var fileId = bucket.Upload(testfile, data, opts);

            fileId.Should().NotBeEmpty();

            var info = bucket.GetFileInfoByNameAsync(testfile, -1).WaitSync();

            info.ChunkSizeBytes.Should().Be(1024);

            //verify chunks
            var chunks = GridUtility.EnumerateChunks(bucket, info.Id).ToList();

            chunks.Count.Should().Be(2);

            foreach( var chunk in chunks )
            {
                chunk.Data.Length.Should().Be(1024);
            }
        }

        public class AppMeta
        {
            public string User { get; set; }
            public DateTime? LastAccess { get; set; }
            public string[] Roles { get; set; }
            public string ContentType { get; set; }
        }

        [Test]
        public void can_store_metadata_with_upload()
        {
            var meta = new AppMeta()
                {
                    User = "cowboy",
                    LastAccess = DateTime.Now,
                    Roles = new[] {"admin", "office"},
                    ContentType = "application/pdf"
                };

            var opts = new UploadOptions();
            opts.SetMetadata(meta);

            var id = bucket.Upload(testfile, TestBytes.HalfChunk, opts);

            var fileInfo = bucket.GetFileInfo(id);
            fileInfo.Metadata.Should().NotBeNull();

            var otherMeta = fileInfo.Metadata.ToObject<AppMeta>(Converter.Serializer);

            otherMeta.User.Should().Be(meta.User);
            otherMeta.LastAccess.Should().BeCloseTo(meta.LastAccess.Value, 2000);
            otherMeta.Roles.Should().Equal(meta.Roles);
            otherMeta.ContentType.Should().Be(meta.ContentType);
        }

        [Test]
        public void can_store_simple_meta_with_upload()
        {
            var opts = new UploadOptions();

            opts.SetMetadata(new
                {
                    UserId = "123",
                    LastAccess = R.now(),
                    Roles = R.array("admin", "office"),
                    ContentType = "application/pdf"
                });

            var id = bucket.Upload(testfile, TestBytes.HalfChunk, opts);

            var fileInfo = bucket.GetFileInfo(id);

            fileInfo.Metadata["UserId"].Value<string>().Should().Be("123");
            fileInfo.Dump();
        }

        [Test]
        public void test_upload_stream()
        {
            Guid uploadId;
            using( var fileStream = new MemoryStream(TestBytes.OneHalfChunk) )
            using( var uploadStream = bucket.OpenUploadStream(testfile) )
            {
                uploadId = uploadStream.FileInfo.Id;
                fileStream.CopyTo(uploadStream);
            }

            var upload = bucket.DownloadAsBytesByName(testfile);

            upload.Should().Equal(TestBytes.OneHalfChunk);
        }

        [Test]
        public void upload_files_to_different_paths()
        {
            TestFiles.DifferentPathsNoRevisions(this.bucket);
        }


        [Test]
        public void upload_files_to_different_paths_and_revisions()
        {
            TestFiles.DifferentPathsAndRevisions(this.bucket);
        }

        //DEBUG INDEXES:
        //r.db("query").table("fs_files").map(function(x) { return x('filename').split("/").slice(0, -1); })

        //INDEX: r.db("query").table('fs_files').indexCreate("path_array", function(x) { return x('filename').split("/").slice(1, -1); })

        // Includes duplicates
        //FIND: r.db("query").table("fs_files").getAll(["animals"],{index:"path_array"})

        //FIND: 
        /*
        r.db("query").table("fs_files").between(
                  [["animals"], r.minval],[["animals"], r.maxval],
                {index:"prefix_ix"})
                .group("filename").max("finishedAt").ungroup()("reduction")
        */

    }
}