using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Timers;
using GbService.ASTM;
using GbService.Common;
using GbService.Communication;
using GbService.Communication.Common;
using GbService.Communication.Serial;
using GbService.Model.Contexts;
using GbService.Model.Domain;
using NLog;

namespace GbService.Other
{
	public class Advia2120MessageHandler
	{
		public Advia2120MessageHandler(ILowManager manager, Instrument instrument)
		{
			this._manager = manager;
			this._instrument = instrument;
			this._manager.MessageReceived += this.OnMessageReceived;
			this.Init();
			this.ScheduleRefresh(20);
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
			Advia2120MessageHandler._logger.Info("_lastMessageTime = " + this._lastMessageTime.ToString());
			bool flag = this._lastMessageTime.AddSeconds(20.0) < DateTime.Now;
			if (flag)
			{
				this._state = Advia2120State.Blocked;
				this.Init();
			}
		}

		private void Init()
		{
			Thread.Sleep(3000);
			bool flag = this._state == Advia2120State.Active;
			if (!flag)
			{
				this._token = '0';
				this._state = Advia2120State.Active;
				bool flag2 = this.SendMsg(this._token, "I |002||");
				if (flag2)
				{
					this._token = this.NextToken(this._token);
					this.SendMsg(this._token, "S          ");
				}
				else
				{
					this._state = Advia2120State.Blocked;
					this._manager.Close();
				}
			}
		}

		private void OnMessageReceived(object sender, MessageReceivedEventArgs args)
		{
			this.Parse(args.Message);
		}

		public void Parse(string msg)
		{
			try
			{
				this._state = Advia2120State.Active;
				this._lastMessageTime = DateTime.Now;
				Tuple<char, string> message = Advia2120MessageHandler.GetMessage(msg);
				bool flag = message == null;
				if (!flag)
				{
					bool flag2 = message.Item1 == '\u0015';
					if (flag2)
					{
						Thread.Sleep(37000);
						this._state = Advia2120State.Idle;
						this.Init();
					}
					else
					{
						bool flag3 = message.Item2 == null;
						if (!flag3)
						{
							this._token = message.Item1;
							string item = message.Item2;
							Thread.Sleep(100);
							this._manager.SendLow(this._token.ToString(), Coding.Asc);
							this._token = this.NextToken(this._token);
							bool flag4 = item.StartsWith("Q");
							if (flag4)
							{
								string data = this.OrderSample(item.Substring(2));
								this.SendMsg(this._token, data);
							}
							else
							{
								bool flag5 = item.StartsWith("R");
								if (flag5)
								{
									try
									{
										this.LoadResults(item);
									}
									catch (Exception ex)
									{
										Advia2120MessageHandler._logger.Info(new LogMessageGenerator(ex.ToString));
									}
									string data2 = "Z" + "".PadRight(17) + " 0";
									this.SendMsg(this._token, data2);
								}
								else
								{
									bool flag6 = item.StartsWith("S");
									if (flag6)
									{
										Thread.Sleep(5000);
										this.SendMsg(this._token, "S          ");
									}
								}
							}
						}
					}
				}
			}
			catch (Exception ex2)
			{
				Advia2120MessageHandler._logger.Info(new LogMessageGenerator(ex2.ToString));
			}
		}

		private string OrderSample(string rid)
		{
			LaboContext laboContext = new LaboContext();
			long sampleCode;
			bool flag = !long.TryParse(rid, out sampleCode);
			string result;
			if (flag)
			{
				Advia2120MessageHandler._logger.Info(rid + " ??");
				result = this.NoWork(rid);
			}
			else
			{
				Sample sample = laboContext.Sample.FirstOrDefault((Sample x) => x.SampleCode == (long?)sampleCode);
				bool flag2 = sample == null;
				if (flag2)
				{
					Advia2120MessageHandler._logger.Info(sampleCode.ToString() + " not found");
					result = this.NoWork(rid);
				}
				else
				{
					string text = sampleCode.ToString("D14");
					Patient patient = sample.AnalysisRequest.Patient;
					DateTime? patientDateNaiss = patient.PatientDateNaiss;
					string text2;
					if (patientDateNaiss != null)
					{
						patientDateNaiss = patient.PatientDateNaiss;
						text2 = ((patientDateNaiss != null) ? patientDateNaiss.GetValueOrDefault().ToString("MM/dd/yyyy") : null);
					}
					else
					{
						text2 = "01/01/2000";
					}
					string text3 = text2;
					string text4 = TextUtil.Now.ToString("MM/dd/yy HHmm");
					string text5 = string.Concat(new string[]
					{
						"Y",
						"".PadRight(3),
						"A ",
						text,
						"".PadRight(25),
						text,
						"".PadRight(3),
						patient.PatientNomPrenom.PadRight(30),
						"".PadRight(1),
						text3,
						"".PadRight(1),
						patient.ShortSexe,
						" ",
						text4,
						" LOCAT1 DOCTO1 ",
						Tu.NL,
						this.GetTests(sample)
					});
					result = text5;
				}
			}
			return result;
		}

		private string NoWork(string rid)
		{
			return "N W " + rid;
		}

