using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Timers;
using GbService.Communication;
using GbService.Communication.Common;
using GbService.Communication.Serial;
using GbService.HL7;
using GbService.Model.Contexts;
using GbService.Model.Domain;
using GbService.Other;
using NLog;

namespace GbService.ASTM
{
	public class AstmManager
	{
		public AstmManager(ILowManager il, Instrument instrument)
		{
			this.Init(il, instrument);
		}

		private void Init(ILowManager il, Instrument instrument)
		{
			bool flag = il == null;
			if (flag)
			{
				this.LogFile.Error("IL is null");
			}
			else
			{
				this._il = il;
				this._il.MessageReceived += this.OnMessageReceived;
				this.Instrument = instrument;
				this.InstrumentCode = this.Instrument.InstrumentCode;
				this.Kind = this.Instrument.Kind;
				this.InstrumentId = this.Instrument.InstrumentId;
				int seconds = (this.Instrument.Kind == Jihas.Kenza240) ? 10 : 20;
				this.ScheduleRefresh(seconds);
			}
		}

		public Instrument Instrument { get; set; }

		private void ScheduleRefresh(int seconds)
		{
			this._serviceTimer = new System.Timers.Timer
			{
				Enabled = true,
				Interval = (double)(1000 * seconds)
			};
			this._serviceTimer.Elapsed += this.Refresh;
			this._serviceTimer.Start();
		}

		private void Refresh(object sender, ElapsedEventArgs e)
		{
			List<Jihas> list = new List<Jihas>
			{
				Jihas.Kenza240,
				Jihas.CobasE411
			};
			int num = list.Contains(this.Kind) ? 10 : ((this.Kind == Jihas.Vidas) ? 120 : 51);
			bool flag = AstmManager.LowState == LowState.Send && this.TimeState.AddSeconds((double)num) < DateTime.Now;
			if (flag)
			{
				AstmManager.LowState = LowState.Idle;
			}
			bool flag2 = AstmManager.LowState == LowState.Idle;
			if (flag2)
			{
				this.SendAstmMsg(2);
			}
		}

		public virtual void OnMessageReceived(object sender, MessageReceivedEventArgs e)
		{
			this.LastReceived = DateTime.Now;
			this.Handle(e.Message);
		}

		protected void Handle(string s)
		{
			List<Jihas> list = new List<Jihas>
			{
				Jihas.CobasC311
			};
			try
			{
				bool flag = list.Contains(this.Kind);
				if (flag)
				{
					List<Frame> matches = Tu.GetMatches(s);
					foreach (Frame frame in matches)
					{
						this.HandleMessagePro(frame.Kind, frame.Value);
					}
				}
				else
				{
					this.HandleMessage(s);
				}
			}
			catch (Exception ex)
			{
				this.LogFile.Error(new LogMessageGenerator(ex.ToString));
			}
		}

		protected virtual void HandleMessagePro(FrameKind fk, string m)
		{
			this.LogFile.Trace(string.Format("pro={0}, m={1}", fk, m));
			bool flag = fk == FrameKind.Enq;
			if (flag)
			{
				bool flag2 = AstmManager.LowState == LowState.Send && this.TimeState.AddSeconds(2.0) > DateTime.Now && (this.Kind == Jihas.OrthoVision || this.Kind == Jihas.BA200_BA400);
				if (flag2)
				{
					bool flag3 = this.Kind == Jihas.BA200_BA400;
					if (flag3)
					{
						this._il.SendLow(5);
					}
				}
				else
				{
					this._lastFrameNumber = 0;
					AstmManager._currentMessage.Clear();
					AstmManager._msgReceived = "";
					this.PutInReceiveState();
				}
			}
			else
			{
				bool flag4 = AstmManager.LowState == LowState.Send;
				if (flag4)
				{
					bool flag5 = fk == FrameKind.Ack;
					if (flag5)
					{
						this.HandleAck();
					}
					else
					{
						bool flag6 = fk == FrameKind.Nak;
						if (flag6)
						{
							this.HandleNak();
						}
						else
						{
							bool flag7 = fk == FrameKind.Eot;
							if (flag7)
							{
								AstmManager._currentMessage.Clear();
								this.PutInIdleState(500, 3);
							}
							else
							{
								this.HandleNak();
							}
						}
					}
				}
				else
				{
					bool flag8 = AstmManager.LowState == LowState.Receive;
					if (flag8)
					{
						bool flag9 = fk == FrameKind.Eot;
						if (flag9)
						{
							string text = string.Copy(AstmManager._msgReceived);
							AstmManager._msgReceived = "";
							this.PutInState(LowState.Idle, -1);
							this._lastFrameNumber = 0;
							bool flag10 = this.Kind == Jihas.Iris;
							if (flag10)
							{
								new IrisIQ200MessageHandler(this).Parse(text);
							}
							else
							{
								AstmHigh.Parse(this, text);
							}
						}
						else
						{
							bool flag11 = this.Kind == Jihas.Maglumi;
							if (flag11)
							{
								m = m.Replace('\u0006'.ToString(), "");
							}
							this.HandleTextPro(fk, m);
						}
					}
				}
			}
		}

