using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using GbService.ASTM;
using GbService.Model.Domain;
using NLog;

namespace GbService.Communication
{
	internal static class HttpServer
	{
		private static string GetRequestPostData(HttpListenerRequest request)
		{
			bool flag = !request.HasEntityBody;
			string result;
			if (flag)
			{
				result = null;
			}
			else
			{
				using (Stream inputStream = request.InputStream)
				{
					using (StreamReader streamReader = new StreamReader(inputStream, request.ContentEncoding))
					{
						result = streamReader.ReadToEnd();
					}
				}
			}
			return result;
		}

		public static void Handle(string url)
		{
			HttpListener httpListener = new HttpListener();
			httpListener.Prefixes.Add(url);
			httpListener.Start();
			HttpServer._log.Info("Listening for connections on " + url);
			for (;;)
			{
				try
				{
					HttpListenerContext context = httpListener.GetContext();
					HttpListenerRequest request = context.Request;
					HttpListenerResponse response = context.Response;
					HttpServer._log.Info(string.Format("{0} {1}", request.Url, request.ContentType));
					string text = HttpServer.GetRequestPostData(request).Replace("<ROOT><DATAPARAM><PatID>", "").Replace("</PatID></DATAPARAM></ROOT>", "").Replace("$-", "").Replace("é", "");
					long? num = TextUtil.Long(text);
					HttpServer._log.Info(string.Format("id = {0}, arid = {1}", text, num));
					bool flag = num == null;
					if (!flag)
					{
						List<Sample> samplesYonder = AstmHigh.GetSamplesYonder(num.Value);
						string str = samplesYonder.Aggregate("", (string c, Sample x) => c + HttpServer.GetSample(x));
						string text2 = "<DATAPARAM><![CDATA[<?XML VERSION=\"1.0\" ENCODING2312=\"GB2312\"?><CALLSYSINTERFACE><INPATINFOS>" + str + "</INPATINFOS></CALLSYSINTERFACE>]]></DATAPARAM>";
						byte[] bytes = Encoding.UTF8.GetBytes(text2);
						response.ContentType = "application/xml";
						response.ContentEncoding = Encoding.UTF8;
						response.ContentLength64 = (long)bytes.Length;
						response.OutputStream.Write(bytes, 0, bytes.Length);
						HttpServer._log.Info(text2);
						response.Close();
					}
				}
				catch (Exception ex)
				{
					HttpServer._log.Info<Exception>(ex);
				}
			}
		}

		private static string GetSample(Sample sample)
		{
			string formattedSampleCode = sample.FormattedSampleCode;
			Patient patient = sample.AnalysisRequest.Patient;
			string text = string.Join(", ", from a in sample.Analysis
			where a.ParentAnalysisId == null || a.AnalysisType.WaitingTime > TimeSpan.Zero || a.AnalysisType.SamplingHoure > 0
			select a into x
			select x.AnalysisType.AnalysisTypeShortName);
			string text2 = TextUtil.RemoveDiacritics(sample.TubeType.TubeTypeName).Replace("/", " ");
			string text3 = string.Concat(new string[]
			{
				"<INPATINFO><REQUESTID>",
				formattedSampleCode,
				"</REQUESTID><PATIENTID>",
				string.Format("{0}</PATIENTID><NAME>{1}</NAME><AGE>{2}</AGE><SEX>{3}</SEX><WARD>Sadelaoud Lab</WARD><WARDID></WARDID><BEDNO></BEDNO>", new object[]
				{
					sample.AnalysisRequestId,
					patient.PatientNomPrenom,
					patient.Age,
					patient.PatientSexe
				}),
				"<TUBECONVERT>",
				sample.TubeType.Color.ToUpper(),
				"</TUBECONVERT><BARCODELABEL></BARCODELABEL><MNEMOTESTS>",
				text,
				"</MNEMOTESTS><NOTELABEL></NOTELABEL><STATE>0</STATE><STATION>A</STATION><DATEBZ></DATEBZ><DESCBZ></DESCBZ><RESERVE1>",
				text2,
				"</RESERVE1><RESERVE2></RESERVE2><RESERVE3></RESERVE3><RESERVE4></RESERVE4><RESERVE5></RESERVE5></INPATINFO>"
			});
			HttpServer._log.Info(text3);
			return text3;
		}

		private static Logger _log = LogManager.GetCurrentClassLogger();
	}
}
