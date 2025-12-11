using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Timers;
using GbService.Communication;
using GbService.Model.Contexts;
using GbService.Model.Domain;
using NLog;

namespace GbService.ASTM
{
	public class AstmHigh1
	{
		public AstmHigh1(Instrument instrument, char separator, char repeater = '^')
		{
			this._instrument = instrument;
			this.Separator = separator;
			this.Repeater = repeater;
			string instrumentPortName = instrument.InstrumentPortName;
			string[] array = instrumentPortName.Split(new char[]
			{
				','
			});
			this._upl = array[1];
			this._dnl = array[2];
			this._dnlCopy = array[3];
			this._pathUpl = array[0] + "\\" + this._upl;
			this._pathDnl = array[0] + "\\" + this._dnl;
			this._gs = ParamDictHelper.SettingLong(ParamDictName.AboRhAnalysisTypeId).GetValueOrDefault();
			AstmHigh1._logger.Info("Started");
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
				List<string> list = (from x in Directory.GetFiles(this._pathUpl)
				where x.EndsWith(".UPL")
				select x).ToList<string>();
				foreach (string path in list)
				{
					string fileName = Path.GetFileName(path);
					AstmHigh1._logger.Info("File " + fileName);
					string text = File.ReadAllText(path);
					int? num = this._instrument.InstrumentParity % 10;
					int num2 = 0;
					bool flag = num.GetValueOrDefault() == num2 & num != null;
					if (flag)
					{
						AstmHigh1._logger.Debug("content : \n" + text);
					}
					AstmHigh1.Parse(text, this._instrument, this.Repeater);
					File.Delete(path);
				}
				bool flag2 = !this._instrument.B1;
				if (!flag2)
				{
					List<Sample> samples = this.GetSamples();
					foreach (Sample sample in samples)
					{
						this.EncodeSample(sample);
					}
				}
			}
			catch (Exception ex)
			{
				AstmHigh1._logger.Error(new LogMessageGenerator(ex.ToString));
			}
		}

		public static void Parse(string msg, Instrument instrument, char repeater)
		{
			try
			{
				AstmHigh1._logger.Debug(msg);
				ASTM_Message astm_Message = Parser.Parse(msg);
				IEnumerable<AstmOrder> source = astm_Message.patientRecords.SelectMany((ASTM_Patient x) => x.OrderRecords);
				foreach (AstmOrder astm in from x in source
				where x.ResultRecords.Count > 0
				select x)
				{
					try
					{
						JihazResult result = AstmHigh1.GetResult(astm, repeater);
						AstmHigh.LoadResults(result, instrument, new char?(repeater));
					}
					catch (Exception ex)
					{
						AstmHigh1._logger.Error(new LogMessageGenerator(ex.ToString));
					}
				}
			}
			catch (Exception ex2)
			{
				AstmHigh1._logger.Error(new LogMessageGenerator(ex2.ToString));
			}
		}

		public static JihazResult GetResult(AstmOrder astm, char repeater)
		{
			string text = astm.Instrument.Split(new char[]
			{
				repeater
			}).FirstOrDefault<string>();
			string text2 = (text != null) ? text.TrimEnd(new char[]
			{
				'^'
			}) : null;
			AstmHigh1._logger.Info("id=" + text2);
			JihazResult jihazResult = new JihazResult(text2);
			foreach (AstmResult astmResult in astm.ResultRecords)
			{
				string text3 = astmResult.Test.Replace(new string(repeater, 3), "");
				AstmHigh1._logger.Info(text3 + " " + astmResult.Value);
				string text4 = AstmHigh1.ResultTxt(astmResult.Value.Split(new char[]
				{
					repeater
				}), text3);
				AstmHigh1._logger.Info(string.Concat(new string[]
				{
					text3,
					" : ",
					text4,
					" : ",
					astmResult.AbnormalFlag
				}));
				jihazResult.Results.Add(new LowResult(text3, text4, astmResult.Units, astmResult.AbnormalFlag, null));
			}
			return jihazResult;
		}

		private static string ResultTxt(string[] results, string test)
		{
			bool flag = test.StartsWith("Result^");
			string result;
			if (flag)
			{
				result = TextUtil.GetABO(results[0]) + TextUtil.GetRhesus(results[1]);
			}
			else
			{
				foreach (string str in results)
				{
					AstmHigh1._logger.Info("result : " + str);
				}
				int num;
				result = (int.TryParse(results[0], out num) ? ((num >= 30) ? "+" : ((num == 0) ? "-" : null)) : null);
			}
			return result;
		}

		public List<Sample> GetSamples()
		{
			LaboContext laboContext = new LaboContext();
			DateTime lastweek = DateTime.Now.Date.AddDays(-7.0);
			IQueryable<Analysis> source = from x in laboContext.Analysis
			where (int)x.AnalysisState == 0 && x.CreatedDate > lastweek && x.AnalysisTypeId == this._gs
			select x;
			List<Sample> list = (from y in source
			select y.Sample).Distinct<Sample>().ToList<Sample>();
			AstmHigh1._logger.Info("count = " + list.Count.ToString());
			foreach (Analysis analysis in source.ToList<Analysis>())
			{
				analysis.AnalysisState = AnalysisState.EnvoyerAutomate;
			}
			laboContext.SaveChanges();
			return list;
		}

		private void EncodeSample(Sample sample)
		{
			string text = TextUtil.GetDate(sample, this._instrument).ToString("yyyyMMddHHmmss");
			Patient patient = sample.AnalysisRequest.Patient;
            DateTime? dateNaiss = patient.PatientDateNaiss;
            string text2 = dateNaiss.HasValue
                ? dateNaiss.Value.ToString("yyyyMMdd")
                : "";
            string formattedSampleCode = sample.FormattedSampleCode;
			string contents = string.Concat(new string[]
			{
				"H|\\^&|||Bio-Rad|IH v4.2||||||||",
				text,
				"\r\nP|1||",
				formattedSampleCode,
				"||",
				patient.Nom,
				"^",
				patient.Prenom,
				"||",
				text2,
				"|",
				patient.ShortSexe,
				"||||||||||||||||||||||||^\r\nO|1||",
				formattedSampleCode,
				"^^^\\^^^|^^^MO01A|R|",
				text,
				"||||||||||||2|||||||O\r\nO|2||",
				formattedSampleCode,
				"^^^\\^^^|^^^PR08|R|",
				text,
				"||||||||||||2|||||||O\r\nL|1|N\r\n"
			});
			string text3 = Path.Combine(this._pathDnl, DateTime.Now.ToString("yyyyMMdd_HHmmss_" + Guid.NewGuid().ToString()) + ".DNL");
			File.AppendAllText(text3, contents);
			File.AppendAllText(text3.Replace("\\" + this._dnl + "\\", "\\" + this._dnlCopy + "\\"), contents);
			AstmHigh1._logger.Info("envoyé automate: " + text3 + " " + sample.SampleCode.ToString());
		}

		private string _upl;

		private string _dnl;

		public char Separator;

		private static Logger _logger = LogManager.GetCurrentClassLogger();

		private Instrument _instrument;

		public char Repeater;

		private Timer _serviceTimer;

		private string _pathUpl;

		private string _pathDnl;

		private long _gs;

		private string _dnlCopy;
	}
}
