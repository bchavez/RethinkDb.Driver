using System;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using Newtonsoft.Json.Linq;

namespace RethinkDb.Driver.ReGrid
{
    public class FileInfo
    {
        internal const string CreatedDateJsonName = "createdAt";
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

        [JsonProperty(CreatedDateJsonName)]
        public DateTimeOffset? CreatedDate { get; set; }

        [JsonProperty("startedAt")]
        public DateTimeOffset StartedDate { get; set; }

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
        Completed
    }
}
