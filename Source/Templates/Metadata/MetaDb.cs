using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Templates.CodeGen.Util;

namespace Templates.Metadata
{
    public static class MetaDb
    {
        public static void Initialize(string pathToJson)
        {
            if( Protocol != null ) throw new InvalidOperationException("MetaDb was already initialized.");

            Protocol = JsonConvert.DeserializeObject<JObject>(File.ReadAllText(Path.Combine(pathToJson, "proto_basic.json")));
            Global = JsonConvert.DeserializeObject<JObject>(File.ReadAllText(Path.Combine(pathToJson, "global_info.json")));
            JavaTermInfo = JsonConvert.DeserializeObject<JObject>(File.ReadAllText(Path.Combine(pathToJson, "java_term_info.json")));

            ReadDocMeta(pathToJson);
        }

        public static void ReadDocMeta(string pathToJson)
        {
            var json = File.ReadAllText(Path.Combine(pathToJson, "reql_docs.js"));
            json = json.Substring(json.IndexOf("reql_docs = ") + 12);

            Docs = JsonConvert.DeserializeObject<Dictionary<string, Documentation>>(json)
                .ToDictionary(
                    kvp => // KEY Transform
                        {
                            var m = Regex.Match(kvp.Key, "(?<=javascript/).*(?<!/)");
                            if( !m.Success )
                            {
                                Console.WriteLine("DOC ERROR: " + kvp.Key);
                            }
                            return m.Value;
                        },
                    kvp => // VALUE Transform
                        {
                            var doc = kvp.Value;
                            doc.CleanUp();
                            return doc;
                        }, StringComparer.OrdinalIgnoreCase);
        }

        public static JObject Protocol;
        public static JObject Global;
        public static JObject JavaTermInfo;
        public static Dictionary<string, Documentation> Docs;
    }


    public class Documentation
    {
        public string Body { get; set; }
        public string Description { get; set; }
        public string Example { get; set; }
        public string Name { get; set; }

        public void CleanUp()
        {
            this.Description = RemoveUnwantedTags(this.Description);
            this.Example = RemoveUnwantedTags(this.Example);
        }
        internal static string RemoveUnwantedTags(string data)
        {
            return data
                .Replace("<pre>", "")
                .Replace("</pre>", "")
                .Replace("<p>", "")
                .Replace("</p>", "")
                .Replace("\n","\r\n/// "
                .Replace("&rarr", "JArray"));

        }
    }
}