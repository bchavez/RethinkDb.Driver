namespace RethinkDb.Driver.Extras.Dao
{
    /// <summary>
    /// Document Access Object interface
    /// </summary>
    /// <typeparam name="T">Document entity</typeparam>
    /// <typeparam name="IdT">Type of Id property</typeparam>
    public interface IDao<T, IdT> 
    {
        /// <summary>
        /// Get a document by Id.
        /// </summary>
        T GetById(IdT id);

        ///// <summary>
        ///// Get all documents by Id
        ///// </summary>
        ///// <param name="ids">The Ids of the documents</param>
        //IEnumerable<T> GetAllById(params IdT[] ids);

        /// <summary>
        /// Save document. If the document exists, an exception will be thrown. Returns and deserializes the returned document.
        /// </summary>
        /// <returns>Returns and deserializes the returned document</returns>
        T Save(T doc);
        /// <summary>
        /// Updates an existing document. If the document does not exist, an exception will be thrown. 
        /// </summary>
        void Update(T doc);
        /// <summary>
        /// Saves or updates a document. If the document doesn't exist, it will be saved. If the document exists, it will be updated.
        /// </summary>
        /// <param name="doc"></param>
        T SaveOrUpdate(T doc);
        /// <summary>
        /// Deletes a document.
        /// </summary>
        void Delete(T doc);
        /// <summary>
        /// Delete a document by Id.
        /// </summary>
        /// <param name="id"></param>
        void DeleteById(IdT id);
    }
}