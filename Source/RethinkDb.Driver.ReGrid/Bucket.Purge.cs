using System;
using System.Collections.Generic;
using System.IO;
using System.Security.Cryptography;
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
        /// <summary>
        /// Erases all files from the system inside the bucket.
        /// </summary>
        public void Purge()
        {
            DestroyAsync().WaitSync();
        }

        public async Task DestroyAsync()
        {
            try
            {
                await this.db.TableDrop(this.fileTableName).RunResultAsync(this.conn)
                    .ConfigureAwait(false);
            }
            catch
            {
            }

            try
            {
                await this.db.TableDrop(this.chunkTableName).RunResultAsync(this.conn)
                    .ConfigureAwait(false);
            }
            catch
            {
            }
            this.Mounted = false;
        }
    }
}