		protected virtual void HandleMessage(string m)
		{
			bool flag = m.Contains('\u0005');
			if (flag)
			{
				bool flag2 = AstmManager.LowState == LowState.Send && this.TimeState.AddSeconds(2.0) > DateTime.Now && (this.Kind == Jihas.OrthoVision || this.Kind == Jihas.BA200_BA400);
				if (flag2)
				{
					bool flag3 = this.Kind == Jihas.BA200_BA400;
					if (flag3)
					{
						this._il.SendLow(5);
					}
				}
				else
				{
					this._lastFrameNumber = 0;
					AstmManager._currentMessage.Clear();
					AstmManager._msgReceived = "";
					this.PutInReceiveState();
				}
			}
			else
			{
				bool flag4 = AstmManager.LowState == LowState.Send;
				if (flag4)
				{
					bool flag5 = m.Contains('\u0006');
					if (flag5)
					{
						this.HandleAck();
					}
					else
					{
						bool flag6 = m.Contains('\u0015');
						if (flag6)
						{
							this.HandleNak();
						}
						else
						{
							bool flag7 = m.Contains('\u0004');
							if (flag7)
							{
								AstmManager._currentMessage.Clear();
								this.PutInIdleState(500, 3);
							}
							else
							{
								this.HandleNak();
							}
						}
					}
				}
				else
				{
					bool flag8 = AstmManager.LowState == LowState.Receive;
					if (flag8)
					{
						bool flag9 = m.Contains('\u0004');
						if (flag9)
						{
							this.LogFile.Debug(this.Kind.ToString() + " " + this.Instrument.Mode.ToString());
							bool flag10 = this.Kind == Jihas.CobasC311 && this.Instrument.Mode == 2;
							if (flag10)
							{
								this._il.SendLow(6);
							}
							string text = string.Copy(AstmManager._msgReceived);
							AstmManager._msgReceived = "";
							this.PutInState(LowState.Idle, -1);
							this._lastFrameNumber = 0;
							bool flag11 = this.Kind == Jihas.Iris;
							if (flag11)
							{
								new IrisIQ200MessageHandler(this).Parse(text);
							}
							else
							{
								bool flag12 = this.Kind == Jihas.ErbaEc90 || this.Kind == Jihas.SysmexSuit;
								if (flag12)
								{
									Hl7Manager.HandleHprim(text, this.Instrument);
								}
								else
								{
									AstmHigh.Parse(this, text);
								}
							}
						}
						else
						{
							bool flag13 = this.Kind == Jihas.Maglumi;
							if (flag13)
							{
								m = m.Replace('\u0006'.ToString(), "");
							}
							this.HandleText(m);
						}
					}
				}
			}
		}

		protected virtual void PutInIdleState(int t, int location = 0)
		{
			this.PutInState(LowState.Idle, location);
			Thread.Sleep(t);
			this.SendAstmMsg(0);
		}

