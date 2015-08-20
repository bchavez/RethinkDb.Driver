using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using com.rethinkdb;
using com.rethinkdb.model;
using com.rethinkdb.net;
using Newtonsoft.Json.Linq;
using RethinkDb.Driver.Ast;
using RethinkDb.Driver.Model;
using RethinkDb.Driver.Proto;

namespace RethinkDb.Driver.Net
{
	internal class Response
	{
		public readonly long token;
		public readonly ResponseType type;
		public readonly List<ResponseNote> notes;

		public readonly JArray data;
		public readonly Profile profile;
		public readonly Backtrace backtrace;
		public readonly ErrorType errorType;


		public static Response parseFrom(long token, ByteBuffer buf)
		{
			Console.WriteLine("Received: " + Util.bufferToString(buf));
			StreamReader codepointReader = new StreamReader(new ByteArrayInputStream(buf.array()));
			JObject jsonResp = (JObject) JSONValue.parse(codepointReader);
			ResponseType responseType = ResponseType.fromValue(((long?) jsonResp.get("t")).intValue());
			List<int?> responseNoteVals = (List<int?>) jsonResp.getOrDefault("n", new ArrayList());
			List<ResponseNote> responseNotes = responseNoteVals.stream().map(Proto.ResponseNote::fromValue).collect(Collectors.toCollection(System.Collections.ArrayList::new));
			ErrorType et = (ErrorType) jsonResp.getOrDefault("e", null);
			Builder res = new Builder(token, responseType);
			if (jsonResp.containsKey("e"))
			{
				res.ErrorType = (int) jsonResp.get("e");
			}
			return res.setProfile((JSONArray) jsonResp.getOrDefault("p", null)).setBacktrace((JSONArray) jsonResp.getOrDefault("b", null)).setData((JSONArray) jsonResp.getOrDefault("r", new JSONArray())).build();
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
			string msg = data.size() > 0 ? (string) data.get(0) : "Unknown error message";
			return (new ErrorBuilder(msg, type)).setBacktrace(backtrace).setErrorType(errorType).setTerm(query).build();
		}

		public override string ToString()
		{
			return "Response{" + "token=" + token + ", type=" + type + ", notes=" + notes + ", data=" + data + ", profile=" + profile + ", backtrace=" + backtrace + '}';
		}
	}

}