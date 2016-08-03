using System;
using System.Collections.Generic;
using Newtonsoft.Json.Linq;
using RethinkDb.Driver.Ast;
using RethinkDb.Driver.Model;
using RethinkDb.Driver.Utils;

namespace RethinkDb.Driver.Net.Newtonsoft
{
    /// <summary>
    /// Newtonsoft response converter
    /// </summary>
    public class NewtonsoftResponseConverter : IResponseConverter
    {
        /// <summary>
        /// Called when <see cref="Connection"/> needs to build an error out of the response.
        /// </summary>
        public Exception MakeError(Query query, Response response)
        {
            var frame = NewtonsoftParser.ParseFrom(response.Json);
            return frame.MakeError(query);
        }
        /// <summary>
        /// Called when <see cref="Connection"/> needs to build a cursor out of the response.
        /// </summary>
        public Cursor<T> MakeCursor<T>(Query query, Response response, Connection conn)
        {
            return new Cursor<T>(conn, query, response);
        }

        /// <summary>
        /// Called when <see cref="Connection"/> needs to build an atom object out of the response.
        /// </summary>
        public T MakeAtom<T>(Query query, Response response)
        {
            var frame = NewtonsoftParser.ParseFrom(response.Json);
            try
            {
                if( typeof(T).IsJToken() )
                {
                    if( frame.Data[0].Type == JTokenType.Null ) return (T)(object)null;
                    var fmt = FormatOptions.FromOptArgs(query.GlobalOptions);
                    Converter.ConvertPseudoTypes(frame.Data[0], fmt);
                    return (T)(object)frame.Data[0]; //ugh ugly. find a better way to do this.
                }
                return frame.Data[0].ToObject<T>(Converter.Serializer);

            }
            catch( IndexOutOfRangeException ex )
            {
                throw new ReqlDriverError("Atom response was empty!", ex);
            }
        }

        /// <summary>
        /// Normally, the Response would be a request for a Cursor, but the server said
        /// the sequence is already complete, so just directly cast the response type
        /// into T without having to create a Cursor. T is probably IList of T already.
        /// </summary>
        public T MakeSequenceComplete<T>(Query query, Response response)
        {
            var frame = NewtonsoftParser.ParseFrom(response.Json);
            if (typeof(T).IsJToken())
            {
                var fmt = FormatOptions.FromOptArgs(query.GlobalOptions);
                Converter.ConvertPseudoTypes(frame.Data, fmt);
                return (T)(object)frame.Data; //ugh ugly. find a better way to do this.
            }
            return frame.Data.ToObject<T>(Converter.Serializer);
        }
    }

    /// <summary>
    /// Newtonsoft cursor buffer.
    /// </summary>
    public class NewtonsoftCursorBuffer<T> : ICursorBuffer<T>
    {
        private readonly Queue<JToken> items = new Queue<JToken>();

        /// <summary>
        /// Creates a newtonsoft cursor buffer provided the first response.
        /// </summary>
        public NewtonsoftCursorBuffer(Response firstResponse)
        {
            var frame = NewtonsoftParser.ParseFrom(firstResponse.Json);
            this.InitialResponseIsFeed = frame.IsFeed;
            ExtendBuffer(frame);
        }

        /// <summary>
        /// Called when a cursor response for more items is fulfilled.
        /// </summary>
        public void ExtendBuffer(Response res)
        {
            var frame = NewtonsoftParser.ParseFrom(res.Json);
            ExtendBuffer(frame);
        }

        void ExtendBuffer(NewtonsoftParser response)
        {
            foreach (var jToken in response.Data)
            {
                items.Enqueue(jToken);
            }
        }

        /// <summary>
        /// Clears the buffer
        /// </summary>
        public void AdvanceCurrent(FormatOptions fmt)
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
    }
}