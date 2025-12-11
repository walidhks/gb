using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using GbService.ASTM;
using GbService.Common;
using GbService.Communication;
using GbService.Communication.Common;
using GbService.Model.Contexts;
using GbService.Model.Domain;
using NHapi.Base;
using NHapi.Base.Model;
using NHapi.Base.Parser;
using NHapi.Base.Util;
using NHapi.Model.V231.Message;
using NLog;

namespace GbService.HL7.V231
{
	public class DsrQ03Handler
	{
		private static string CommunicationName
		{
			get
			{
				string result;
				if ((result = DsrQ03Handler._communicationName) == null)
				{
					result = (DsrQ03Handler._communicationName = ConfigurationSettings.AppSettings["ApplicationCommunicationName"]);
				}
				return result;
			}
		}

		private static string EnvironmentIdentifier
		{
			get
			{
				string result;
				if ((result = DsrQ03Handler._environmentIdentifier) == null)
				{
					result = (DsrQ03Handler._environmentIdentifier = ConfigurationSettings.AppSettings["EnvironmentIdentifier"]);
				}
				return result;
			}
		}

		public void SetMessage(IMessage msg)
		{
			this._message = (DSR_Q03)msg;
		}

		public DsrQ03Handler(QRY_Q02 qryMessage, Instrument instrument)
		{
			this._qryMessage = qryMessage;
			this._instrument = instrument;
		}

		public string GetMessage(long sampleCode, string messageControlId, Instrument instrument, string dsc = null, List<AnalysisType> analysisTypes = null)
		{
			bool flag = this._instrument.Kind == Jihas.Bs300;
			string result;
			if (flag)
			{
				LaboContext laboContext = new LaboContext();
				Sample sample = laboContext.Sample.FirstOrDefault((Sample x) => x.SampleCode == (long?)sampleCode);
				List<string> list = (sample != null) ? AstmHigh.GetTests(sample.SampleId, instrument, false) : new List<string>();
				string tests = DsrQ03Handler.GetTests(list);
				string now = this._instrument.Now.ToString("yyyyMMddHHmmss");
				result = DsrQ03Handler.Bs300Format(sample, tests, now);
			}
			else
			{
				this._message = this.MakeDsrMessage(sampleCode, messageControlId, instrument, dsc);
				PipeParser pipeParser = new PipeParser();
				string text = pipeParser.Encode(this._message);
				result = text.Replace("\r", "|\r").Replace("/S/", "^").Replace("/F/", "");
			}
			return result;
		}
        public static string GetTestsUrit(List<string> list)
        {
            string text = "";
            int num = 1;
            foreach (string arg in list)
            {
                text += string.Format("DSP|{0}||{1}^{2}^^|||\r", num + 28, num, arg);
                num++;
            }
            return text;
        }
        public static string Bs300Format(Sample sample, string tests, string now)
		{
			Patient patient = sample.AnalysisRequest.Patient;
            DateTime? birthDate = patient.PatientDateNaiss;
            string text = birthDate?.ToString("yyyyMMdd") ?? "20000101";
            string text2 = Helper.ID++.ToString();
			string text3 = string.Format("MSH|^~\\&|||Company|ChemistryAnalyzer|{0}||DSR^Q03|1|P|2.3.1|\r\nMSA|AA|1|Message accepted|||0|\r\nERR|0|\r\nQAK|SR|OK|\r\nQRD|{1}|D|D|1|||RD|{2}|OTH||||\r\nDSP|1||{3}|||\r\nDSP|2|||||\r\nDSP|3||{4} {5}|||\r\nDSP|4||{6}|||\r\nDSP|5||{7}|||\r\nDSP|6|||||\r\nDSP|7|||||\r\nDSP|8|||||\r\nDSP|9|||||\r\nDSP|10|||||\r\nDSP|11|||||\r\nDSP|12|||||\r\nDSP|13|||||\r\nDSP|14|||||\r\nDSP|15|||||\r\nDSP|16|||||\r\nDSP|17|||||\r\nDSP|18|||||\r\nDSP|19|||||\r\nDSP|20|||||\r\nDSP|21||{8}|||\r\nDSP|22||{9}|||\r\nDSP|23|||||\r\nDSP|24|||||\r\nDSP|25|||||\r\nDSP|26||serum|||\r\nDSP|27|||||\r\nDSP|28|||||\r\n{10}DSC||\r\n", new object[]
			{
				now,
				now,
				sample.SampleCode,
				patient.PatientID,
				patient.Nom,
				patient.Prenom,
				text,
				patient.ShortSexe,
				sample.SampleCode,
				text2,
				tests
			});
			return text3.Replace(Tu.NL, '\r'.ToString());
		}

		public static string GetTests(List<string> list)
		{
			string text = "";
			int num = 29;
			foreach (string arg in list)
			{
				text += string.Format("DSP|{0}||{1}^^^|||\r", num, arg);
				num++;
			}
			return text;
		}

