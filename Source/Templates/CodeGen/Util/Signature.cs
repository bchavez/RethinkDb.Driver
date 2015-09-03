using Newtonsoft.Json;

namespace Templates.CodeGen.Util
{
    public class Signature
    {
        [JsonProperty("first_arg")]
        public string FirstArg { get; set; }
        public SigArg[] Args { get; set; }
        public class SigArg
        {
            public string Var { get; set; }
            public string Type { get; set; }
        }
    }
}