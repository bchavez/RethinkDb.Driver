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
        private static readonly RethinkDB R = RethinkDB.R;

        internal readonly IConnection conn;
        private readonly BucketConfig config;
        private Db db;
        private string databaseName;

        private string chunkTableName;
        internal string chunkIndexName;

        private string fileTableName;
        internal string fileIndex;
        internal string fileIndexPrefix;

        private object tableOpts;
        internal Table fileTable;
        internal Table chunkTable;

        public bool Mounted { get; set; }

        public Bucket(IConnection conn, string databaseName, string bucketName = "fs", BucketConfig config = null)
        {
            this.conn = conn;

            this.databaseName = databaseName;
            this.db = R.Db(this.databaseName);

            config = config ?? new BucketConfig();

            this.tableOpts = config.TableOptions;

            this.fileTableName = $"{bucketName}_{config.FileTableName}";
            this.fileTable = this.db.Table(fileTableName);
            this.fileIndex = config.FileIndex;
            this.fileIndexPrefix = config.FileIndexPrefix;

            this.chunkTableName = $"{bucketName}_{config.ChunkTable}";
            this.chunkTable = this.db.Table(chunkTableName);
            this.chunkIndexName = config.ChunkIndex;
        }
        

        private void ThrowIfNotMounted()
        {
            if( !this.Mounted )
                throw new InvalidOperationException($"Please call {nameof(Mount)} before performing any operation.");
        }

        public void Mount()
        {
            MountAsync().WaitSync();
        }

        public async Task MountAsync()
        {
            if (this.Mounted)
                return;

            var filesTableResult = await EnsureTable(this.fileTableName)
                .ConfigureAwait(false);

            if( filesTableResult.TablesCreated == 1 )
            {
                //index the file paths of completed files and status
                ReqlFunction1 pathIx = row =>
                    {
                        return R.Array(row[FileInfo.StatusJsonName], row[FileInfo.FileNameJsonName], row[FileInfo.FinishedDateJsonName]);
                    };
                await CreateIndex(this.fileTableName, this.fileIndex,pathIx)
                    .ConfigureAwait(false);


                //prefix IX
                ReqlFunction1 prefixIx = doc =>
                    {
                        //return r.array(doc[FileInfo.FileNameJsonName].split("/").slice(1, -1), doc[FileInfo.FinishedDateJsonName]);
                        return R.Branch(doc[FileInfo.StatusJsonName].Eq(Status.Completed),
                            R.Array(doc[FileInfo.FileNameJsonName].Split("/").Slice(1, -1), doc[FileInfo.FinishedDateJsonName]),
                            R.Error());
                    };
                await CreateIndex(this.fileTableName, this.fileIndexPrefix, prefixIx)
                    .ConfigureAwait(false);
            }


            // CHUNK TAABLE INDEXES

            var chunkTableResult = await EnsureTable(this.chunkTableName)
                .ConfigureAwait(false);

            if( chunkTableResult.TablesCreated == 1 )
            {
                //Index the chunks and their parent [fileid, n].
                ReqlFunction1 chunkIx = row =>
                    {
                        return R.Array(row[Chunk.FilesIdJsonName], row[Chunk.NumJsonName]);
                    };
                await CreateIndex(this.chunkTableName, this.chunkIndexName, chunkIx)
                    .ConfigureAwait(true);
            }

            this.Mounted = true;
        }


        protected internal async Task<JArray> CreateIndex(string tableName, string indexName, ReqlFunction1 indexFunc)
        {
            await this.db.Table(tableName)
                .IndexCreate(indexName, indexFunc).RunAtomAsync<JObject>(conn)
                .ConfigureAwait(false);

            return await this.db.Table(tableName)
                .IndexWait(indexName).RunAtomAsync<JArray>(conn)
                .ConfigureAwait(false);
        }

        protected internal async Task<Result> EnsureTable(string tableName)
        {
            return await this.db.TableList().Contains(tableName)
                .Do_(tableExists =>
                    R.Branch(tableExists, new {tables_created = 0}, db.TableCreate(tableName)[this.tableOpts])
                ).RunResultAsync(this.conn)
                .ConfigureAwait(false);
        }
    }
}
