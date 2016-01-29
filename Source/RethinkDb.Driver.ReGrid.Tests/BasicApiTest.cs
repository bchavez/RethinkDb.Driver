using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using FluentAssertions;
using NUnit.Framework;
using RethinkDb.Driver.Ast;
using RethinkDb.Driver.Model;
using RethinkDb.Driver.Net;
using RethinkDb.Driver.Tests;
using RethinkDb.Driver.Tests.Utils;
using RethinkDb.Driver.Utils;

namespace RethinkDb.Driver.ReGrid.Tests
{
    [TestFixture]
    public class BucketTests : QueryTestFixture
    {
        private Table fileTable;
        private Table chunkTable;
        private Db db;
        private string chunkIndex;
        private string fileIndexPath;
        private Bucket bucket;
        private string fileTableName;
        private string chunkTableName;


        public BucketTests()
        {
            Log.TruncateBinaryTypes = true;

            fileTable = r.db(DbName).table("fs_files");
            chunkTable = r.db(DbName).table("fs_chunk");
            db = r.db(DbName);
            var opts = new BucketConfig();
            chunkIndex = opts.ChunkIndex;
            fileIndexPath = opts.FileIndexPath;
            fileTableName = "fs_files";
            chunkTableName = "fs_chunks";
        }

        [TestFixtureSetUp]
        public void BeforeRunningTestSession()
        {
            bucket = new Bucket(conn, DbName);
        }

        private void DropFilesTable()
        {
            var result = db.tableDrop(this.fileTableName).runResult(this.conn);
            result.AssertTablesDropped(1);
        }

        [Test]
        public void test_upload()
        {
            DropFilesTable();

            bucket.Initialize();

            var bytes = TestBytes.OneHalfChunk;

            var fileId = bucket.Upload("foobar.mp3", bytes);

            fileId.Should().NotBeEmpty();

            var info = bucket.GetFileInfoByNameAsync("foobar.mp3", -1).WaitSync();

            info.Dump();
        }

        [Test]
        public void test_download_as_bytes()
        {
            var bucket = new Bucket(conn, "query");

            bucket.Initialize();

            var bytes = TestBytes.OneHalfChunk;

            Console.WriteLine(">>>>> UPLOAD");
            bucket.Upload("foobar.mp3", bytes);

            Console.WriteLine(">>>>> DOWNLOAD");
            var grid = bucket.DownloadBytesByName("foobar.mp3");
            bytes.Should().Equal(grid);
        }

        [Test]
        public void thow_exception_when_file_isnt_found()
        {
            bucket.Initialize();

            Action act = () => bucket.GetFileInfoByNameAsync("foooooooobar.mp3", -99)
                .WaitSync();

            act.ShouldThrow<FileNotFoundException>();
        }

        [Test]
        public void test_download_as_steream()
        {
            //CreateBucketWithTwoRevisions();
            var bucket = new Bucket(conn, "query");
            //bucket.Drop();

            bucket.Initialize();

            var bytes = TestBytes.OneHalfChunk;

            //Console.WriteLine(">>>>> UPLOAD");
            //bucket.Upload("foobar.mp3", bytes);

            Console.WriteLine(">>>>> DOWNLOAD");
            var fs = File.Open("test_stream.mp3", FileMode.Create);
            bucket.DownloadToStreamByName("foobar.mp3", fs);
            fs.Close();

            var outBytes = File.ReadAllBytes("test_stream.mp3");

            bytes.Should().Equal(outBytes);
        }

        [Test]
        public void test_upload_with_revision()
        {
            bucket.Drop();
            bucket.Initialize();

            var bytes = TestBytes.OneHalfChunk;

            Console.WriteLine(">>>>> UPLOAD");
            bucket.Upload("foobar.mp3", bytes);

            Thread.Sleep(1000);

            Console.WriteLine(">>>>> UPLOAD 2");
            bucket.Upload("foobar.mp3", bytes.Reverse().ToArray());
        }

        private void CreateBucketWithTwoRevisions()
        {
            var bucket = new Bucket(conn, "query");

            bucket.Initialize();

            var bytes = TestBytes.OneHalfChunk;

            bucket.Upload("foobar.mp3", bytes);

            Thread.Sleep(1000);

            bucket.Upload("foobar.mp3", bytes.Reverse().ToArray());
        }


        [Test]
        public void path_ix_list_history()
        {
            var index = new { index = this.fileIndexPath };

            //var fileInfos = this.fileTable
            //    .between(r.array("foobar.mp3", r.minval()), r.array("foobar.mp3", r.maxval()))[index]
            //    .orderBy(r.desc("uploadDate"))
            //    .runCursor<FileInfo>(conn);
            var files = this.fileTable
                .between(r.array("foobar.mp3", r.minval()), r.array("foobar.mp3", r.maxval()))
                [new { index = this.fileIndexPath }]
                .orderBy(r.desc("uploadDate"))
                .runAtom<List<FileInfo>>(conn);

            foreach (var info in files)
            {
                Console.WriteLine($"{info.Id} -- {info.FileName} { info.UploadDate}");
            }
        }
        [Test]
        public void path_ix_test()
        {
            Console.WriteLine(">>>> BETWEEN");
            //var files = this.fileTable
            //    .between(r.array("foobar.mp3", r.minval()), r.array("foobar.mp3", r.maxval()))
            //    [new {index = this.fileIndexPath}]
            //    .orderBy(r.asc("uploadDate"))
            //    .runAtom<List<FileInfo>>(conn);

            

            //Console.WriteLine($"{fileInfo.Id} -- {fileInfo.FileName} { fileInfo.UploadDate}");

            //foreach ( var info in files )
            //{
            //    Console.WriteLine($"{info.Id} -- {info.FileName} { info.UploadDate}");
            //}
        }
    }

    public static class TestBytes
    {
        public static byte[] OneHalfChunk;
        public static byte[] NoChunks = new byte[0];

        static TestBytes()
        {
            OneHalfChunk = Generate((1024 * 255) + (1024 * 128));// 1.5 chunks
        }

        public static byte[] Generate(int amount)
        {
            return Enumerable.Range(0, amount)
                .Select(i => (byte)(i % 256))
                .ToArray();
        }
    }
}
