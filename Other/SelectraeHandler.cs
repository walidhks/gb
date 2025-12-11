using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Timers;
using GbService.ASTM;
using GbService.Communication;
using GbService.Communication.Common;
using GbService.Model.Contexts;
using GbService.Model.Domain;
using NLog;

namespace GbService.Other
{
	public class SelectraeHandler
	{
		public SelectraeHandler(ILowManager il, Instrument instrument)
		{
			this._il = il;
			this._instrument = instrument;
			SelectraeHandler._logger.Info("here");
			this.ScheduleRefresh(30);
		}

		private void ScheduleRefresh(int seconds)
		{
			this._serviceTimer = new System.Timers.Timer
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
				LaboContext laboContext = new LaboContext();
				DateTime lastweek = DateTime.Now.Date.AddDays(-7.0);
				List<Analysis> list = (from x in laboContext.Analysis
				where (int)x.AnalysisState == 10 && x.CreatedDate > lastweek && x.InstrumentId == (int?)this._instrument.InstrumentId
				select x).ToList<Analysis>();
				SelectraeHandler._logger.Info("q count = " + list.Count.ToString());
				bool flag = list.Count == 0;
				if (!flag)
				{
					IEnumerable<Analysis> source = from x in list
					where x.AnalysisType.AnalysisTypeInstrumentMappings.Any((AnalysisTypeInstrumentMapping y) => y.InstrumentCode == this._instrument.InstrumentCode && !string.IsNullOrWhiteSpace(y.AnalysisTypeCode))
					select x;
					int count = source.ToList<Analysis>().Count;
					SelectraeHandler._logger.Info("qq count = " + count.ToString());
					bool flag2 = count == 0;
					if (!flag2)
					{
						List<Sample> list2 = (from y in source
						select y.Sample).Distinct<Sample>().ToList<Sample>();
						SelectraeHandler._logger.Info("samples count = " + list2.Count.ToString());
						bool flag3 = list2.Count == 0;
						if (!flag3)
						{
							this.OrderSamples(list2);
						}
					}
				}
			}
			catch (Exception ex)
			{
				SelectraeHandler._logger.Error(new LogMessageGenerator(ex.ToString));
			}
		}

		public void OrderSamples(List<Sample> samples)
		{
			this._il.SendLow(string.Format("{0}{{I;}}{1}{2}{3}", new object[]
			{
				'\u0002',
				'\u0003',
				'\r',
				'\n'
			}), Coding.Asc);
			Thread.Sleep(3000);
			SelectraeHandler._logger.Info("after 3 secondes");
			foreach (Sample sample in samples)
			{
				this.EncodeSample(sample);
				Thread.Sleep(1000);
			}
		}

		private void EncodeSample(Sample sample)
		{
			bool flag = sample == null;
			if (!flag)
			{
				SelectraeHandler._logger.Info(string.Format("SampleCode = {0}, SampleId = {1}", sample.SampleCode, sample.SampleId));
				string tests = this.GetTests(sample.SampleId);
				bool flag2 = tests == null;
				if (flag2)
				{
					SelectraeHandler._logger.Info(string.Format("no tests for {0}", sample.SampleCode));
				}
				else
				{
					Patient patient = sample.AnalysisRequest.Patient;
					string formattedSampleCode = sample.FormattedSampleCode;
					string text = (patient.Nom + " " + patient.Prenom).PadRight(20).Truncate(20);
					string text2 = string.Concat(new string[]
					{
						"Q;",
						formattedSampleCode,
						";N;",
						text,
						";           ;M;",
						tests,
						";"
					});
					string msg = string.Format("{0}{{{1}}}{2}{3}{4}", new object[]
					{
						'\u0002',
						text2,
						'\u0003',
						'\r',
						'\n'
					});
					this._il.SendLow(msg, Coding.Asc);
				}
			}
		}

		public static void Parse(string msg, Instrument instrument)
		{
			try
			{
				SelectraeHandler._logger.Debug("--msg--: " + Tu.DataToString(msg.ToCharArray()));
				string message = SelectraeHandler.GetMessage(msg);
				bool flag = message == null || message.StartsWith("q");
				if (!flag)
				{
					bool flag2 = message.StartsWith("i") || message.StartsWith("I");
					if (flag2)
					{
						SelectraeHandler.LoadOrder(message);
					}
					else
					{
						AstmHigh.LoadResults(SelectraeHandler.GetResult(message), instrument, null);
					}
				}
			}
			catch (Exception ex)
			{
				SelectraeHandler._logger.Info(new LogMessageGenerator(ex.ToString));
			}
		}

