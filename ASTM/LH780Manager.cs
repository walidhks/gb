using System;
using System.Linq;
using System.Threading;
using GbService.Communication;
using GbService.Communication.Common;
using GbService.Model.Domain;
using GbService.Other;

namespace GbService.ASTM
{
	public class LH780Manager : AstmManager
	{
		public LH780Manager(ILowManager il, Instrument instrument) : base(il, instrument)
		{
		}

		protected override void HandleMessage(string m)
		{
			bool flag = base.Instrument.Kind == Jihas.LH780U;
			if (flag)
			{
				new LH780MessageHandler(this).ParseUni(m);
			}
			else
			{
				bool flag2 = m.Contains('\u0016');
				if (flag2)
				{
					bool flag3 = AstmManager.LowState == LowState.Idle;
					if (flag3)
					{
						this._il.SendLow(22);
						base.PutInState(LowState.Receive, -1);
					}
					else
					{
						bool flag4 = AstmManager.LowState == LowState.Send;
						if (flag4)
						{
							AstmManager._currentMessage.Clear();
							this._il.SendLow(22);
							base.PutInState(LowState.Receive, -1);
						}
						else
						{
							string msg = string.Copy(AstmManager._msgReceived);
							AstmManager._msgReceived = "";
							base.PutInState(LowState.Idle, -1);
							this._il.SendLow(6);
							new LH780MessageHandler(this).Parse(msg);
						}
					}
				}
				else
				{
					bool flag5 = AstmManager.LowState == LowState.Send;
					if (flag5)
					{
						bool flag6 = m.Contains('\u0005');
						if (flag6)
						{
							string text = AstmManager._currentMessage.First<string>();
							bool flag7 = text == '\u0005'.ToString();
							if (flag7)
							{
								this.HandleAck();
							}
							else
							{
								this._il.SendLow(text, Coding.Asc);
							}
						}
						else
						{
							bool flag8 = m.Contains('\u0010') && m.Contains('\u0006');
							if (flag8)
							{
								this.HandleAck();
								base.RemoveCurrent();
								this.PutInIdleState(500, 0);
							}
							else
							{
								bool flag9 = m.Contains('\u0006');
								if (flag9)
								{
									this.HandleAck();
								}
								else
								{
									bool flag10 = m.Contains('\u0015');
									if (flag10)
									{
										this.HandleNak();
									}
									else
									{
										bool flag11 = m.Contains('\u0010');
										if (flag11)
										{
											base.RemoveCurrent();
											this.PutInIdleState(500, 0);
										}
										else
										{
											this.HandleNak();
										}
									}
								}
							}
						}
					}
					else
					{
						bool flag12 = AstmManager.LowState == LowState.Receive;
						if (flag12)
						{
							this.HandleText(m);
						}
					}
				}
			}
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
					this._il.SendLow(AstmManager._currentMessage.First<string>(), Coding.Asc);
				}
			}
		}

		protected override void HandleNak()
		{
			this._nakNumber++;
			bool flag = this._nakNumber < 3;
			if (flag)
			{
				this._il.SendLow(AstmManager._currentMessage.First<string>(), Coding.Asc);
			}
			else
			{
				AstmManager._currentMessage.Clear();
				base.CancelCurrent(LabMessageState.Annulé);
				this.PutInIdleState(500, 0);
			}
		}

		protected override void PutInIdleState(int t, int s = 0)
		{
			base.PutInState(LowState.Idle, -1);
			Thread.Sleep(t);
			this.SendAstmMsg(0);
		}

		protected override void Compose(string data, int max = 1024)
		{
			AstmManager._currentMessage.Clear();
			data = data.Replace('\n'.ToString(), "\r\n");
			data = ("\u0001" + data).PadRight(256, '\0');
			string str = Crc.calc_crc(data);
			string item = "\u000201" + data + str + "\u0003";
			AstmManager._currentMessage.Add('\u0005'.ToString());
			AstmManager._currentMessage.Add("01");
			AstmManager._currentMessage.Add(item);
			AstmManager._currentMessage.Add('\u0005'.ToString());
		}

		protected override void HandleText(string m)
		{
			bool flag = m.Contains('\u0003');
			if (flag)
			{
				this.Ack(m);
			}
			else
			{
				bool flag2 = m.Length == 2;
				if (flag2)
				{
					this._il.SendLow(6);
				}
				else
				{
					this.LogFile.Debug("message non connu: " + m);
				}
			}
		}

		protected override void Ack(string msg)
		{
			string str = Tu.CleanMsgLH780(msg, this.LogFile);
			this._il.SendLow(6);
			AstmManager._msgReceived += str;
		}
	}
}