		private char NextToken(char code)
		{
            char c = (char)(code + 1); 
            if (c > 'Z')
            {
                c = '0';
            }
            return c;
        }

		private bool SendMsg(char token, string data)
		{
			string text = token.ToString() + data + Tu.NL;
			string str = Crc.exclusiveOR(text);
			return this._manager.SendLow("\u0002" + text + str + "\u0003", Coding.Asc);
		}

		public static Tuple<char, string> GetMessage(string msg)
		{
			bool flag = msg.Length == 1;
			Tuple<char, string> result;
			if (flag)
			{
				result = new Tuple<char, string>(msg[0], null);
			}
			else
			{
				try
				{
					int num = msg.IndexOf('\u0002');
					int num2 = msg.IndexOf('\u0003');
					string text = msg.Substring(num + 1, num2 - 4);
					result = new Tuple<char, string>(text[0], text.Substring(1));
				}
				catch (Exception ex)
				{
					Advia2120MessageHandler._logger.Info(new LogMessageGenerator(ex.ToString));
					result = null;
				}
			}
			return result;
		}

		public string GetTests(Sample sample)
		{
			List<string> tests = AstmHigh.GetTests(sample.SampleId, this._instrument, false);
			return tests.Aggregate("", (string current, string a) => current + a.PadLeft(3, '0'));
		}

		public void LoadResults(string msg)
		{
			LaboContext laboContext = new LaboContext();
			string text = msg.Substring(2, 14);
			Advia2120MessageHandler._logger.Info("looking for sample {0}", text);
			long sampleCode;
			bool flag = !long.TryParse(text, out sampleCode);
			if (!flag)
			{
				Sample sample = laboContext.Sample.FirstOrDefault((Sample s) => s.SampleCode == (long?)sampleCode);
				bool flag2 = sample == null;
				if (flag2)
				{
					Advia2120MessageHandler._logger.Info("Sample not found!");
				}
				else
				{
					Advia2120MessageHandler._logger.Info<long?>("Sample {0} found", sample.SampleCode);
					this._results = this.GetItems(msg.Substring(56));
					bool flag3 = this._results.Contains(null);
					if (!flag3)
					{
						foreach (Result result in this._results)
						{
							string code = result.Code;
							AnalysisTypeInstrumentMapping map = laboContext.AnalysisTypeInstrumentMappings.FirstOrDefault((AnalysisTypeInstrumentMapping m) => m.InstrumentCode == this._instrument.InstrumentCode && m.AnalysisTypeCode == code);
							bool flag4 = map == null;
							if (flag4)
							{
								Advia2120MessageHandler._logger.Info("Map not found!" + code);
							}
							else
							{
								Analysis analysis = sample.Analysis.FirstOrDefault((Analysis x) => x.AnalysisTypeId == map.AnalysisTypeId && x.AnalysisState <= AnalysisState.EnvoyerAutomate);
								bool flag5 = analysis == null;
								if (!flag5)
								{
									string value = result.Value;
									Advia2120MessageHandler._logger.Info("value {0}", value);
									Advia2120MessageHandler._logger.Info("Flag {0}", result.Flag);
									analysis.ResultTxt = value.Replace(",", ".");
									analysis.Flag = result.Flag;
									int instrumentId = this._instrument.InstrumentId;
									Helper.ChangeSate(analysis, instrumentId, AnalysisState.ReçuAutomate);
									Helper.ChangeSate(analysis.Parent, instrumentId, AnalysisState.ReçuAutomate);
								}
							}
						}
						laboContext.SaveChanges();
					}
				}
			}
		}

		public List<Result> GetItems(string msg)
		{
			bool flag = msg.Length == 0;
			List<Result> result;
			if (flag)
			{
				result = new List<Result>();
			}
			else
			{
				bool flag2 = msg.Length < 9;
				if (flag2)
				{
					Advia2120MessageHandler._logger.Error("Fatal Error");
					result = null;
				}
				else
				{
					List<Result> list = new List<Result>();
					string code = msg.Substring(0, 3);
					string value = msg.Substring(3, 5);
					char c = '|';
					bool flag3 = msg.Length >= 10 && msg[9] == c;
					int num;
					Result item;
					if (flag3)
					{
						num = msg.IndexOf(c, msg.IndexOf(c) + 1) + 1;
						bool flag4 = num == -1;
						if (flag4)
						{
							Advia2120MessageHandler._logger.Error("Fatal Error 1");
							return null;
						}
						string flag5 = msg.Substring(10, num - 11);
						item = new Result(code, value, flag5);
					}
					else
					{
						num = 9;
						item = new Result(code, value, "");
					}
					list.Add(item);
					try
					{
						List<Result> items = this.GetItems(msg.Substring(num));
						list.AddRange(items);
					}
					catch (Exception value2)
					{
						Console.WriteLine(value2);
					}
					result = list;
				}
			}
			return result;
		}

		private static Logger _logger = LogManager.GetCurrentClassLogger();

		private ILowManager _manager;

		private char _token = '0';

		private Advia2120State _state = Advia2120State.Idle;

		private List<Result> _results;

		private System.Timers.Timer _serviceTimer;

		private DateTime _lastMessageTime;

		private Instrument _instrument;
	}
}
