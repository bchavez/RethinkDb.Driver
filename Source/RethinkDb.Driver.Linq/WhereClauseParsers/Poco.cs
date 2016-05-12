using Newtonsoft.Json.Linq;
using RethinkDb.Driver.Ast;
using RethinkDb.Driver.Net;
using RethinkDb.Driver.Proto;

namespace RethinkDb.Driver.Linq.WhereClauseParsers
{
    internal class Poco : ReqlExpr
    {
        private readonly object _obj;

        public Poco( object obj ) : base( new TermType(), null, null )
        {
            _obj = obj;
        }


        internal class PocoWriter : JTokenWriter
        {
            public override void WriteStartArray()
            {
                base.WriteStartArray();
                WriteValue( TermType.MAKE_ARRAY );
                base.WriteStartArray();
            }

            public override void WriteEndArray()
            {
                base.WriteEndArray();
                base.WriteEndArray();
            }
        }


        protected override object Build()
        {
            JToken token;
            using( var writer = new PocoWriter() )
            {
                Converter.Serializer.Serialize( writer, _obj );
                token = writer.Token;
            }
            return token;
        }
    }
}