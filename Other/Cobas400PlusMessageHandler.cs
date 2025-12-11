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
	public class Cobas400PlusMessageHandler
	{
		public Cobas400PlusMessageHandler(ILowManager manager, Instrument instrument)
		{
			Cobas400PlusMessageHandler._manager = manager;
			new Thread(delegate()
			{
				Thread.CurrentThread.IsBackground = true;
				this.Synch();
			}).Start();
		}

		private void Synch()
		{
			try
			{
				Cobas400PlusMessageHandler.SendMsg(null, "00");
				Thread.Sleep(5000);
				this.ScheduleRefresh(30);
			}
			catch (Exception ex)
			{
				Cobas400PlusMessageHandler._logger.Info(new LogMessageGenerator(ex.ToString));
			}
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
			Cobas400PlusMessageHandler.SendMsg("40 1\n", "60");
			Thread.Sleep(15000);
			Cobas400PlusMessageHandler.SendMsg("10 01\n", "09");
		}

        public static void Parse(string msg, Instrument instrument)
        {
            try
            {
                string str = Tu.DataToString(msg.ToCharArray());
                Cobas400PlusMessageHandler._logger.Debug("--RX--: " + str);

                Tuple<string, string> message = Cobas400PlusMessageHandler.GetMessage(msg);
                if (message == null)
                    return;

                string item = message.Item2;

                if (item.StartsWith("42"))
                {
                    LaboContext laboContext = new LaboContext();
                    string[] lines = item.Split('\n');

                    for (int i = 0; i < lines.Length; i++)
                    {
                        string line = lines[i];

                        if (!line.StartsWith("42"))
                            continue;

                        string text2 = line.Substring(12, 15);
                        if (!long.TryParse(text2, out long sampleCode))
                        {
                            Cobas400PlusMessageHandler._logger.Info(text2 + " ??");
                            continue;
                        }

                        Sample sample = laboContext.Sample.FirstOrDefault(s => s.SampleCode == (long?)sampleCode);
                        if (sample == null)
                        {
                            Cobas400PlusMessageHandler._logger.Info(sampleCode + " not found");
                            continue;
                        }

                        string arg = sampleCode.ToString("D" + ParamDictHelper.NumberPositionBarcode)  // padding
                                            .PadRight(15);
                        string text3 = $"53 {arg} 00/00/0000\n";

                        IEnumerable<Analysis> analysis = sample.Analysis;

                        // Predicate lambda — no cached variable needed
                        bool hasMatchingAnalysis = analysis.Any(x =>
                            x.AnalysisState > AnalysisState.EnCours &&
                            x.InstrumentId.HasValue &&
                            x.InstrumentId.Value == instrument.InstrumentId
                        );

                        if (!hasMatchingAnalysis)
                        {
                            text3 += "54 000 00 A These are 21 chars   \n";
                        }

                        text3 += Cobas400PlusMessageHandler.GetTests(sample.SampleId, instrument);
                        Cobas400PlusMessageHandler.SendMsg(text3, "10");
                    }
                }
                else if (item.StartsWith("53"))
                {
                    JihazResult result = Cobas400PlusMessageHandler.GetResult(item);
                    AstmHigh.LoadResults(result, instrument, null);
                    Cobas400PlusMessageHandler.SendMsg("10 01\n", "09");
                }
                else if (item.StartsWith("55"))
                {
                    Cobas400PlusMessageHandler.SendMsg("10 01\n", "09");
                }
            }
            catch (Exception ex)
            {
                Cobas400PlusMessageHandler._logger.Info(new LogMessageGenerator(ex.ToString));
            }
        }


        private static void SendMsg(string data, string code)
		{
			List<char> list = new List<char>
			{
				'\u0001',
				'\n'
			};
			list.AddRange("09 COBAS INTEGRA400 ".ToCharArray());
			list.AddRange(code.ToCharArray());
			list.Add('\n');
			list.Add('\u0002');
			list.Add('\n');
			bool flag = data != null;
			if (flag)
			{
				list.AddRange(data.ToCharArray());
			}
			list.Add('\u0003');
			list.Add('\n');
			list.Add('\u0004');
			list.Add('\n');
			Cobas400PlusMessageHandler._manager.SendLow(list);
		}

		private static Tuple<string, string> GetMessage(string msg)
		{
			int num = msg.IndexOf('\u0002');
			int num2 = msg.IndexOf('\u0003');
			Tuple<string, string> result;
			try
			{
				string item = msg.Substring(num - 3, 2);
				int num3 = num2 - num - 3;
				string item2 = (num3 >= 0) ? msg.Substring(num + 2, num3) : "";
				result = new Tuple<string, string>(item, item2);
			}
			catch (Exception ex)
			{
				Cobas400PlusMessageHandler._logger.Error(string.Format("i = {0}, j = {1}", num, num2));
				Cobas400PlusMessageHandler._logger.Error(new LogMessageGenerator(ex.ToString));
				result = null;
			}
			return result;
		}

		public static string GetTests(long sampleId, Instrument instrument)
		{
			List<string> tests = AstmHigh.GetTests(sampleId, instrument, false);
			string text = "";
			foreach (string text2 in tests)
			{
				string text3 = text2.PadLeft(3, ' ');
				bool flag = text3 == "529";
				if (flag)
				{
					text += string.Format("55 228{0}55 128{1}", '\n', '\n');
				}
				else
				{
					bool flag2 = text3 == "275";
					if (flag2)
					{
						text += string.Format("55 265{0}55 266{1}", '\n', '\n');
					}
					else
					{
						text += string.Format("55 {0}{1}", text3, '\n');
					}
				}
			}
			return text;
		}

		public static JihazResult GetResult(string msg)
		{
			string[] array = msg.Split(new char[]
			{
				'\n'
			});
			string text = array[0].Substring(3, 15);
			Cobas400PlusMessageHandler._logger.Info("code = " + text);
			JihazResult jihazResult = new JihazResult(text);
			string code = array[1].Substring(3, 3).TrimStart(new char[0]);
			string value = array[2].Substring(3, 13);
			jihazResult.Results.Add(new LowResult(code, value, null, null, null));
			return jihazResult;
		}

		private static Logger _logger = LogManager.GetCurrentClassLogger();

		private static ILowManager _manager;

		private System.Timers.Timer _serviceTimer;
	}
}
