#pragma warning disable 1591 // Missing XML comment for publicly visible type or member

using System.Collections;
using System.Collections.Generic;
using System.Linq;
using RethinkDb.Driver.Ast;

namespace RethinkDb.Driver.Model
{
    public class Arguments : List<ReqlAst>
    {
        public Arguments()
        {
        }

        public Arguments(object arg)
        {
            var list = arg as IList;
            if( list != null )
            {
                this.CoerceAndAddAll(list);
            }
            else
            {
                this.CoerceAndAdd(arg);
            }
        }

        public Arguments(Arguments args)
        {
            this.AddRange(args);
        }

        public Arguments(ReqlAst arg)
        {
            this.Add(arg);
        }

        public Arguments(object[] args)
        {
            CoerceAndAddAll(args);
        }

        public Arguments(IList<object> args)
        {
            var ast = args.Select(Util.ToReqlAst);
            this.AddRange(ast);
        }

        public void CoerceAndAdd(object o)
        {
            this.Add(Util.ToReqlAst(o));
        }

        public void CoerceAndAddAll(object[] args)
        {
            CoerceAndAddAll(args as ICollection);
        }

        public void CoerceAndAddAll(ICollection list)
        {
            foreach( var item in list )
            {
                this.Add(Util.ToReqlAst(item));
            }
        }
        public void CoerceAndAddAll<T>(ICollection<T> list)
        {
            CoerceAndAddAll(list as ICollection);
        }

        public static Arguments Make(params object[] args)
        {
            return new Arguments(args);
        }
    }
}