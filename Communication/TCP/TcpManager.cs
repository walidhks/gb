using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Net;
using System.Net.Sockets;
using System.Text;
using GbService.Communication.Common;
using GbService.Communication.Serial;
using NLog;

namespace GbService.Communication.TCP
{
	public class TcpManager : ILowManager
	{
		public TcpManager(string tcp, Jihas kind, bool loglow, string end, bool checkEnd = true)
		{
			string[] array = tcp.Split(new char[]
			{
				','
			});
			this._logLow = loglow;
			this._checkEnd = checkEnd;
			this._end = end;
			this._kind = kind;
			this.Start(array[0], int.Parse(array[1]));
			this.Listen();
		}

		public virtual void HandleMessage(string m)
		{
			this.LogMsg(m, false);
			this.OnMessageReceived(new MessageReceivedEventArgs
			{
				Message = m
			});
		}

		public bool SendLow(string msg, Coding enc = Coding.Asc)
		{
			string str = Tu.DataToString(msg.ToCharArray());
			this._logger.Trace(string.Format("--TX {0}--: ", enc) + str);
			byte[] msg2 = (enc == Coding.Unicode) ? Encoding.Unicode.GetBytes(msg) : Encoding.ASCII.GetBytes(msg);
			this.SendBytes(msg2);
			return true;
		}

		public void SendLow(List<char> c)
		{
			char[] array = c.ToArray();
			byte[] bytes = Encoding.ASCII.GetBytes(array);
			this._logger.Trace("--TX--: " + Tu.DataToString(array));
			this.SendBytes(bytes);
		}

		public void SendLow(byte b)
		{
			this._logger.Trace("--TX--: " + Tu.DataToString(new char[]
			{
				(char)b
			}));
			this.SendBytes(new byte[]
			{
				b
			});
		}

		private void LogMsg(string m, bool low = false)
		{
			string str = Tu.DataToString(m.ToCharArray());
			this._logger.Trace("--" + (low ? "RL" : "RX") + "--: " + str);
		}

		public void Start(string ip, int port)
		{
			try
			{
				this._permission = new SocketPermission(NetworkAccess.Accept, TransportType.Tcp, ip, -1);
				this._sListener = null;
				this._permission.Demand();
				IPAddress ipaddress = IPAddress.Parse(ip);
				this._ipEndPoint = new IPEndPoint(ipaddress, port);
				this._sListener = new Socket(ipaddress.AddressFamily, SocketType.Stream, ProtocolType.Tcp);
				this._sListener.Bind(this._ipEndPoint);
				this._logger.Info("Server started.");
			}
			catch (Exception ex)
			{
				this._logger.Error(ex.ToString());
			}
		}

		public void Listen()
		{
			try
			{
				this._sListener.Listen(100);
				AsyncCallback callback = new AsyncCallback(this.AcceptCallback);
				this._sListener.BeginAccept(callback, this._sListener);
				Logger logger = this._logger;
				string str = "Server is now listening on ";
				IPAddress address = this._ipEndPoint.Address;
				logger.Info(str + ((address != null) ? address.ToString() : null) + " port: " + this._ipEndPoint.Port.ToString());
			}
			catch (Exception ex)
			{
				this._logger.Error(ex.ToString());
			}
		}

		public void AcceptCallback(IAsyncResult ar)
		{
			try
			{
				byte[] array = new byte[1024];
				Socket socket = (Socket)ar.AsyncState;
				Socket socket2 = socket.EndAccept(ar);
				socket2.NoDelay = false;
				object[] state = new object[]
				{
					array,
					socket2
				};
				socket2.BeginReceive(array, 0, array.Length, SocketFlags.None, new AsyncCallback(this.ReceiveCallback), state);
				AsyncCallback callback = new AsyncCallback(this.AcceptCallback);
				socket.BeginAccept(callback, socket);
			}
			catch (Exception ex)
			{
				this._logger.Error(ex.ToString());
			}
		}

