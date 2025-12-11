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
	public class BeckmanCX9Handler
	{
		public BeckmanCX9Handler(BeckmanCX9Manager manager)
		{
			this._manager = manager;
			this._instrument = manager.Instrument;
		}

		public BeckmanCX9Handler(Instrument instrument)
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
				bool flag = list.Count > 0;
				if (flag)
				{
					this.OrderSamples(list);
				}
			}
			catch (Exception ex)
			{
				BeckmanCX9Handler._logger.Error(new LogMessageGenerator(ex.ToString));
			}
		}

		public void OrderSamples(List<Sample> samples)
		{
			string text = "";
			BeckmanCX9Handler._logger.Info("samples.Count : " + samples.Count.ToString());
			foreach (Sample sample in samples)
			{
				bool flag = sample == null;
				if (!flag)
				{
					Logger logger = BeckmanCX9Handler._logger;
					long? sampleCode = sample.SampleCode;
					logger.Info(sampleCode.ToString() + " " + sample.SampleId.ToString());
					List<string> tests = AstmHigh.GetTests(sample.SampleId, this._instrument, false);
					bool flag2 = tests == null || tests.Count == 0;
					if (!flag2)
					{
						Patient patient = sample.AnalysisRequest.Patient;
						sampleCode = sample.SampleCode;
						string text2 = (sampleCode != null) ? sampleCode.GetValueOrDefault().ToString("D10").PadExact(11) : null;
						string text3 = patient.Nom.PadExact(18);
						string text4 = patient.Prenom.PadExact(15);
						List<string> list = (from x in tests
						where x.Length < 5
						select x).ToList<string>();
						bool flag3 = tests.Count != list.Count;
						if (flag3)
						{
							BeckmanCX9Handler._logger.Error("Mapping > 4");
						}
                        DateTime? birthDate = patient.PatientDateNaiss;
                        string text5 = (birthDate?.ToString("ddMMyy") ?? "").PadRight(6);
                        string text6 = TextUtil.GetDate(sample, this._instrument).ToString("ddMMyy,HHmm");
						string text7 = list.Aggregate("", (string c, string x) => c + "," + x.PadExact(4) + ",0");
						string text8 = list.Count.ToString("D3");
						string text9 = patient.PatientID.ToString().PadExact(12);
						text = string.Concat(new string[]
						{
							text,
							"[0 ,701,01,00,00,1,ST,SE,",
							text2,
							",                    ,                         ,                         ,",
							text3,
							",",
							text4,
							",",
							string.Format(" ,{0},                  ,{1},                    ,   , ,{2},{3},                         ,       ,    ,    ,      ,{4}{5}]{6}", new object[]
							{
								text9,
								text6,
								text5,
								patient.ShortSexe,
								text8,
								text7,
								'\r'
							})
						});
					}
				}
			}
			this._manager.PutMsgInSendingQueue(text, 0L);
		}

		public void Down(string m, Instrument instrument)
		{
			BeckmanCX9Handler._logger.Debug("m = " + m);
			string[] array = m.Split(new string[]
			{
				Tu.NL
			}, StringSplitOptions.RemoveEmptyEntries);
			foreach (string text in array)
			{
				BeckmanCX9Handler._logger.Debug("x = " + text);
				this.Handle(text, instrument);
			}
		}

		public void Handle(string m, Instrument instrument)
		{
			m = m.Substring(1);
			m = m.Substring(0, m.IndexOf("]"));
			string[] array = m.Split(",");
			bool flag = array[1] == "702" && array[2] == "03";
			if (flag)
			{
				int numberPositionBarcode = ParamDictHelper.NumberPositionBarcode;
				string text = array[9].Trim();
				BeckmanCX9Handler._logger.Info("code = " + text);
				JihazResult jihazResult = new JihazResult(text);
				string text2 = array[10].Trim();
				string text3 = array[15].Trim();
				BeckmanCX9Handler._logger.Info("a = " + text2 + "b = " + text3);
				jihazResult.Results.Add(new LowResult(text2, text3, null, null, null));
				AstmHigh.LoadResults(jihazResult, instrument, null);
			}
			else
			{
                bool flag2 = array[1] == "701" && array[2] == "06";
                if (flag2)
                {
                    List<string> range = array.ToList<string>().GetRange(3, array.Length - 3);
                    using (LaboContext laboContext = new LaboContext())
                    {
                        List<Sample> samples = new List<Sample>();

                        foreach (string s in range)
                        {
                            if (long.TryParse(s, out long code))
                            {
                                Sample sample = laboContext.Sample
                                    .SingleOrDefault(x => x.SampleCode == (long?)code);

                                if (sample != null)
                                {
                                    samples.Add(sample);
                                }
                            }
                        }

                        this.OrderSamples(samples);
                    }
                }
            }
		}

		private static Logger _logger = LogManager.GetCurrentClassLogger();

		private AstmManager _manager;

		private Instrument _instrument;
	}
}
