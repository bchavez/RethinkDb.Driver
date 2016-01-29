using System;
using Newtonsoft.Json;

namespace RethinkDb.Driver.ReGrid
{
    public class Chunk
    {
        [JsonProperty("id", DefaultValueHandling = DefaultValueHandling.Ignore)]
        public Guid Id { get; set; }

        [JsonProperty("files_id")]
        public Guid FilesId { get; set; }

        [JsonProperty("n")]
        public int Num { get; set; }

        [JsonProperty("data")]
        public byte[] Data { get; set; }
    }
}
