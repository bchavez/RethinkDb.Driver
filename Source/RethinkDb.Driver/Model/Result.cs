using System;
using Newtonsoft.Json;

namespace RethinkDb.Driver.Model
{
    public class Result
    {
        [JsonProperty("generated_keys")]
        public Guid[] GeneratedKeys { get; set; }
        public long Inserted { get; set; }
        public long Deleted { get; set; }
        public long Skipped { get; set; }
        public long Replaced { get; set; }
        public long Unchanged { get; set; }
        public long Errors { get; set; }
    }
}