		public static void LoadOrder(string s)
		{
			string[] array = s.Split(new char[]
			{
				';'
			});
			SelectraeHandler._dict.Clear();
			int num = s.StartsWith("i") ? 1 : 0;
			for (int i = 0; i < 32; i++)
			{
				string text = array[i + num + 2].TrimEnd(new char[0]);
				bool flag = !string.IsNullOrEmpty(text);
				if (flag)
				{
					SelectraeHandler._dict.Add(text, i);
				}
			}
		}

		public static JihazResult GetResult(string m)
		{
			string text = m.Substring(11, 12);
			SelectraeHandler._logger.Info(text);
			JihazResult jihazResult = new JihazResult(text);
			string str = m.Substring(62);
			List<string> list = TextUtil.Split(str, 58);
			foreach (string text2 in list)
			{
				string[] array = text2.Split(new char[]
				{
					';'
				});
				string text3 = array[0].TrimEnd(new char[0]);
				SelectraeHandler._logger.Info(text3 + ":" + array[1]);
				jihazResult.Results.Add(new LowResult(text3, array[1], null, null, null));
			}
			return jihazResult;
		}

		public static string GetMessage(string m)
		{
			string result;
			try
			{
				int num = m.IndexOf('\u0002');
				int num2 = m.IndexOf('\u0003');
				bool flag = num != 0 || num2 < 0;
				if (flag)
				{
					result = null;
				}
				else
				{
					m = m.Substring(num + 2);
					result = m.Substring(0, num2 - 3);
				}
			}
			catch (Exception ex)
			{
				SelectraeHandler._logger.Info(new LogMessageGenerator(ex.ToString));
				result = null;
			}
			return result;
		}

        public string GetTests(long sid)
        {
            LaboContext laboContext = new LaboContext();
            try
            {
                // Get all analyses for the sample with AnalysisState == 10
                List<Analysis> source = laboContext.Analysis
                    .Where(x => x.SampleId == sid && x.AnalysisState == AnalysisState.EnvoyerAutomate)
                    .ToList();

                List<Analysis> list;

                if (this._instrument.InstrumentStd == null)
                {
                    // Filter by instrument
                    list = source.Where(x =>
                        x.InstrumentId.HasValue &&
                        x.InstrumentId.Value == this._instrument.InstrumentId
                    ).ToList();
                }
                else
                {
                    list = source;
                }

                List<int> mappedTests = new List<int>();
                string bitString = "";

                foreach (var analysis in list)
                {
                    // Find mapping for this instrument
                    var mapping = analysis.AnalysisType.AnalysisTypeInstrumentMappings
                        .FirstOrDefault(m => m.InstrumentCode == this._instrument.InstrumentCode);

                    if (mapping != null && mapping.AnalysisTypeCode != null)
                    {
                        string code = mapping.AnalysisTypeCode.Contains('-')
                            ? mapping.AnalysisTypeCode.Split('-')[1]
                            : mapping.AnalysisTypeCode;

                        code = code.Trim();

                        if (SelectraeHandler._dict.ContainsKey(code))
                        {
                            int num = SelectraeHandler._dict[code];
                            SelectraeHandler._logger.Info($"key = {code}, value = {num}");
                            mappedTests.Add(num);
                        }
                        else
                        {
                            SelectraeHandler._logger.Error($"map= {mapping.AnalysisTypeCode}, c= {code} : not found in _dict");
                        }
                    }
                    else
                    {
                        SelectraeHandler._logger.Info("map null : " + analysis.AnalysisType.AnalysisTypeName);
                    }

                    // Update analysis state
                    analysis.AnalysisState = AnalysisState.EnvoyerAutomate;
                }

                laboContext.SaveChanges();

                if (mappedTests.Count == 0)
                    return null;

                for (int i = 0; i < 32; i++)
                {
                    bitString += mappedTests.Contains(i) ? "1" : "0";
                }

                return bitString;
            }
            finally
            {
                laboContext.Dispose();
            }
        }


        private static Logger _logger = LogManager.GetCurrentClassLogger();

		private ILowManager _il;

		private Instrument _instrument;

		private System.Timers.Timer _serviceTimer;

		private static Dictionary<string, int> _dict = new Dictionary<string, int>();
	}
}
