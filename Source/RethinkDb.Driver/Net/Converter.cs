using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using RethinkDb.Driver.Ast;
using RethinkDb.Driver.Model;

namespace RethinkDb.Driver.Net
{
    public class Converter
	{
	    //public static Func<long, string, Response> ResponseBuillder = Response.ParseFrom;

	    public const string PSEUDOTYPE_KEY = "$reql_type$";
	    public const string TIME = "TIME";
	    public const string GROUPED_DATA = "GROUPED_DATA";
	    public const string GEOMETRY = "GEOMETRY";
	    public const string BINARY = "BINARY"; 

	    public static object ConvertPesudoTypes(object obj, FormatOptions fmt)
	    {
	        var list = obj as JArray;
	        var dict = obj as JObject;

	        if( list != null )
	        {
	            return list.Select(item => ConvertPesudoTypes(item, fmt))
	                .ToList();
	        }
            else if( dict != null )
            {
                if( dict[PSEUDOTYPE_KEY] != null )
                {
                    return ConvertPesudo(dict, fmt);
                }

                return dict.Properties()
                    .ToDictionary(p => p.Name, p => ConvertPesudoTypes(p.Value, fmt));

            }
            else
            {
                return obj;
            }
	    }

	    public static object ConvertPesudo(JObject value, FormatOptions fmt)
	    {
	        if( value == null ) return null;

	        var reqlType = value[PSEUDOTYPE_KEY].ToString();

	        switch( reqlType )
	        {
                case TIME:
	                return fmt.RawTime ? value : (object)GetTime(value);
                case GROUPED_DATA:
	                return fmt.RawGroups ? value : (object)GetGrouped(value);
                case BINARY:
	                return fmt.RawBinary ? value : (object)GetBinary(value);
                case GEOMETRY:
	                return value;

                default:
	                return value;
	        }
	    }

	    private static DateTimeOffset GetTime(JObject value)
	    {
	        double epoch_time = value["epoch_time"].ToObject<double>();
	        string timezone = value["timezone"].ToString();

	        var epoch = new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc);
	        var dt = epoch + TimeSpan.FromSeconds(epoch_time);

	        var tz = TimeSpan.Parse(timezone.Substring(1));
	        if( !timezone.StartsWith("+") )
	            tz = -tz;

	        return new DateTimeOffset(dt, tz);
	    }

	    private static byte[] GetBinary(JObject value)
	    {
	        var base64 = value["data"].Value<string>();
	        return Convert.FromBase64String(base64);
	    }

	    private static List<GroupedResult> GetGrouped(JObject value)
	    {
	        return value["data"].ToObject<List<List<object>>>()
	            .Select(g =>
	                {
	                    var group = g[0];
	                    g.RemoveAt(0);
	                    return new GroupedResult(group, g);
	                }).ToList();
	    }

	    public static object ToBinary(byte[] data)
	    {
	        var mob = new MapObject();
	        mob.With(PSEUDOTYPE_KEY, BINARY);
	        mob.With("data", Convert.ToBase64String(data));
	        return mob;
	    }
	}

}