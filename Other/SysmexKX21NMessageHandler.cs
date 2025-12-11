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
	public class SysmexKX21NMessageHandler
	{
		public static void Parse(Instrument instrument, string msg)
		{
			try
			{
				bool flag = msg == null;
				if (!flag)
				{
					JihazResult result = SysmexKX21NMessageHandler.GetResult(instrument, msg);
					AstmHigh.LoadResults(result, instrument, null);
				}
			}
			catch (Exception ex)
			{
				SysmexKX21NMessageHandler._logger.Error(new LogMessageGenerator(ex.ToString));
			}
		}

		public static JihazResult GetResult(Instrument instrument, string msg)
		{
			bool flag = instrument.Mode == 0;
			int startIndex;
			int length;
			int startIndex2;
			if (flag)
			{
				startIndex = 16;
				length = 12;
				startIndex2 = 35;
			}
			else
			{
				startIndex = 17;
				length = 6;
				startIndex2 = 30;
			}
			string text = msg.Substring(startIndex, length);
			SysmexKX21NMessageHandler._logger.Info("code = " + text);
			JihazResult jihazResult = new JihazResult(text);
			string text2 = msg.Substring(startIndex2);
			LaboContext laboContext = new LaboContext();
			List<AnalysisTypeInstrumentMapping> source = (from m in laboContext.AnalysisTypeInstrumentMappings
			where m.InstrumentCode == instrument.InstrumentCode && m.AnalysisTypeCode != null
			select m).ToList<AnalysisTypeInstrumentMapping>();
			List<Mapping> list = TextUtil.Mappings(from x in source
			select x.AnalysisTypeCode, '.');
			foreach (Mapping mapping in list)
			{
				try
				{
					SysmexKX21NMessageHandler._logger.Info("AnalysisTypeCode =" + mapping.Code + "<cr>");
					int p = mapping.P1;
					int num = mapping.Order * 5 - 5;
					string text3 = text2.Substring(num, 5);
					double num2 = TextUtil.Double(text3).GetValueOrDefault() / Math.Pow(10.0, (double)p);
					SysmexKX21NMessageHandler._logger.Info("value string  {0} value double {1} code {2} i {3}", new object[]
					{
						text3,
						num2,
						mapping.Code,
						num
					});
					jihazResult.Results.Add(new LowResult(mapping.Code, num2.ToString(new CultureInfo("en-US")), null, null, null));
				}
				catch (Exception ex)
				{
					SysmexKX21NMessageHandler._logger.Error(new LogMessageGenerator(ex.ToString));
				}
			}
			return jihazResult;
		}

		private static Logger _logger = LogManager.GetCurrentClassLogger();
	}
}
