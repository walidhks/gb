using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using GbService.Communication;
using GbService.Communication.Common;
using GbService.Model.Contexts;
using GbService.Model.Domain;
using NLog;

namespace GbService.Other
{
	public class Advia560Handler
	{
		public Advia560Handler(Instrument instrument)
		{
			this._instrument = instrument;
		}

		public void Parse(string msg)
		{
			try
			{
				int num = msg.IndexOf('\u0001');
				int num2 = msg.IndexOf('\u0004', num);
				string text = msg.Substring(num, num2 - num);
				Advia560Handler._logger.Debug("--msg : " + text);
				string[] source = text.Split(new string[]
				{
					Tu.NL
				}, StringSplitOptions.RemoveEmptyEntries);
				List<string[]> records = (from s in source
				select s.Split(new char[]
				{
					'\t'
				})).ToList<string[]>();
				string valueById = this.GetValueById(records, "Patient ID:", 1);
				string valueById2 = this.GetValueById(records, "Test date(ymd):", 1);
				this.LoadResults(records, valueById, valueById2);
			}
			catch (Exception ex)
			{
				Advia560Handler._logger.Error(new LogMessageGenerator(ex.ToString));
			}
		}

		public void LoadResults(List<string[]> records, string id, string analysisDate)
		{
			LaboContext laboContext = new LaboContext();
			Advia560Handler._logger.Info<string, string>("looking for sample {0} for the date {1}", id, analysisDate);
			Sample sample = TextUtil.Sample(laboContext, id, this._instrument);
			bool flag = sample == null;
			if (!flag)
			{
				Advia560Handler._logger.Info<long?>("Sample {0} found", sample.SampleCode);
				List<AnalysisTypeInstrumentMapping> list = (from m in laboContext.AnalysisTypeInstrumentMappings
				where m.InstrumentCode == this._instrument.InstrumentCode
				select m).ToList<AnalysisTypeInstrumentMapping>();
				using (List<AnalysisTypeInstrumentMapping>.Enumerator enumerator = list.GetEnumerator())
				{
					while (enumerator.MoveNext())
					{
						AnalysisTypeInstrumentMapping map = enumerator.Current;
						Analysis analysis = sample.Analysis.FirstOrDefault((Analysis x) => x.AnalysisTypeId == map.AnalysisTypeId && x.AnalysisState <= AnalysisState.EnvoyerAutomate);
						string valueById = this.GetValueById(records, map.AnalysisTypeCode, 2);
						bool flag2 = analysis != null;
						if (flag2)
						{
							Advia560Handler._logger.Info("value  {0}", valueById);
							bool flag3 = !string.IsNullOrWhiteSpace(valueById);
							if (flag3)
							{
								analysis.ResultTxt = valueById.ToString(new CultureInfo("en-US"));
								decimal num;
								bool flag4 = decimal.TryParse(valueById, NumberStyles.Any, new CultureInfo("en-US"), out num);
								if (flag4)
								{
									analysis.AnalysisState = AnalysisState.ReçuAutomate;
								}
							}
							analysis.InstrumentId = new int?(this._instrument.InstrumentId);
							laboContext.SaveChanges();
						}
						else
						{
							Advia560Handler._logger.Info<string, string>("analysis {0} not found {1}", map.AnalysisTypeCode, valueById);
						}
					}
				}
				Advia560Handler._logger.Info("Results loaded successfuly!");
			}
		}

		private string GetValueById(List<string[]> records, string id, int i = 2)
		{
			string[] array = records.FirstOrDefault((string[] x) => x.First<string>() == id);
			return (array != null) ? array[i] : null;
		}

		private static Logger _logger = LogManager.GetCurrentClassLogger();

		private Instrument _instrument;

		public long PatientId;

		public long SampleCode;

		public string AnalysisDate;
	}
}
