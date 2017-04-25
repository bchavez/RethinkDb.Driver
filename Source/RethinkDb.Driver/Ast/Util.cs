using System;
using System.Collections;
using System.Collections.Generic;
using Newtonsoft.Json.Linq;
using RethinkDb.Driver.Model;

namespace RethinkDb.Driver.Ast
{
    internal static class Util
    {
        public static ReqlAst ToReqlAst(object val)
        {
            return ToReqlAst(val, 100, null);
        }
        public static ReqlAst ToReqlAst(object val, Func<object, ReqlAst> hook)
        {
            return ToReqlAst(val, 100, hook);
        }

        public static ReqlExpr ToReqlExpr(object val)
        {
            var converted = ToReqlAst(val);
            var reqlAst = converted as ReqlExpr;
            if( !ReferenceEquals(reqlAst, null) )
            {
                return reqlAst;
            }
            throw new ReqlDriverError($"Cannot convert {val} to ReqlExpr");
        }

        private static ReqlAst ToReqlAst(object val, int remainingDepth, Func<object, ReqlAst> hook = null )
        {
            if( remainingDepth <= 0 )
            {
                throw new ReqlDriverCompileError("Recursion limit reached converting to ReqlAst");
            }
            if( hook != null )
            {
                var converted = hook(val);
                if( !ReferenceEquals(converted, null) )
                {
                    return converted;
                }
            }
            var ast = val as ReqlAst;
            if( !ReferenceEquals(ast, null) )
            {
                return ast;
            }

            if( val is JToken token )
            {
                return new Poco(token);
            }

            if( val is IList lst )
            {
                Arguments innerValues = new Arguments();
                foreach( object innerValue in lst )
                {
                    innerValues.Add(ToReqlAst(innerValue, remainingDepth - 1));
                }
                return new MakeArray(innerValues, null);
            }

            if( val is IDictionary dict )
            {
                var obj = new Dictionary<string, ReqlAst>();
                foreach( var keyObj in dict.Keys )
                {
                    var key = keyObj as string;
                    if( key == null )
                    {
                        throw new ReqlDriverCompileError("Object keys can only be strings");
                    }

                    obj[key] = ToReqlAst(dict[keyObj]);
                }
                return MakeObj.fromMap(obj);
            }

            if( val is Delegate del )
            {
                return Func.FromLambda(del);
            }

            var dt = val as DateTime?;
            if( dt != null )
            {
                return new Poco(dt);
            }
            var dto = val as DateTimeOffset?;
            if( dto != null )
            {
                return new Poco(dto);
            }

            var @int = val as int?;
            if( @int != null )
            {
                return new Datum(@int);
            }

            if( IsNumber(val) )
            {
                return new Datum(val);
            }

            var @bool = val as bool?;
            if( @bool != null )
            {
                return new Datum(@bool);
            }

            if( val is string str )
            {
                return new Datum(str);
            }
            if( val == null )
            {
                return new Datum(null);
            }

            return new Poco(val);
        }

        public static bool IsNumber(object value)
        {
            return value is sbyte
                   || value is byte //maybe have char here?
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