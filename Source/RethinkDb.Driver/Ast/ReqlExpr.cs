namespace RethinkDb.Driver.Ast
{
    //Partial class for overloads expression overloads! :) 
    //Cool man. Yeeah. That's right. So 1337.
    public partial class ReqlExpr
    {
        /// <summary>
        /// Get a single field from an object. If called on a sequence, gets that field from every object in the sequence, skipping objects that lack it.
        /// </summary>
        /// <param name="getField"></param>
        public GetField this[string getField] => this.getField(getField);


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
    }
}