using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using GbService.ASTM;
using GbService.Communication;
using GbService.Model.Contexts;
using GbService.Model.Domain;
using NLog;

namespace GbService.Other
{
	public class KermitHigh
	{
		public KermitHigh(AstmManager manager)
		{
			this._manager = manager;
		}

		public void Parse(string msg)
		{
			try
			{
				KermitHigh._logger.Debug("--msg : " + msg);
				msg = msg.Replace("##", "#");
				string id = msg.Substring(25, 15);
				int length = KermitHigh.Length(msg);
				string[] items = msg.Substring(49, length).Split(new char[]
				{
					'}'
				}, StringSplitOptions.RemoveEmptyEntries);
				this.LoadOk(items, id);
			}
			catch (Exception ex)
			{
				KermitHigh._logger.Error(new LogMessageGenerator(ex.ToString));
			}
		}

		public static int Length(string msg)
		{
			Match match = Regex.Match(msg, "\\|.{10}]");
			bool success = match.Success;
			int result;
			if (success)
			{
				result = match.Index - 49;
			}
			else
			{
				result = -1;
			}
			return result;
		}

		public void OrderSamples(List<Sample> samples)
		{
			List<string> list = this.EncodeSamples(samples);
			bool flag = list.Count == 0;
			if (!flag)
			{
				foreach (string msg in list)
				{
					this._manager.PutMsgInSendingQueue(msg, 0L);
				}
			}
		}

		private List<string> EncodeSamples(List<Sample> samples)
		{
			List<string> list = new List<string>();
			int num = samples.Count / 10;
			bool flag = samples.Count % 10 != 0;
			if (flag)
			{
				num++;
			}
			for (int i = 0; i < num; i++)
			{
				list.Add(this.EncodeSamples10(samples, i));
			}
			return list;
		}

		private string EncodeSamples10(List<Sample> samples, int i)
		{
			string text = "";
			int num = i * 10;
			int num2 = (i + 1) * 10;
			bool flag = num2 > samples.Count;
			if (flag)
			{
				num2 = samples.Count;
			}
			for (int j = num; j < num2; j++)
			{
				text += this.EncodeSample(samples[j]);
			}
			return text;
		}

		private string EncodeSample(Sample sample)
		{
			bool flag = sample == null;
			string result;
			if (flag)
			{
				result = "";
			}
			else
			{
				string text = this.UrineSources.Contains(sample.SampleSource.SampleSourceCode) ? "3" : ((sample.SampleSource.SampleSourceCode == 14L) ? "2" : "1");
				result = string.Concat(new string[]
				{
					sample.SampleCode.ToString().PadRight(15),
					text,
					"0 1.000",
					this.GetTests(sample.SampleId),
					"]"
				});
			}
			return result;
		}

		public string GetTests(long sid)
		{
			List<string> tests = AstmHigh.GetTests(sid, this._manager.Instrument, false);
			return tests.Aggregate("", (string current, string a) => current + a);
		}

		public void LoadOk(string[] items, string id)
		{
			bool flag = items.Any((string x) => x.Length != 13 && x.Length != 15 && x.Length != 17 && x.Length != 19 && x.Length != 21);
			if (flag)
			{
				KermitHigh._logger.Info("Fatal error");
			}
			else
			{
				JihazResult result = KermitHigh.GetResult(items, id);
				AstmHigh.LoadResults(result, this._manager.Instrument, null);
			}
		}

		private static JihazResult GetResult(string[] records, string id)
		{
			JihazResult jihazResult = new JihazResult(id);
			foreach (string text in records)
			{
				string text2 = text.Substring(0, 1).TrimStart(new char[0]);
				bool flag = text2 == "#";
				if (flag)
				{
					text2 = "##";
				}
				string text3 = text.Substring(1, 9);
				KermitHigh._logger.Info(text2 + " : " + text3);
				jihazResult.Results.Add(new LowResult(text2, text3, null, null, null));
			}
			return jihazResult;
		}

		public void Upload()
		{
			try
			{
				LaboContext laboContext = new LaboContext();
				DateTime lastweek = DateTime.Now.Date.AddDays(-2.0);
				List<Analysis> source = (from x in laboContext.Analysis
				where (int)x.AnalysisState == 0 && x.CreatedDate > lastweek && x.InstrumentId == (int?)this._manager.InstrumentId
				select x).ToList<Analysis>();
				IEnumerable<Analysis> source2 = from x in source
				where x.AnalysisType.AnalysisTypeInstrumentMappings.Any((AnalysisTypeInstrumentMapping y) => y.InstrumentCode == this._manager.InstrumentCode && !string.IsNullOrEmpty(y.AnalysisTypeCode))
				select x;
				List<Sample> samples = (from x in (from y in source2
				select y.Sample).Distinct<Sample>()
				where x != null
				select x).ToList<Sample>();
				this.OrderSamples(samples);
			}
			catch (Exception ex)
			{
				KermitHigh._logger.Error(new LogMessageGenerator(ex.ToString));
			}
		}

		private static Logger _logger = LogManager.GetCurrentClassLogger();

		private AstmManager _manager;

		private List<long> UrineSources = new List<long>
		{
			8L,
			9L,
			10L
		};
	}
}
