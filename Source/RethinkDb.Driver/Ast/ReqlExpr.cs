#pragma warning disable 1591 // Missing XML comment for publicly visible type or member


using System;

namespace RethinkDb.Driver.Ast
{
    //Partial class for overloads expression overloads! :) 
    //Cool man. Yeeah. That's right. So 1337.
#pragma warning disable 660,661
    public partial class ReqlExpr
#pragma warning restore 660,661
    {
        /// <summary>
        /// Get a single field from an object. If called on a sequence, gets that field from every object in the sequence, skipping objects that lack it.
        /// </summary>
        /// <param name="bracket"></param>
        public Bracket this[string bracket] => this.bracket(bracket);

        /// <summary>
        /// Get the nth element of a sequence, counting from zero. If the argument is negative, count from the last element.
        /// </summary>
        /// <param name="bracket"></param>
        /// <returns></returns>
        public Bracket this[int bracket] => this.bracket(bracket);


        public static ReqlExpr operator >(ReqlExpr a, ReqlExpr b)
        {
            return a.gt(b);
        }

        public static ReqlExpr operator <(ReqlExpr a, ReqlExpr b)
        {
            return a.lt(b);
        }

        public static ReqlExpr operator >=(ReqlExpr a, ReqlExpr b)
        {
            return a.ge(b);
        }

        public static ReqlExpr operator <=(ReqlExpr a, ReqlExpr b)
        {
            return a.le(b);
        }

        public static ReqlExpr operator ==(ReqlExpr a, ReqlExpr b)
        {
            return a.eq(b);
        }

        public static ReqlExpr operator !=(ReqlExpr a, ReqlExpr b)
        {
            return a.ne(b);
        }

        public static ReqlExpr operator +(ReqlExpr a, ReqlExpr b)
        {
            return a.add(b);
        }

        public static ReqlExpr operator -(ReqlExpr a, ReqlExpr b)
        {
            return a.sub(b);
        }

        public static ReqlExpr operator *(ReqlExpr a, ReqlExpr b)
        {
            return a.mul(b);
        }

        public static ReqlExpr operator /(ReqlExpr a, ReqlExpr b)
        {
            return a.div(b);
        }

        public static ReqlExpr operator %(ReqlExpr a, ReqlExpr b)
        {
            return a.mod(b);
        }

        public static ReqlExpr operator !(ReqlExpr a)
        {
            return a.not();
        }


        //&& and || might be a bit tricky here.
        public static bool operator false(ReqlExpr a)
        {
            return false;
        }

        public static bool operator true(ReqlExpr a)
        {
            return false; //forces evaluation of & and | ?
            //this might be a bug here.
        }

        public static ReqlExpr operator &(ReqlExpr a, ReqlExpr b)
        {
            return a.and(b);
        }

        public static ReqlExpr operator |(ReqlExpr a, ReqlExpr b)
        {
            return a.or(b);
        }


        //Can we do implicit operators???
        public static implicit operator ReqlExpr(string a)
        {
            return Util.ToReqlExpr(a);
        }

        public static implicit operator ReqlExpr(int a)
        {
            return Util.ToReqlExpr(a);
        }

        public static implicit operator ReqlExpr(uint a)
        {
            return Util.ToReqlExpr(a);
        }

        public static implicit operator ReqlExpr(byte a)
        {
            return Util.ToReqlExpr(a);
        }

        public static implicit operator ReqlExpr(sbyte a)
        {
            return Util.ToReqlExpr(a);
        }

        public static implicit operator ReqlExpr(long a)
        {
            return Util.ToReqlExpr(a);
        }

        public static implicit operator ReqlExpr(ulong a)
        {
            return Util.ToReqlExpr(a);
        }

        public static implicit operator ReqlExpr(double a)
        {
            return Util.ToReqlExpr(a);
        }

        public static implicit operator ReqlExpr(decimal a)
        {
            return Util.ToReqlExpr(a);
        }

        public static implicit operator ReqlExpr(float a)
        {
            return Util.ToReqlExpr(a);
        }

        public static implicit operator ReqlExpr(DateTime a)
        {
            return Util.ToReqlExpr(a);
        }

        public static implicit operator ReqlExpr(DateTimeOffset a)
        {
            return Util.ToReqlExpr(a);
        }

        public static implicit operator ReqlExpr(Delegate a)
        {
            return Util.ToReqlExpr(a);
        }
    }
}