using RethinkDb.Driver.Ast;
using RethinkDb.Driver.Model;

namespace RethinkDb.Driver.Net
{
    /// <summary>
    /// ReQL pseudo type format options for JToken (JObject, JArray) derivatives.
    /// </summary>
    public class FormatOptions
    {
        /// <summary>
        /// Leave $reql_time$:TIME types as raw
        /// </summary>
        public bool RawTime { get; set; }
        /// <summary>
        /// Leave $reql_time$:GROUPED_DATA types as raw
        /// </summary>
        public bool RawGroups { get; set; }

        /// <summary>
        /// Leave $reql_time$:BINARY types as raw
        /// </summary>
        public bool RawBinary { get; set; }

        /// <summary>
        /// Format options for JToken
        /// </summary>
        public FormatOptions()
        {    
        }

        /// <summary>
        /// Factory method for FormatOptions from OptArgs
        /// </summary>
        public static FormatOptions FromOptArgs(OptArgs args)
        {
            var fmt = new FormatOptions();
            // TODO: find a better way to do this.
            ReqlAst datum;
            var value = args.TryGetValue("time_format", out datum) ? ((Datum)datum).datum : new Datum("native").datum;
            fmt.RawTime = value.Equals("raw");

            value = args.TryGetValue("binary_format", out datum) ? ((Datum)datum).datum : new Datum("native").datum;
            fmt.RawBinary = value.Equals("raw");

            value = args.TryGetValue("group_format", out datum) ? ((Datum)datum).datum : new Datum("native").datum;
            fmt.RawGroups = value.Equals("raw");

            return fmt;
        }
    }
}