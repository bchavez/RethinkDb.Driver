namespace RethinkDb.Driver.ReGrid
{
    public class DownloadOptions
    {
        public bool CheckMD5 { get; set; } = false;
        public bool Seekable { get; set; } = false;
    }
}
