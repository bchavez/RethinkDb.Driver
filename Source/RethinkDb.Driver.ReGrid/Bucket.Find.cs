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

        public Cursor<FileInfo> Find(Func<Table, string, ReqlExpr> filter)
        {
            return FindAysnc(filter).WaitSync();
        }

        public async Task<Cursor<FileInfo>> FindAysnc(Func<Table, string, ReqlExpr> filter)
        {
            var query = filter(this.fileTable, this.fileIndexPath);
            return await query.runCursorAsync<FileInfo>(conn)
                .ConfigureAwait(false);
        }


    }
}
