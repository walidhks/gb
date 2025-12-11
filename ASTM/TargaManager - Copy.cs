using System;
using System.Linq;
using GbService.Communication;
using GbService.Communication.Common;
using GbService.Model.Domain;
using GbService.Other;

namespace GbService.ASTM
{
	public class TargaManager : AstmManager
	{
		public TargaManager(ILowManager il, Instrument instrument) : base(il, instrument)
		{
		}

		protected override void HandleMessage(string m)
		{
			bool flag = m.Contains('\u0006');
			if (flag)
			{
				this.HandleAck();
			}
			else
			{
				bool flag2 = m.Length == 2;
				if (flag2)
				{
					bool flag3 = m.StartsWith("Y");
					if (flag3)
					{
						base.CancelCurrent(LabMessageState.Ok);
					}
					else
					{
						bool flag4 = m.StartsWith("N");
						if (flag4)
						{
							base.CancelCurrent(LabMessageState.Annulé);
						}
					}
					this.PutInIdleState(100, 1);
				}
				else
				{
					bool flag5 = m.Contains('\u0015');
					if (flag5)
					{
						base.PutInState(LowState.Idle, -1);
					}
					else
					{
						this.HandleText(m);
					}
				}
			}
		}

		public void Download()
		{
			this.LogFile.Debug("d = " + AstmManager.LowState.ToString());
			bool flag = AstmManager.LowState == LowState.Receive && base.TimeState.AddSeconds(70.0) < DateTime.Now;
			if (flag)
			{
				AstmManager.LowState = LowState.Idle;
			}
			bool flag2 = AstmManager.LowState > LowState.Idle;
			if (!flag2)
			{
				base.PutInState(LowState.Receive, -1);
				this._il.SendLow(2);
			}
		}

		protected override void HandleText(string m)
		{
			bool flag = !m.EndsWith('\u0004'.ToString());
			if (!flag)
			{
				TargaHandler.Handle(m);
				this._il.SendLow(2);
			}
		}

		protected override void HandleAck()
		{
			bool flag = AstmManager.LowState == LowState.Send;
			if (flag)
			{
				bool flag2 = AstmManager._currentMessage.Count < 2;
				if (!flag2)
				{
					this._il.SendLow(AstmManager._currentMessage[1], Coding.Asc);
				}
			}
			else
			{
				this._il.SendLow("R\u0004", Coding.Asc);
			}
		}

		protected override void Compose(string message, int max = 240)
		{
			AstmManager._currentMessage.Clear();
			AstmManager._currentMessage.Add('\u0002'.ToString());
			AstmManager._currentMessage.Add(message);
		}
	}
}
