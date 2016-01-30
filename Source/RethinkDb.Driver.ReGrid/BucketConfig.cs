namespace RethinkDb.Driver.ReGrid
{
    public class BucketConfig
    {
        public string FileTableName { get; set; } = "files";
        public string FileIndexPath { get; set; } = "path_ix";
        public string ChunkTable { get; set; } = "chunks";
        public string ChunkIndex { get; set; } = "chunks_ix";
        public object TableOptions { get; set; }
    }
}
