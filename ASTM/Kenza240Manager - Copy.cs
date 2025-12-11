using System;
using System.Linq;
using GbService.Communication.Common;
using GbService.Model.Domain;
using GbService.Other;

namespace GbService.ASTM
{
	public class Kenza240Manager : AstmManager
	{
		public Kenza240Manager(ILowManager il, Instrument instrument) : base(il, instrument)
		{
		}

		protected override void HandleMessage(string m)
		{
			bool flag = m == '\u0005'.ToString();
			if (flag)
			{
				AstmManager._currentMessage.Clear();
				AstmManager._msgReceived = "";
				base.PutInReceiveState();
			}
			else
			{
				bool flag2 = AstmManager.LowState == LowState.Send;
				if (flag2)
				{
					bool flag3 = m.Contains('\u0006');
					if (flag3)
					{
						this.HandleAck();
					}
					else
					{
						bool flag4 = m.Contains('\u0015');
						if (flag4)
						{
							this.HandleNak();
						}
					}
				}
				else
				{
					bool flag5 = AstmManager.LowState == LowState.Receive;
					if (flag5)
					{
						bool flag6 = m.Contains('\u0004');
						if (flag6)
						{
							string m2 = string.Copy(AstmManager._msgReceived);
							AstmManager._msgReceived = "";
							base.PutInState(LowState.Idle, -1);
							Kenza240Handler.Down(m2, base.Instrument);
						}
						else
						{
							this.HandleText(m);
						}
					}
				}
			}
		}

		protected override void HandleText(string m)
		{
			AstmManager._msgReceived = AstmManager._msgReceived + m.Substring(1).Substring(0, m.Length - 4) + "\r";
			this._il.SendLow(6);
		}

		protected override void Compose(string message, int max = 240)
		{
			AstmManager._currentMessage.Clear();
			AstmManager._currentMessage.Add('\u0005'.ToString());
			string[] array = message.Split(new string[]
			{
				'\r'.ToString()
			}, StringSplitOptions.RemoveEmptyEntries);
			foreach (string text in array)
			{
				AstmManager._currentMessage.Add("\u0002" + text + Crc.Kenza(text) + "\u0003");
			}
			AstmManager._currentMessage.Add('\u0004'.ToString());
		}
	}
}
