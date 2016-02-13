using System;
using Newtonsoft.Json;

namespace RethinkDb.Driver.ReGrid
{
    public class Chunk
    {
        internal const string FilesIdJsonName = "file_id";
        internal const string NumJsonName = "num";

        [JsonProperty("id", DefaultValueHandling = DefaultValueHandling.Ignore)]
        public Guid Id { get; set; }

        [JsonProperty(FilesIdJsonName)]
        public Guid FileId { get; set; }

        [JsonProperty(NumJsonName)]
        public int Num { get; set; }

        [JsonProperty("data")]
        public byte[] Data { get; set; }
    }
}
