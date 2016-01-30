using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;
using RethinkDb.Driver.Ast;
using RethinkDb.Driver.Model;
using RethinkDb.Driver.Net;
using RethinkDb.Driver.Utils;

namespace RethinkDb.Driver.ReGrid
{
    public partial class Bucket
    {
        private static readonly RethinkDB r = RethinkDB.r;

        private readonly IConnection conn;
        private readonly BucketConfig config;
        private Db db;
        private string databaseName;

        private string chunkTableName;
        private string chunkIndexName;

        private string fileTableName;
        private string fileIndexPath;

        private object tableOpts;

        public bool Initialized { get; set; }

        public Bucket(IConnection conn, string databaseName, string bucketName = "fs", BucketConfig config = null)
        {
            this.conn = conn;

            this.databaseName = databaseName;
            this.db = r.db(this.databaseName);

            config = config ?? new BucketConfig();

            this.tableOpts = config.TableOptions;

            this.fileTableName = $"{bucketName}_{config.FileTableName}";
            this.fileIndexPath = config.FileIndexPath;

            this.chunkTableName = $"{bucketName}_{config.ChunkTable}";
            this.chunkIndexName = config.ChunkIndex;
        }
        
        public void Delete(string objectId)
        {

        }

        public void DeleteAsync(string objectId)
        {

        }

        public Cursor<FileInfo> Find(Func<Table, string, ReqlExpr> filter)
        {
            return FindAysnc(filter).WaitSync();
        }

        public async Task<Cursor<FileInfo>> FindAysnc(Func<Table, string, ReqlExpr> filter)
        {
            var table = this.db.table(this.chunkTableName);
            var query = filter(table, this.fileIndexPath);
            return await query.runCursorAsync<FileInfo>(conn)
                .ConfigureAwait(false);
        }

        public void Drop()
        {
            DropAsync().WaitSync();
        }

        public async Task DropAsync()
        {
            try
            {
                await this.db.tableDrop(this.fileTableName).runResultAsync(this.conn)
                    .ConfigureAwait(false);
            }
            catch
            {
            }

            try
            {
                await this.db.tableDrop(this.chunkTableName).runResultAsync(this.conn)
                    .ConfigureAwait(false);
            }
            catch
            {
            }
            this.Initialized = false;
        }


        internal async Task<FileInfo> GetFileInfoByNameAsync(string fileName, int revision)
        {
            var index = new { index = this.fileIndexPath };

            var between = this.db.table(this.fileTableName)
                .between(r.array(Status.Completed, fileName, r.minval()), r.array(Status.Completed, fileName, r.maxval()))[index];

            var sort = revision >= 0 ? r.asc(this.fileIndexPath) : r.desc(this.fileIndexPath) as ReqlExpr;

            revision = revision >= 0 ? revision : (revision * -1) - 1;

            var selection = await between.orderBy()[new {index = sort}]
                .skip(revision).limit(1) // so the driver doesn't throw an error when a file isn't found.
                .runResultAsync<List<FileInfo>>(conn)
                .ConfigureAwait(false);

            var fileInfo = selection.FirstOrDefault();
            if( fileInfo == null )
            {
                throw new FileNotFoundException(fileName, revision);
            }

            return fileInfo;
        }

        internal async Task<FileInfo> GetFileInfoAsync(Guid fileId)
        {
            var fileInfo = await this.db.table(this.fileTableName)
                .get(fileId).runAtomAsync<FileInfo>(conn)
                .ConfigureAwait(false);

            if( fileInfo == null )
            {
                throw new FileNotFoundException(fileId);
            }

            return fileInfo;
        }

        private void ThrowIfNotInitialized()
        {
            if( !this.Initialized )
                throw new InvalidOperationException("Please call Bucket.Initialize() first before performing any operation.");
        }

        public void Initialize()
        {
            InitializeAsync().WaitSync();
        }

        public async Task InitializeAsync()
        {
            if (this.Initialized)
                return;

            var filesTableResult = await EnsureTable(this.fileTableName)
                .ConfigureAwait(false);

            if( filesTableResult.TablesCreated == 1 )
            {
                //index the file paths of completed files and status
                await CreateIndex(this.fileTableName, this.fileIndexPath,
                    row => r.array(row[FileInfo.StatusJsonName], row[FileInfo.FileNameJsonName], row[FileInfo.CreatedDateJsonName]))
                    .ConfigureAwait(false);
            }

            var chunkTableResult = await EnsureTable(this.chunkTableName)
                .ConfigureAwait(false);

            if( chunkTableResult.TablesCreated == 1 )
            {
                //Index the chunks and their parent [fileid, n].
                await CreateIndex(this.chunkTableName, this.chunkIndexName,
                    row => r.array(row[Chunk.FilesIdJsonName], row[Chunk.NumJsonName]))
                    .ConfigureAwait(true);
            }

            this.Initialized = true;
        }



        protected internal async Task<JArray> CreateIndex(string tableName, string indexName, ReqlFunction1 indexFunc)
        {
            await this.db.table(tableName)
                .indexCreate(indexName, indexFunc).runAtomAsync<JObject>(conn)
                .ConfigureAwait(false);

            return await this.db.table(tableName)
                .indexWait(indexName).runAtomAsync<JArray>(conn)
                .ConfigureAwait(false);
        }

        protected internal async Task<Result> EnsureTable(string tableName)
        {
            return await this.db.tableList().contains(tableName)
                .do_(tableExists =>
                    r.branch(tableExists, new {tables_created = 0}, db.tableCreate(tableName)[this.tableOpts])
                ).runResultAsync(this.conn)
                .ConfigureAwait(false);
        }
    }
}
