using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using GbService.Communication.Common;
using GbService.Communication.Serial;
using NLog;

namespace GbService.Communication.TCP
{
	public class TcpClient : ILowManager
	{
		public TcpClient(string tcp, Jihas kind, bool logLow = false, string endMsg = null, bool checkEnd = true)
		{
			string[] array = tcp.Split(new char[]
			{
				','
			});
			this._checkEnd = checkEnd;
			this._kind = kind;
			this._logLow = logLow;
			this._endMsg = endMsg;
			this._ip = array[0];
			this._port = int.Parse(array[1]);
			this.Start();
		}

		public virtual void HandleMessage(string m)
		{
			string str = Tu.DataToString(m.ToCharArray());
			TcpClient._logger.Trace("--RX c--: " + str);
			this.OnMessageReceived(new MessageReceivedEventArgs
			{
				Message = m
			});
		}

		public bool SendLow(string msg, Coding enc = Coding.Asc)
		{
			string str = Tu.DataToString(msg.ToCharArray());
			TcpClient._logger.Trace("--TX--: " + str);
			return this.SendBytes(TcpClient._client, Encoding.ASCII.GetBytes(msg));
		}

		public void SendLow(List<char> c)
		{
			char[] array = c.ToArray();
			byte[] bytes = Encoding.ASCII.GetBytes(array);
			TcpClient._logger.Trace("--TX--: " + Tu.DataToString(array));
			this.SendBytes(TcpClient._client, bytes);
		}

		public void SendLow(byte b)
		{
			TcpClient._logger.Trace("--TX--: " + Tu.DataToString(new char[]
			{
				(char)b
			}));
			this.SendBytes(TcpClient._client, new byte[]
			{
				b
			});
		}

		private void Start()
		{
			try
			{
				IPAddress address = IPAddress.Parse(this._ip);
				IPEndPoint remoteEP = new IPEndPoint(address, this._port);
				Socket client = TcpClient._client;
				if (client != null)
				{
					client.Close();
				}
				bool connected;
				do
				{
					TcpClient._client = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
					TcpClient._logger.Info<string, int>("Try Connect to {0} {1}", this._ip, this._port);
					IAsyncResult asyncResult = TcpClient._client.BeginConnect(remoteEP, null, TcpClient._client);
					asyncResult.AsyncWaitHandle.WaitOne();
					connected = TcpClient._client.Connected;
					bool flag = !connected;
					if (flag)
					{
						TcpClient._client.Close();
						Thread.Sleep(30000);
					}
				}
				while (!connected);
				TcpClient._logger.Info<EndPoint>("Connected to {0}", TcpClient._client.RemoteEndPoint);
				this.Receive(TcpClient._client);
			}
			catch (Exception ex)
			{
				TcpClient._logger.Error(ex.ToString());
			}
		}

		public void Close()
		{
			TcpClient._client.Shutdown(SocketShutdown.Both);
			TcpClient._client.Close();
		}

		private void LogMsg(string m)
		{
			string str = Tu.DataToString(m.ToCharArray());
			TcpClient._logger.Trace("--RL--: " + str);
		}

		private void ConnectCallback(IAsyncResult ar)
		{
			try
			{
				Socket socket = (Socket)ar.AsyncState;
				socket.EndConnect(ar);
				TcpClient._logger.Info<EndPoint>("Connected to {0}", socket.RemoteEndPoint);
				TcpClient.connectDone.Set();
			}
			catch (Exception ex)
			{
				TcpClient._logger.Error(ex.ToString());
			}
		}

		private void Receive(Socket client)
		{
			try
			{
				StateObject stateObject = new StateObject
				{
					workSocket = client
				};
				client.BeginReceive(stateObject.buffer, 0, 256, SocketFlags.None, new AsyncCallback(this.ReceiveCallback), stateObject);
			}
			catch (Exception ex)
			{
				TcpClient._logger.Error(ex.ToString());
			}
		}

		private void ReceiveCallback(IAsyncResult ar)
		{
			try
			{
				StateObject stateObject = (StateObject)ar.AsyncState;
				Socket workSocket = stateObject.workSocket;
				int num = workSocket.EndReceive(ar);
				bool flag = num <= 0;
				if (!flag)
				{
					string @string = Encoding.ASCII.GetString(stateObject.buffer, 0, num);
					this._msg += @string;
					bool logLow = this._logLow;
					if (logLow)
					{
						this.LogMsg(@string);
					}
					SerialManager.MsgState msgState = this.ComplteMsg(this._msg);
					bool flag2 = msgState == SerialManager.MsgState.Extra;
					if (flag2)
					{
						this._msg = "";
					}
					else
					{
						bool flag3 = msgState == SerialManager.MsgState.Ok;
						if (flag3)
						{
							SerialManager.HandleDuplicated(this._msg, new SerialManager.Handle(this.HandleMessage), this._kind);
							this._msg = "";
						}
					}
					workSocket.BeginReceive(stateObject.buffer, 0, 256, SocketFlags.None, new AsyncCallback(this.ReceiveCallback), stateObject);
				}
			}
			catch (Exception ex)
			{
				TcpClient._logger.Error(ex.ToString());
				this.Start();
			}
		}

		private SerialManager.MsgState ComplteMsg(string m)
		{
			return SerialManager.CompleteMsg(m, this._endMsg, this._kind, this._checkEnd);
		}

		private static void Send(Socket client, string data)
		{
			byte[] bytes = Encoding.ASCII.GetBytes(data);
			client.BeginSend(bytes, 0, bytes.Length, SocketFlags.None, new AsyncCallback(TcpClient.SendCallback), client);
		}

		public bool SendBytes(Socket client, byte[] byteData)
		{
			bool result;
			try
			{
				client.BeginSend(byteData, 0, byteData.Length, SocketFlags.None, new AsyncCallback(TcpClient.SendCallback), client);
				result = true;
			}
			catch (Exception ex)
			{
				TcpClient._logger.Error(ex.ToString());
				this.Start();
				client.BeginSend(byteData, 0, byteData.Length, SocketFlags.None, new AsyncCallback(TcpClient.SendCallback), client);
				result = true;
			}
			return result;
		}

		private static void SendCallback(IAsyncResult ar)
		{
			try
			{
				Socket socket = (Socket)ar.AsyncState;
				int num = socket.EndSend(ar);
				TcpClient.sendDone.Set();
			}
			catch (Exception ex)
			{
				TcpClient._logger.Error(ex.ToString());
			}
		}

		//[DebuggerBrowsable(DebuggerBrowsableState.Never)]
		public event EventHandler<MessageReceivedEventArgs> MessageReceived;

		protected virtual void OnMessageReceived(MessageReceivedEventArgs e)
		{
			EventHandler<MessageReceivedEventArgs> messageReceived = this.MessageReceived;
			if (messageReceived != null)
			{
				messageReceived(this, e);
			}
		}

		private static Logger _logger = LogManager.GetCurrentClassLogger();

		private static ManualResetEvent connectDone = new ManualResetEvent(false);

		private static ManualResetEvent sendDone = new ManualResetEvent(false);

		private static ManualResetEvent receiveDone = new ManualResetEvent(false);

		private static Socket _client;

		private string _msg = "";

		private string _endMsg;

		private bool _checkEnd;

		public Jihas _kind;

		private bool _logLow;

		private int _port;

		private string _ip;
	}
}
