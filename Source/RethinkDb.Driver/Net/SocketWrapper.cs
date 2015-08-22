using System.Collections.Generic;
using System.IO;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using com.rethinkdb.net;

namespace RethinkDb.Driver.Net
{
	public class SocketWrapper
	{
        private TcpClient socketChannel;
		private int? timeout = null;
	    private NetworkStream ns = null;
	    private BinaryWriter bw = null;
	    private BinaryReader br = null;


		public readonly string hostname;
		public readonly int port;

		public SocketWrapper(string hostname, int port, int? timeout)
		{
			this.hostname = hostname;
			this.port = port;
			this.timeout = timeout;
		    this.socketChannel = new TcpClient();
		}

		public virtual void connect(byte[] handshake)
		{
		    int? deadline = Util.deadline(timeout.GetValueOrDefault(60));
		    var timedout = false;
			try
			{
			    socketChannel.NoDelay = true;
                socketChannel.Client.Blocking = true;
                timedout = socketChannel.ConnectAsync(this.hostname, this.port).Wait(timeout.GetValueOrDefault(60));
			    if( timedout )
			    {
			        throw new ReqlDriverError("Connection timed out.");
			    }
			    this.ns = socketChannel.GetStream();
			    this.bw = new BinaryWriter(ns);
			    this.br = new BinaryReader(ns);

			    this.bw.Write(handshake);

				string msg = readNullTerminatedString(deadline);
				if (!msg.Equals("SUCCESS"))
				{
				    throw new ReqlDriverError($"Server dropped connection with message: '{msg}'");
				}
			}
			catch when(!timedout)
			{
			    socketChannel.Close();
			    throw;
			}
		}

		public virtual void write(byte[] buffer)
		{
			try
			{
			    this.bw.Write(buffer);
			}
			catch (IOException e)
			{
				throw new ReqlError(e);
			}
		}

		private string readNullTerminatedString(int? deadline)
		{
		    return br.ReadString();
		}

		public virtual void writeLEInt(int i)
		{
		    this.bw.Write(i);
		}

		public virtual void writeStringWithLength(string s)
		{
		    var buffer = Encoding.UTF8.GetBytes(s);
            writeLEInt(buffer.Length);
		    write(buffer);
		}

		public virtual void write(sbyte[] bytes)
		{
			writeLEInt(bytes.Length);
			write(bytes);
		}

		public virtual Response read()
		{
		    var token = this.br.ReadInt64();
		    var responseLength = this.br.ReadInt32();
		    var response = this.br.ReadBytes(responseLength);
		    return Response.parseFrom(token, Encoding.UTF8.GetString(response));
		}

		public virtual bool Closed => !socketChannel.Connected;

	    public virtual bool Open => socketChannel.Connected;

	    public virtual void close()
		{
			try
			{
				socketChannel.Close();
			}
			catch (IOException e)
			{
				throw new ReqlError(e);
			}
		}
	}

}