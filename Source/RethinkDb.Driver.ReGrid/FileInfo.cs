using System;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using Newtonsoft.Json.Linq;

namespace RethinkDb.Driver.ReGrid
{
    public class FileInfo
    {
        public FileInfo()
        {
            this.Status = Status.Incomplete;
        }

        [JsonProperty("id", DefaultValueHandling = DefaultValueHandling.Ignore)]
        public Guid Id { get; set; }

        [JsonProperty("chunkSizeBytes")]
        public int ChunkSizeBytes { get; set; }

        [JsonProperty("filename")]
        public string FileName { get; set; }

        [JsonProperty("length")]
        public long Length { get; set; }

        [JsonProperty("uploadDate")]
        public DateTimeOffset? UploadDate { get; set; }

        [JsonProperty("startedDate")]
        public DateTimeOffset StartedDate { get; set; }

        [JsonProperty("sha256")]
        public string SHA256 { get; set; }

        [JsonProperty("metadata")]
        public JObject Metadata { get; set; }

        [JsonProperty("status")]
        public Status Status { get; set; }
    }

    [JsonConverter(typeof(StringEnumConverter))]
    public enum Status
    {
        Incomplete,
        Completed
    }
}
