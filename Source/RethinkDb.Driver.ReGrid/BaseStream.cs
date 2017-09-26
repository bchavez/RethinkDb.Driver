using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace RethinkDb.Driver.ReGrid
{
    /// <summary>
    /// Base stream
    /// </summary>
    public abstract class BaseStream : Stream
    {
        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="fileInfo">File info the stream represents</param>
        protected BaseStream(FileInfo fileInfo)
        {
            this.FileInfo = fileInfo;
        }

        /// <summary>
        /// File info the stream represents
        /// </summary>
        public FileInfo FileInfo { get; set; }

        /// <summary>
        /// Async closure of the stream.
        /// </summary>
        public abstract Task CloseAsync(CancellationToken cancelToken = default);
    }
}