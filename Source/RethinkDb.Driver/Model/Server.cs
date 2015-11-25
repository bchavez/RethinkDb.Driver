using System;
using System.Collections.Generic;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace RethinkDb.Driver.Model
{
    /// <summary>
    /// A type helper that represents the result of conn.server()
    /// </summary>
    public class Server
    {
        public Guid Id { get; set; }
        public string Name { get; set; }

        [JsonExtensionData]
        public IDictionary<string, JToken> ExtraData { get; set; }
    }
}