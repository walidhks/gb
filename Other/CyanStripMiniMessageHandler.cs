using System;
using System.Collections.Generic;
using System.Linq;
using GbService.Model.Contexts;
using GbService.Model.Domain;
using NLog;

namespace GbService.Other
{
	public class CyanStripMiniMessageHandler
	{
		public CyanStripMiniMessageHandler(Instrument instrument)
		{
			this._instrument = instrument;
		}

		public void Parse(string msg)
		{
			try
			{
				CyanStripMiniMessageHandler._logger.Debug("--msg : " + msg);
				bool flag = long.TryParse((msg != null) ? msg.Substring(6, 16) : null, out this.SampleCode);
				if (flag)
				{
					this.LoadResults((msg != null) ? msg.Substring(64, 10) : null);
				}
			}
			catch (Exception ex)
			{
				CyanStripMiniMessageHandler._logger.Error(new LogMessageGenerator(ex.ToString));
			}
		}

		public void LoadResults(string records)
		{
			LaboContext laboContext = new LaboContext();
			CyanStripMiniMessageHandler._logger.Info<long, string>("looking for sample {0} for the date {1}", this.SampleCode, this.AnalysisDate);
			DateTime d = DateTime.Now.AddDays(-30.0);
			long p = (long)Math.Pow(10.0, 6.0);
			Sample sample = laboContext.Sample.FirstOrDefault((Sample s) => s.SampleCode % (long?)p == (long?)this.SampleCode && s.DateCreated >= d);
			bool flag = sample == null;
			if (flag)
			{
				CyanStripMiniMessageHandler._logger.Info("Sample not found!");
			}
			CyanStripMiniMessageHandler._logger.Info<long?>("Sample {0} found", (sample != null) ? sample.SampleCode : null);
			List<AnalysisTypeInstrumentMapping> list = (from m in laboContext.AnalysisTypeInstrumentMappings
			where m.InstrumentCode == this._instrument.InstrumentCode
			select m).ToList<AnalysisTypeInstrumentMapping>();
			using (List<AnalysisTypeInstrumentMapping>.Enumerator enumerator = list.GetEnumerator())
			{
				while (enumerator.MoveNext())
				{
					AnalysisTypeInstrumentMapping codeMapping = enumerator.Current;
					string analysisTypeCode = codeMapping.AnalysisTypeCode;
					int num = int.Parse(analysisTypeCode);
					int num2 = int.Parse(records.Substring(num - 1, 1));
					CyanStripMiniMessageHandler._logger.Info<int, string>("value {0} code {1}", num2, codeMapping.AnalysisTypeCode);
					Analysis analysis = (sample != null) ? sample.Analysis.SingleOrDefault((Analysis x) => x.AnalysisTypeId == codeMapping.AnalysisTypeId && x.AnalysisState == AnalysisState.EnCours) : null;
					bool flag2 = analysis == null;
					if (!flag2)
					{
						analysis.ResultTxt = this._list[num - 1][num2];
						analysis.InstrumentId = new int?(this._instrument.InstrumentId);
						laboContext.SaveChanges();
					}
				}
			}
			CyanStripMiniMessageHandler._logger.Info("Results loaded successfuly!");
		}

		private static Logger _logger = LogManager.GetCurrentClassLogger();

		private Instrument _instrument;

		public long PatientId;

		public long SampleCode;

		public string AnalysisDate;

		private List<List<string>> _list = new List<List<string>>
		{
			new List<string>
			{
				"Norm",
				"1+",
				"2+",
				"3+"
			},
			new List<string>
			{
				"Neg",
				"+/-",
				"1+",
				"2+",
				"3+"
			},
			new List<string>
			{
				"Neg",
				"1+",
				"2+",
				"3+"
			},
			new List<string>
			{
				"Neg",
				"+/-",
				"1+",
				"2+",
				"3+"
			},
			new List<string>
			{
				"1.000",
				"1.005",
				"1.010",
				"1.015",
				"1.020",
				"1.025",
				"1.030"
			},
			new List<string>
			{
				"Neg",
				"+/-",
				"1+",
				"2+",
				"3+"
			},
			new List<string>
			{
				"5",
				"6",
				"6.5",
				"7",
				"8",
				"9"
			},
			new List<string>
			{
				"Neg",
				"+/-",
				"1+",
				"2+",
				"3+",
				"4+"
			},
			new List<string>
			{
				"Neg",
				"Pos"
			},
			new List<string>
			{
				"Neg",
				"1+",
				"2+",
				"3+"
			}
		};
	}
}
