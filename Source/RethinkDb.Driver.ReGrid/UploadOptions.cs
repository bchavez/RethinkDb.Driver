using Newtonsoft.Json.Linq;
using RethinkDb.Driver.Net;

namespace RethinkDb.Driver.ReGrid
{
    public class UploadOptions
    {
        public UploadOptions()
        {
            this.Metadata = new JObject();
        }

        public const int DefaultCunkSize = 255 * 1024;
        public const int DefaultBatchSize = 16 * 1024 * 1024;

        public int ChunkSizeBytes { get; set; } = DefaultCunkSize;
        public int BatchSize { get; set; } = DefaultBatchSize;
        public object ChunkInsertOptions { get; set; }

        public JObject Metadata { get; set; }

        public void SetMetadata(object obj)
        {
            this.Metadata = JObject.FromObject(obj, Converter.Serializer);
        }

        public T GetMetadata<T>()
        {
            return this.Metadata.ToObject<T>(Converter.Serializer);
        }
    }
}
