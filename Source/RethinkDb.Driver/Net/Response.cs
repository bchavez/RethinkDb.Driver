using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using com.rethinkdb;
using com.rethinkdb.net;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using RethinkDb.Driver.Ast;
using RethinkDb.Driver.Model;
using RethinkDb.Driver.Proto;

namespace RethinkDb.Driver.Net
{
    public class Response
	{
		public readonly long token;
		public readonly ResponseType type;
		public readonly List<ResponseNote> notes;

		public readonly JArray data;
		public readonly Profile profile;
		public readonly Backtrace backtrace;
		public readonly ErrorType errorType;


		public static Response parseFrom(long token, string buf)
		{
			//Console.WriteLine("Received: " + buf);
		    var jsonResp = JObject.Parse(buf);
            Console.WriteLine("Received: " + jsonResp);
            var responseType = jsonResp["t"].ToObject<ResponseType>();
		    var responseNotes = jsonResp["n"]?.ToObject<List<ResponseNote>>() ?? new List<ResponseNote>();
			ErrorType? et = jsonResp["e"]?.ToObject<ErrorType>();

            Builder res = new Builder(token, responseType);
			if (et != null)
			{
			    res.errorType = et.Value;
			}
		    return res.setProfile((JArray)jsonResp["p"])
		        .setBacktrace((JArray)jsonResp["b"])
		        .setData((JArray)jsonResp["r"] ?? new JArray())
		        .build();
		}

		private Response(long token, ResponseType responseType, JArray data, List<ResponseNote> responseNotes, Profile profile, Backtrace backtrace, ErrorType errorType)
		{
			this.token = token;
			this.type = responseType;
			this.data = data;
			this.notes = responseNotes;
			this.profile = profile;
			this.backtrace = backtrace;
			this.errorType = errorType;
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

			internal virtual Builder setNotes(List<ResponseNote> notes)
			{
				this.notes.AddRange(notes);
				return this;
			}

			internal virtual Builder setData(JArray data)
			{
				if (data != null)
				{
					this.data = data;
				}
				return this;
			}

			internal virtual Builder setProfile(JArray profile)
			{
				this.profile = Profile.fromJSONArray(profile);
				return this;
			}

			internal virtual Builder setBacktrace(JArray backtrace)
			{
				this.backtrace = Backtrace.fromJSONArray(backtrace);
				return this;
			}

			internal virtual Builder setErrorType(int value)
			{
				this.errorType = (ErrorType)value;
				return this;
			}

			internal virtual Response build()
			{
				return new Response(token, responseType, data, notes, profile, backtrace, errorType);
			}
		}

		internal static Builder make(long token, ResponseType type)
		{
			return new Builder(token, type);
		}

		internal virtual bool WaitComplete
		{
			get
			{
				return type == ResponseType.WAIT_COMPLETE;
			}
		}

		/* Whether the response is any kind of feed */
		internal virtual bool IsFeed
		{
			get
			{
			    return notes.TrueForAll(rn => rn.IsFeed());
			}
		}

		/* Whether the response is any kind of error */
		internal virtual bool IsError
		{
			get
			{
				return type.IsError();
			}
		}

		public static JArray convertPseudotypes(JArray obj, Profile profile)
		{
			return obj; // TODO remove pass-through
		}

		/* What type of success the response contains */
		internal virtual bool Atom
		{
			get
			{
				return type == ResponseType.SUCCESS_ATOM;
			}
		}

		internal virtual bool Sequence
		{
			get
			{
				return type == ResponseType.SUCCESS_SEQUENCE;
			}
		}

		internal virtual bool Partial
		{
			get
			{
				return type == ResponseType.SUCCESS_PARTIAL;
			}
		}

		internal virtual ReqlError makeError(Query query)
		{
			string msg = data.Count > 0 ? (string) data[0] : "Unknown error message";
			return (new ErrorBuilder(msg, type)).setBacktrace(backtrace).setErrorType(errorType).setTerm(query).build();
		}

		public override string ToString()
		{
		    return JsonConvert.SerializeObject(this, Formatting.Indented);
		}
	}

}