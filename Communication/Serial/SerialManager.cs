using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO.Ports;
using System.Text;
using System.Text.RegularExpressions;
using GbService.Communication.Common;
using NLog;
using NPOI.SS.Formula.Functions;

namespace GbService.Communication.Serial
{
	public class SerialManager : ILowManager
	{
		public SerialManager(string serial, Jihas kind, bool logLow = false, string endMsg = null, Handshake hs = Handshake.None, bool checkEnd = true, bool dtr = false, bool rts = false)
		{
			string[] array = serial.Split(new char[]
			{
				','
			});
			string text = array[4];
			SerialManager._logFile.Info("port = " + text);
			this._comPort.DataReceived += this.comPort_DataReceived;
			this._endMsg = endMsg;
			this._kind = kind;
			this._logLow = logLow;
			this._checkEnd = checkEnd;
			try
			{
				bool isOpen = this._comPort.IsOpen;
				if (isOpen)
				{
					this._comPort.Close();
				}
				this._comPort.BaudRate = int.Parse(array[0]);
				this._comPort.Parity = (Parity)Enum.Parse(typeof(Parity), array[1]);
				this._comPort.StopBits = (StopBits)Enum.Parse(typeof(StopBits), array[2]);
				this._comPort.DataBits = int.Parse(array[3]);
				this._comPort.PortName = text;
				this._comPort.Handshake = hs;
				this._comPort.DtrEnable = dtr;
				this._comPort.RtsEnable = rts;
				this._comPort.Open();
				SerialManager._logFile.Info(text + " opened at " + DateTime.Now.ToString() + "\n");
			}
			catch (Exception ex)
			{
				SerialManager._logFile.Error(new LogMessageGenerator(ex.ToString));
			}
		}

		public virtual void HandleMessage(string m)
		{
			string str = Tu.DataToString(m.ToCharArray());
			SerialManager._logFile.Trace("--RX--: " + str);
			this.OnMessageReceived(new MessageReceivedEventArgs
			{
				Message = m
			});
		}

		public bool SendLow(string msg, Coding enc = Coding.Asc)
		{
			string str = Tu.DataToString(msg.ToCharArray());
			SerialManager._logFile.Trace("--TX--: " + str);
			this.WriteData(msg);
			return true;
		}

		public void SendLow(List<char> c)
		{
			char[] array = c.ToArray();
			byte[] bytes = Encoding.ASCII.GetBytes(array);
			SerialManager._logFile.Trace("--TX--: " + Tu.DataToString(array));
			this._comPort.Write(bytes, 0, bytes.Length);
		}

		public void SendLow(byte b)
		{
			char c = (char)b;
			SerialManager._logFile.Trace("--TX--: " + Tu.DataToString(new char[]
			{
				c
			}));
			this.WriteData(c.ToString());
		}

		public void WriteData(string msg)
		{
			try
			{
				bool flag = !this._comPort.IsOpen;
				if (flag)
				{
					this._comPort.Open();
				}
				this._comPort.Write(msg);
			}
			catch (Exception ex)
			{
				SerialManager._logFile.Error(new LogMessageGenerator(ex.ToString));
			}
		}

		public void Close()
		{
			this._comPort.Close();
		}

       public virtual void comPort_DataReceived(object sender, SerialDataReceivedEventArgs e)
		{
			try
			{
				int bytesToRead = this._comPort.BytesToRead;
				byte[] array = new byte[bytesToRead];
				this._comPort.Read(array, 0, bytesToRead);
                string @string = Encoding.ASCII.GetString(array);
                this._msg += @string;

                // [FIX] Send ACK immediately when ETX is detected for Sysmex UC-1000
                if (this._kind == Jihas.SysmexUC1000 && @string.IndexOf('\u0003') >= 0)
                {
                    this._comPort.Write(new byte[] { 0x06 }, 0, 1);
                }
                bool logLow = this._logLow;
				if (logLow)
				{
					SerialManager._logFile.Trace("--RL--: " + Tu.DataToString(@string.ToCharArray()));
				}
			}
			catch (Exception ex)
			{
				SerialManager._logFile.Error(new LogMessageGenerator(ex.ToString));
			}
			SerialManager.MsgState msgState = SerialManager.CompleteMsg(this._msg, this._endMsg, this._kind, this._checkEnd);
			bool flag = msgState == SerialManager.MsgState.Extra;
			if (flag)
			{
				this._msg = "";
			}
			else
			{
				bool flag2 = msgState == SerialManager.MsgState.Ok;
				if (flag2)
				{
					SerialManager.HandleDuplicated(this._msg, new SerialManager.Handle(this.HandleMessage), this._kind);
					this._msg = "";
				}
			}
		}
        // [ADD THIS AT THE TOP OF THE CLASS, inside SerialManager]
       
