using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json.Linq;
using RethinkDb.Driver.Utils;

namespace RethinkDb.Driver.Net.Newtonsoft
{
    /// <summary>
    /// Newtonsoft cursor buffer.
    /// </summary>
    public class NewtonsoftCursorBuffer<T> : ICursorBuffer<T>
    {
        private readonly Queue<JToken> items = new Queue<JToken>();
        private readonly FormatOptions fmt;

        /// <summary>
        /// Creates a newtonsoft cursor buffer provided the first response.
        /// Note: This constructor will deserialize the JSON of the first response
        /// and extend it's own internal buffer, ready for enumeration by the
        /// cursor using the this buffer. So, Cursor implementors (not ICursorBuffer implementors)
        /// should not extend ExtendBuffer on the first response, only PrimeCursor that primes
        /// the internal cursor logic.
        /// </summary>
        public NewtonsoftCursorBuffer(Response firstResponse, FormatOptions fmt)
        {
            this.fmt = fmt;
            //we need these to keep track of the run options
            //for things like "time_format: 'raw'"
            var frame = NewtonsoftResponse.ParseFrom(firstResponse.Json);
            this.InitialResponseIsFeed = frame.IsFeed;
            ExtendBuffer(frame);
        }

        /// <summary>
        /// Called when a cursor response for more items is fulfilled.
        /// </summary>
        public void ExtendBuffer(Response res)
        {
            var frame = NewtonsoftResponse.ParseFrom(res.Json);
            ExtendBuffer(frame);
        }

        void ExtendBuffer(NewtonsoftResponse response)
        {
            foreach (var jToken in response.Data)
            {
                items.Enqueue(jToken);
            }
        }

        /// <summary>
        /// Clears the buffer
        /// </summary>
        public void AdvanceCurrent()
        {
            var item = items.Dequeue();

            this.Current = Convert(item, fmt);
        }

        T Convert(JToken token, FormatOptions fmt)
        {
            if (typeof(T).IsJToken())
            {
                Converter.ConvertPseudoTypes(token, fmt);
                return (T)(object)token; //ugh ugly. find a better way to do this.
            }
            return token.ToObject<T>(Converter.Serializer);
        }

        /// <summary>
        /// Clears the buffer
        /// </summary>
        public void Clear()
        {
            items.Clear();
        }

        /// <summary>
        /// Gets the number of currently buffered items.
        /// </summary>
        public int Count { get; }
        /// <summary>
        /// Gets the current item.
        /// </summary>
        public T Current { get; private set; }
        /// <summary>
        /// Should represent the initial response notes about the feed.
        /// The response notes about a feed are in the first response
        /// that created the cursor buffer.
        /// </summary>
        public bool InitialResponseIsFeed { get; }

        /// <summary>
        /// The list of items in the queue. This does not include the Current item.
        /// </summary>
        public List<T> BufferedItems => this.items.Select(i => Convert(i, this.fmt)).ToList();
    }
}