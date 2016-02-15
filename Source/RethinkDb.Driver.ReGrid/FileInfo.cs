using System;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using Newtonsoft.Json.Linq;

namespace RethinkDb.Driver.ReGrid
{
    /// <summary>
    /// A logical view of a file in ReGrid.
    /// </summary>
    public class FileInfo
    {
        internal const string FinishedDateJsonName = "finishedAt";
        internal const string DeletedDateJsonName = "deletedAt";
        internal const string FileNameJsonName = "filename";
        internal const string StatusJsonName = "status";

        /// <summary>
        /// Construct a new FileInfo. Initial status is incomplete.
        /// </summary>
        public FileInfo()
        {
            this.Status = Status.Incomplete;
        }

        /// <summary>
        /// Unique File ID in the bucket.
        /// </summary>
        [JsonProperty("id", DefaultValueHandling = DefaultValueHandling.Ignore)]
        public Guid Id { get; set; }

        /// <summary>
        /// The size of the chunk files.
        /// </summary>
        [JsonProperty("chunkSizeBytes")]
        public int ChunkSizeBytes { get; set; }

        /// <summary>
        /// The full rooted file path name.
        /// </summary>
        [JsonProperty(FileNameJsonName)]
        public string FileName { get; set; }

        /// <summary>
        /// The full length of the file.
        /// </summary>
        [JsonProperty("length")]
        public long Length { get; set; }

        /// <summary>
        /// The time the upload was finished.
        /// </summary>
        [JsonProperty(FinishedDateJsonName)]
        public DateTimeOffset? FinishedAtDate { get; set; }

        /// <summary>
        /// The time the upload started.
        /// </summary>
        [JsonProperty("startedAt")]
        public DateTimeOffset StartedAtDate { get; set; }

        /// <summary>
        /// The time the file was soft-deleted
        /// </summary>
        [JsonProperty(DeletedDateJsonName)]
        public DateTimeOffset? DeletedAtDate { get; set; }

        /// <summary>
        /// The signature of the file.
        /// </summary>
        [JsonProperty("sha256")]
        public string SHA256 { get; set; }

        /// <summary>
        /// Status of the file.
        /// </summary>
        [JsonProperty(StatusJsonName)]
        public Status Status { get; set; }

        /// <summary>
        /// Raw metadata belonging to the file.
        /// </summary>
        [JsonProperty("metadata")]
        public JObject Metadata { get; set; }

        /// <summary>
        /// Get Metadata as T
        /// </summary>
        public T MetadataAs<T>() where T : class 
        {
            return this.Metadata?.ToObject<T>(Net.Converter.Serializer);
        }
    }

    /// <summary>
    /// Status flag for FileInfo
    /// </summary>
    [JsonConverter(typeof(StringEnumConverter))]
    public enum Status
    {
        /// <summary>
        /// Incomplete status indicates that the file has not finished uploading.
        /// </summary>
        Incomplete,
        
        /// <summary>
        /// Completed status indicates that the file was successfully uploaded.
        /// </summary>
        Completed,

        /// <summary>
        /// Deleted status indicates that the file was soft-deleted.
        /// </summary>
        Deleted
    }
}
