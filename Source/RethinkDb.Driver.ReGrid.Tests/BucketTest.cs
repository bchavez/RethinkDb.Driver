using System.Threading;
using NUnit.Framework;
using RethinkDb.Driver.Ast;
using RethinkDb.Driver.Model;
using RethinkDb.Driver.Tests;

namespace RethinkDb.Driver.ReGrid.Tests
{
    public class BucketTest : QueryTestFixture
    {
        protected Table fileTable;
        protected Table chunkTable;
        protected Db db;
        protected string chunkIndex;
        protected string fileIndexPath;
        protected Bucket bucket;
        protected string fileTableName;
        protected string chunkTableName;

        protected string testFile = "foobar.mp3";

        public BucketTest()
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

        [SetUp]
        public void BeforeEachTest()
        {
            //make sure we get a new conn on each test.
            bucket = new Bucket(conn, DbName);
        }

        protected void DropFilesTable()
        {
            var result = db.tableDrop(this.fileTableName).runResult(this.conn);
            result.AssertTablesDropped(1);
        }

        protected void CreateBucketWithTwoFileRevisions()
        {
            bucket.Purge();
            bucket.Mount();

            //original reversed
            bucket.Upload(testFile, TestBytes.OneHalfChunk);

            Thread.Sleep(1500);

            //latest
            bucket.Upload(testFile, TestBytes.OneHalfChunkReversed);
        }
    }
}