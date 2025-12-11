using System;
using System.Collections.Generic;
using System.Linq;
using GbService.Communication;
using GbService.Communication.Common;
using GbService.Model.Contexts;
using GbService.Model.Domain;
using GbService.Other;

namespace GbService.ASTM
{
	public class AU480Manager : AstmManager
	{
		public AU480Manager(ILowManager il, Instrument instrument) : base(il, instrument)
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
				bool flag2 = m.Contains('\u0015');
				if (flag2)
				{
					this.HandleNak();
				}
				else
				{
					this.HandleText(m);
				}
			}
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
				this.LogFile.Debug("message non connu: " + m);
			}
		}

		protected override void HandleAck()
		{
			this._nakNumber = 0;
			base.RemoveCurrent();
			bool flag = this.SendAstmMsg(0);
			if (!flag)
			{
				this._il.SendLow("\u0002SE\u0003", Coding.Asc);
			}
		}

		protected override void HandleNak()
		{
			this._nakNumber++;
			bool flag = this._nakNumber < 4;
			if (flag)
			{
				this.SendAstmMsg(0);
			}
			else
			{
				this.HandleAck();
			}
		}

		public override bool SendAstmMsg(int i = 0)
		{
			LaboContext laboContext = new LaboContext();
			LabMessage labMessage = laboContext.LabMessage.FirstOrDefault((LabMessage x) => x.InstrumentId == (int?)this.InstrumentId && (int)x.LabMessageStatus == 0 && x.LabMessageRetry < 2);
			bool flag = labMessage == null;
			bool result;
			if (flag)
			{
				result = false;
			}
			else
			{
				LabMessage labMessage2 = labMessage;
				int labMessageRetry = labMessage2.LabMessageRetry;
				labMessage2.LabMessageRetry = labMessageRetry + 1;
				laboContext.SaveChanges();
				this._currentId = labMessage.LabMessageID;
				this._il.SendLow("\u0002" + labMessage.LabMessageValue + "\u0003", Coding.Asc);
				result = true;
			}
			return result;
		}

		protected override void Ack(string msg1)
		{
			List<string> list = msg1.Split(new char[]
			{
				'\u0003'
			}, StringSplitOptions.RemoveEmptyEntries).ToList<string>();
			this._il.SendLow(6);
			foreach (string text in list)
			{
				this.HandleMsg(text.Substring(1));
			}
		}

		public void HandleMsg(string t)
		{
			AU480MessageHandler au480MessageHandler = new AU480MessageHandler(this);
			bool flag = t.StartsWith("R ");
			if (flag)
			{
				string text = au480MessageHandler.HandleRequest(t);
				bool flag2 = text != null;
				if (flag2)
				{
					this.PutMsgInSendingQueue(text, 0L);
				}
			}
			else
			{
				bool flag3 = t.StartsWith("D ");
				if (flag3)
				{
					au480MessageHandler.LoadOk(t);
				}
			}
		}
	}
}
