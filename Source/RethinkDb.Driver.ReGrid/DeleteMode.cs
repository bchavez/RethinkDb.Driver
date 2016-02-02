namespace RethinkDb.Driver.ReGrid
{
    /// <summary>
    /// Delete modes for a file.
    /// </summary>
    public enum DeleteMode
    {
        /// <summary>
        /// Soft-delete operation
        /// </summary>
        Soft = 1,

        /// <summary>
        /// Hard-delete operation
        /// </summary>
        Hard
    }
}