namespace RethinkDb.Driver.ReGrid
{
    public class BucketConfig
    {
        public string FileTableName { get; set; } = "files";
        public string FileIndex { get; set; } = "file_ix";
        public string FileIndexPrefix { get; set; } = "prefix_ix";
        public string ChunkTable { get; set; } = "chunks";
        public string ChunkIndex { get; set; } = "chunk_ix";
        public object TableOptions { get; set; }
    }
}
