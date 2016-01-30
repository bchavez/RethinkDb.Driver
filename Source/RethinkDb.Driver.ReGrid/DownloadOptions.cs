namespace RethinkDb.Driver.ReGrid
{
    public class DownloadOptions
    {
        public bool CheckSHA256 { get; set; } = false;
        public bool Seekable { get; set; } = false;
    }
}
