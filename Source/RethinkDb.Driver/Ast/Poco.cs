using System;
using Newtonsoft.Json.Linq;
using RethinkDb.Driver.Model;
using RethinkDb.Driver.Proto;

namespace RethinkDb.Driver.Ast
{
    public class Poco : ReqlAst
    {
        public static Func<object, JObject> Converter = DefaultConverter;

        private readonly object obj;

        public Poco(object obj)
        {
            this.obj = obj;
        }

        public Poco(TermType termType, Arguments args, OptArgs optargs) : base(termType, args, optargs)
        {
        }

        public Poco(TermType termType, Arguments args) : base(termType, args)
        {
        }

        protected internal override object Build()
        {
            return Converter(obj);
        }

        public static JObject DefaultConverter(object value)
        {
            return JObject.FromObject(value);
        }
    }
}
