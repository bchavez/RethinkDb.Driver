using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using RethinkDb.Driver.Ast;
using Util = com.rethinkdb.net.Util;

namespace RethinkDb.Driver.Net
{
	public class SocketWrapper
	{
        private TcpClient socketChannel;
		private TimeSpan timeout;
	    private NetworkStream ns = null;
	    private BinaryWriter bw = null;
	    private BinaryReader br = null;


		public readonly string hostname;
		public readonly int port;

		public SocketWrapper(string hostname, int port, TimeSpan? timeout)
		{
			this.hostname = hostname;
			this.port = port;

            this.timeout = timeout ?? TimeSpan.FromSeconds(60);

		    this.socketChannel = new TcpClient();
		}

		public virtual void connect(byte[] handshake)
		{
		    var deadline = Util.deadline(this.timeout);
		    var taskComplete = false;
			try
			{
			    socketChannel.NoDelay = true;
                socketChannel.Client.Blocking = true;
                taskComplete = socketChannel.ConnectAsync(this.hostname, this.port).Wait(this.timeout);
			    if( deadline < DateTime.UtcNow.Ticks || (taskComplete && !socketChannel.Connected) )
			    {
			        throw new ReqlDriverError("Connection timed out.");
			    }
			    this.ns = socketChannel.GetStream();
			    this.bw = new BinaryWriter(ns);
                this.br = new BinaryReader(this.ns);

                this.bw.Write(handshake);
                this.bw.Flush();

                string msg = this.readNullTerminatedString(timeout);
				if (!msg.Equals("SUCCESS"))
				{
				    throw new ReqlDriverError($"Server dropped connection with message: '{msg}'");
				}
			}
			catch when(!taskComplete)
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

	    public virtual void writeQuery(long token, string json)
	    {
	        this.bw.Write(token);
	        var jsonBytes = Encoding.UTF8.GetBytes(json);
	        this.bw.Write(jsonBytes.Length);
	        this.bw.Write(jsonBytes);
	    }

		private string readNullTerminatedString(TimeSpan deadline)
		{
		    var sb = new StringBuilder();
		    char c;
		    while( (c = this.br.ReadChar()) != '\0' )
		    {
		        sb.Append(c);
		    }
		    return sb.ToString();
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