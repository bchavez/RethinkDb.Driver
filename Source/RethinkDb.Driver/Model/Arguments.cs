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
                this.CoerceAndAddAll(list.OfType<object>().ToList());
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
            CoerceAndAddAll(args.ToList());
        }

        public void CoerceAndAddAll(IList<object> list)
        {
            var ast = list.Select(Util.ToReqlAst);
            this.AddRange(ast);
        }


        public static Arguments Make(params object[] args)
        {
            return new Arguments(args);
        }
    }
}