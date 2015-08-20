using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Sockets;
using com.rethinkdb.net;
using RethinkDb.Driver;

namespace RethinkDb.Driver.Net
{
	public class SocketWrapper
	{
		private TcpClient socketChannel;
		private int? timeout = null;
		private ByteBuffer readBuffer = null;

		public readonly string hostname;
		public readonly int port;

		public SocketWrapper(string hostname, int port, int? timeout)
		{
			this.hostname = hostname;
			this.port = port;
			this.timeout = timeout;
		    this.socketChannel = new TcpClient();
		}

		public virtual void connect(ByteBuffer handshake)
		{
			int? deadline = timeout.map(com.rethinkdb.net.Util.deadline);
			try
			{
				socketChannel.configureBlocking(true);
				socketChannel.setOption(StandardSocketOptions.TCP_NODELAY, true);
				socketChannel.socket().connect(new InetSocketAddress(hostname, port), timeout.orElse(0));
				socketChannel.write(handshake);
				string msg = readNullTerminatedString(deadline);
				if (!msg.Equals("SUCCESS"))
				{
					throw new ReqlDriverError("Server dropped connection with message: \"%s\"", msg);
				}
			}
			catch (SocketTimeoutException)
			{
				throw new TimeoutException("Connect timed out.");
			}
			catch (IOException e)
			{
				throw new ReqlRuntimeError(e);
			}
		}

		public virtual void write(ByteBuffer buffer)
		{
			try
			{
				buffer.flip();
				while (buffer.hasRemaining())
				{
					socketChannel.write(buffer);
				}
			}
			catch (IOException e)
			{
				throw new ReqlError(e);
			}
		}

		private string readNullTerminatedString(Optional<int?> deadline)
		{
			ByteBuffer byteBuf = Util.leByteBuffer(1);
			IList<sbyte?> bytelist = new List<sbyte?>();
			while (true)
			{
				int bytesRead = socketChannel.read(byteBuf);
				if (bytesRead == 0)
				{
					continue;
				}
				// Maybe we read -1? Throw an error

				deadline.ifPresent(d =>
				{
					if (d <= Util.Timestamp)
					{
						throw new ReqlDriverError("Connection timed out.");
					}
				});
				if (byteBuf.get(0) == (sbyte)0)
				{
					sbyte[] raw = new sbyte[bytelist.Count];
					for (int i = 0; i < raw.Length; i++)
					{
						raw[i] = bytelist[i].Value;
					}
					return StringHelperClass.NewString(raw, StandardCharsets.UTF_8);
				}
				else
				{
					bytelist.Add(byteBuf.get(0));
				}
				byteBuf.flip();
			}
		}

		private ByteBuffer readBytes(int i, bool strict)
		{
			ByteBuffer buffer = Util.leByteBuffer(i);
			try
			{
				int totalRead = 0;
				int read = socketChannel.read(buffer);
				totalRead += read;

				while (strict && read != 0 && read != i)
				{
					read = socketChannel.read(buffer);
					totalRead += read;
				}

				if (totalRead != i && strict)
				{
					throw new ReqlError("Error receiving data, expected " + i + " bytes but received " + totalRead);
				}

				buffer.flip();
				return buffer;
			}
			catch (IOException e)
			{
				throw new ReqlError(e);
			}
		}

		public virtual void writeLEInt(int i)
		{
			ByteBuffer buffer = Util.leByteBuffer(4);
			buffer.putInt(i);
			write(buffer);
		}

		public virtual void writeStringWithLength(string s)
		{
			writeLEInt(s.Length);

			ByteBuffer buffer = Util.leByteBuffer(s.Length);
			buffer.put(s.GetBytes());
			write(buffer);
		}

		public virtual void write(sbyte[] bytes)
		{
			writeLEInt(bytes.Length);
			write(ByteBuffer.wrap(bytes));
		}

		public virtual ByteBuffer recvall(int bufsize)
		{
			return recvall(bufsize, Optional.empty());
		}

		public virtual ByteBuffer recvall(int bufsize, Optional<int?> deadline)
		{
			// TODO: make deadline work
			ByteBuffer buf = Util.leByteBuffer(bufsize);
			try
			{
				int bytesRead = socketChannel.read(buf);
				if (bytesRead != bufsize)
				{
					do
					{
						bytesRead += socketChannel.read(buf);
					} while (bytesRead < bufsize);
				}
			}
			catch (IOException ex)
			{
				throw new ReqlDriverError(ex);
			}
			buf.flip();
			return buf;
		}

		public virtual Response read()
		{
			long token = recvall(8).Long;
			int responseLength = recvall(4).Int;
			ByteBuffer responseBytes = recvall(responseLength);
			return Response.parseFrom(token, responseBytes);
		}

		public virtual bool Closed
		{
			get
			{
				return !socketChannel.Connected;
			}
		}

		public virtual bool Open
		{
			get
			{
				return socketChannel.Connected;
			}
		}

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