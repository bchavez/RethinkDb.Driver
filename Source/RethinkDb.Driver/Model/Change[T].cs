using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace RethinkDb.Driver.Model
{
    public enum ChangeState
    {
        Initializing = 1,
        Ready
    }

    public class Change<T>
    {
        [JsonProperty("old_val")]
        public T OldValue { get; set; }
        [JsonProperty("new_val")]
        public T NewValue { get; set; }

        [JsonConverter(typeof(StringEnumConverter))]
        public ChangeState? State { get; set; }
    }
}