using System;
using System.Collections.Generic;
using System.Linq;
using GbService.ASTM;
using GbService.Common;
using GbService.Communication;
using GbService.Model.Contexts;
using GbService.Model.Domain;
using NLog;

namespace GbService.Other
{
	public class Kenza240Handler
	{
		public Kenza240Handler(Kenza240Manager manager)
		{
			this._manager = manager;
			this._instrument = manager.Instrument;
		}

		public Kenza240Handler(Instrument instrument)
		{
			this._instrument = instrument;
		}

		public void Upload()
		{
			try
			{
				LaboContext laboContext = new LaboContext();
				DateTime lastweek = DateTime.Now.Date.AddDays(-2.0);
				List<Analysis> source = (from x in laboContext.Analysis
				where (int)x.AnalysisState == 10 && x.CreatedDate > lastweek && x.InstrumentId == (int?)this._instrument.InstrumentId
				select x).ToList<Analysis>();
				IEnumerable<Analysis> source2 = from x in source
				where x.AnalysisType.AnalysisTypeInstrumentMappings.Any((AnalysisTypeInstrumentMapping y) => y.InstrumentCode == this._instrument.InstrumentCode && !string.IsNullOrWhiteSpace(y.AnalysisTypeCode))
				select x;
				List<Sample> list = (from y in source2
				select y.Sample).Distinct<Sample>().ToList<Sample>();
				Kenza240Handler._logger.Info("samples.Count : " + list.Count.ToString());
				bool flag = list.Count > 0;
				if (flag)
				{
					this.OrderSamples(list);
				}
			}
			catch (Exception ex)
			{
				Kenza240Handler._logger.Error(new LogMessageGenerator(ex.ToString));
			}
		}

		public void OrderSamples(List<Sample> samples)
		{
			string text = "";
			foreach (Sample sample in samples)
			{
				bool flag = sample == null;
				if (!flag)
				{
					List<string> tests = AstmHigh.GetTests(sample.SampleId, this._instrument, false);
					bool flag2 = tests == null || tests.Count == 0;
					if (!flag2)
					{
						Patient patient = sample.AnalysisRequest.Patient;
						int num = (ParamDictHelper.NumberPositionBarcode <= 8) ? 8 : 9;
						long? num2;
						string text2 = sample.SampleCode?.ToString("D");
						string text3 = (patient.Nom + " " + patient.Prenom).PadExact(30);
						string text4 = (num == 8) ? "" : "Man".PadExact(32);
						List<string> list = (from x in tests
						where x.Length <= 3
						select x).ToList<string>();
						bool flag3 = tests.Count != list.Count;
						if (flag3)
						{
							Kenza240Handler._logger.Error("Mapping length > 3");
						}
						string text5 = list.Aggregate("", (string c, string i) => c + i.PadExact(3));
						text += string.Format("{0}{1}{2}{3}{4}", new object[]
						{
							text2,
							text3,
							text4,
							text5,
							'\r'
						});
					}
				}
			}
			this._manager.PutMsgInSendingQueue(text, 0L);
		}

		public static void Down(string m, Instrument instrument)
		{
			Kenza240Handler._logger.Debug("m = " + m);
			string[] array = m.Split(new char[]
			{
				'\r'
			}, StringSplitOptions.RemoveEmptyEntries);
			foreach (string text in array)
			{
				Kenza240Handler._logger.Debug("x = " + text);
				JihazResult result = Kenza240Handler.GetResult(text);
				bool flag = result != null;
				if (flag)
				{
					AstmHigh.LoadResults(result, instrument, null);
				}
			}
		}

		public static JihazResult GetResult(string m)
		{
			int num = (ParamDictHelper.NumberPositionBarcode <= 8) ? 8 : 9;
			string text = m.Substring(0, num);
			Kenza240Handler._logger.Info("code = " + text);
			JihazResult jihazResult = new JihazResult(text);
			List<string> list = TextUtil.Split(m.Substring((num == 8) ? 38 : 71), 12);
			foreach (string text2 in list)
			{
				string text3 = text2.Substring(0, 3).Trim();
				string text4 = text2.Substring(3);
				Kenza240Handler._logger.Info(text3 + " : " + text4);
				jihazResult.Results.Add(new LowResult(text3, text4, null, null, null));
			}
			return jihazResult;
		}

		private static Logger _logger = LogManager.GetCurrentClassLogger();

		private AstmManager _manager;

		private Instrument _instrument;
	}
}
