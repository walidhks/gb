using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Timers;
using GbService.Communication;
using GbService.Model.Contexts;
using GbService.Model.Domain;
using NLog;

namespace GbService.ASTM
{
	public class FileHigh
	{
		public FileHigh(Instrument instrument)
		{
			this._instrument = instrument;
			this._path1 = instrument.InstrumentPortName;
			this._path2 = instrument.InstrumentPortName + "\\b\\";
			this.ScheduleRefresh(20);
		}

		private void ScheduleRefresh(int seconds)
		{
			this._serviceTimer = new Timer
			{
				Enabled = true,
				Interval = (double)(1000 * seconds)
			};
			this._serviceTimer.Elapsed += this.Refresh;
			this._serviceTimer.Start();
		}

		private void Refresh(object sender, ElapsedEventArgs e)
		{
			try
			{
				List<string> list = (from x in Directory.GetFiles(this._path1)
				where x.EndsWith("OUT.DAT")
				select x).ToList<string>();
				foreach (string text in list)
				{
					string fileName = Path.GetFileName(text);
					string[] array = File.ReadAllLines(text);
					foreach (string m in array)
					{
						this.Parse(m);
					}
					FileHigh._logger.Info("File " + fileName);
					File.Move(text, this._path2 + DateTime.Now.ToString("yyyyddMM HHmmss") + fileName);
				}
			}
			catch (Exception ex)
			{
				FileHigh._logger.Error(new LogMessageGenerator(ex.ToString));
			}
		}

		public void Parse(string m)
		{
			bool flag = m.Length == 16;
			if (!flag)
			{
				this.LoadResults(m);
			}
		}

		public void LoadResults(string msg)
		{
			LaboContext laboContext = new LaboContext();
			string text = msg.Substring(5, 15);
			FileHigh._logger.Info("looking for sample {0}", text);
			text = text.Trim();
			long sampleCode;
			bool flag = !long.TryParse(text, out sampleCode);
			if (!flag)
			{
				Sample sample = laboContext.Sample.FirstOrDefault((Sample s) => s.SampleCode == (long?)sampleCode);
				bool flag2 = sample == null;
				if (flag2)
				{
					FileHigh._logger.Error("Sample not found!");
				}
				else
				{
					FileHigh._logger.Info("Sample {0} found", sample.SampleId);
					List<string> list = TextUtil.Split(msg.Substring(266, 100), 10).ToList<string>();
					List<string> list2 = TextUtil.Split(msg.Substring(366, 100), 5).ToList<string>();
					string text2 = msg.Substring(0, 1);
					string v = msg.Substring(90, 5);
					this.SetValue(laboContext, sample, text2, v);
					for (int i = 0; i < 10; i++)
					{
						string text3 = list[i].Trim();
						this.SetValue(laboContext, sample, text3, list2[i]);
						bool flag3 = text2 == "J";
						if (flag3)
						{
							this.SetValue(laboContext, sample, text3 + " %", list2[i + 10]);
						}
					}
					string text4 = msg.Substring(789);
				}
			}
		}

		private void SetValue(LaboContext db, Sample sample, string code, string v)
		{
			AnalysisTypeInstrumentMapping map = db.AnalysisTypeInstrumentMappings.FirstOrDefault((AnalysisTypeInstrumentMapping m) => m.InstrumentCode == this._instrument.InstrumentCode && m.AnalysisTypeCode == code);
			bool flag = map == null;
			if (flag)
			{
				FileHigh._logger.Info("Map not found!" + code);
			}
			else
			{
				Analysis analysis = sample.Analysis.FirstOrDefault((Analysis x) => x.AnalysisTypeId == map.AnalysisTypeId && x.AnalysisState <= AnalysisState.EnvoyerAutomate);
				bool flag2 = analysis == null;
				if (!flag2)
				{
					decimal num;
					bool flag3 = !decimal.TryParse(v, NumberStyles.Any, CultureInfo.InvariantCulture, out num);
					if (flag3)
					{
						FileHigh._logger.Info("error {0}", v);
					}
					else
					{
						analysis.ResultTxt = num.ToString();
						analysis.InstrumentId = new int?(this._instrument.InstrumentId);
						db.SaveChanges();
					}
				}
			}
		}

		public List<Sample> GetSamples()
		{
			LaboContext laboContext = new LaboContext();
			DateTime lastweek = DateTime.Now.Date.AddDays(-7.0);
			List<Analysis> source = (from x in laboContext.Analysis
			where (int)x.AnalysisState == 0 && x.CreatedDate > lastweek && x.InstrumentId == (int?)this._instrument.InstrumentId
			select x).ToList<Analysis>();
			List<Sample> result = (from y in source
			select y.Sample).Distinct<Sample>().ToList<Sample>();
			foreach (Analysis analysis in source.ToList<Analysis>())
			{
				analysis.AnalysisState = AnalysisState.EnvoyerAutomate;
			}
			laboContext.SaveChanges();
			return result;
		}

		public char Separator;

		private static Logger _logger = LogManager.GetCurrentClassLogger();

		private Instrument _instrument;

		public char Repeater;

		private Timer _serviceTimer;

		private string _path1;

		private string _path2;
	}
}
