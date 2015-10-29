using System;
using Newtonsoft.Json.Linq;
using RethinkDb.Driver.Model;
using RethinkDb.Driver.Net;
using RethinkDb.Driver.Proto;

namespace RethinkDb.Driver.Ast
{
    public class Poco : ReqlAst
    {
        private readonly object obj;

        public Poco(object obj) : base(new TermType(), null)
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
            return Converter3.PocoConverter(obj);
        }
    }
}