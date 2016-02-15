namespace RethinkDb.Driver.ReGrid
{
    /// <summary>
    /// Download options
    /// </summary>
    public class DownloadOptions
    {
        /// <summary>
        /// Calculates the SHA256 as the file is downloaded.
        /// </summary>
        public bool CheckSHA256 { get; set; } = false;
        
        /// <summary>
        /// Creates a seekable download stream
        /// </summary>
        public bool Seekable { get; set; } = false;
    }
}
