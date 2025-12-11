using System;
using System.Collections.Generic;
using GbService.ASTM;
using GbService.Communication;
using GbService.Model.Domain;
using NLog;

namespace GbService.Other
{
	public class CelltakHandler
	{
		public CelltakHandler(Instrument instrument)
		{
			this._instrument = instrument;
		}

		public void Parse(string msg)
		{
			try
			{
				CelltakHandler._logger.Debug("--msg : " + msg);
				bool flag = !msg.StartsWith('\u0002'.ToString()) || !msg.EndsWith('\u0003'.ToString());
				if (!flag)
				{
					msg = msg.Substring(1, msg.Length - 2);
					JihazResult result = CelltakHandler.GetResult(msg, this._instrument);
					AstmHigh.LoadResults(result, this._instrument, null);
				}
			}
			catch (Exception ex)
			{
				CelltakHandler._logger.Error(new LogMessageGenerator(ex.ToString));
			}
		}

		public static JihazResult GetResult(string msg, Instrument instrument)
		{
			bool flag = instrument.Mode == 0 || instrument.Mode == 2;
			JihazResult result;
			if (flag)
			{
				string[] records = msg.Split(new char[]
				{
					'\r'
				}, StringSplitOptions.RemoveEmptyEntries);
				result = CelltakHandler.GetResultCellTak(records, instrument);
			}
			else
			{
				bool flag2 = instrument.Mode == 1;
				if (flag2)
				{
					string id = msg.Substring(24, 15);
					List<string> records2 = TextUtil.Split(msg.Substring(145, 90), 5);
					result = CelltakHandler.GetResultCelly(id, records2, instrument);
				}
				else
				{
					result = null;
				}
			}
			return result;
		}

		public static JihazResult GetResultCellTak(string[] records, Instrument instrument)
		{
			string text = records[(instrument.Mode == 0) ? 1 : 2];
			CelltakHandler._logger.Info("id=" + text);
			JihazResult jihazResult = new JihazResult(text);
			for (int i = 2; i < records.Length; i++)
			{
				string text2 = i.ToString();
				string text3 = records[i];
				string text4 = (text3 != null) ? text3.Replace("L", " ").Replace("H", " ") : null;
				bool flag = text4 != null && text4.Contains("*");
				if (flag)
				{
					text4 = text4.Remove(text4.IndexOf("*"));
				}
				text4 = ((text4 != null) ? text4.Trim() : null);
				CelltakHandler._logger.Info(text2 + " : " + text4);
				jihazResult.Results.Add(new LowResult(text2, text4, null, null, null));
			}
			return jihazResult;
		}

		public static JihazResult GetResultCelly(string id, List<string> records, Instrument instrument)
		{
			CelltakHandler._logger.Info("id=" + id);
			JihazResult jihazResult = new JihazResult(id);
			for (int i = 0; i < records.Count; i++)
			{
				string text = (i + 1).ToString();
				string text2 = records[i];
				string text3 = (text2 != null) ? text2.Replace("L ", " ").Replace("H ", " ") : null;
				CelltakHandler._logger.Info(text + " : " + text3);
				jihazResult.Results.Add(new LowResult(text, text3, null, null, null));
			}
			return jihazResult;
		}

		private static Logger _logger = LogManager.GetCurrentClassLogger();

		private Instrument _instrument;
	}
}
