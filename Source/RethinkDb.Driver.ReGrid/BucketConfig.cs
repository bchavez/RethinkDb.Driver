namespace RethinkDb.Driver.ReGrid
{
    /// <summary>
    /// Low level bucket configuration
    /// </summary>
    public class BucketConfig
    {
        /// <summary>
        /// The file table name
        /// </summary>
        public string FileTableName { get; set; } = "files";

        /// <summary>
        /// The file index
        /// </summary>
        public string FileIndex { get; set; } = "file_ix";

        /// <summary>
        /// The prefix index for "virtual" folder search
        /// </summary>
        public string FileIndexPrefix { get; set; } = "prefix_ix";

        /// <summary>
        /// The chunk table
        /// </summary>
        public string ChunkTable { get; set; } = "chunks";

        /// <summary>
        /// The chunk index
        /// </summary>
        public string ChunkIndex { get; set; } = "chunk_ix";

        /// <summary>
        /// Table create options for <see cref="FileTableName"/> and <see cref="ChunkTable"/> names.
        /// </summary>
        public object TableCreateOptions { get; set; }
    }
}