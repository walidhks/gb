using System;
using System.Collections.Generic;
using System.Linq;
using GbService.ASTM;
using GbService.Communication;
using GbService.Model.Contexts;
using GbService.Model.Domain;
using NLog;

namespace GbService.Other
{
	public class AU480MessageHandler
	{
		public AU480MessageHandler(AstmManager manager)
		{
			this._manager = manager;
			this._instrument = manager.Instrument;
			this._cl = ((this._instrument.Mode == 0) ? 2 : 3);
			this._pos = ParamDictHelper.NumberPositionBarcode;
		}

		public void LoadOk(string msg)
		{
			try
			{
				msg = msg.Replace("\r\n", "");
				string text = msg.Substring(15, this._pos);
				AU480MessageHandler._logger.Info(text);
				string text2 = msg.Substring(146 + this._pos);
				AU480MessageHandler._logger.Debug(string.Format("msg= {0}, data = {1}, pos = {2}", msg, text2, this._pos));
				List<string> list = TextUtil.Split(text2, 17 + this._cl).ToList<string>();
				AU480MessageHandler._logger.Debug<int>(list.Count);
				List<string[]> records = (from x in list
				select new string[]
				{
					x.Substring(0, this._cl),
					x.Substring(this._cl, 9),
					x.Substring(9 + this._cl, 4)
				}).ToList<string[]>();
				JihazResult result = AU480MessageHandler.GetResult(records, text);
				AstmHigh.LoadResults(result, this._instrument, null);
			}
			catch (Exception ex)
			{
				AU480MessageHandler._logger.Error(new LogMessageGenerator(ex.ToString));
			}
		}

		public string HandleRequest(string msg)
		{
			AU480MessageHandler._logger.Debug("h2");
			LaboContext laboContext = new LaboContext();
			string text = msg.Substring(15, this._pos);
			long code;
			bool flag = !long.TryParse(text, out code);
			string result;
			if (flag)
			{
				AU480MessageHandler._logger.Error(text + " not long, check image 6");
				result = null;
			}
			else
			{
				Sample sample = laboContext.Sample.FirstOrDefault((Sample x) => x.SampleCode == (long?)code);
				bool flag2 = sample == null;
				if (flag2)
				{
					AU480MessageHandler._logger.Error(string.Format("{0} sample not found ", code));
					result = null;
				}
				else
				{
					Patient patient = sample.AnalysisRequest.Patient;
					string text2 = "S " + msg.Substring(4, 11 + this._pos);
					string str = (patient.Age == 0) ? patient.AgeMonths.ToString("D5") : (patient.Age.ToString("D3") + "00");
					text2 = text2 + "    EF" + str;
					text2 += patient.Nom.PadRight(20);
					text2 += patient.Prenom.PadRight(20);
                    DateTime? birthDate = patient.PatientDateNaiss;
                    text2 += (birthDate?.ToString("dd/MM/yyyy") ?? " ").PadRight(20);
                    text2 += "LAM Z".PadRight(20);
					text2 += "X".PadRight(20);
					text2 += "12-15-89".PadRight(20);
					text2 += this.GetTests(sample);
					AU480MessageHandler._logger.Debug(text2);
					result = text2;
				}
			}
			return result;
		}

		private string GetTests(Sample sample)
		{
			List<string> tests = AstmHigh.GetTests(sample.SampleId, this._manager.Instrument, false);
			return tests.Aggregate("", (string current, string a) => current + a.PadLeft(this._cl, '0'));
		}

		public static JihazResult GetResult(List<string[]> records, string id)
		{
			JihazResult jihazResult = new JihazResult(id);
			foreach (string[] array in records)
			{
				int? @int = TextUtil.GetInt(array[0]);
				bool flag = @int == null;
				if (!flag)
				{
					string value = array[1];
					jihazResult.Results.Add(new LowResult(@int.ToString(), value, null, null, null));
				}
			}
			return jihazResult;
		}

		private static Logger _logger = LogManager.GetCurrentClassLogger();

		private AstmManager _manager;

		private int _cl;

		private Instrument _instrument;

		private readonly int _pos;
	}
}
