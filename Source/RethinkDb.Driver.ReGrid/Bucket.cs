using System;
using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;
using RethinkDb.Driver.Ast;
using RethinkDb.Driver.Model;
using RethinkDb.Driver.Net;
using RethinkDb.Driver.Net.Clustering;
using RethinkDb.Driver.Utils;

namespace RethinkDb.Driver.ReGrid
{
    /// <summary>
    /// A ReGrid bucket
    /// </summary>
    public partial class Bucket
    {
        private static readonly RethinkDB R = RethinkDB.R;

        internal readonly IConnection conn;

        private Db db;
        private string databaseName;

        private string chunkTableName;
        internal string chunkIndexName;

        private string fileTableName;
        internal string fileIndex;
        internal string fileIndexPrefix;

        private object tableCreateOpts;
        internal Table fileTable;
        internal Table chunkTable;

        /// <summary>
        /// Flag indicating if the bucket is mounted.
        /// </summary>
        public bool Mounted { get; set; }

        /// <summary>
        /// Creates a new bucket.
        /// </summary>
        /// <param name="conn">A <see cref="Connection"/> or <see cref="ConnectionPool"/></param>
        /// <param name="databaseName">The database name to use. The database must exist.</param>
        /// <param name="bucketName">The bucket name to use.</param>
        /// <param name="config">Low level bucket configuration options.</param>
        public Bucket(IConnection conn, string databaseName, string bucketName = "fs", BucketConfig config = null)
        {
            this.conn = conn;

            this.databaseName = databaseName;
            this.db = R.Db(this.databaseName);

            config = config ?? new BucketConfig();

            this.tableCreateOpts = config.TableCreateOptions;

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

        /// <summary>
        /// Mounts the bucket. Mount is necessary before using a bucket to ensure the existence of tables and indexes.
        /// </summary>
        public void Mount()
        {
            MountAsync().WaitSync();
        }

        /// <summary>
        /// Mounts the bucket. Mount is necessary before using a bucket to ensure the existence of tables and indexes.
        /// </summary>
        public async Task MountAsync(CancellationToken cancelToken = default)
        {
            if( this.Mounted )
                return;

            var filesTableResult = await EnsureTable(this.fileTableName, cancelToken)
                .ConfigureAwait(false);

            if( filesTableResult.TablesCreated == 1 )
            {
                //index the file paths of completed files and status
                ReqlFunction1 pathIx = row => { return R.Array(row[FileInfo.StatusJsonName], row[FileInfo.FileNameJsonName], row[FileInfo.FinishedDateJsonName]); };
                await CreateIndex(this.fileTableName, this.fileIndex, pathIx, cancelToken)
                    .ConfigureAwait(false);


                //prefix IX
                ReqlFunction1 prefixIx = doc =>
                    {
                        //return r.array(doc[FileInfo.FileNameJsonName].split("/").slice(1, -1), doc[FileInfo.FinishedDateJsonName]);
                        return R.Branch(doc[FileInfo.StatusJsonName].Eq(Status.Completed),
                            R.Array(doc[FileInfo.FileNameJsonName].Split("/").Slice(1, -1), doc[FileInfo.FinishedDateJsonName]),
                            R.Error());
                    };
                await CreateIndex(this.fileTableName, this.fileIndexPrefix, prefixIx, cancelToken)
                    .ConfigureAwait(false);
            }


            // CHUNK TAABLE INDEXES

            var chunkTableResult = await EnsureTable(this.chunkTableName, cancelToken)
                .ConfigureAwait(false);

            if( chunkTableResult.TablesCreated == 1 )
            {
                //Index the chunks and their parent [fileid, n].
                ReqlFunction1 chunkIx = row => { return R.Array(row[Chunk.FilesIdJsonName], row[Chunk.NumJsonName]); };
                await CreateIndex(this.chunkTableName, this.chunkIndexName, chunkIx, cancelToken)
                    .ConfigureAwait(false);
            }

            this.Mounted = true;
        }


        /// <summary>
        /// Helper function to create an index
        /// </summary>
        protected internal async Task<JArray> CreateIndex(string tableName, string indexName, ReqlFunction1 indexFunc, CancellationToken cancelToken = default)
        {
            await this.db.Table(tableName)
                .IndexCreate(indexName, indexFunc).RunAtomAsync<JObject>(conn, cancelToken)
                .ConfigureAwait(false);

            return await this.db.Table(tableName)
                .IndexWait(indexName).RunAtomAsync<JArray>(conn, cancelToken)
                .ConfigureAwait(false);
        }

        /// <summary>
        /// Helper function to ensure table exists.
        /// </summary>
        protected internal async Task<Result> EnsureTable(string tableName, CancellationToken cancelToken = default)
        {
            return await this.db.TableList().Contains(tableName)
                .Do_(tableExists =>
                    R.Branch(tableExists, new {tables_created = 0}, db.TableCreate(tableName)[this.tableCreateOpts])
                ).RunWriteAsync(this.conn, cancelToken)
                .ConfigureAwait(false);
        }
    }
}