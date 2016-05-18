using System.Net.Sockets;

namespace RethinkDb.Driver.Utils
{
    internal static class ExtensionsForTcpClient
    {
        public static void Shutdown(this TcpClient tcp)
        {
#if NETSTANDARD15
            tcp.Dispose();
#else
            tcp.Close();
#endif
        }
    }
}