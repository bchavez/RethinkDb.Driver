using System;
using System.Collections;
using System.Collections.Generic;
using RethinkDb.Driver.Model;

namespace RethinkDb.Driver.Ast
{

	public static class Util
	{

		public static ReqlAst ToReqlAst(object val)
		{
			return ToReqlAst(val, 1000);
		}

	    public static ReqlExpr ToReqlExpr(object val)
	    {
	        var converted = ToReqlAst(val);
	        var reqlAst = converted as ReqlExpr;
	        if( reqlAst != null )
	        {
	            return reqlAst;
	        }
	        throw new ReqlDriverError($"Cannot convert {val} to ReqlExpr");
	    }


        //TODO: don't use "is" for performance
		private static ReqlAst ToReqlAst(object val, int remainingDepth)
		{
			if (val is ReqlAst)
			{
				return (ReqlAst) val;
			}

			if (val is IList)
			{
				Arguments innerValues = new Arguments();
				foreach (object innerValue in (IList) val)
				{
					innerValues.Add(ToReqlAst(innerValue, remainingDepth - 1));
				}
				return new MakeArray(innerValues, null);
			}

			if (val is IDictionary)
			{
				var obj = new Dictionary<string, ReqlAst>();
				foreach (var entry in val as IDictionary<string, object>)
				{
					if (!(entry.Key is string))
					{
						throw new ReqlError("Object key can only be strings");
					}

					obj[(string) entry.Key] = ToReqlAst(entry.Value);
				}
				return MakeObj.fromMap(obj);
			}

			if (val is Delegate)
			{
			    return Func.FromLambda((Delegate)val);
			}
			
			if (val is DateTime)
			{
			    var dt = (DateTime)val;
			    var isoStr = dt.ToUniversalTime().ToString("o");
				return Iso8601.FromString(isoStr);
			}

			if (val is int?)
			{
				return new Datum((int?) val);
			}
			if (IsNumber(val))
			{
				return new Datum(val);
			}
			if (val is bool?)
			{
				return new Datum((bool?) val);
			}
			if (val is string)
			{
				return new Datum((string) val);
			}
		    if( val == null )
		    {
		        return new Datum(null);
		    }

			throw new ReqlDriverError($"Can't convert {val} to a ReqlAst");
		}

        public static bool IsNumber(object value)
        {
            return value is sbyte
                    || value is byte
                    || value is short
                    || value is ushort
                    || value is int
                    || value is uint
                    || value is long
                    || value is ulong
                    || value is float
                    || value is double
                    || value is decimal;
        }

    
    }

}