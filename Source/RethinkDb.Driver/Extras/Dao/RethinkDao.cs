using System.Threading.Tasks;
using RethinkDb.Driver.Ast;
using RethinkDb.Driver.Model;
using RethinkDb.Driver.Net;
using RethinkDb.Driver.Utils;

namespace RethinkDb.Driver.Extras.Dao
{
    /// <summary>
    /// Generic Document Access Object for RethinkDB
    /// </summary>
    /// <typeparam name="T">Type of Document (aka. domain object / entity)</typeparam>
    /// <typeparam name="IdT">The type of the Id property in Document</typeparam>
    public abstract class RethinkDao<T, IdT> : IDao<T, IdT> where T : IDocument<IdT>
    {
        /// <summary>
        /// RethinkDB query root.
        /// </summary>
        protected static readonly RethinkDB R = RethinkDB.R;
        /// <summary>
        /// RethinkDB connection
        /// </summary>
        protected readonly IConnection conn;
        /// <summary>
        /// RethinkDB database name
        /// </summary>
        protected readonly string DbName;
        /// <summary>
        /// RethinkDB table name
        /// </summary>
        protected readonly string TableName;
        /// <summary>
        /// Table AST term used to build queries stating at the table term.
        /// </summary>
        protected readonly Table Table;

        private readonly object returnChanges;

        /// <summary>
        /// Constructor for <see cref="RethinkDao{T,IdT}"/>
        /// </summary>
        /// <param name="conn">The connection</param>
        /// <param name="dbName">The database</param>
        /// <param name="tableName">The table</param>
        protected RethinkDao(IConnection conn, string dbName, string tableName)
        {
            this.conn = conn;
            this.DbName = dbName;
            this.TableName = tableName;
            this.Table = R.Db(dbName).Table(tableName);

            this.returnChanges = new { return_changes = true};
        }

        /// <summary>
        /// Get a document by Id.
        /// </summary>
        public virtual T GetById(IdT id)
        {
            return GetByIdAsync(id).WaitSync();
        }

        /// <summary>
        /// Get a document by Id.
        /// </summary>
        public virtual Task<T> GetByIdAsync(IdT id)
        {
            return this.Table.Get(id).RunAtomAsync<T>(conn);
        }


        /// <summary>
        /// Save document. If the document exists, an exception will be thrown. Returns and deserializes the returned document.
        /// </summary>
        /// <returns>Returns and deserializes the returned document</returns>
        public virtual T Save(T doc)
        {
            return SaveAsync(doc).WaitSync();
        }

        /// <summary>
        /// Save document. If the document exists, an exception will be thrown. Returns and deserializes the returned document.
        /// </summary>
        /// <returns>Returns and deserializes the returned document</returns>
        public virtual async Task<T> SaveAsync(T doc)
        {
            var result = await this.Table
                .Insert(doc)[returnChanges].OptArg("conflict", "error")
                .RunWriteAsync(conn)
                .ConfigureAwait(false);

            result.AssertNoErrors();
            result.AssertInserted(1);
            return result.ChangesAs<T>()[0].NewValue;
        }

        /// <summary>
        /// Updates an existing document. If the document does not exist, an exception will be thrown. 
        /// </summary>
        public virtual void Update(T doc)
        {
            //var result = this.Table
            //    .Get(doc.Id)
            //    .Update(doc)
            //    .RunResult(conn);

            //var result = this.Table
            //    .Replace(doc)
            //    .RunResult(conn);

            //var result = this.Table
            //    .Get(doc.Id)
            //    .Replace(dbDoc =>
            //        R.Branch(dbDoc == null,
            //            R.Error("The document doesn't exist"),
            //            doc
            //            )).RunResult(conn);

            UpdateAsync(doc).WaitSync();
        }

        /// <summary>
        /// Updates an existing document. If the document does not exist, an exception will be thrown. 
        /// </summary>
        public virtual async Task UpdateAsync(T doc)
        {
            var result = await this.Table
                .GetAll(doc.Id).OptArg("index", "id")
                .Replace(doc)
                .RunWriteAsync(conn)
                .ConfigureAwait(false);

            result.AssertNoErrors();
            result.AssertReplaced(1);
        }

        /// <summary>
        /// Saves or updates a document. If the document doesn't exist, it will be saved. If the document exists, it will be updated.
        /// </summary>
        /// <param name="doc"></param>
        public virtual T SaveOrUpdate(T doc)
        {
            return SaveOrUpdateAsync(doc).WaitSync();
        }

        /// <summary>
        /// Saves or updates a document. If the document doesn't exist, it will be saved. If the document exists, it will be updated.
        /// </summary>
        /// <param name="doc"></param>
        public virtual async Task<T> SaveOrUpdateAsync(T doc)
        {
            var result = await this.Table
                .Insert(doc)[returnChanges].OptArg("conflict", "replace")
                .RunWriteAsync(conn)
                .ConfigureAwait(false);

            result.AssertNoErrors();

            if (result.Inserted != 0)
            {
                result.AssertInserted(1);
                return result.ChangesAs<T>()[0].NewValue;
            }
            if (result.Replaced != 0)
            {
                result.AssertReplaced(1);
                return result.ChangesAs<T>()[0].NewValue;
            }
            throw new ReqlAssertFailure($"{nameof(SaveOrUpdate)} failed.");
        }

        /// <summary>
        /// Deletes a document.
        /// </summary>
        public virtual void Delete(T doc)
        {
            DeleteAsync(doc).WaitSync();
        }

        /// <summary>
        /// Deletes a document.
        /// </summary>
        public virtual Task DeleteAsync(T doc)
        {
            return DeleteByIdAsync(doc.Id);
        }

        /// <summary>
        /// Delete a document by Id.
        /// </summary>
        /// <param name="id"></param>
        public virtual void DeleteById(IdT id)
        {
            DeleteByIdAsync(id).WaitSync();
        }

        /// <summary>
        /// Delete a document by Id.
        /// </summary>
        /// <param name="id"></param>
        public virtual async Task DeleteByIdAsync(IdT id)
        {
            var result = await this.Table.Get(id).Delete()
                .RunWriteAsync(conn)
                .ConfigureAwait(false);
            result.AssertDeleted(1);
        }
    }
}