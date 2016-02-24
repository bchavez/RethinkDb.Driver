using Newtonsoft.Json.Linq;
using RethinkDb.Driver.Net;
using RethinkDb.Driver.Proto;

namespace RethinkDb.Driver.Ast
{
    internal class Poco : ReqlExpr
    {
        private readonly object obj;

        public Poco(object obj) : base(new TermType(), null, null)
        {
            this.obj = obj;
        }


        internal class PocoWriter : JTokenWriter
        {
            public override void WriteStartArray()
            {
                base.WriteStartArray();
                this.WriteValue(Proto.TermType.MAKE_ARRAY);
                base.WriteStartArray();
            }

            public override void WriteEndArray()
            {
                base.WriteEndArray();
                base.WriteEndArray();
            }
        }


        protected internal override object Build()
        {
            JToken token;
            using( var writer = new PocoWriter() )
            {
                Converter.Serializer.Serialize(writer, this.obj);
                token = writer.Token;
            }
            return token;
        }
    }
}