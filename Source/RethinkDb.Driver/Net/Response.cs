using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using RethinkDb.Driver.Ast;
using RethinkDb.Driver.Model;
using RethinkDb.Driver.Proto;

namespace RethinkDb.Driver.Net
{
    /// <summary>
    /// Represents a fast response read from the driver.
    /// </summary>
    public class Response
    {
        /// <summary>
        /// Factory method.
        /// </summary>
        public static Response ReadFrom(long token, byte[] buffer)
        {
            var longType = BitConverter.ToInt64(buffer, 0);

            ResponseType responseType = ResponseType.SUCCESS_ATOM;
            switch( longType )
            {
                case ResponseTypeLong.SUCCESS_ATOM:
                    break;
                case ResponseTypeLong.SUCCESS_SEQUENCE:
                    responseType = ResponseType.SUCCESS_SEQUENCE;
                    break;
                case ResponseTypeLong.SUCCESS_PARTIAL:
                    responseType = ResponseType.SUCCESS_PARTIAL;
                    break;
                case ResponseTypeLong.RUNTIME_ERROR:
                    responseType = ResponseType.RUNTIME_ERROR;
                    break;
                case ResponseTypeLong.COMPILE_ERROR:
                    responseType = ResponseType.COMPILE_ERROR;
                    break;
                case ResponseTypeLong.CLIENT_ERROR:
                    responseType = ResponseType.CLIENT_ERROR;
                    break;
                case ResponseTypeLong.WAIT_COMPLETE:
                    responseType = ResponseType.WAIT_COMPLETE;
                    break;
                case ResponseTypeLong.SERVER_INFO:
                    responseType = ResponseType.SERVER_INFO;
                    break;
                default:
                    Log.Trace($"ERROR: Unknown Response Type Long: {longType}");
                    break;
            }

            var json = Encoding.UTF8.GetString(buffer);
            Log.Trace($"JSON Recv: Token: {token}, JSON: {json}");
            return new Response(token, responseType, json);
        }

        internal Response(long token, ResponseType type, string json)
        {
            this.Token = token;
            this.Type = type;
            this.Json = json;
        }

        /// <summary>
        /// The token ID associated with the query.
        /// </summary>
        public long Token { get; }

        /// <summary>
        /// The response type <see cref="ResponseType"/>.
        /// </summary>
        public ResponseType Type { get; }

        /// <summary>
        /// The raw JSON read over the wire.
        /// </summary>
        public string Json { get; }

        /// <summary>
        /// Whether or not a wait operation is completed for a given query.
        /// </summary>
        public bool IsWaitComplete => this.Type == ResponseType.WAIT_COMPLETE;

        /// <summary>
        /// Whether or not the response from the server was an error for <see cref="Token"/>
        /// </summary>
        public bool IsError => this.Type.IsError();

        /// <summary>
        /// Whether or not the response represents an object.
        /// </summary>
        public bool IsAtom => this.Type == ResponseType.SUCCESS_ATOM;

        /// <summary>
        /// Whether or not the response represents a completed sequence (aka completed cursor).
        /// </summary>
        public bool IsSequence => this.Type == ResponseType.SUCCESS_SEQUENCE;

        /// <summary>
        /// Whether or not the response represents a partial success (aka cursor)
        /// </summary>
        public bool IsPartial => this.Type == ResponseType.SUCCESS_PARTIAL;
    }

    /// <summary>
    /// Response from the server.
    /// </summary>
    public class OldResponse
    {
        private const string TypeKey = "t";
        private const string NotesKey = "n";
        private const string ErrorKey = "e";
        private const string ProfileKey = "p";
        private const string BacktraceKey = "b";
        private const string DataKey = "r";

        /// <summary>
        /// The token ID associated with the query.
        /// </summary>
        public long Token { get; }

        /// <summary>
        /// The response type <see cref="ResponseType"/>.
        /// </summary>
        public ResponseType Type { get; }

        /// <summary>
        /// Notes about the response <see cref="ResponseNote"/>
        /// </summary>
        public List<ResponseNote> Notes { get; private set; }

        /// <summary>
        /// The data payload.
        /// </summary>
        public JArray Data { get; private set; }

        /// <summary>
        /// Profile information about the query
        /// </summary>
        public Profile Profile { get; private set; }

        /// <summary>
        /// Backtrace information about a failed query.
        /// </summary>
        public Backtrace Backtrace { get; private set; }

        /// <summary>
        /// The error type, if any.
        /// </summary>
        public ErrorType? ErrorType { get; private set; }

        private OldResponse(long token, ResponseType responseType)
        {
            this.Token = token;
            this.Type = responseType;
        }

        /// <summary>
        /// Parses a Response from a raw JSON string
        /// </summary>
        public static OldResponse ParseFrom(long token, string buf)
        {
            Log.Trace($"JSON Recv: Token: {token}, JSON: {buf}");

            var jsonResp = ParseJson(buf);
            var responseType = jsonResp[TypeKey].ToObject<ResponseType>();
            var responseNotes = jsonResp[NotesKey]?.ToObject<List<ResponseNote>>() ?? new List<ResponseNote>();
            ErrorType? et = jsonResp[ErrorKey]?.ToObject<ErrorType>();

            var profile = Profile.FromJsonArray((JArray)jsonResp[ProfileKey]);
            var backtrace = Backtrace.FromJsonArray((JArray)jsonResp[BacktraceKey]);

            var res = new OldResponse(token, responseType)
                {
                    ErrorType = et,
                    Profile = profile,
                    Backtrace = backtrace,
                    Data = (JArray)jsonResp[DataKey] ?? new JArray(),
                    Notes = responseNotes
                };

            return res;
        }

        private static JObject ParseJson(string buf)
        {
            using( var reader = new JsonTextReader(new StringReader(buf)) )
            {                
                return Converter.Serializer.Deserialize<JObject>(reader);
            }
        }


        internal virtual bool IsWaitComplete => this.Type == ResponseType.WAIT_COMPLETE;

        /* Whether the response is any kind of feed */

        internal virtual bool IsFeed => this.Notes.Any(rn => rn.IsFeed());

        /* Whether the response is any kind of error */

        internal virtual bool IsError => this.Type.IsError();

        /* What type of success the response contains */

        internal virtual bool IsAtom => this.Type == ResponseType.SUCCESS_ATOM;

        internal virtual bool IsSequence => this.Type == ResponseType.SUCCESS_SEQUENCE;

        internal virtual bool IsPartial => this.Type == ResponseType.SUCCESS_PARTIAL;

        internal virtual ReqlError MakeError(Query query)
        {
            string msg = this.Data.Count > 0 ? (string)Data[0] : "Unknown error message";
            return new ErrorBuilder(msg, this.Type)
                .SetBacktrace(this.Backtrace)
                .SetErrorType(this.ErrorType.GetValueOrDefault())
                .SetTerm(query)
                .Build();
        }

        /// <summary>
        /// Pretty printing a response for diagnostics.
        /// </summary>
        public override string ToString()
        {
            return JsonConvert.SerializeObject(this, Formatting.Indented);
        }
    }
}