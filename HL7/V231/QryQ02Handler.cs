using System;
using System.Configuration;
using System.Linq;
using GbService.Communication;
using GbService.Model.Contexts;
using GbService.Model.Domain;
using NHapi.Base;
using NHapi.Base.Model;
using NHapi.Base.Parser;
using NHapi.Base.Util;
using NHapi.Model.V231.Datatype;
using NHapi.Model.V231.Message;

namespace GbService.HL7.V231
{
	public class QryQ02Handler : IParser
	{
		private static string CommunicationName
		{
			get
			{
				bool flag = QryQ02Handler.communicationName == null;
				if (flag)
				{
					QryQ02Handler.communicationName = ConfigurationSettings.AppSettings["ApplicationCommunicationName"];
				}
				return QryQ02Handler.communicationName;
			}
		}

		private static string EnvironmentIdentifier
		{
			get
			{
				bool flag = QryQ02Handler.environmentIdentifier == null;
				if (flag)
				{
					QryQ02Handler.environmentIdentifier = ConfigurationSettings.AppSettings["EnvironmentIdentifier"];
				}
				return QryQ02Handler.environmentIdentifier;
			}
		}

		public QryQ02Handler(IMessage message)
		{
			this._message = (QRY_Q02)message;
		}

		public void SetMessage(IMessage msg)
		{
			this._message = (QRY_Q02)msg;
		}

		public string Parse()
		{
			bool flag = this._message.QRD.GetWhoSubjectFilter().Any<XCN>();
			if (flag)
			{
				string value = this._message.QRD.GetWhoSubjectFilter()[0].IDNumber.Value;
				long.TryParse(value, out this.SampleCode);
			}
			else
			{
				this.IsAllQuery = true;
				this.StartDateTime = this._message.QRF.WhenDataStartDateTime.TimeOfAnEvent.GetAsDate();
				this.EndDateTime = this._message.QRF.WhenDataEndDateTime.TimeOfAnEvent.GetAsDate();
			}
			bool flag2 = this.IsSampleExist() || this.IsAllQuery;
			string result;
			if (flag2)
			{
				result = this.CreateAckMessage();
			}
			else
			{
				result = this.CreateNackMessage();
			}
			return result;
		}

		private string CreateNackMessage()
		{
			QCK_Q02 qck_Q = this.MakeQryACK(this._message, "NF");
			PipeParser pipeParser = new PipeParser();
			string text = pipeParser.Encode(qck_Q);
			string str = text.Split(new char[]
			{
				Convert.ToChar("\r")
			})[0] + "|||\r";
			string str2 = text.Split(new char[]
			{
				Convert.ToChar("\r")
			})[1] + "|\r";
			string str3 = text.Split(new char[]
			{
				Convert.ToChar("\r")
			})[2] + "|\r";
			string str4 = text.Split(new char[]
			{
				Convert.ToChar("\r")
			})[3] + "|\r";
			return str + str2 + str3 + str4;
		}

		public bool IsSampleExist()
		{
			bool result;
			using (LaboContext laboContext = new LaboContext())
			{
				bool flag = this.SampleCode == 0L;
				if (flag)
				{
					result = false;
				}
				else
				{
					result = (laboContext.Sample.FirstOrDefault((Sample s) => s.SampleCode == (long?)this.SampleCode) != null);
				}
			}
			return result;
		}

		private string CreateAckMessage()
		{
			QCK_Q02 qck_Q = this.MakeQryACK(this._message, "OK");
			PipeParser pipeParser = new PipeParser();
			string text = pipeParser.Encode(qck_Q);
			return text.Replace("\r", "|\r").Replace("/F/", "");
		}

		public QCK_Q02 MakeQryACK(IMessage inboundMessage, string queryResponseStatus)
		{
			QCK_Q02 qck_Q = new QCK_Q02();
			Terser terser = new Terser(inboundMessage);
			ISegment segment = null;
			segment = terser.getSegment("MSH");
			string text = null;
			try
			{
				text = Terser.Get(segment, 12, 0, 1, 1);
			}
			catch (HL7Exception)
			{
				throw new HL7Exception("Failed to get valid HL7 version from inbound MSH-12-1");
			}
			Terser terser2 = new Terser(qck_Q);
			ISegment segment2 = terser2.getSegment("MSH");
			DeepCopy.copy(segment, segment2);
			string text2 = terser2.Get("/MSH-3");
			string text3 = terser2.Get("/MSH-4");
			terser2.Set("/MSH-3", QryQ02Handler.CommunicationName);
			terser2.Set("/MSH-4", QryQ02Handler.EnvironmentIdentifier);
			terser2.Set("/MSH-5", text2);
			terser2.Set("/MSH-6", text3);
			terser2.Set("/MSH-7", TextUtil.Now.ToString("yyyyMMddmmhh"));
			terser2.Set("/MSH-9", "QCK");
			qck_Q.MSH.ApplicationAcknowledgmentType.Value = "";
			terser2.Set("/MSH-12", text);
			terser2.Set("/MSH-19", "/F/");
			terser2.Set("/MSH-20", "/F/");
			terser2.Set("/MSA-1", "AA");
			terser2.Set("/MSA-2", Terser.Get(segment, 10, 0, 1, 1));
			terser2.Set("/MSA-3", "Message accepted");
			terser2.Set("/MSA-6", "0");
			terser2.Set("/ERR-1", "0");
			terser2.Set("/QAK-1", "SR");
			terser2.Set("/QAK-2", queryResponseStatus);
			return qck_Q;
		}

		protected static string GetVariesValue(Varies value)
		{
			string result = string.Empty;
			bool flag = value != null;
			if (flag)
			{
				IType data = value.Data;
				bool flag2 = data != null;
				if (flag2)
				{
					result = data.ToString();
				}
			}
			return result;
		}

		private static string communicationName;

		private static string environmentIdentifier;

		private QRY_Q02 _message;

		public long SampleCode;

		public bool IsAllQuery;

		public DateTime StartDateTime;

		public DateTime EndDateTime;
	}
}
