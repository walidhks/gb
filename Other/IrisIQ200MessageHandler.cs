using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Xml;
using GbService.ASTM;
using GbService.Model.Contexts;
using GbService.Model.Domain;

namespace GbService.Other
{
	public class IrisIQ200MessageHandler
	{
		public IrisIQ200MessageHandler(AstmManager manager)
		{
			this._manager = manager;
		}

		public void Parse(string message)
		{
			LaboContext laboContext = new LaboContext();
			this._manager.LogFile.Debug("--RX--: " + message);
			bool flag = message.StartsWith("<IRISPing/>");
			if (flag)
			{
				this._manager.PutMsgInSendingQueue("<IRISPing/>", 0L);
			}
			else
			{
				bool flag2 = message.StartsWith("<SIQ>");
				if (flag2)
				{
					string text = message.Substring(5, message.IndexOf("</", StringComparison.Ordinal) - 5);
					string msg = this.GetOrder(text, laboContext) ?? ("<UNKID>" + text + "</UNKID>");
					this._manager.PutMsgInSendingQueue(msg, 0L);
				}
				else
				{
					XmlDocument xmlDocument = new XmlDocument();
					xmlDocument.LoadXml(message);
					XmlNode xmlNode = xmlDocument.SelectSingleNode("/SA");
					bool flag3 = ((xmlNode != null) ? xmlNode.Attributes : null) == null;
					if (!flag3)
					{
						string value = xmlNode.Attributes["ID"].Value;
						long sampleCode;
						bool flag4 = !long.TryParse(value, out sampleCode);
						if (!flag4)
						{
							Sample sample = laboContext.Sample.FirstOrDefault((Sample s) => s.SampleCode == (long?)sampleCode);
							bool flag5 = sample == null;
							if (!flag5)
							{
								List<AnalysisTypeInstrumentMapping> list = (from m in laboContext.AnalysisTypeInstrumentMappings
								where m.InstrumentCode == this._manager.InstrumentCode
								select m).ToList<AnalysisTypeInstrumentMapping>();
								using (List<AnalysisTypeInstrumentMapping>.Enumerator enumerator = list.GetEnumerator())
								{
									while (enumerator.MoveNext())
									{
										AnalysisTypeInstrumentMapping map = enumerator.Current;
										Analysis analysis = sample.Analysis.FirstOrDefault((Analysis x) => x.AnalysisTypeId == map.AnalysisTypeId);
										bool flag6 = analysis == null;
										if (!flag6)
										{
											XmlNode xmlNode2 = xmlDocument.SelectSingleNode("/SA/AC/AR[@Key = '" + map.AnalysisTypeCode + "']");
											bool flag7 = xmlNode2 == null;
											if (!flag7)
											{
												analysis.ResultTxt = xmlNode2.InnerText;
												analysis.ResultTxt = analysis.ResultTxt.Replace(" /µl", "");
												analysis.ResultTxt = analysis.ResultTxt.Replace("[none]", "::");
												analysis.AnalysisState = AnalysisState.ReçuAutomate;
												analysis.InstrumentId = new int?(this._manager.InstrumentId);
												laboContext.SaveChanges();
											}
										}
									}
								}
							}
						}
					}
				}
			}
		}

		private string GetOrder(string sid, LaboContext db)
		{
			long sampleCode;
			bool flag = !long.TryParse(sid, out sampleCode);
			string result;
			if (flag)
			{
				result = null;
			}
			else
			{
				Sample sample = db.Sample.FirstOrDefault((Sample s) => s.SampleCode == (long?)sampleCode);
				bool flag2 = sample == null;
				if (flag2)
				{
					result = null;
				}
				else
				{
					AnalysisTypeInstrumentMapping map = db.AnalysisTypeInstrumentMappings.FirstOrDefault((AnalysisTypeInstrumentMapping y) => y.InstrumentCode == this._manager.InstrumentCode && y.AnalysisTypeCode == "ECBU");
					bool flag3 = map != null;
					if (flag3)
					{
						Analysis analysis = sample.Analysis.FirstOrDefault((Analysis x) => x.AnalysisTypeId == map.AnalysisTypeId);
						bool flag4 = analysis != null;
						if (flag4)
						{
							analysis.AnalysisState = AnalysisState.EnvoyerAutomate;
							foreach (Analysis analysis2 in analysis.ChildAnalysises)
							{
								analysis2.AnalysisState = AnalysisState.EnvoyerAutomate;
							}
						}
					}
					XmlDocument xmlDocument = new XmlDocument();
					xmlDocument.Load("C:\\BMServices\\log2.xml");
					XmlNode xmlNode = xmlDocument.SelectSingleNode("/SI");
					XmlNode xmlNode2 = xmlNode.ChildNodes[1];
					XmlAttributeCollection attributes = xmlNode2.Attributes;
					xmlNode.ChildNodes[0].Value = sid;
					Patient patient = sample.AnalysisRequest.Patient;
					attributes["LName"].Value = patient.Prenom;
					attributes["FName"].Value = patient.Nom;
					attributes["Gender"].Value = patient.ShortSexe;
					attributes["RecNum"].Value = patient.PatientID.ToString();
					bool flag5 = patient.PatientDateNaiss != null;
					if (flag5)
					{
						attributes["DOB"].Value = patient.PatientDateNaiss.Value.ToString("yyyy-MM-dd");
					}
					xmlDocument.Save("C:\\BMServices\\log2.xml");
					result = File.ReadAllText("C:\\BMServices\\log2.xml");
				}
			}
			return result;
		}

		private AstmManager _manager;
	}
}
