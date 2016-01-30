using System;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using Newtonsoft.Json.Linq;

namespace RethinkDb.Driver.ReGrid
{
    public class FileInfo
    {
        internal const string FinishedDateJsonName = "finishedAt";
        internal const string DeletedDateJsonName = "deletedAt";
        internal const string FileNameJsonName = "filename";
        internal const string StatusJsonName = "status";

        public FileInfo()
        {
            this.Status = Status.Incomplete;
        }

        [JsonProperty("id", DefaultValueHandling = DefaultValueHandling.Ignore)]
        public Guid Id { get; set; }

        [JsonProperty("chunkSizeBytes")]
        public int ChunkSizeBytes { get; set; }

        [JsonProperty(FileNameJsonName)]
        public string FileName { get; set; }

        [JsonProperty("length")]
        public long Length { get; set; }

        [JsonProperty(FinishedDateJsonName)]
        public DateTimeOffset? FinishedAtDate { get; set; }

        [JsonProperty("startedAt")]
        public DateTimeOffset StartedAtDate { get; set; }

        [JsonProperty(DeletedDateJsonName)]
        public DateTimeOffset? DeletedAtDate { get; set; }

        [JsonProperty("sha256")]
        public string SHA256 { get; set; }

        [JsonProperty("metadata")]
        public JObject Metadata { get; set; }

        [JsonProperty(StatusJsonName)]
        public Status Status { get; set; }
    }

    [JsonConverter(typeof(StringEnumConverter))]
    public enum Status
    {
        Incomplete,
        Completed,
        Deleted
    }
}
