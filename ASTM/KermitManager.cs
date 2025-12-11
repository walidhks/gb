using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using GbService.Communication;
using GbService.Communication.Common;
using GbService.Model.Contexts;
using GbService.Model.Domain;
using GbService.Other;
using NLog;

namespace GbService.ASTM
{
	public class KermitManager : AstmManager
	{
		public KermitManager(ILowManager il, Instrument instrument) : base(il, instrument)
		{
		}

		protected override void HandleMessage(string m)
		{
			char c = m[3];
			char c2 = m[2];
			bool flag = c == 'S';
			if (flag)
			{
				bool flag2 = AstmManager.LowState == LowState.Idle;
				if (flag2)
				{
					this._seq = 0;
					this.SendLow("Y~* @-#N1");
					base.PutInState(LowState.Receive, -1);
				}
			}
			else
			{
				bool flag3 = AstmManager.LowState == LowState.Send;
				if (flag3)
				{
					bool flag4 = c == 'Y';
					if (flag4)
					{
						bool flag5 = c2 == KermitManager.Char(this._seq - 2);
						if (flag5)
						{
							this.HandleNak();
						}
						else
						{
							bool flag6 = c2 == KermitManager.Char(this._seq - 1);
							if (flag6)
							{
								this.HandleAck();
							}
							else
							{
								this.SendLow("E0000 Invalid Sequence");
							}
						}
					}
					else
					{
						bool flag7 = c == 'N';
						if (flag7)
						{
							bool flag8 = c2 == KermitManager.Char(this._seq - 1);
							if (flag8)
							{
								this.HandleNak();
							}
							else
							{
								this.SendLow("E0000 Invalid Sequence");
							}
						}
						else
						{
							bool flag9 = c == 'E';
							if (flag9)
							{
								AstmManager._currentMessage.Clear();
								this.PutInIdleState(3000, 3);
							}
							else
							{
								AstmManager._currentMessage.Clear();
								this.PutInIdleState(10000, 3);
							}
						}
					}
				}
				else
				{
					bool flag10 = AstmManager.LowState == LowState.Receive;
					if (flag10)
					{
						this.HandleText(m);
					}
				}
			}
		}

		public void SendLow(string s)
		{
			char c = KermitManager.Char(s.Length + 2);
			char c2 = KermitManager.Char(this._seq);
			string text = string.Format("{0}{1}{2}", c, c2, s);
			string text2 = Crc.Kermit(text);
			string msg = string.Format("{0}{1}{2}{3}", new object[]
			{
				'\u0001',
				text,
				text2,
				'\r'
			});
			this._il.SendLow(msg, Coding.Asc);
			this._seq = (this._seq + 1) % 64;
		}

		private static char Char(int s)
		{
			return (char)(s + 32);
		}

		protected override void HandleAck()
		{
			bool flag = AstmManager._currentMessage.Count == 0;
			if (!flag)
			{
				AstmManager._currentMessage.RemoveAt(0);
				this._nakNumber = 0;
				bool flag2 = AstmManager._currentMessage.Count > 0;
				if (flag2)
				{
					this.SendLow(AstmManager._currentMessage.First<string>());
				}
				else
				{
					base.CancelCurrent(LabMessageState.Ok);
					this.PutInIdleState(0, 0);
				}
			}
		}

		protected override void HandleNak()
		{
			this._nakNumber++;
			bool flag = this._nakNumber < 6;
			if (flag)
			{
				this.SendLow(AstmManager._currentMessage.First<string>());
			}
			else
			{
				AstmManager._currentMessage.Clear();
				base.CancelCurrent(LabMessageState.Annulé);
				this.PutInIdleState(2000, 0);
			}
		}

		protected override void PutInIdleState(int t, int s = 0)
		{
			this._seq = 0;
			base.PutInState(LowState.Idle, -1);
			Thread.Sleep(t);
			this.SendAstmMsg(0);
		}

		public override void PutMsgInSendingQueue(string msg, long sid = 0L)
		{
			try
			{
				LaboContext laboContext = new LaboContext();
				laboContext.LabMessage.Add(new LabMessage(msg, sid, this.InstrumentId));
				laboContext.SaveChanges();
			}
			catch (Exception ex)
			{
				this.LogFile.Error(new LogMessageGenerator(ex.ToString));
			}
		}

		public override bool SendAstmMsg(int s = 0)
		{
			bool flag = AstmManager.LowState == LowState.Idle;
			if (flag)
			{
				LaboContext laboContext = new LaboContext();
				LabMessage labMessage = laboContext.LabMessage.FirstOrDefault((LabMessage x) => x.InstrumentId == (int?)this.InstrumentId && (int)x.LabMessageStatus == 0 && x.LabMessageRetry < 6);
				bool flag2 = labMessage == null;
				if (flag2)
				{
					return false;
				}
				base.PutInState(LowState.Send, -1);
				LabMessage labMessage2 = labMessage;
				int labMessageRetry = labMessage2.LabMessageRetry;
				labMessage2.LabMessageRetry = labMessageRetry + 1;
				laboContext.SaveChanges();
				this._currentId = labMessage.LabMessageID;
				string labMessageValue = labMessage.LabMessageValue;
				this.LogFile.Info("TimeState : " + base.TimeState.ToString());
				this.Compose(labMessageValue, 720);
				this.SendLow(AstmManager._currentMessage.First<string>());
			}
			return true;
		}

		protected override void Compose(string message, int max = 240)
		{
			AstmManager._currentMessage.Clear();
			AstmManager._currentMessage.Add("S~* @-#N1");
			List<string> list = TextUtil.ChunksUpto(message, max).ToList<string>();
			foreach (string chank in list)
			{
				List<string> files = KermitManager.GetFiles(chank);
				AstmManager._currentMessage.AddRange(files);
				AstmManager._currentMessage.Add("B");
			}
		}

		private static List<string> GetFiles(string chank)
		{
			List<string> list = new List<string>
			{
				"FS" + DateTime.Now.ToString("yyyyMMddHHmmss")
			};
			list.AddRange((from x in TextUtil.ChunksUpto(chank, 90)
			select "D" + x).ToList<string>());
			list.Add("Z");
			return list;
		}

		protected override void HandleText(string m)
		{
			this.Ack(m);
		}

		protected override void Ack(string m)
		{
			char c = m[3];
			bool flag = c == 'E';
			if (flag)
			{
				this.PutInIdleState(2000, 0);
			}
			else
			{
				this.SendLow("Y");
				bool flag2 = c == 'D';
				if (flag2)
				{
					AstmManager._msgReceived += Tu.KermitMessage(m);
				}
				else
				{
					bool flag3 = c == 'Z';
					if (flag3)
					{
						string msg = string.Copy(AstmManager._msgReceived);
						AstmManager._msgReceived = "";
						new KermitHigh(this).Parse(msg);
					}
					else
					{
						bool flag4 = c == 'B';
						if (flag4)
						{
							this.PutInIdleState(200, 0);
						}
					}
				}
			}
		}

		private int _seq;

		private List<string> ll = new List<string>
		{
			"FS0003448",
			"D" + "912".PadRight(15) + "10 .0001 %Y$462+]",
			"Z",
			"B"
		};
	}
}
