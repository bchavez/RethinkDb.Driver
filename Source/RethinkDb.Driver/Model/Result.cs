using System;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace RethinkDb.Driver.Model
{
    /// <summary>
    /// A typed helper for reading response meta data.
    /// </summary>
    public class Result
    {
        /// <summary>
        /// A list of generated primary keys for inserted documents whose primary keys were not specified (capped to 100,000).
        /// </summary>
        [JsonProperty("generated_keys")]
        public Guid[] GeneratedKeys { get; set; }

        /// <summary>
        /// The number of documents successfully inserted.
        /// </summary>
        public ulong Inserted { get; set; }

        /// <summary>
        /// The number of documents updated when conflict is set to "replace" or "update".
        /// </summary>
        public ulong Replaced { get; set; }

        /// <summary>
        /// The number of documents whose fields are identical to existing documents with the same primary key when conflict is set to "replace" or "update".
        /// </summary>
        public ulong Unchanged { get; set; }

        /// <summary>
        /// The number of errors encountered while performing the insert.
        /// </summary>
        public ulong Errors { get; set; }

        /// <summary>
        /// If errors were encountered, contains the text of the first error.
        /// </summary>
        [JsonProperty("first_error")]
        public string FirstError { get; set; }

        /// <summary>
        /// Deleted: 0 for an insert operation.
        /// </summary>
        public ulong Deleted { get; set; }

        /// <summary>
        /// Skipped: 0 for an insert operation.
        /// </summary>
        public ulong Skipped { get; set; }

        /// <summary>
        /// If the field generated_keys is truncated, you will get the warning “Too many generated keys (...), array truncated to 100000.”.
        /// </summary>
        public string[] Warnings { get; set; }

        /// <summary>
        /// If returnChanges is set to true, this will be an array of objects, one for each objected affected by the insert operation. Each object will have two keys: {new_val: -new value-, old_val: null}.
        /// </summary>
        public JArray Changes { get; set; }

        public Change<T>[] ChangesAs<T>()
        {
            return this.Changes?.ToObject<Change<T>[]>(Net.Converter.Serializer);
        }


        /// <summary>
        /// The value is an integer indicating the number of tables waited for. It will always be 1 when wait is called on a table, and the total number of tables when called on a database.
        /// </summary>
        public uint? Ready { get; set; } //probably need to move this to something like AdminResult or DdlResult

        [JsonProperty("dbs_created")]
        public uint DatabasesCreated { get; set; }
        [JsonProperty("dbs_dropped")]
        public uint DatabasesDropped { get; set; }

        [JsonProperty("tables_created")]
        public uint TablesCreated { get; set; }
        [JsonProperty("tables_dropped")]
        public uint TablesDropped { get; set; }
    }
}