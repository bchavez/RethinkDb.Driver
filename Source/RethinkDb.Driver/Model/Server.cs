using System;
using System.Collections.Generic;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using RethinkDb.Driver.Net;

namespace RethinkDb.Driver.Model
{
    /// <summary>
    /// A type helper that represents the result of <see cref="Connection.Server"/>
    /// and <seealso cref="Connection.ServerAsync"/>.
    /// </summary>
    public class Server
    {
        public Guid Id { get; set; }
        public string Name { get; set; }

        [JsonExtensionData]
        public IDictionary<string, JToken> ExtraData { get; set; }
    }
}