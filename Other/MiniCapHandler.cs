using System;
using System.Collections.Generic;
using System.Linq;
using GbService.ASTM;
using GbService.Communication;
using GbService.Communication.Common;
using GbService.Model.Contexts;
using GbService.Model.Domain;
using NLog;

namespace GbService.Other
{
	public class MiniCapHandler
	{
		public MiniCapHandler(ILowManager manager, Instrument instrument)
		{
			this._manager = manager;
			this._instrument = instrument;
		}

		public void Handle(string m)
		{
			this._msg += m;
			int num = this._msg.IndexOf('\u0003');
			bool flag = num < 0;
			if (flag)
			{
				num = this._msg.IndexOf('\u0004');
			}
			bool flag2 = num < 0;
			if (!flag2)
			{
				string m2 = this._msg.Substring(this._msg.IndexOf('\u0002') + 1, num - 1);
				this.Parse(m2);
				this._msg = "";
			}
		}

		public void Parse(string m)
		{
			bool flag = m.Length == 1;
			if (!flag)
			{
				this.SendMsg('\u0006'.ToString());
				bool flag2 = m.Length == 16;
				if (flag2)
				{
					this.GetSample(m);
				}
				else
				{
					JihazResult result = MiniCapHandler.GetResult(m);
					AstmHigh.LoadResults(result, this._instrument, null);
				}
			}
		}

		public static JihazResult GetResult(string msg)
		{
			string scode = msg.Substring(5, 15);
			JihazResult jihazResult = new JihazResult(scode);
			List<string> list = TextUtil.Split(msg.Substring(266, 100), 10).ToList<string>();
			List<string> list2 = TextUtil.Split(msg.Substring(366, 100), 5).ToList<string>();
			string text = msg.Substring(0, 1);
			string value = msg.Substring(90, 5);
			jihazResult.Results.Add(new LowResult(text, value, null, null, null));
			for (int i = 0; i < 10; i++)
			{
				string text2 = list[i].Trim();
				jihazResult.Results.Add(new LowResult(text2, list2[i], null, null, null));
				bool flag = text == "J";
				if (flag)
				{
					jihazResult.Results.Add(new LowResult(text2 + " %", list2[i + 10], null, null, null));
				}
			}
			return jihazResult;
		}

		private void GetSample(string m)
		{
			LaboContext laboContext = new LaboContext();
			string text = m.Substring(1);
			Sample sample = TextUtil.Sample(laboContext, text, this._instrument);
			bool flag = sample == null;
			if (!flag)
			{
				Analysis analysis = laboContext.Analysis.FirstOrDefault((Analysis x) => x.AnalysisRequestId == sample.AnalysisRequestId && x.AnalysisTypeId == this._instrument.L3 && (int)x.AnalysisState == 3);
				string text2 = (analysis != null) ? analysis.ResultTxt : null;
				DateTime dateCreated = sample.DateCreated;
				Patient patient = sample.AnalysisRequest.Patient;
                DateTime? birthDate = patient.PatientDateNaiss;
                string text3 = (birthDate?.ToString("ddMMyyyy") ?? "01012000");
                string data = string.Format("{0}0000{1,15}{2,-30}{3}{4}000{5,-20}{6:ddMMyyyy}{7,-155}", new object[]
				{
					m.Substring(0, 1),
					text,
					patient.PatientNomPrenom.Truncate(30),
					text3,
					patient.ShortSexe,
					"BMLab ",
					dateCreated,
					text2
				});
				this.SendMsg(data);
			}
		}

		private void SendMsg(string data)
		{
			List<char> list = new List<char>
			{
				'\u0002'
			};
			list.AddRange(data.ToCharArray());
			list.Add('\u0003');
			this._manager.SendLow(list);
		}

		private static Logger _logger = LogManager.GetCurrentClassLogger();

		private ILowManager _manager;

		private string _msg;

		private Instrument _instrument;
	}
}
