using System.Net.Sockets;

namespace RethinkDb.Driver.Utils
{
    internal static class ExtensionsForTcpClient
    {
        public static void Shutdown(this TcpClient tcp)
        {
#if STANDARD
            tcp.Dispose();
#else
            tcp.Close();
#endif
        }
    }
}