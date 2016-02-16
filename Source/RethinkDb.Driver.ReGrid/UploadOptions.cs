using Newtonsoft.Json.Linq;
using RethinkDb.Driver.Net;

namespace RethinkDb.Driver.ReGrid
{
    /// <summary>
    /// Upload Options
    /// </summary>
    public class UploadOptions
    {
        /// <summary>
        /// Constructor
        /// </summary>
        public UploadOptions()
        {
            this.Metadata = new JObject();
        }

        /// <summary>
        /// The default chunk size in bytes
        /// </summary>
        public const int DefaultCunkSize = 255 * 1024;

        /// <summary>
        /// The default batch size in bytes
        /// </summary>
        public const int DefaultBatchSize = 16 * 1024 * 1024;

        /// <summary>
        /// The chunk storage size for the file chunks.
        /// </summary>
        public int ChunkSizeBytes { get; set; } = DefaultCunkSize;

        /// <summary>
        /// The number of chunks to buffer before writing a batch of chunks to the server.
        /// </summary>
        public int BatchSize { get; set; } = DefaultBatchSize;

        /// <summary>
        /// Chunk table insert options. Useful for specifying durability requirements.
        /// </summary>
        public object ChunkInsertOptions { get; set; }

        /// <summary>
        /// The metadata to store with the file. Stores RAW JObject (WARN: no pseudo-type conversion).
        /// See <see cref="SetMetadata"/> pseudo-type conversion is necessary.
        /// </summary>
        public JObject Metadata { get; set; }

        /// <summary>
        /// Sets the metadata JObject with pseudo-type conversion.
        /// </summary>
        /// <param name="obj"></param>
        public void SetMetadata(object obj)
        {
            this.Metadata = JObject.FromObject(obj, Converter.Serializer);
        }
    }
}