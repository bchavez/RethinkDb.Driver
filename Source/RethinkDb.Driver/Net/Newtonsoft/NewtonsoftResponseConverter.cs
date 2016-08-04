using System;
using Newtonsoft.Json.Linq;
using RethinkDb.Driver.Ast;
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
            var frame = NewtonsoftResponse.ParseFrom(response.Json);
            return frame.MakeError(query);
        }

        /// <summary>
        /// Called when <see cref="Connection"/> needs to build an atom object out of the response.
        /// </summary>
        public T MakeAtom<T>(Query query, Response response)
        {
            var frame = NewtonsoftResponse.ParseFrom(response.Json);
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
            var frame = NewtonsoftResponse.ParseFrom(response.Json);
            if (typeof(T).IsJToken())
            {
                var fmt = FormatOptions.FromOptArgs(query.GlobalOptions);
                Converter.ConvertPseudoTypes(frame.Data, fmt);
                return (T)(object)frame.Data; //ugh ugly. find a better way to do this.
            }
            return frame.Data.ToObject<T>(Converter.Serializer);
        }
    }
}