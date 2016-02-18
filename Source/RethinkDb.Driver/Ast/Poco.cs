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

        protected internal override object Build()
        {
            return JToken.FromObject(obj, Converter.Serializer);
        }
    }
}