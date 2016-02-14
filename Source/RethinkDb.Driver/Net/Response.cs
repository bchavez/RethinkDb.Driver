using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using RethinkDb.Driver.Ast;
using RethinkDb.Driver.Model;
using RethinkDb.Driver.Proto;

namespace RethinkDb.Driver.Net
{
    public class Response
    {
        private const string TypeKey = "t";
        private const string NotesKey = "n";
        private const string ErrorKey = "e";
        private const string ProfileKey = "p";
        private const string BacktraceKey = "b";
        private const string DataKey = "r";

        public long Token { get; }
        public ResponseType Type { get; }
        public List<ResponseNote> Notes { get; private set; }

        public JArray Data { get; private set; }
        public Profile Profile { get; private set; }
        public Backtrace Backtrace { get; private set; }
        public ErrorType? ErrorType { get; private set; }

        private Response(long token, ResponseType responseType)
        {
            this.Token = token;
            this.Type = responseType;
        }

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

        public override string ToString()
        {
            return JsonConvert.SerializeObject(this, Formatting.Indented);
        }
    }
}