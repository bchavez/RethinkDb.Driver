using System.Net.Sockets;

namespace RethinkDb.Driver.Utils
{
    internal static class ExtensionsForTcpClient
    {
        public static void Shutdown(this TcpClient tcp)
        {
#if DOTNET5_4 || DNXCORE50
            tcp.Dispose();
#else
            tcp.Close();
#endif
        }
    }
}