		public void ReceiveCallback(IAsyncResult ar)
		{
			try
			{
				object[] array = (object[])ar.AsyncState;
				byte[] bytes = (byte[])array[0];
				TcpManager._handler = (Socket)array[1];
				int num = 0;
				SocketError socketError = SocketError.Success;
				try
				{
					num = TcpManager._handler.EndReceive(ar, out socketError);
				}
				catch (Exception ex)
				{
					this._logger.Error(new LogMessageGenerator(ex.ToString));
				}
				bool flag = socketError > SocketError.Success;
				if (flag)
				{
					this._logger.Error<SocketError>(socketError);
				}
				else
				{
					bool flag2 = num <= 0;
					if (!flag2)
					{
						Coding coding = Coding.Asc;
						bool flag3 = this._kind == Jihas.Urilyser;
						if (flag3)
						{
							coding = Coding.Utf8;
						}
						bool flag4 = this._kind == Jihas.DiruiCsT180;
						if (flag4)
						{
							coding = Coding.Unicode;
						}
						string text = (coding == Coding.Unicode) ? Encoding.Unicode.GetString(bytes, 0, num) : ((coding == Coding.Utf8) ? Encoding.UTF8.GetString(bytes, 0, num) : Encoding.ASCII.GetString(bytes, 0, num));
						this._msg += text;
						bool logLow = this._logLow;
						if (logLow)
						{
							this.LogMsg(text, true);
						}
						SerialManager.MsgState msgState = this.ComplteMsg(this._msg);
						bool flag5 = msgState == SerialManager.MsgState.Extra;
						if (flag5)
						{
							this._msg = "";
						}
						else
						{
							bool flag6 = msgState == SerialManager.MsgState.Ok;
							if (flag6)
							{
								SerialManager.HandleDuplicated(this._msg, new SerialManager.Handle(this.HandleMessage), this._kind);
								this._msg = "";
							}
						}
						byte[] array2 = new byte[1024];
						array[0] = array2;
						array[1] = TcpManager._handler;
						TcpManager._handler.BeginReceive(array2, 0, array2.Length, SocketFlags.None, new AsyncCallback(this.ReceiveCallback), array);
					}
				}
			}
			catch (Exception ex2)
			{
				Logger logger = this._logger;
				string str = "a1 ";
				Exception ex3 = ex2;
				logger.Error(str + ((ex3 != null) ? ex3.ToString() : null));
			}
		}

		private SerialManager.MsgState ComplteMsg(string m)
		{
			return SerialManager.CompleteMsg(m, this._end, this._kind, this._checkEnd);
		}

		public void SendBytes(byte[] msg)
		{
			try
			{
				TcpManager._handler.BeginSend(msg, 0, msg.Length, SocketFlags.None, new AsyncCallback(this.SendCallback), TcpManager._handler);
			}
			catch (Exception ex)
			{
				this._logger.Error(ex.ToString());
			}
		}

		public void SendCallback(IAsyncResult ar)
		{
			try
			{
				Socket socket = (Socket)ar.AsyncState;
				socket.EndSend(ar);
			}
			catch (Exception ex)
			{
				this._logger.Error(ex.ToString());
			}
		}

		public void Close()
		{
			try
			{
				bool connected = TcpManager._handler.Connected;
				if (connected)
				{
					TcpManager._handler.Shutdown(SocketShutdown.Both);
					TcpManager._handler.Close();
				}
				bool connected2 = this._sListener.Connected;
				if (connected2)
				{
					this._sListener.Shutdown(SocketShutdown.Both);
					this._sListener.Close();
				}
			}
			catch (Exception ex)
			{
				this._logger.Error(ex.ToString());
			}
		}

		//DebuggerBrowsable(DebuggerBrowsableState.Never)]
		public event EventHandler<MessageReceivedEventArgs> MessageReceived;

		protected virtual void OnMessageReceived(MessageReceivedEventArgs e)
		{
			EventHandler<MessageReceivedEventArgs> messageReceived = this.MessageReceived;
			if (messageReceived != null)
			{
				messageReceived(this, e);
			}
		}

		public Logger _logger = LogManager.GetCurrentClassLogger();

		private SocketPermission _permission;

		private Socket _sListener;

		private IPEndPoint _ipEndPoint;

		private static Socket _handler;

		private string _msg = "";

		private bool _logLow;

		private Jihas _kind;

		private bool _checkEnd;

		private string _end;
	}
}
