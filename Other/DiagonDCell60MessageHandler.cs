using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using GbService.ASTM;
using GbService.Communication;
using GbService.Model.Contexts;
using GbService.Model.Domain;
using NLog;

namespace GbService.Other
{
	public class DiagonDCell60MessageHandler
	{
		public DiagonDCell60MessageHandler(Instrument instrument)
		{
			this._instrument = instrument;
		}

		public static void Parse(Instrument instrument, string msg)
		{
			try
			{
				LaboContext laboContext = new LaboContext();
				List<AnalysisTypeInstrumentMapping> list = (from m in laboContext.AnalysisTypeInstrumentMappings
				where m.InstrumentCode == instrument.InstrumentCode && m.AnalysisTypeCode != null
				select m).ToList<AnalysisTypeInstrumentMapping>();
				bool flag = list.Any((AnalysisTypeInstrumentMapping x) => x.AnalysisTypeCode.Contains('-'));
				if (flag)
				{
					list.ForEach(delegate(AnalysisTypeInstrumentMapping x)
					{
						x.AnalysisTypeCode = x.AnalysisTypeCode.Replace('-', '.');
					});
					laboContext.SaveChanges();
				}
				JihazResult jihazResult;
				if (instrument.Mode != 0)
				{
					jihazResult = DiagonDCell60MessageHandler.GetResultDcellPro(instrument, msg, from x in list
					select x.AnalysisTypeCode);
				}
				else
				{
					jihazResult = DiagonDCell60MessageHandler.GetResultDcell(instrument, msg);
				}
				JihazResult jr = jihazResult;
				AstmHigh.LoadResults(jr, instrument, null);
			}
			catch (Exception ex)
			{
				DiagonDCell60MessageHandler._logger.Error(new LogMessageGenerator(ex.ToString));
			}
		}

		public static JihazResult GetResultDcellPro(Instrument instrument, string msg, IEnumerable<string> maps)
		{
			Inf inf = Inf.Get(instrument);
			bool flag = inf == null;
			JihazResult result;
			if (flag)
			{
				result = null;
			}
			else
			{
				bool flag2 = instrument.Kind == Jihas.GenusKT6400;
				if (flag2)
				{
					msg = msg.Substring(msg.IndexOf("@a", StringComparison.Ordinal));
				}
				string text = msg.Substring(inf.I, inf.J);
				DiagonDCell60MessageHandler._logger.Info("code = " + text);
				JihazResult jihazResult = new JihazResult(text);
				string text2 = msg.Substring(inf.K);
				bool flag3 = instrument.Kind == Jihas.GenusKT6400;
				if (flag3)
				{
					text2 = text2.Replace("#", "").Replace("h", "").Replace("l", "");
				}
				List<Mapping> list = TextUtil.Mappings(maps, '.');
				DiagonDCell60MessageHandler._logger.Info(string.Format(" Count maps {0}", list.Count));
				int num = (instrument.Kind == Jihas.SfriH18) ? 1 : 0;
				foreach (Mapping mapping in list)
				{
					int valueOrDefault = mapping.P2.GetValueOrDefault();
					int num2 = mapping.P1 + valueOrDefault;
					string text3 = text2.Substring(mapping.Order, num2);
					DiagonDCell60MessageHandler._logger.Info(string.Format("value {0} code {1} i={2} valueLength= {3}", new object[]
					{
						text3,
						mapping.Code,
						mapping.Order,
						num2
					}));
					double? num3 = TextUtil.Double(text3);
					bool flag4 = num3 == null;
					if (!flag4)
					{
						double num4 = num3.Value / Math.Pow(10.0, (double)valueOrDefault);
						jihazResult.Results.Add(new LowResult(mapping.Code, num4.ToString(new CultureInfo("en-US")), null, null, null));
					}
				}
				result = jihazResult;
			}
			return result;
		}

		public static JihazResult GetResultDcell(Instrument instrument, string msg)
		{
			Inf inf = Inf.Get(instrument);
			bool flag = inf == null;
			JihazResult result;
			if (flag)
			{
				result = null;
			}
			else
			{
				bool flag2 = instrument.Kind == Jihas.GenusKT6400;
				if (flag2)
				{
					msg = msg.Substring(msg.IndexOf("@a", StringComparison.Ordinal));
				}
				string text = msg.Substring(inf.I, inf.J);
				DiagonDCell60MessageHandler._logger.Info("code = " + text);
				JihazResult jihazResult = new JihazResult(text);
				string text2 = msg.Substring(inf.K);
				bool flag3 = instrument.Kind == Jihas.GenusKT6400;
				if (flag3)
				{
					text2 = text2.Replace("#", "").Replace("h", "").Replace("l", "");
				}
				LaboContext laboContext = new LaboContext();
				List<AnalysisTypeInstrumentMapping> list = (from m in laboContext.AnalysisTypeInstrumentMappings
				where m.InstrumentCode == instrument.InstrumentCode && m.AnalysisTypeCode != null
				select m).ToList<AnalysisTypeInstrumentMapping>();
				bool flag4 = list.Any((AnalysisTypeInstrumentMapping x) => x.AnalysisTypeCode.Contains('-'));
				if (flag4)
				{
					list.ForEach(delegate(AnalysisTypeInstrumentMapping x)
					{
						x.AnalysisTypeCode = x.AnalysisTypeCode.Replace('-', '.');
					});
					laboContext.SaveChanges();
				}
				List<Mapping> list2 = TextUtil.Mappings(from x in list
				select x.AnalysisTypeCode, '.');
				DiagonDCell60MessageHandler._logger.Info(string.Format(" Count maps {0}", list2.Count));
				int num = (instrument.Kind == Jihas.SfriH18) ? 1 : 0;
				int num2 = 0;
				foreach (Mapping mapping in list2)
				{
					int valueOrDefault = mapping.P2.GetValueOrDefault();
					int num3 = mapping.P1 + valueOrDefault;
					string text3 = text2.Substring(num2, num3);
					DiagonDCell60MessageHandler._logger.Info(string.Format("value {0} code {1} i={2} valueLength= {3}", new object[]
					{
						text3,
						mapping.Code,
						num2,
						num3
					}));
					double? num4 = TextUtil.Double(text3);
					bool flag5 = num4 == null;
					if (!flag5)
					{
						double num5 = num4.Value / Math.Pow(10.0, (double)valueOrDefault);
						num2 += num3 + num + mapping.P3.GetValueOrDefault();
						jihazResult.Results.Add(new LowResult(mapping.Code, num5.ToString(new CultureInfo("en-US")), null, null, null));
					}
				}
				result = jihazResult;
			}
			return result;
		}

		private static Logger _logger = LogManager.GetCurrentClassLogger();

		private readonly Instrument _instrument;
	}
}
