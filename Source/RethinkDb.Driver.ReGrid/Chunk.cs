using System;
using Newtonsoft.Json;

namespace RethinkDb.Driver.ReGrid
{
    /// <summary>
    /// A chunk document
    /// </summary>
    public class Chunk
    {
        internal const string FilesIdJsonName = "file_id";
        internal const string NumJsonName = "num";

        /// <summary>
        /// The chunk ID
        /// </summary>
        [JsonProperty("id", DefaultValueHandling = DefaultValueHandling.Ignore)]
        public Guid Id { get; set; }

        /// <summary>
        /// The file ID this chunk belongs to.
        /// </summary>
        [JsonProperty(FilesIdJsonName)]
        public Guid FileId { get; set; }

        /// <summary>
        /// The sequence in which the chunk appears in the list of chunks.
        /// </summary>
        [JsonProperty(NumJsonName)]
        public int Num { get; set; }

        /// <summary>
        /// The raw data belonging to a chunk
        /// </summary>
        [JsonProperty("data")]
        public byte[] Data { get; set; }
    }
}
