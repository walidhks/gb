using System;
using System.Linq;
using GbService.Communication.Common;
using GbService.Model.Domain;
using GbService.Other;

namespace GbService.ASTM
{
	public class BeckmanCX9Manager : AstmManager
	{
		public BeckmanCX9Manager(ILowManager il, Instrument instrument) : base(il, instrument)
		{
		}

		protected override void HandleMessage(string m)
		{
			bool flag = m == "\u0004\u0001";
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
					bool flag3 = m.Contains('\u0006') || m.Contains('\u0003');
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
							new BeckmanCX9Handler(this).Down(m2, base.Instrument);
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
			this._ackNumber++;
			AstmManager._msgReceived += m;
			//this._il.SendLow((this._ackNumber % 2 == 0) ? 6 : 3);

            byte control = (_ackNumber % 2 == 0)
			   ? (byte)6    // ACK
			   : (byte)3;

            _il.SendLow(control);
        }

		protected override void Compose(string message, int max = 240)
		{
			AstmManager._currentMessage.Clear();
			AstmManager._currentMessage.Add("\u0004\u0001");
			string[] array = message.Split(new char[]
			{
				'\r'
			}, StringSplitOptions.RemoveEmptyEntries);
			foreach (string text in array)
			{
				AstmManager._currentMessage.Add(text + Crc.BeckmanCx(text) + "\r\n");
			}
			AstmManager._currentMessage.Add('\u0004'.ToString());
		}
	}
}
