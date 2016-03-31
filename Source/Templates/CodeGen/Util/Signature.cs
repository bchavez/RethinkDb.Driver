using Newtonsoft.Json;

namespace Templates.CodeGen.Util
{
    public class Signature
    {
        [JsonProperty("first_arg")]
        public string FirstArg { get; set; }

        [JsonProperty("args")]
        public SigArg[] Args { get; set; }

        public class SigArg
        {
            public string Var { get; set; }
            public string Type { get; set; }
        }
    }

    internal static class ExtensionHelpersForSignature
    {
        public static bool IsParams(this Signature.SigArg args)
        {
            return args.Type.EndsWith("...");
        }

        public static bool OnlyHasParams(this Signature sig)
        {
            if (sig.Args.Length == 2)
            {
                return IsParams(sig.Args[1]);
            }
            return false;
        }
    }
}