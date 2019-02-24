using System.Threading;
using System.Threading.Tasks;
using RethinkDb.Driver.Utils;

namespace RethinkDb.Driver.ReGrid
{
    public partial class Bucket
    {
        /// <summary>
        /// Erases all files from the system inside the bucket.
        /// </summary>
        public void Purge()
        {
            PurgeAsync().WaitSync();
        }

        /// <summary>
        /// Erases all files from the system inside the bucket.
        /// </summary>
        public async Task PurgeAsync(CancellationToken cancelToken = default)
        {
            try
            {
                await this.db.TableDrop(this.fileTableName).RunWriteAsync(this.conn, cancelToken)
                    .ConfigureAwait(false);
            }
            catch
            {
            }

            try
            {
                await this.db.TableDrop(this.chunkTableName).RunWriteAsync(this.conn, cancelToken)
                    .ConfigureAwait(false);
            }
            catch
            {
            }
            this.Mounted = false;
        }
    }
}