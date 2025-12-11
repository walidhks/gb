using System;
using System.Collections.Generic;
using System.Linq;
using GbService.Communication;
using GbService.Communication.Common;
using GbService.Model.Domain;
using GbService.Other;
using NLog;

namespace GbService.ASTM
{
	public class VidasManager : AstmManager
	{
		public VidasManager(ILowManager il, Instrument instrument) : base(il, instrument)
		{
		}

		protected override void HandleMessage(string m)
		{
			bool flag = m == '\u0005'.ToString();
			if (flag)
			{
				this._il.SendLow(6);
			}
			else
			{
				bool flag2 = m.Contains('\u0006');
				if (flag2)
				{
					this.HandleAck();
				}
				else
				{
					bool flag3 = m.Contains('\u0015');
					if (flag3)
					{
						this.HandleNak();
					}
					else
					{
						this.HandleText(m);
					}
				}
			}
		}

		protected override void HandleText(string m)
		{
			bool flag = m.Contains('\u0004'.ToString());
			if (!flag)
			{
				bool flag2 = m.Contains('\u001d');
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

		protected override void Compose(string message, int max = 240)
		{
			AstmManager._currentMessage.Clear();
			message = message.Replace("<CR>", '\r'.ToString());
			string[] array = message.Split(new char[]
			{
				'\r'
			}, StringSplitOptions.RemoveEmptyEntries);
			AstmManager._currentMessage.Add('\u0005'.ToString());
			foreach (string message2 in array)
			{
				AstmManager._currentMessage.Add(VidasManager.ComposeTest(message2));
			}
			AstmManager._currentMessage.Add('\u0004'.ToString());
		}

		private static string ComposeTest(string message)
		{
			List<string> source = TextUtil.ChunksUpto(message, 80).ToList<string>();
			List<string> values = (from chank in source
			select "\u001e" + chank).ToList<string>();
			string text = string.Join("", values);
			return "\u0002" + text + VidasManager.Item(text + "\u001d") + "\u0003";
		}

		protected override void Ack(string msg)
		{
			try
			{
				this._il.SendLow(6);
				VidasManager.HandleMsg(msg, base.Instrument);
			}
			catch (Exception ex)
			{
				this.LogFile.Error(new LogMessageGenerator(ex.ToString));
			}
		}

		public static void HandleMsg(string msg, Instrument instrument)
		{
			string text = msg.Remove(msg.IndexOf('\u001d'));
			text = text.Substring(text.IndexOf('\u001e'));
			text = text.Replace('\u001e'.ToString(), "");
			bool flag = instrument.Kind == Jihas.Vitek;
			if (flag)
			{
				VidasHandler.ParseVitek(text, instrument);
			}
			else
			{
				VidasHandler.Parse(text, instrument);
			}
		}

		private static string Item(string m)
		{
			return "\u001d" + Crc.Vidas(m);
		}

		private static Logger _logger = LogManager.GetCurrentClassLogger();
	}
}
