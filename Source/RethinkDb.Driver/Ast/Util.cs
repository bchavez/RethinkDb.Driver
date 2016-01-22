using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using RethinkDb.Driver.Model;
using RethinkDb.Driver.Net;

namespace RethinkDb.Driver.Ast
{
    public static class Util
    {
        public static ReqlAst ToReqlAst(object val)
        {
            return ToReqlAst(val, 100);
        }

        public static ReqlExpr ToReqlExpr(object val)
        {
            var converted = ToReqlAst(val);
            var reqlAst = converted as ReqlExpr;
            if( !ReferenceEquals(reqlAst, null))
            {
                return reqlAst;
            }
            throw new ReqlDriverError($"Cannot convert {val} to ReqlExpr");
        }

        private static ReqlAst ToReqlAst(object val, int remainingDepth)
        {
            if( remainingDepth <= 0 )
            {
                throw new ReqlDriverCompileError("Recursion limit reached converting to ReqlAst");
            }
            var ast = val as ReqlAst;
            if( !ReferenceEquals(ast, null) )
            {
                return ast;
            }

            //GitHub Issue #21 - Request - Allow Inserting JObject
            var token = val as JToken;
            if( token != null )
            {
                //Another way to do it is to convert the whole thing into JSON and have
                //the server parse it. One thing we'd need to watch out for is 
                //the DateTime conversion. ReqlDateTimeConverter would need to change
                //so that it directly serializes to $reql_type$:TIME instead of a protocol
                //Iso8601(string). It's probably more proper to seralize to $reql_type$ than
                //a ReQL command Iso8601(string).
                var json = token.ToString(Formatting.None, Converter.Serializer.Converters.ToArray());
                return new Json(json);

                //One way to do it: De-construct the JObject like we do to an IDictionary...
                //Pro: More complete
                //Con: Easy to get wrong, lots more code, code-duplication with IDictionary
                //     
                //if( token.Type == JTokenType.Object )
                //{
                //    var jobj = val as IDictionary<string, JToken>;
                //    var obj = new Dictionary<string, ReqlAst>();
                //    foreach( var t in jobj )
                //    {
                //        obj[t.Key] = ToReqlAst(t.Value);
                //    }
                //    return MakeObj.fromMap(obj);
                //}
                //else if( token.Type == )
                //{
                //}
            }

            var lst = val as IList;
            if( lst != null )
            {
                Arguments innerValues = new Arguments();
                foreach( object innerValue in lst )
                {
                    innerValues.Add(ToReqlAst(innerValue, remainingDepth - 1));
                }
                return new MakeArray(innerValues, null);
            }

            var dict = val as IDictionary;
            if( dict != null )
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

            var del = val as Delegate;
            if( del != null )
            {
                return Func.FromLambda(del);
            }


            if( val is DateTime )
            {
                var dt = (DateTime)val;
                var isoStr = dt.ToString("o");
                return Iso8601.FromString(isoStr);
            }
            if( val is DateTimeOffset )
            {
                var dt = (DateTimeOffset)val;
                var isoStr = dt.ToString("o");
                return Iso8601.FromString(isoStr);
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

            var str = val as string;
            if( str != null )
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