using System;
using System.Collections.Generic;
using System.Linq;
using GbService.ASTM;
using GbService.Communication;
using GbService.Communication.Common;
using GbService.Model.Domain;
using NLog;

namespace GbService.Other
{
	public class HumaCount5Handler
	{
		public static void Parse5(string msg, Instrument instrument)
		{
			try
			{
				HumaCount5Handler._logger.Debug("--msg : " + msg);
				string[] source = msg.Split(new char[]
				{
					'\n'
				});
				List<string[]> records = (from s in source
				select s.Split(new char[]
				{
					'\t'
				})).ToList<string[]>();
				string valueById = HumaCount5Handler.GetValueById(records, "SID", 1);
				string valueById2 = HumaCount5Handler.GetValueById(records, "DATE", 1);
				HumaCount5Handler.LoadOk(records, valueById, 1, instrument);
			}
			catch (Exception ex)
			{
				HumaCount5Handler._logger.Error(new LogMessageGenerator(ex.ToString));
			}
		}

		public static void Parse60(string msg, Instrument instrument)
		{
			try
			{
				HumaCount5Handler._logger.Debug("--msg : " + msg);
				string[] source = msg.Split(new string[]
				{
					Tu.NL
				}, StringSplitOptions.RemoveEmptyEntries);
				List<string[]> records = (from s in source
				select s.Split(new char[]
				{
					'\t'
				})).ToList<string[]>();
                string valueById = HumaCount5Handler.GetValueById(records, "Sample ID:", 1);
				HumaCount5Handler.LoadOk(records, valueById, 2, instrument);
               /* string valueById = HumaCount5Handler.GetValueById(records, "Sample ID.:", 1);*/

                // Fallback: If Serial No is empty, try Sample ID (optional)
               /* if (string.IsNullOrEmpty(valueById))
                {
                    valueById = HumaCount5Handler.GetValueById(records, "Sample ID:", 1);
                }*/
            }
			catch (Exception ex)
			{
				HumaCount5Handler._logger.Error(new LogMessageGenerator(ex.ToString));
			}
		}

		public static void ParseGh900(string m, Instrument instrument)
		{
			try
			{
				HumaCount5Handler._logger.Trace("m=" + m);
				m = m.Substring(m.IndexOf('\u0002') + 1);
				int valueOrDefault = TextUtil.GetInt(m.Substring(7, 2)).GetValueOrDefault();
				bool flag = valueOrDefault == 0;
				if (flag)
				{
					HumaCount5Handler._logger.Info("error lenght sid");
				}
				else
				{
					string sid = m.Substring(9, valueOrDefault);
					List<int> list = TextUtil.SplitInt(instrument.S3);
					Inf inf = (list != null) ? new Inf(list[0], list[1], 0) : new Inf(98, 5, 0);
					string value = m.Substring(inf.I + valueOrDefault, inf.J);
					ProHandler.LoadOk(sid, value, instrument);
				}
			}
			catch (Exception ex)
			{
				HumaCount5Handler._logger.Error(new LogMessageGenerator(ex.ToString));
			}
		}

		public static void LoadOk(List<string[]> records, string id, int index, Instrument instrument)
		{
			JihazResult result = TextUtil.GetResult(records, id, index);
			AstmHigh.LoadResults(result, instrument, null);
		}

		private static string GetValueById(List<string[]> records, string id, int index = 1)
		{
			string[] array = records.FirstOrDefault((string[] x) => x.First<string>() == id);
			return (array != null) ? array[index] : null;
		}

		private static Logger _logger = LogManager.GetCurrentClassLogger();
	}
}
