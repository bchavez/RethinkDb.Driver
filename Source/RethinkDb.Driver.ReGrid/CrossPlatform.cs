using System;
using System.Security.Cryptography;
using System.Threading;
using System.Threading.Tasks;
using RethinkDb.Driver.Utils;

#if DNX
using System.Reflection;
#endif

namespace RethinkDb.Driver.ReGrid
{
    /// <summary>
    /// Cross platform SHA 256 Hasher
    /// </summary>
    public class Hasher : IDisposable
    {
#if DNX
        private IncrementalHash hasher;
#else
        private SHA256 hasher;
#endif

        /// <summary>
        /// Constructor
        /// </summary>
        public Hasher()
        {
#if DNX
            hasher = IncrementalHash.CreateHash(HashAlgorithmName.SHA256);
#else
            hasher = SHA256.Create();
#endif
        }

        /// <summary>
        /// Updates the hash value
        /// </summary>
        public void AppendData(byte[] data)
        {
#if DNX
            hasher.AppendData(data);
#else
            hasher.TransformBlock(data, 0, data.Length, null, 0);
#endif
        }

        /// <summary>
        /// Gets the final hash calculation.
        /// </summary>
        public string GetHashAndReset()
        {
#if DNX
            return Util.GetHexString(hasher.GetHashAndReset());
#else
            hasher.TransformFinalBlock(new byte[0], 0, 0);
            return Util.GetHexString(hasher.Hash);
#endif
        }

        /// <summary>
        /// Disposes the hasher.
        /// </summary>
        public void Dispose()
        {
            hasher.Dispose();
        }
    }


    public abstract partial class DownloadStream
    {
#if !DNX
        /// <summary>
        /// Closes the stream.
        /// </summary>
        public override void Close()
        {
            CloseAsync().WaitSync();
        }
#endif

        /// <summary>
        /// Closes the stream asynchronously.
        /// </summary>
        /// <param name="cancelToken"><see cref="CancellationToken"/></param>
        /// <returns></returns>
        public override Task CloseAsync(CancellationToken cancelToken = default(CancellationToken))
        {
#if !DNX
            base.Close();
#endif
            return TaskHelper.CompletedTask;
        }
    }

    internal partial class DownloadStreamForwardOnly 
    {
#if !DNX
        public override void Close()
        {
            CloseHelper();
            base.Close();
        }
#endif  
    }


    public partial class UploadStream
    {
#if !DNX
        /// <summary>
        /// Closes the stream.
        /// </summary>
        public override void Close()
        {
            CloseAsync().WaitSync();
        }
#endif

        /// <summary>
        /// Async close the upload stream.
        /// </summary>
        public override async Task CloseAsync(CancellationToken cancelToken = default(CancellationToken))
        {
            await this.CloseInternalAsync(cancelToken).ConfigureAwait(false);
#if !DNX
            base.Close();
#endif
        }
    }


}