		protected virtual void HandleTextPro(FrameKind fk, string m)
		{
			bool flag = fk == FrameKind.Etb || fk == FrameKind.Etx;
			if (flag)
			{
				this.Ack(m);
			}
			else
			{
				this.LogFile.Debug("message non connu: " + m);
				this._il.SendLow(4);
				AstmManager._msgReceived = "";
				this.PutInState(LowState.Idle, -1);
				this._lastFrameNumber = 0;
			}
		}

		protected virtual void HandleText(string m)
		{
			bool flag = m.Contains('\u0017');
			if (flag)
			{
				this.Ack(m);
			}
			else
			{
				bool flag2 = m.Contains('\u0003');
				if (flag2)
				{
					this.Ack(m);
				}
				else
				{
					this.LogFile.Debug("message non connu: " + m);
				}
			}
		}

		protected virtual void HandleAck()
		{
			bool flag = AstmManager._currentMessage.Count == 0;
			if (!flag)
			{
				AstmManager._currentMessage.RemoveAt(0);
				this._nakNumber = 0;
				string text = AstmManager._currentMessage.First<string>();
				this._il.SendLow(text, Coding.Asc);
				bool flag2 = text == '\u0004'.ToString();
				if (flag2)
				{
					this.CancelCurrent(LabMessageState.Ok);
					int t = (this.Kind == Jihas.Dialab || this.Kind == Jihas.Pictus200) ? 5000 : 100;
					this.PutInIdleState(t, 1);
				}
			}
		}

		protected virtual void HandleNak()
		{
			this._nakNumber++;
			bool flag = this._nakNumber <= 6;
			if (flag)
			{
				this._il.SendLow(AstmManager._currentMessage.First<string>(), Coding.Asc);
			}
			else
			{
				this._il.SendLow(4);
				this._nakNumber = 0;
				AstmManager._currentMessage.Clear();
				this.CancelCurrent(LabMessageState.Annulé);
				int t = (this.Kind == Jihas.Dialab || this.Kind == Jihas.Pictus200) ? 5000 : 1000;
				this.PutInIdleState(t, 2);
			}
		}

		protected virtual void Ack(string msg)
		{
			bool flag = this.Kind == Jihas.Maglumi;
			if (flag)
			{
				this._il.SendLow(6);
				AstmManager._msgReceived += Tu.CleanMsgMaglumi(msg);
			}
			else
			{
				bool flag2 = this.Kind == Jihas.ErbaEc90;
				if (flag2)
				{
					this._il.SendLow(6);
					AstmManager._msgReceived = AstmManager._msgReceived + Tu.CleanMsgEc90(msg) + "\r";
				}
				else
				{
					Tuple<int, string> tuple = (this.Kind == Jihas.CobasE411) ? Tu.CleanMsgCobas411(msg, this.LogFile) : Tu.CleanMsg(msg, this.LogFile, this.Kind);
					this._il.SendLow(6);
					this._lastFrameNumber = tuple.Item1;
					AstmManager._msgReceived += tuple.Item2;
					bool flag3 = this.Kind == Jihas.Ismart || this.Kind == Jihas.SysmexCA600 || this.Kind == Jihas.MaglumiX8 || this.Kind == Jihas.Advia1800;
					if (flag3)
					{
						AstmManager._msgReceived += "\r";
					}
				}
			}
		}

		public virtual bool SendAstmMsg(int s = 0)
		{
			object obj = this._obj;
			bool result;
			lock (obj)
			{
				int millisecondsTimeout = 50;
				Thread.Sleep(millisecondsTimeout);
				result = this.SendAstmMsgBase(s);
			}
			return result;
		}

