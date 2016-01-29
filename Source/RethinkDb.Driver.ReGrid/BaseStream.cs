using System.IO;
using System.Threading.Tasks;

namespace RethinkDb.Driver.ReGrid
{
    public abstract class BaseStream : Stream
    {
        protected BaseStream(FileInfo fileInfo)
        {
            this.FileInfo = fileInfo;
        }

        public FileInfo FileInfo { get; set; }
        public abstract Task CloseAsync();
    }
}
