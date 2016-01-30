using System;
using System.Threading.Tasks;
using RethinkDb.Driver.Model;
using RethinkDb.Driver.Utils;

namespace RethinkDb.Driver.ReGrid
{
    public partial class Bucket
    {
        public void Delete(Guid fileId, bool softDelete = true, object deleteOpts = null)
        {
            DeleteAsync(fileId, softDelete, deleteOpts).WaitSync();
        }

        public async Task DeleteAsync(Guid fileId, bool softDelete = true, object deleteOpts = null)
        {
            var result = await this.fileTable.get(fileId)
                .update(r.hashMap(FileInfo.StatusJsonName, Status.Deleted))[deleteOpts]
                .runResultAsync(conn)
                .ConfigureAwait(false);

            result.AssertReplaced(1);

            if( !softDelete )
            {
                //delete the chunks....
                await this.chunkTable.between(
                    r.array(fileId, r.minval()),
                    r.array(fileId, r.maxval()))[new {index = this.chunkIndexName}]
                    .delete()[deleteOpts]
                    .runResultAsync(conn)
                    .ConfigureAwait(false);

                //then delete the file.
                await this.fileTable.get(fileId).delete()[deleteOpts]
                    .runResultAsync(conn)
                    .ConfigureAwait(false);
            }
        }

    }
}