        public static void HandleDuplicated(string msg, SerialManager.Handle handle, Jihas kind)
		{
			bool flag = msg.Contains('\u0005'.ToString()) && msg.StartsWith('\u0004'.ToString()) && kind != Jihas.CobasC311;
			if (flag)
			{
				handle('\u0004'.ToString());
				handle('\u0005'.ToString());
			}
			else
			{
				bool flag2 = msg.Contains('\u0002'.ToString()) && msg.Contains('\u0004'.ToString()) && kind == Jihas.Maglumi;
				if (flag2)
				{
					handle(msg.Substring(0, msg.IndexOf('\u0004')));
					handle('\u0004'.ToString());
				}
				else
				{
					handle(msg);
				}
			}
		}

		public static string GetString(byte[] comBuffer, string encoding)
		{
			Encoding encoding2 = Encoding.GetEncoding(encoding);
			byte[] bytes = encoding2.GetBytes(encoding2.GetChars(comBuffer));
			byte[] bytes2 = Encoding.Convert(encoding2, Encoding.ASCII, bytes);
			return Encoding.ASCII.GetString(bytes2);
		}

		public static SerialManager.MsgState CompleteMsg(string m, string endMsg, Jihas kind, bool checkEnd)
		{
			bool flag = !checkEnd;
			SerialManager.MsgState result;
			if (flag)
			{
				result = SerialManager.MsgState.Ok;
			}
			else
			{
				bool flag2 = kind == Jihas.Urit8031;
				if (flag2)
				{
					m = m.Replace("\0", "");
				}
				if (kind <= Jihas.Advia2120)
				{
					if (kind <= Jihas.Vitek)
					{
						if (kind <= Jihas.Ichroma)
						{
							if (kind == Jihas.Vidas || kind == Jihas.MiniVidas)
							{
								goto IL_849;
							}
							switch (kind)
							{
							case Jihas.Esr:
								return (m.StartsWith("XC begin") && m.EndsWith("XC end" + Tu.NL)) ? SerialManager.MsgState.Ok : SerialManager.MsgState.None;
							case Jihas.Euro:
							case Jihas.Psm:
							case Jihas.Yonder:
							case Jihas.OrthoIM:
								goto IL_B24;
							case Jihas.Medconn:
								return (m.Contains("\u0002<SEND>") && m.Contains("</SEND>\u0003" + Tu.NL)) ? SerialManager.MsgState.Ok : SerialManager.MsgState.None;
							case Jihas.SwelabSmall:
								return (m.Contains("<!--:Begin") && m.Contains("<!--:End") && m.EndsWith(Tu.NL)) ? SerialManager.MsgState.Ok : SerialManager.MsgState.None;
							case Jihas.Sysmex_Xt2000i:
								break;
							case Jihas.ABL800:
								return (m.StartsWith('\u0001'.ToString()) && m.EndsWith('\u0004'.ToString())) ? SerialManager.MsgState.Ok : SerialManager.MsgState.None;
							case Jihas.Ichroma:
								return (m.StartsWith("MSH") && m.EndsWith('\r'.ToString())) ? SerialManager.MsgState.Ok : SerialManager.MsgState.None;
							default:
								goto IL_B24;
							}
						}
						else
						{
							if (kind == Jihas.LH780U)
							{
								int num = m.LastIndexOf('\u0003'.ToString(), StringComparison.Ordinal);
								string value = string.Format("--------------{0}{1}", '\r', '\n');
								return (num == m.Length - 1 && m.LastIndexOf(value) != m.IndexOf(value)) ? SerialManager.MsgState.Ok : SerialManager.MsgState.None;
							}
							if (kind == Jihas.EasyLite)
							{
								return (m.Contains("ID#") && m.EndsWith(string.Format("{0}{1}{2}", '\r', '\r', '\r'))) ? SerialManager.MsgState.Ok : SerialManager.MsgState.None;
							}
							switch (kind)
							{
							case Jihas.Minicap:
								return (m.StartsWith('\u0002'.ToString()) && (m.EndsWith('\u0004'.ToString()) || m.EndsWith('\u0003'.ToString()))) ? SerialManager.MsgState.Ok : SerialManager.MsgState.None;
							case Jihas.SysmexUf1000:
							case Jihas.Immulite2000:
							case Jihas.Urilyser:
							case Jihas.Vitros5600:
							case Jihas.Biolis30i:
								goto IL_B24;
							case Jihas.Kenza240:
							{
								bool flag3 = m == '\u0005'.ToString() || m == '\u0006'.ToString() || m == '\u0015'.ToString() || m == '\u0004'.ToString();
								if (flag3)
								{
									return SerialManager.MsgState.Ok;
								}
								return (m.StartsWith('\u0002'.ToString()) && m.EndsWith('\u0003'.ToString())) ? SerialManager.MsgState.Ok : SerialManager.MsgState.None;
							}
							case Jihas.V8:
								break;
							case Jihas.Ge300:
								return (m.StartsWith('\u0002'.ToString()) && m.EndsWith("\u0003" + Tu.NL)) ? SerialManager.MsgState.Ok : SerialManager.MsgState.None;

								case Jihas.SysmexUC1000: // <--- ADD THIS (Sysmex UC-1000)
                               
                                case Jihas.Roller:
								return (m.StartsWith('\u0002'.ToString()) && m.EndsWith('\u0003'.ToString())) ? SerialManager.MsgState.Ok : SerialManager.MsgState.None;
							case Jihas.OrthoVision:
								goto IL_944;
							case Jihas.HumaLytePlus5:
								goto IL_2B5;
							case Jihas.Vitek:
								goto IL_849;
							default:
								goto IL_B24;
							}
						}
						return (m.StartsWith("H|\\^&") && (m.EndsWith("\rL|1|N\r") || m.EndsWith("\rL|1\r"))) ? SerialManager.MsgState.Ok : SerialManager.MsgState.None;
						IL_849:
						bool flag4 = m.Contains('\u0005'.ToString()) || m == '\u0006'.ToString() || m == '\u0015'.ToString() || m.Contains('\u0004'.ToString());
						if (flag4)
						{
							return SerialManager.MsgState.Ok;
						}
						return (m.StartsWith('\u0002'.ToString()) && m.Contains('\u001d'.ToString())) ? SerialManager.MsgState.Ok : SerialManager.MsgState.None;
					}
					else if (kind <= Jihas.Xpand)
					{
						if (kind == Jihas.KenzaMax)
						{
							return m.EndsWith("                   " + Tu.NL) ? SerialManager.MsgState.Ok : SerialManager.MsgState.None;
						}
						if (kind == Jihas.Precision)
						{
							return (m.StartsWith("SAMPLE|") && m.EndsWith('\n'.ToString())) ? SerialManager.MsgState.Ok : SerialManager.MsgState.None;
						}
						if (kind != Jihas.Xpand)
						{
							goto IL_B24;
						}
					}
					else
					{
						if (kind == Jihas.LabNovation)
						{
							return (m.StartsWith("<TRANSMIT>") && m.Contains("</TRANSMIT>")) ? SerialManager.MsgState.Ok : SerialManager.MsgState.None;
						}
						switch (kind)
						{
						case Jihas.CobasE411:
							goto IL_944;
						case Jihas.Iris:
							goto IL_B24;
						case Jihas.LH780:
						{
							bool flag5 = m == '\u0005'.ToString() || m.Contains('\u0006'.ToString()) || m == '\u0015'.ToString() || m.Contains('\u0016'.ToString());
							if (flag5)
							{
								return SerialManager.MsgState.Ok;
							}
							bool flag6 = m.Length == 2;
							if (flag6)
							{
								return SerialManager.MsgState.Ok;
							}
							return (m.LastIndexOf('\u0003'.ToString(), StringComparison.Ordinal) == m.Length - 1) ? SerialManager.MsgState.Ok : SerialManager.MsgState.None;
						}
						case Jihas.AU480:
						{
							bool flag7 = m == '\u0006'.ToString() || m == '\u0015'.ToString();
							if (flag7)
							{
								return SerialManager.MsgState.Ok;
							}
							return (m.LastIndexOf('\u0003'.ToString(), StringComparison.Ordinal) == m.Length - 1) ? SerialManager.MsgState.Ok : SerialManager.MsgState.None;
						}
						default:
							switch (kind)
							{
							case Jihas.Vision:
								return m.EndsWith('\n'.ToString()) ? SerialManager.MsgState.Ok : SerialManager.MsgState.None;
							case (Jihas)2019:
							case Jihas.Dialab:
								goto IL_B24;
							case Jihas.Mythic:
								return m.Contains("END_RESULT") ? SerialManager.MsgState.Ok : SerialManager.MsgState.None;
							case Jihas.Cyan:
							{
								bool flag8 = m.EndsWith('\n'.ToString());
								if (flag8)
								{
									return m.StartsWith("*T") ? SerialManager.MsgState.Ok : SerialManager.MsgState.Extra;
								}
								return SerialManager.MsgState.None;
							}
							case Jihas.Vitros350:
								return (m.StartsWith('\u0001'.ToString()) && m.EndsWith('\r'.ToString())) ? SerialManager.MsgState.Ok : SerialManager.MsgState.None;
							case Jihas.Advia560:
								return (m.EndsWith('\u0004'.ToString()) && m.Contains('\u0001'.ToString())) ? SerialManager.MsgState.Ok : SerialManager.MsgState.None;
							case Jihas.Advia2120:
								return ((m.Contains('\u0002'.ToString()) && m.EndsWith('\u0003'.ToString())) || m.Length == 1) ? SerialManager.MsgState.Ok : SerialManager.MsgState.None;
							default:
								goto IL_B24;
							}
						}
					}
					IL_2B5:
					return m.EndsWith(Tu.NL) ? SerialManager.MsgState.Ok : SerialManager.MsgState.None;
					IL_944:
					bool flag9 = m.Contains('\u0005'.ToString()) || m.Contains('\u0006'.ToString()) || m.Contains('\u0015'.ToString()) || m.Contains('\u0004'.ToString());
					if (flag9)
					{
						return SerialManager.MsgState.Ok;
					}
				}
				else
				{
					if (kind <= Jihas.TosohGx)
					{
						if (kind <= Jihas.Maglumi)
						{
							if (kind == Jihas.CobasC311)
							{
								return (m.EndsWith('\u0005'.ToString()) || m.EndsWith('\u0004'.ToString()) || m.EndsWith('\n'.ToString()) || m.EndsWith('\u0006'.ToString()) || m.EndsWith('\u0015'.ToString())) ? SerialManager.MsgState.Ok : SerialManager.MsgState.None;
							}
							if (kind == Jihas.Cobas400Plus)
							{
								return (m.Contains('\u0001'.ToString()) && m.EndsWith("\u0004\n")) ? SerialManager.MsgState.Ok : SerialManager.MsgState.None;
							}
							if (kind != Jihas.Maglumi)
							{
								goto IL_B24;
							}
							bool flag10 = SerialManager.AckReg.Match(m).Success || SerialManager.NakReg.Match(m).Success;
							if (flag10)
							{
								return SerialManager.MsgState.Ok;
							}
							bool flag11 = m.Contains('\u0005'.ToString());
							if (flag11)
							{
								return SerialManager.MsgState.Ok;
							}
							bool flag12 = m.Contains('\u0004'.ToString()) && m.IndexOf('\u0004'.ToString(), StringComparison.Ordinal) == m.Length - 1;
							if (flag12)
							{
								return SerialManager.MsgState.Ok;
							}
							return SerialManager.MsgState.None;
						}
						else
						{
							if (kind == Jihas.BC5150)
							{
								goto IL_589;
							}
							if (kind != Jihas.SelectraE)
							{
								if (kind != Jihas.TosohGx)
								{
									goto IL_B24;
								}
								bool flag13 = m == '\u0004'.ToString();
								if (flag13)
								{
									return SerialManager.MsgState.Ok;
								}
								return m.Contains('\u0003'.ToString()) ? SerialManager.MsgState.Ok : SerialManager.MsgState.None;
							}
						}
					}
					else if (kind <= Jihas.Gh900)
					{
						if (kind == Jihas.HumaCount5 || kind == Jihas.HumaCount60)
						{
							return SerialManager.ContainBoth(m, '\u0004', '\u0001');
						}
						if (kind != Jihas.Gh900)
						{
							goto IL_B24;
						}
					}
					else
					{
						if (kind == Jihas.MindrayH50P)
						{
							goto IL_589;
						}
						if (kind != Jihas.Targa)
						{
							if (kind != Jihas.BeckmanCX9)
							{
								goto IL_B24;
							}
							bool flag14 = m == "\u0004\u0001" || m == '\u0006'.ToString() || m == '\u0003'.ToString() || m == '\u0015'.ToString() || m == '\u0004'.ToString();
							if (flag14)
							{
								return SerialManager.MsgState.Ok;
							}
							return (m.EndsWith(Tu.NL) && m.StartsWith("[") && m.Contains("]")) ? SerialManager.MsgState.Ok : SerialManager.MsgState.None;
						}
						else
						{
							bool flag15 = m.EndsWith('\u0004'.ToString()) || m == '\u0006'.ToString() || m == '\u0015'.ToString() || (m.Length == 2 && (m.StartsWith("Y") || m.StartsWith("N")));
							if (flag15)
							{
								return SerialManager.MsgState.Ok;
							}
							goto IL_B24;
						}
					}
					return SerialManager.ContainBoth(m, '\u0003', '\u0002');
					IL_589:
					bool success = SerialManager.Reg.Match(m).Success;
					if (success)
					{
						return SerialManager.MsgState.Extra;
					}
					return (m.Contains('\v'.ToString()) && m.LastIndexOf('\u001c'.ToString(), StringComparison.Ordinal) == m.Length - 2) ? SerialManager.MsgState.Ok : SerialManager.MsgState.None;
				}
				IL_B24:
				bool flag16 = TextUtil._hl7.Contains(kind);
				if (flag16)
				{
					int num2 = m.LastIndexOf('\u001c'.ToString(), StringComparison.Ordinal);
					result = ((m.Contains('\v'.ToString()) && m.Contains('\u001c'.ToString()) && num2 >= m.Length - 2) ? SerialManager.MsgState.Ok : SerialManager.MsgState.None);
				}
				else
				{
					bool flag17 = endMsg != null;
					if (flag17)
					{
						string value2 = ((char)Convert.ToByte(endMsg, 16)).ToString();
						result = (m.Contains(value2) ? SerialManager.MsgState.Ok : SerialManager.MsgState.None);
					}
					else
					{
						m = m.Replace("\u0005\u0004", "");
						bool flag18 = SerialManager.AckReg.Match(m).Success || SerialManager.NakReg.Match(m).Success;
						if (flag18)
						{
							result = SerialManager.MsgState.Ok;
						}
						else
						{
							bool flag19 = m.Contains('\u0005'.ToString()) || m == '\u0004'.ToString();
							if (flag19)
							{
								result = SerialManager.MsgState.Ok;
							}
							else
							{
								bool flag20 = m.Contains('\u0003'.ToString()) && m.IndexOf('\u0003'.ToString(), StringComparison.Ordinal) == m.Length - 5;
								if (flag20)
								{
									result = SerialManager.MsgState.Ok;
								}
								else
								{
									result = ((m.Contains('\u0017'.ToString()) && m.IndexOf('\u0017'.ToString(), StringComparison.Ordinal) == m.Length - 5) ? SerialManager.MsgState.Ok : SerialManager.MsgState.None);
								}
							}
						}
					}
				}
			}
			return result;
		}

		private static SerialManager.MsgState ContainBoth(string m, char a, char b)
		{
			bool flag = m.Contains(a.ToString());
			SerialManager.MsgState result;
			if (flag)
			{
				result = (m.Contains(b.ToString()) ? SerialManager.MsgState.Ok : SerialManager.MsgState.Extra);
			}
			else
			{
				result = SerialManager.MsgState.None;
			}
			return result;
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

		public static Logger _logFile = LogManager.GetCurrentClassLogger();

		private string _msg = "";

		private SerialPort _comPort = new SerialPort();

		private string _endMsg;

		private Jihas _kind;

		private bool _logLow;

		private bool _checkEnd;

		private static readonly Regex Reg = new Regex("^\u0002+$");

		private static readonly Regex AckReg = new Regex("^\u0006+$");

		private static readonly Regex NakReg = new Regex("^\u0015+$");

		public delegate void Handle(string x);

		public enum MsgState
		{
			Ok,
			None,
			Extra
		}
	}
}