		private bool SendAstmMsgBase(int s)
		{
			bool flag = AstmManager.LowState == LowState.Send && this.TimeState.AddSeconds(30.0) < DateTime.Now;
			if (flag)
			{
				AstmManager.LowState = LowState.Idle;
			}
			int maxRetry = (this.Instrument.Kind == Jihas.Kenza240) ? 1500 : ((this.Instrument.Kind == Jihas.Vidas) ? 150 : 6);
			bool flag2 = this.Instrument.Kind == Jihas.Advia1800;
			if (flag2)
			{
				maxRetry = 1;
			}
			bool flag3 = AstmManager.LowState == LowState.Idle;
			if (flag3)
			{
				LaboContext laboContext = new LaboContext();
				LabMessage labMessage = laboContext.LabMessage.FirstOrDefault((LabMessage x) => x.InstrumentId == (int?)this.InstrumentId && (int)x.LabMessageStatus == 0 && x.LabMessageRetry < maxRetry);
				bool flag4 = labMessage == null;
				if (flag4)
				{
					return false;
				}
				LabMessage labMessage2 = labMessage;
				int labMessageRetry = labMessage2.LabMessageRetry;
				labMessage2.LabMessageRetry = labMessageRetry + 1;
				laboContext.SaveChanges();
				this.LogFile.Info(string.Format("MessageId = {0}, Retry : {1}", labMessage.LabMessageID, labMessage.LabMessageRetry));
				this._currentId = labMessage.LabMessageID;
				string labMessageValue = labMessage.LabMessageValue;
				List<Jihas> list = new List<Jihas>
				{
					Jihas.Acl,
					Jihas.Arkray,
					Jihas.Maglumi,
					Jihas.SelectraProM,
					Jihas.Acl9000,
					Jihas.SysmexXS_XN,
					Jihas.Bioflash,
					Jihas.Iris,
					Jihas.BA200_BA400,
					Jihas.Biorad10,
					Jihas.Vidas,
					Jihas.Targa,
					Jihas.BeckmanCX9,
					Jihas.Kenza240,
					Jihas.SysmexUf1000,
					Jihas.Vitek
				};
				bool flag5 = this.Kind == Jihas.Iris;
				if (flag5)
				{
					this.Compose(TextUtil.UnicodeRepresentation(labMessageValue + "\r\n"), 240);
				}
				else
				{
					bool flag6 = this.Kind == Jihas.Response || this.Kind == Jihas.Macura;
					if (flag6)
					{
						this.Compose(labMessageValue, 1024);
					}
					else
					{
						bool flag7 = list.Contains(this.Kind);
						if (flag7)
						{
							this.Compose(labMessageValue, 240);
						}
						else
						{
							this.Compose1(labMessageValue);
						}
					}
				}
				this._il.SendLow(AstmManager._currentMessage.First<string>(), Coding.Asc);
				this.PutInState(LowState.Send, -1);
			}
			return true;
		}

		protected virtual void Compose(string message, int max = 240)
		{
			AstmManager._currentMessage.Clear();
			List<string> list = new List<string>();
			message = message.Replace("<CR>", '\r'.ToString());
			List<string> list2 = TextUtil.ChunksUpto(message, max).ToList<string>();
			int num = 1;
			foreach (string text in list2)
			{
				string text2 = (num % 8).ToString() ?? "";
				string str = ((this.Instrument.Mode == 1 && this.Kind == Jihas.Maglumi) ? "" : text2) + text;
				bool flag = list2.IndexOf(text) == list2.Count - 1;
				string text3 = "\u0002" + str + (flag ? '\u0003' : '\u0017').ToString();
				string item = text3 + Tu.GetCheckSumValue(text3) + "\r\n";
				list.Add(item);
				num++;
			}
			AstmManager._currentMessage.Add('\u0005'.ToString());
			AstmManager._currentMessage.AddRange(list);
			AstmManager._currentMessage.Add('\u0004'.ToString());
		}

