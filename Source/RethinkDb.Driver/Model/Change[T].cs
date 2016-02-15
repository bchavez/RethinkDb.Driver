using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace RethinkDb.Driver.Model
{
    /// <summary>
    /// Change feed states
    /// </summary>
    public enum ChangeState
    {
        /// <summary>
        /// {state: 'initializing'} indicates the following documents represent
        /// initial values on the feed rather than changes. This will be the first
        /// document of a feed that returns initial values.
        /// </summary>
        Initializing = 1,
        
        /// <summary>
        /// {state: 'ready'} indicates the following documents represent changes.
        /// This will be the first document of a feed that does not return initial 
        /// values; otherwise, it will indicate the initial values have all been sent.
        /// </summary>
        Ready
    }

    /// <summary>
    /// Change Document Helper
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public class Change<T>
    {
        /// <summary>
        /// When a document is deleted, new_val will be null; when a document is inserted, old_val will be null.
        /// </summary>
        [JsonProperty("old_val")]
        public T OldValue { get; set; }

        /// <summary>
        /// When a document is deleted, new_val will be null; when a document is inserted, old_val will be null.
        /// </summary>
        [JsonProperty("new_val")]
        public T NewValue { get; set; }

        /// <summary>
        /// If include_states = true optional argument was specified, the changefeed stream
        /// will include special status documents consisting of the state 
        /// indicating a change in the feed’s state. These states can occur at any point
        /// in the feed between the notifications. If includeStates = false (the default),
        /// the state status will not be set. See <see cref="ChangeState"/>.
        /// </summary>
        [JsonConverter(typeof(StringEnumConverter))]
        public ChangeState? State { get; set; }
    }
}