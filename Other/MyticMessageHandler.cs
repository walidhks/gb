using System;
using System.Collections.Generic;
using System.Linq;
using GbService.ASTM;
using GbService.Communication;
using GbService.Model.Domain;
using NLog;

namespace GbService.Other
{
	public class MyticMessageHandler
	{
		public MyticMessageHandler(Instrument instrument)
		{
			this._instrument = instrument;
		}

		public void Parse(string msg)
		{
			try
			{
				MyticMessageHandler._logger.Debug("--msg : " + msg);
				string[] source = msg.Split(new string[]
				{
					"\r\n",
					"\r"
				}, StringSplitOptions.RemoveEmptyEntries);
				List<string[]> records = (from s in source
				select s.Split(new char[]
				{
					';'
				})).ToList<string[]>();
				string valueById = this.GetValueById(records, "SID");
				this.LoadOk(records, valueById);
			}
			catch (Exception ex)
			{
				MyticMessageHandler._logger.Error(new LogMessageGenerator(ex.ToString));
			}
		}

		public void LoadOk(List<string[]> records, string id)
		{
			JihazResult result = TextUtil.GetResult(records, id, 1);
			AstmHigh.LoadResults(result, this._instrument, null);
		}

		private string GetValueById(List<string[]> records, string id)
		{
			string[] array = records.FirstOrDefault((string[] x) => x.First<string>() == id);
			return (array != null) ? array[1] : null;
		}

		private static Logger _logger = LogManager.GetCurrentClassLogger();

		private Instrument _instrument;
	}
}
