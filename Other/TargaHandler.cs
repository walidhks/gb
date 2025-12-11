using System;
using System.Collections.Generic;
using System.Linq;
using GbService.ASTM;
using GbService.Common;
using GbService.Communication;
using GbService.Communication.Common;
using GbService.Model.Contexts;
using GbService.Model.Domain;
using NLog;

namespace GbService.Other
{
	public class TargaHandler
	{
		public TargaHandler(TargaManager manager)
		{
			this._manager = manager;
			TargaHandler._instrument = manager.Instrument;
		}

		public void Upload()
		{
			try
			{
				LaboContext laboContext = new LaboContext();
				DateTime lastweek = DateTime.Now.Date.AddDays(-7.0);
				List<Analysis> source = (from x in laboContext.Analysis
				where (int)x.AnalysisState == 10 && x.CreatedDate > lastweek && x.InstrumentId == (int?)TargaHandler._instrument.InstrumentId
				select x).ToList<Analysis>();
				IEnumerable<Analysis> source2 = from x in source
				where x.AnalysisType.AnalysisTypeInstrumentMappings.Any((AnalysisTypeInstrumentMapping y) => y.InstrumentCode == TargaHandler._instrument.InstrumentCode && !string.IsNullOrWhiteSpace(y.AnalysisTypeCode))
				select x;
				List<Sample> samples = (from y in source2
				select y.Sample).Distinct<Sample>().ToList<Sample>();
				this.OrderSamples(samples);
			}
			catch (Exception ex)
			{
				TargaHandler._logger.Error(new LogMessageGenerator(ex.ToString));
			}
		}

		public void OrderSamples(List<Sample> samples)
		{
			foreach (Sample sample in from x in samples
			orderby x.AnalysisRequestId
			select x)
			{
				this.EncodeSample(sample);
			}
		}

		private void EncodeSample(Sample sample)
		{
			bool flag = sample == null;
			if (!flag)
			{
				List<string> tests = AstmHigh.GetTests(sample.SampleId, TargaHandler._instrument, false);
				bool flag2 = tests == null || tests.Count == 0;
				if (!flag2)
				{
					Patient patient = sample.AnalysisRequest.Patient;
                    string text = sample.SampleCode?.ToString("D12");
                    string text2 = string.Format("select count(*) from LabMessage where LabMessageValue like '{0}%' and InstrumentId = {1}\r\n                                  and LabMessageDate >DATEADD(dd, -2,GETDATE()) and LabMessageStatus = 2", text, TargaHandler._instrument.InstrumentId);
					string text3 = (new LaboContext().Database.SqlQuery<int>(text2, new object[0]).First<int>() > 0) ? "Y" : "N";
					string text4 = patient.Nom.PadExact(15);
					string text5 = patient.Prenom.PadExact(15);
					List<string> list = (from x in tests
					where x.Length < 5
					select x).ToList<string>();
					bool flag3 = tests.Count != list.Count;
					if (flag3)
					{
						TargaHandler._logger.Error("Mapping > 4");
					}
					string text6 = list.Aggregate("", (string c, string i) => c + i.PadExact(4));
					string text7 = string.Format("{0}{1}{2}RS{3}00{4:D2}{5}", new object[]
					{
						text,
						text4,
						text5,
						text3,
						list.Count,
						text6
					});
					string msg = string.Format("{0}{1}{2}", text7, Crc.Targa(text7), '\u0004');
					this._manager.PutMsgInSendingQueue(msg, sample.SampleId);
				}
			}
		}

		public static void Handle(string m)
		{
			JihazResult result = TargaHandler.GetResult(m);
			AstmHigh.LoadResults(result, TargaHandler._instrument, null);
		}

		public static JihazResult GetResult(string m)
		{
			int num = 12;
			string text = m.Substring(0, num);
			TargaHandler._logger.Info(string.Format("length = {0}, code = ", num) + text);
			JihazResult jihazResult = new JihazResult(text);
			string str = m.Substring(num + 3, m.Length - num - 7);
			List<string> list = TextUtil.Split(str, 11);
			foreach (string text2 in list)
			{
				string text3 = text2.Substring(0, 4);
				string text4 = text2.Substring(4);
				TargaHandler._logger.Info(text3 + " : " + text4);
				jihazResult.Results.Add(new LowResult(text3, text4, null, null, null));
			}
			return jihazResult;
		}

		private static Logger _logger = LogManager.GetCurrentClassLogger();

		private AstmManager _manager;

		private static Instrument _instrument;
	}
}
