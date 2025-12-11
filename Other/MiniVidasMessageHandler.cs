using System;
using GbService.ASTM;
using GbService.Communication.Common;
using GbService.Model.Domain;
using NLog;

namespace GbService.Other
{
	public class MiniVidasMessageHandler
	{
		public MiniVidasMessageHandler(Instrument instrument)
		{
			this._instrument = instrument;
		}

		public void Basic(string msg, ILowManager il)
		{
			il.SendLow(6);
			bool flag = msg == '\u0005'.ToString();
			if (!flag)
			{
				try
				{
					bool flag2 = string.IsNullOrEmpty(msg) || msg.Contains('\u0004'.ToString());
					if (!flag2)
					{
						VidasManager.HandleMsg(msg, this._instrument);
					}
				}
				catch (Exception ex)
				{
					MiniVidasMessageHandler._logger.Error(new LogMessageGenerator(ex.ToString));
				}
			}
		}

		private static Logger _logger = LogManager.GetCurrentClassLogger();

		private Instrument _instrument;
	}
}
