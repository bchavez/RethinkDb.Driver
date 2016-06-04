using Newtonsoft.Json;

namespace RethinkDb.Driver.Extras.Dao
{
    /// <summary>
    /// Interface for Document of IdT
    /// </summary>
    /// <typeparam name="IdT">Type of ID property</typeparam>
    public interface IDocument<IdT>
    {
        /// <summary>
        /// The Id of the document
        /// </summary>
        IdT Id { get; set; }
    }

    /// <summary>
    /// All Documents (aka. domain objects / entities) should derive from Document of IdT
    /// </summary>
    /// <typeparam name="IdT">Type of Id property</typeparam>
    public abstract class Document<IdT> : IDocument<IdT>
    {
        /// <summary>
        /// The Id of the document
        /// </summary>
        [JsonProperty("id",
            NullValueHandling = NullValueHandling.Ignore,
            DefaultValueHandling = DefaultValueHandling.Ignore)]
        public virtual IdT Id { get; set; }
    }
}