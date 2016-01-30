using Newtonsoft.Json.Linq;

namespace RethinkDb.Driver.ReGrid
{
    public class UploadOptions
    {
        public const int DefaultCunkSize = 255 * 1024;
        public const int DefaultBatchSize = 16 * 1024 * 1024;

        public int ChunkSizeBytes { get; set; } = DefaultCunkSize;
        public int BatchSize { get; set; } = DefaultBatchSize;
        public object ChunkInsertOptions { get; set; }

        public JObject Metadata { get; set; }
    }
}
