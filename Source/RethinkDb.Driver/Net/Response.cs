using System.Collections.Generic;
using Common.Logging;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using RethinkDb.Driver.Ast;
using RethinkDb.Driver.Model;
using RethinkDb.Driver.Proto;

namespace RethinkDb.Driver.Net
{
    public class Response
	{
        private static ILog log = Log.Instance;
        public long Token { get; }
        public ResponseType Type1 { get; }
        public List<ResponseNote> Notes { get; }

        public JArray Data { get; }
        public Profile Profile { get; }
        public Backtrace Backtrace { get; }
        public ErrorType ErrorType { get; }


        public static Response ParseFrom(long token, string buf)
		{
		    var jsonResp = JObject.Parse(buf);
            log.Debug("Received: " + jsonResp);
            var responseType = jsonResp["t"].ToObject<ResponseType>();
		    var responseNotes = jsonResp["n"]?.ToObject<List<ResponseNote>>() ?? new List<ResponseNote>();
			ErrorType? et = jsonResp["e"]?.ToObject<ErrorType>();

            Builder res = new Builder(token, responseType);
			if (et != null)
			{
			    res.errorType = et.Value;
			}
		    return res.SetProfile((JArray)jsonResp["p"])
		        .SetBacktrace((JArray)jsonResp["b"])
		        .SetData((JArray)jsonResp["r"] ?? new JArray())
		        .Build();
		}

        private Response(long token, ResponseType responseType, JArray data, List<ResponseNote> responseNotes, Profile profile, Backtrace backtrace, ErrorType errorType)
        {
            this.Token = token;
            this.Type1 = responseType;
            this.Data = data;
            this.Notes = responseNotes;
            this.Profile = profile;
            this.Backtrace = backtrace;
            this.ErrorType = errorType;
        }

        internal class Builder
		{
			internal long token;
			internal ResponseType responseType;
			internal List<ResponseNote> notes = new List<ResponseNote>();
			internal JArray data = new JArray();
			internal Profile profile;
			internal Backtrace backtrace;
			internal ErrorType errorType;

			internal Builder(long token, ResponseType responseType)
			{
				this.token = token;
				this.responseType = responseType;
			}

			internal virtual Builder SetNotes(List<ResponseNote> notes)
			{
				this.notes.AddRange(notes);
				return this;
			}

			internal virtual Builder SetData(JArray data)
			{
				if (data != null)
				{
					this.data = data;
				}
				return this;
			}

			internal virtual Builder SetProfile(JArray profile)
			{
				this.profile = Profile.FromJsonArray(profile);
				return this;
			}

			internal virtual Builder SetBacktrace(JArray backtrace)
			{
				this.backtrace = Backtrace.FromJsonArray(backtrace);
				return this;
			}

			internal virtual Builder SetErrorType(int value)
			{
				this.errorType = (ErrorType)value;
				return this;
			}

			internal virtual Response Build()
			{
				return new Response(token, responseType, data, notes, profile, backtrace, errorType);
			}
		}

		internal static Builder Make(long token, ResponseType type)
		{
			return new Builder(token, type);
		}

		internal virtual bool IsWaitComplete
		{
			get
			{
				return Type1 == ResponseType.WAIT_COMPLETE;
			}
		}

		/* Whether the response is any kind of feed */
		internal virtual bool IsFeed
		{
			get
			{
			    return Notes.TrueForAll(rn => rn.IsFeed());
			}
		}

		/* Whether the response is any kind of error */
		internal virtual bool IsError
		{
			get
			{
				return Type1.IsError();
			}
		}

		/* What type of success the response contains */
		internal virtual bool IsAtom
		{
			get
			{
				return Type1 == ResponseType.SUCCESS_ATOM;
			}
		}

		internal virtual bool IsSequence
		{
			get
			{
				return Type1 == ResponseType.SUCCESS_SEQUENCE;
			}
		}

		internal virtual bool IsPartial
		{
			get
			{
				return Type1 == ResponseType.SUCCESS_PARTIAL;
			}
		}

		internal virtual ReqlError MakeError(Query query)
		{
			string msg = Data.Count > 0 ? (string) Data[0] : "Unknown error message";
			return (new ErrorBuilder(msg, Type1)).SetBacktrace(Backtrace).SetErrorType(ErrorType).SetTerm(query).Build();
		}

		public override string ToString()
		{
		    return JsonConvert.SerializeObject(this, Formatting.Indented);
		}
	}

}