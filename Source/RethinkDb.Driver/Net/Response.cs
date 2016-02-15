using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using RethinkDb.Driver.Ast;
using RethinkDb.Driver.Model;
using RethinkDb.Driver.Proto;

namespace RethinkDb.Driver.Net
{
    /// <summary>
    /// Response from the server.
    /// </summary>
    public class Response
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

        private Response(long token, ResponseType responseType)
        {
            this.Token = token;
            this.Type = responseType;
        }

        /// <summary>
        /// Parses a Response from a raw JSON string
        /// </summary>
        public static Response ParseFrom(long token, string buf)
        {
            Log.Trace($"JSON Recv: Token: {token}, JSON: {buf}");
            var jsonResp = JObject.Parse(buf);
            var responseType = jsonResp[TypeKey].ToObject<ResponseType>();
            var responseNotes = jsonResp[NotesKey]?.ToObject<List<ResponseNote>>() ?? new List<ResponseNote>();
            ErrorType? et = jsonResp[ErrorKey]?.ToObject<ErrorType>();

            var profile = Profile.FromJsonArray((JArray)jsonResp[ProfileKey]);
            var backtrace = Backtrace.FromJsonArray((JArray)jsonResp[BacktraceKey]);

            var res = new Response(token, responseType)
                {
                    ErrorType = et,
                    Profile = profile,
                    Backtrace = backtrace,
                    Data = (JArray)jsonResp[DataKey] ?? new JArray(),
                    Notes = responseNotes
                };

            return res;
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