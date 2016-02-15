namespace RethinkDb.Driver
{
    /// <summary>
    /// RethinkDB constants
    /// </summary>
    public class RethinkDBConstants
    {
        /// <summary>
        /// Default database name: test
        /// </summary>
        public const string DefaultDbName = "test";
        
        /// <summary>
        /// Default hostname: localhost
        /// </summary>
        public const string DefaultHostname = "localhost";
        
        /// <summary>
        /// Default auth key: ""
        /// </summary>
        public const string DefaultAuthkey = "";
        
        /// <summary>
        /// Default TCP port: 28015
        /// </summary>
        public const int DefaultPort = 28015;

        /// <summary>
        /// Default connection timeout: 20 seconds
        /// </summary>
        public const int DefaultTimeout = 20;

        /// <summary>
        /// Protocol constants
        /// </summary>
        public static class Protocol
        {
            /// <summary>
            /// What success looks like. :D
            /// </summary>
            public const string Success = "SUCCESS";
        }
    }
}