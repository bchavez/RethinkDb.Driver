using System;
using System.IO;
using System.Net.Sockets;
using System.Text;

namespace RethinkDb.Driver.Net
{
    public class SocketWrapper
    {
        private readonly TcpClient socketChannel;
        private readonly TimeSpan timeout;

        private readonly string hostname;
        private readonly int port;

        private NetworkStream ns = null;
        private BinaryWriter bw = null;
        private BinaryReader br = null;

        public SocketWrapper(string hostname, int port, TimeSpan? timeout)
        {
            this.hostname = hostname;
            this.port = port;

            this.timeout = timeout ?? TimeSpan.FromSeconds(60);

            this.socketChannel = new TcpClient();
        }

        public virtual void Connect(byte[] handshake)
        {
            var deadline = NetUtil.Deadline(this.timeout);
            var taskComplete = false;
            try
            {
                socketChannel.NoDelay = true;
                socketChannel.Client.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.KeepAlive, true);
                socketChannel.Client.Blocking = true;
                taskComplete = socketChannel.ConnectAsync(this.hostname, this.port).Wait(this.timeout);
                if( deadline < DateTime.UtcNow.Ticks || (taskComplete && !socketChannel.Connected) )
                {
                    throw new ReqlDriverError("Connection timed out.");
                }
                this.ns = socketChannel.GetStream();
                this.bw = new BinaryWriter(this.ns);
                this.br = new BinaryReader(this.ns);

                this.bw.Write(handshake);
                this.bw.Flush();

                var msg = this.ReadNullTerminatedString(timeout);

                if( !msg.Equals("SUCCESS") )
                {
                    throw new ReqlDriverError($"Server dropped connection with message: '{msg}'");
                }
            }
            catch when( !taskComplete )
            {
                try
                {
                    this.Close();
                }
                catch
                {
                    // attempt to close, ignored,
                    // and re-throw the original exception
                }
                throw;
            }
        }

        private string ReadNullTerminatedString(TimeSpan? deadline)
        {
            var deadlineInstant = deadline.HasValue ? DateTime.Now.Add(deadline.Value) : (DateTime?)null;

            var sb = new StringBuilder();
            char c;
            while( (c = this.br.ReadChar()) != '\0' )
            {
                if( deadlineInstant.HasValue )
                {
                    if( deadlineInstant <= DateTime.Now )
                    {
                        throw new ReqlDriverError("Connection timed out.");
                    }
                }
                sb.Append(c);
            }
            return sb.ToString();
        }

        public virtual Response Read()
        {
            var token = this.br.ReadInt64();
            var responseLength = this.br.ReadInt32();
            var response = this.br.ReadBytes(responseLength);
            return Response.ParseFrom(token, Encoding.UTF8.GetString(response));
        }

        public virtual void WriteQuery(long token, string json)
        {
            this.bw.Write(token);
            var jsonBytes = Encoding.UTF8.GetBytes(json);
            this.bw.Write(jsonBytes.Length);
            this.bw.Write(jsonBytes);
        }

        public virtual bool Closed => !socketChannel.Connected;

        public virtual bool Open => socketChannel.Connected;

        public virtual void Close()
        {
            try
            {
#if DNXCORE50
                socketChannel.Dispose();
#else
                socketChannel.Close();
#endif
            }
            catch( IOException e )
            {
                throw new ReqlError(e);
            }
        }
    }
}