		private void Compose1(string message)
		{
			AstmManager._currentMessage.Clear();
			List<string> list = new List<string>();
			message = message.Replace("<CR>", '\r'.ToString());
			List<string> list2 = message.Split(new char[]
			{
				'\r'
			}, StringSplitOptions.RemoveEmptyEntries).ToList<string>();
			this.LogFile.Info(string.Format("chanks = {0}", list2.Count));
			int num = 1;
			foreach (string text in list2)
			{
				this.LogFile.Info("chank = " + text);
				string text2 = (num % 8).ToString() + text;
				bool flag = list2.IndexOf(text) == list2.Count - 1;
				bool flag2 = this.Instrument.Kind == Jihas.Immulite2000 && this.Instrument.Mode == 1;
				string item;
				if (flag2)
				{
					string text3 = text2 + "\r";
					item = string.Concat(new string[]
					{
						"\u0002",
						text3,
						(flag ? '\u0003' : '\u0017').ToString(),
						Tu.GetCheckSumValue(text3),
						"\r\n"
					});
				}
				else
				{
					bool flag3 = this.Instrument.Kind == Jihas.Advia1800;
					if (flag3)
					{
						item = "\u0002" + text2 + " \u0003  \r\n";
					}
					else
					{
						string text4 = "\u0002" + text2 + "\r\u0003";
						item = text4 + Tu.GetCheckSumValue(text4) + "\r\n";
					}
				}
				list.Add(item);
				num++;
			}
			AstmManager._currentMessage.Add('\u0005'.ToString());
			AstmManager._currentMessage.AddRange(list);
			AstmManager._currentMessage.Add('\u0004'.ToString());
		}

		public virtual void PutMsgInSendingQueue(string msg, long sid = 0L)
		{
			bool flag = msg == null;
			if (!flag)
			{
				try
				{
					LaboContext laboContext = new LaboContext();
					LabMessage labMessage = (from x in laboContext.LabMessage
					where x.InstrumentId == (int?)this.InstrumentId && x.LabMessageValue == msg && (int)x.LabMessageStatus == 0
					select x).ToList<LabMessage>().LastOrDefault<LabMessage>();
					bool flag2 = labMessage == null;
					if (flag2)
					{
						laboContext.LabMessage.Add(new LabMessage(msg, sid, this.InstrumentId));
						this.LogFile.Debug(string.Format("labmesage added {0} = {1}", sid, msg));
					}
					else
					{
						labMessage.LabMessageRetry = 0;
						this.LogFile.Info(string.Format("labmesage update {0}", sid));
					}
					laboContext.SaveChanges();
					bool flag3 = this.Kind != Jihas.Dialab && this.Kind != Jihas.Pictus200;
					if (flag3)
					{
						this.SendAstmMsg(1);
					}
				}
				catch (Exception ex)
				{
					this.LogFile.Error(new LogMessageGenerator(ex.ToString));
				}
			}
		}

		protected void PutInState(LowState s, int location = -1)
		{
			AstmManager.LowState = s;
			this.TimeState = DateTime.Now;
			this.LogFile.Info(string.Format("State {0}, location {1}", s, location));
		}

		protected void PutInReceiveState()
		{
			this._il.SendLow(6);
			this._ackNumber = 0;
			this.PutInState(LowState.Receive, -1);
		}

		protected void RemoveCurrent()
		{
			LaboContext laboContext = new LaboContext();
			LabMessage labMessage = laboContext.LabMessage.Find(new object[]
			{
				this._currentId
			});
			bool flag = labMessage == null;
			if (!flag)
			{
				laboContext.LabMessage.Remove(labMessage);
				laboContext.SaveChanges();
			}
		}

		protected void CancelCurrent(LabMessageState state = LabMessageState.Annulé)
		{
			LaboContext laboContext = new LaboContext();
			LabMessage labMessage = laboContext.LabMessage.Find(new object[]
			{
				this._currentId
			});
			bool flag = labMessage == null;
			if (!flag)
			{
				labMessage.LabMessageStatus = state;
				this.LogFile.Info(string.Format("SetCurrent State: MessageId = {0}, State = {1}", labMessage.LabMessageID, state));
				laboContext.SaveChanges();
			}
		}

		public DateTime TimeState { get; set; }

		public DateTime LastReceived { get; set; }

		public static LowState LowState { get; set; }

		public void Close()
		{
			this._il.Close();
		}

		private readonly object _obj = new object();

		protected static readonly List<string> _currentMessage = new List<string>();

		protected static string _msgReceived = "";

		protected int _nakNumber;

		protected int _ackNumber;

		protected int _lastFrameNumber;

		public ILowManager _il;

		public Logger LogFile = LogManager.GetCurrentClassLogger();

		public int InstrumentCode;

		public Jihas Kind;

		public int InstrumentId;

		private System.Timers.Timer _serviceTimer;

		protected long _currentId;
	}
}
