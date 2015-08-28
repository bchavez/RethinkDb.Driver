using System;
using System.Collections;
using System.Collections.Generic;
using RethinkDb.Driver.Model;

namespace RethinkDb.Driver.Ast
{

	public class Util
	{
		private Util()
		{
		}
		/// <summary>
		/// Coerces objects from their native type to ReqlAst
		/// </summary>
		/// <param name="val"> val </param>
		/// <returns> ReqlAst </returns>
		public static ReqlAst ToReqlAst(object val)
		{
			return ToReqlAst(val, 20);
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
				foreach (var entry in val as IDictionary<string, ReqlAst>)
				{
					if (!(entry.Key is string))
					{
						throw new ReqlError("Object key can only be strings");
					}

					obj[(string) entry.Key] = ToReqlAst(entry.Value);
				}
				return MakeObj.FromMap(obj);
			}

			if (val is ReqlFunction)
			{
				return new Func((ReqlFunction) val);
			}
			if (val is ReqlFunction2)
			{
				return new Func((ReqlFunction2) val);
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

        // /*
        //     Called on arguments that should be functions
        //  */
        // public static ReqlAst funcWrap(java.lang.Object o) {
        //     final ReqlAst ReqlQuery = toReqlAst(o);

        //     if (hasImplicitVar(ReqlQuery)) {
        //         return new Func(new ReqlFunction() {
        //             @Override
        //             public ReqlAst apply(ReqlAst row) {
        //                 return ReqlQuery;
        //             }
        //         });
        //     } else {
        //         return ReqlQuery;
        //     }
        // }


        // public static boolean hasImplicitVar(ReqlAst node) {
        //     if (node.getTermType() == Q2L.Term.TermType.IMPLICIT_VAR) {
        //         return true;
        //     }
        //     for (ReqlAst arg : node.getArgs()) {
        //         if (hasImplicitVar(arg)) {
        //             return true;
        //         }
        //     }
        //     for (Map.Entry<String, ReqlAst> kv : node.getOptionalArgs().entrySet()) {
        //         if (hasImplicitVar(kv.getValue())) {
        //             return true;
        //         }
        //     }

        //     return false;
        // }

        // public static Q2L.Datum createDatum(java.lang.Object value) {
        //     Q2L.Datum.Builder builder = Q2L.Datum.newBuilder();

        //     if (value == null) {
        //         return builder
        //                 .setType(Q2L.Datum.DatumType.R_NULL)
        //                 .build();
        //     }

        //     if (value instanceof String) {
        //         return builder
        //                 .setType(Q2L.Datum.DatumType.R_STR)
        //                 .setRStr((String) value)
        //                 .build();
        //     }

        //     if (value instanceof Number) {
        //         return builder
        //                 .setType(Q2L.Datum.DatumType.R_NUM)
        //                 .setRNum(((Number) value).doubleValue())
        //                 .build();
        //     }

        //     if (value instanceof Boolean) {
        //         return builder
        //                 .setType(Q2L.Datum.DatumType.R_BOOL)
        //                 .setRBool((Boolean) value)
        //                 .build();
        //     }

        //     if (value instanceof Collection) {
        //         Q2L.Datum.Builder arr = builder
        //                 .setType(Q2L.Datum.DatumType.R_ARRAY);

        //         for (java.lang.Object o : (Collection) value) {
        //             arr.addRArray(createDatum(o));
        //         }

        //         return arr.build();

        //     }

        //     throw new ReqlError("Unknown Value can't create datatype for : " + value.getClass());
        // }

    }

}