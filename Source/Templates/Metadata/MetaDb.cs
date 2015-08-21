using System;
using System.IO;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Templates.Metadata
{
    public static class MetaDb
    {
        public static void Initialize(string pathToJson)
        {
            if( Protocol != null ) throw new InvalidOperationException("MetaDb was already initialized.");

            Protocol = JsonConvert.DeserializeObject<JObject>(File.ReadAllText(Path.Combine(pathToJson, "proto_basic.json")));
            Global = JsonConvert.DeserializeObject<JObject>(File.ReadAllText(Path.Combine(pathToJson, "global_info.json")));
            TermInfo = JsonConvert.DeserializeObject<JObject>(File.ReadAllText(Path.Combine(pathToJson, "term_info.json")));
            
        }

        public static JObject Protocol;
        public static JObject Global;
        public static JObject TermInfo;
    }
}