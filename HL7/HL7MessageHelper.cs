using System;
using System.Configuration;
using System.Text;
using GbService.Communication;
using NHapi.Base;
using NHapi.Base.Model;
using NHapi.Base.Util;
using NHapi.Model.V23.Message;
using NHapi.Model.V231.Message;
using NHapi.Model.V24.Message;
using NHapi.Model.V25.Message;

namespace GbService.HL7
{
	public abstract class HL7MessageHelper
	{
		private static string CommunicationName
		{
			get
			{
				string result;
				if ((result = HL7MessageHelper.communicationName) == null)
				{
					result = (HL7MessageHelper.communicationName = ConfigurationSettings.AppSettings["ApplicationCommunicationName"]);
				}
				return result;
			}
		}

		private static string EnvironmentIdentifier
		{
			get
			{
				string result;
				if ((result = HL7MessageHelper.environmentIdentifier) == null)
				{
					result = (HL7MessageHelper.environmentIdentifier = ConfigurationSettings.AppSettings["EnvironmentIdentifier"]);
				}
				return result;
			}
		}

		public static IMessage MakeACK(IMessage inboundMessage, string ackCode, string errorMessage, string ver)
		{
			bool flag = ver == "2.3.1";
			IMessage message;
			if (flag)
			{
				message = new NHapi.Model.V231.Message.ACK();
			}
			else
			{
				bool flag2 = ver == "2.3";
				if (flag2)
				{
					message = new NHapi.Model.V23.Message.ACK();
				}
				else
				{
					bool flag3 = ver == "2.4";
					if (flag3)
					{
						message = new NHapi.Model.V24.Message.ACK();
					}
					else
					{
						bool flag4 = ver == "2.5";
						if (!flag4)
						{
							throw new NotImplementedException();
						}
						message = new NHapi.Model.V25.Message.ACK();
					}
				}
			}
			Terser terser = new Terser(inboundMessage);
			ISegment segment = null;
			segment = terser.getSegment("MSH");
			string text;
			try
			{
				text = Terser.Get(segment, 12, 0, 1, 1);
			}
			catch (HL7Exception)
			{
				throw new HL7Exception("Failed to get valid HL7 version from inbound MSH-12-1");
			}
			Terser terser2 = new Terser(message);
			ISegment segment2 = terser2.getSegment("MSH");
			DeepCopy.copy(segment, segment2);
			string text2 = terser2.Get("/MSH-3");
			string text3 = terser2.Get("/MSH-4");
			terser2.Set("/MSH-3", HL7MessageHelper.CommunicationName);
			terser2.Set("/MSH-4", HL7MessageHelper.EnvironmentIdentifier);
			terser2.Set("/MSH-5", text2);
			terser2.Set("/MSH-6", text3);
			terser2.Set("/MSH-7", TextUtil.Now.ToString("yyyyMMddmmhh"));
			terser2.Set("/MSH-9", "ACK");
			terser2.Set("/MSH-12", text);
			terser2.Set("/MSH-20", "");
			terser2.Set("/MSH-21", " ");
			terser2.Set("/MSH-22", null);
			terser2.Set("/MSA-1", ackCode ?? "AA");
			terser2.Set("/MSA-2", Terser.Get(segment, 10, 0, 1, 1));
			terser2.Set("/MSA-3", "Message accepted");
			terser2.Set("/MSA-4", null);
			terser2.Set("/MSA-5", null);
			terser2.Set("/MSA-6", "0");
			bool flag5 = errorMessage != null;
			if (flag5)
			{
				terser2.Set("/ERR-7", errorMessage);
			}
			return message;
		}

		public static string FormatMessage(string message)
		{
			StringBuilder stringBuilder = new StringBuilder(message);
			stringBuilder.Remove(0, message.IndexOf("MSH"));
			stringBuilder.Replace("\r", "");
			stringBuilder.Replace("\n", "");
			stringBuilder.Replace("PID|", "\rPID|");
			stringBuilder.Replace("OBR|", "\rOBR|");
			stringBuilder.Replace("OBX|", "\rOBX|");
			stringBuilder.Replace("ORC|", "\rORC|");
			stringBuilder.Replace("NTE|", "\rNTE|");
			return stringBuilder.ToString();
		}

		private static string communicationName;

		private static string environmentIdentifier;
	}
}