		private DSR_Q03 MakeDsrMessage(long sampleCode, string messageControlId, Instrument instrument, string dsc = null)
		{
			DsrQ03Handler._logger.Info("sampleCode: " + sampleCode.ToString());
			DSR_Q03 dsr_Q = new DSR_Q03();
			Terser terser = new Terser(this._qryMessage);
			string text;
			try
			{
				text = Terser.Get(terser.getSegment("MSH"), 12, 0, 1, 1);
			}
			catch (HL7Exception)
			{
				throw new HL7Exception("Failed to get valid HL7 version from inbound MSH-12-1");
			}
			Terser terser2 = new Terser(dsr_Q);
			DeepCopy.copy(terser.getSegment("MSH"), terser2.getSegment("MSH"));
			DeepCopy.copy(terser.getSegment("QRD"), terser2.getSegment("QRD"));
			DeepCopy.copy(terser.getSegment("QRF"), terser2.getSegment("QRF"));
			string text2 = terser2.Get("/MSH-3");
			string text3 = terser2.Get("/MSH-4");
			terser2.Set("/MSH-3", DsrQ03Handler.CommunicationName);
			terser2.Set("/MSH-4", DsrQ03Handler.EnvironmentIdentifier);
			terser2.Set("/MSH-5", text2);
			terser2.Set("/MSH-6", text3);
			terser2.Set("/MSH-7", TextUtil.Now.ToString("yyyyMMddHHmmss"));
			dsr_Q.MSH.MessageType.MessageType.Value = "DSR";
			dsr_Q.MSH.MessageType.TriggerEvent.Value = "Q03";
			dsr_Q.MSH.MessageControlID.Value = messageControlId;
			dsr_Q.MSH.ApplicationAcknowledgmentType.Value = "";
			terser2.Set("/MSH-12", text);
			terser2.Set("/MSH-16", "0");
			terser2.Set("/MSH-19", "/F/");
			terser2.Set("/MSH-20", "/F/");
			terser2.Set("/MSA-1", "AA");
			terser2.Set("/MSA-2", Terser.Get(terser.getSegment("MSH"), 10, 0, 1, 1));
			terser2.Set("/MSA-3", "Message accepted");
			terser2.Set("/MSA-6", "0");
			terser2.Set("/QRF-9", "/F/");
			terser2.Set("/ERR-1", "0");
			terser2.Set("/QAK-1", "SR");
			terser2.Set("/QAK-2", "OK");
			LaboContext laboContext = new LaboContext();
			Sample sample = laboContext.Sample.FirstOrDefault((Sample x) => x.SampleCode == (long?)sampleCode);
			DsrQ03Handler._logger.Info(string.Format("sid = {0}, patientid = {1}", (sample != null) ? new long?(sample.SampleId) : null, (sample != null) ? new long?(sample.AnalysisRequest.PatientId) : null));
			bool flag = sample != null;
			if (flag)
			{
				for (int i = 0; i < 28; i++)
				{
					Patient patient = sample.AnalysisRequest.Patient;
					int num = i + 1;
					int num2 = num;
					string value;
					if (num2 != 3)
					{
						if (num2 != 4)
						{
							switch (num2)
							{
							case 21:
								value = sample.FormattedSampleCode;
								goto IL_515;
							case 22:
								value = ((this._instrument.Kind == Jihas.MindrayBs200 || this._instrument.Kind == Jihas.MindrayCL1000i) ? sample.SampleCode.ToString() : Helper.ID++.ToString());
								goto IL_515;
							case 23:
								value = TextUtil.GetDate(sample, this._instrument).ToString("yyyyMMddHHmmss");
								goto IL_515;
							case 24:
								value = "N";
								goto IL_515;
							case 26:
							{
								SampleSource sampleSource = sample.SampleSource;
								string text4 = (sampleSource != null) ? sampleSource.SampleTypeName : null;
								value = ((text4 != null && text4.StartsWith("Urine")) ? "Urine" : text4);
								goto IL_515;
							}
							}
							value = "/F/";
						}
						else
						{
                            DateTime? birthDate = patient.PatientDateNaiss;
                            value = birthDate?.ToString("yyyyMMddHHmmss") ?? "";
                        }
					}
					else
					{
						value = string.Concat(new string[]
						{
							sample.AnalysisRequest.AnalysisRequestId2.ToString(),
							"-",
							patient.Nom,
							" ",
							patient.Prenom
						});
					}
					IL_515:
					dsr_Q.GetDSP(i).SetIDDSP.Value = (i + 1).ToString();
					dsr_Q.GetDSP(i).DataLine.Value = value;
					dsr_Q.GetDSP(i).LogicalBreakPoint.Value = "/F/";
					dsr_Q.GetDSP(i).ResultID.Value = "/F/";
				}
				int num3 = 28;
				List<string> tests = AstmHigh.GetTests(sample.SampleId, instrument, false);
				foreach (string text5 in tests)
				{
					bool flag2 = string.IsNullOrEmpty(text5);
					if (!flag2)
					{
						dsr_Q.GetDSP(num3).SetIDDSP.Value = (num3 + 1).ToString();
						dsr_Q.GetDSP(num3).DataLine.Value = text5 + "/S//S//S/";
						dsr_Q.GetDSP(num3).LogicalBreakPoint.Value = "/F/";
						dsr_Q.GetDSP(num3).ResultID.Value = "/F/";
						num3++;
					}
				}
			}
			terser2.Set("/DSC-1", string.IsNullOrEmpty(dsc) ? "/F/" : dsc);
			return dsr_Q;
		}

		private DSR_Q03 _message;

		private static Logger _logger = LogManager.GetCurrentClassLogger();

		private static string _communicationName;

		private static string _environmentIdentifier;

		private Instrument _instrument;

		private QRY_Q02 _qryMessage;
	}
}
