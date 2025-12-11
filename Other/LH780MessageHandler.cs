using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using GbService.ASTM;
using GbService.Communication;
using GbService.Model.Contexts;
using GbService.Model.Domain;
using NLog;

namespace GbService.Other
{
	public class LH780MessageHandler
	{
		public LH780MessageHandler(LH780Manager manager)
		{
			this._manager = manager;
		}

		public void ParseUni(string msg)
		{
			string[] array = msg.Split(new string[]
			{
				string.Format("{0}{1}", '\u0003', '\u0002')
			}, StringSplitOptions.RemoveEmptyEntries);
			string text = "";
			foreach (string text2 in array)
			{
				LH780MessageHandler._logger.Info(text2);
				string text3 = text2.Substring(2).Substring(0, text2.Length - 6);
				LH780MessageHandler._logger.Info(text3);
				text += text3;
			}
			this.Parse(text);
		}

		public void Parse(string msg)
		{
			try
			{
				LH780MessageHandler._logger.Debug("--msg : " + msg);
				string[] source = msg.Split(new char[]
				{
					'\n'
				});
				List<string[]> records = (from s in source
				select s.Split(new char[]
				{
					' '
				})).ToList<string[]>();
				string valueById = this.GetValueById(records, "ID1");
				string valueById2 = this.GetValueById(records, "DATE");
				this.LoadResults(records, valueById, valueById2);
			}
			catch (Exception ex)
			{
				LH780MessageHandler._logger.Error(new LogMessageGenerator(ex.ToString));
			}
		}

		public void LoadResults(List<string[]> records, string id, string analysisDate)
		{
			LaboContext laboContext = new LaboContext();
			LH780MessageHandler._logger.Info<string, string>("looking for sample {0} for the date {1}", id, analysisDate);
			long sampleCode;
			bool flag = !long.TryParse(id, out sampleCode);
			if (!flag)
			{
				Sample sample = laboContext.Sample.FirstOrDefault((Sample s) => s.SampleCode == (long?)sampleCode);
				bool flag2 = sample == null;
				if (flag2)
				{
					LH780MessageHandler._logger.Info("Sample not found!");
				}
				else
				{
					LH780MessageHandler._logger.Info<long?>("Sample {0} found", sample.SampleCode);
					List<AnalysisTypeInstrumentMapping> list = (from m in laboContext.AnalysisTypeInstrumentMappings
					where m.InstrumentCode == this._manager.InstrumentCode
					select m).ToList<AnalysisTypeInstrumentMapping>();
					AnalysisTypeInstrumentMapping cd = laboContext.AnalysisTypeInstrumentMappings.FirstOrDefault((AnalysisTypeInstrumentMapping y) => y.InstrumentCode == this._manager.InstrumentCode && y.AnalysisTypeCode == "CD");
					AnalysisTypeInstrumentMapping cbc = laboContext.AnalysisTypeInstrumentMappings.FirstOrDefault((AnalysisTypeInstrumentMapping y) => y.InstrumentCode == this._manager.InstrumentCode && y.AnalysisTypeCode == "CBC");
					using (List<AnalysisTypeInstrumentMapping>.Enumerator enumerator = list.GetEnumerator())
					{
						while (enumerator.MoveNext())
						{
							AnalysisTypeInstrumentMapping map = enumerator.Current;
							Analysis analysis = sample.Analysis.FirstOrDefault((Analysis x) => x.AnalysisTypeId == map.AnalysisTypeId && x.AnalysisState <= AnalysisState.EnvoyerAutomate);
							string valueById = this.GetValueById(records, map.AnalysisTypeCode);
							bool flag3 = analysis != null;
							if (flag3)
							{
								LH780MessageHandler._logger.Info("value  {0}", valueById);
								bool flag4 = !string.IsNullOrWhiteSpace(valueById);
								if (flag4)
								{
									analysis.ResultTxt = valueById.ToString(new CultureInfo("en-US"));
									decimal num;
									bool flag5 = decimal.TryParse(valueById, NumberStyles.Any, new CultureInfo("en-US"), out num);
									if (flag5)
									{
										analysis.AnalysisState = AnalysisState.ReçuAutomate;
									}
								}
								analysis.InstrumentId = new int?(this._manager.InstrumentId);
							}
							else
							{
								LH780MessageHandler._logger.Info<string, string>("analysis {0} not found {1}", map.AnalysisTypeCode, valueById);
							}
						}
					}
					Analysis analysis2 = sample.Analysis.FirstOrDefault(delegate(Analysis x)
					{
						bool result;
						if (x.AnalysisTypeId == cbc.AnalysisTypeId && x.AnalysisState == AnalysisState.EnvoyerAutomate)
						{
							int? instrumentId = x.InstrumentId;
							int instrumentId2 = this._manager.InstrumentId;
							result = (instrumentId.GetValueOrDefault() == instrumentId2 & instrumentId != null);
						}
						else
						{
							result = false;
						}
						return result;
					});
					bool flag6;
					if (analysis2 != null)
					{
						flag6 = analysis2.ChildAnalysises.Any((Analysis x) => x.AnalysisState == AnalysisState.ReçuAutomate);
					}
					else
					{
						flag6 = false;
					}
					bool flag7 = flag6;
					if (flag7)
					{
						analysis2.AnalysisState = AnalysisState.ReçuAutomate;
					}
					Analysis analysis3 = sample.Analysis.FirstOrDefault(delegate(Analysis x)
					{
						bool result;
						if (x.AnalysisTypeId == cd.AnalysisTypeId && x.AnalysisState == AnalysisState.EnvoyerAutomate)
						{
							int? instrumentId = x.InstrumentId;
							int instrumentId2 = this._manager.InstrumentId;
							result = (instrumentId.GetValueOrDefault() == instrumentId2 & instrumentId != null);
						}
						else
						{
							result = false;
						}
						return result;
					});
					bool flag8;
					if (analysis3 != null)
					{
						flag8 = analysis3.ChildAnalysises.Any((Analysis x) => x.AnalysisState == AnalysisState.ReçuAutomate);
					}
					else
					{
						flag8 = false;
					}
					bool flag9 = flag8;
					if (flag9)
					{
						analysis3.AnalysisState = AnalysisState.ReçuAutomate;
					}
					laboContext.SaveChanges();
					LH780MessageHandler._logger.Info("Results loaded successfuly!");
				}
			}
		}

		private string GetValueById(List<string[]> records, string id)
		{
			string[] array = records.FirstOrDefault((string[] x) => x.First<string>() == id);
			return (array != null) ? array[1] : null;
		}

        public void Upload()
        {
            LaboContext laboContext = new LaboContext();
            DateTime now = DateTime.Now.Date.AddDays(-7);

            var sampleIds = laboContext.Analysis
                .Where(x => x.CreatedDate > now &&
                            x.AnalysisState == AnalysisState.EnCours &&
                            x.InstrumentId == this._manager.InstrumentId)
                .Select(x => x.SampleId)
                .Distinct()
                .ToList();

            var cdMapping = laboContext.AnalysisTypeInstrumentMappings
                .FirstOrDefault(y => y.InstrumentCode == this._manager.InstrumentCode &&
                                     y.AnalysisTypeCode == "CD");

            var cbcMapping = laboContext.AnalysisTypeInstrumentMappings
                .FirstOrDefault(y => y.InstrumentCode == this._manager.InstrumentCode &&
                                     y.AnalysisTypeCode == "CBC");

            try
            {
                foreach (long? sampleId in sampleIds)
                {
                    Sample sample = laboContext.Sample.Find(sampleId);
                    if (sample == null) continue;

                    string orderType = null;

                    // Handle CBC
                    if (cbcMapping != null)
                    {
                        var cbcAnalysis = sample.Analysis.FirstOrDefault(x =>
                            x.AnalysisTypeId == cbcMapping.AnalysisTypeId &&
                            x.AnalysisState == AnalysisState.EnCours &&
                            x.InstrumentId.HasValue &&
                            x.InstrumentId.Value == this._manager.InstrumentId
                        );

                        if (cbcAnalysis != null)
                        {
                            orderType = "CBC";
                            cbcAnalysis.AnalysisState = AnalysisState.EnvoyerAutomate;
                            cbcAnalysis.AnalysisStateChangeDate = DateTime.Now;

                            foreach (var child in cbcAnalysis.ChildAnalysises)
                            {
                                child.AnalysisState = AnalysisState.EnvoyerAutomate;
                            }
                        }
                    }
                    else
                    {
                        LH780MessageHandler._logger.Error("No Fns Found");
                    }

                    // Handle CD
                    if (cdMapping != null)
                    {
                        var cdAnalysis = sample.Analysis.FirstOrDefault(x =>
                            x.AnalysisTypeId == cdMapping.AnalysisTypeId &&
                            x.AnalysisState == AnalysisState.EnCours &&
                            x.InstrumentId.HasValue &&
                            x.InstrumentId.Value == this._manager.InstrumentId
                        );

                        if (cdAnalysis != null)
                        {
                            orderType = "DIFF";
                            cdAnalysis.AnalysisState = AnalysisState.EnvoyerAutomate;
                            cdAnalysis.AnalysisStateChangeDate = DateTime.Now;

                            foreach (var child in cdAnalysis.ChildAnalysises)
                            {
                                child.AnalysisState = AnalysisState.EnvoyerAutomate;
                            }
                        }
                    }
                    else
                    {
                        LH780MessageHandler._logger.Error("No EQ Found");
                    }

                    if (orderType != null)
                    {
                        this.OrderSample(sample, orderType);
                    }

                    laboContext.SaveChanges();
                }
            }
            catch (Exception ex)
            {
                LH780MessageHandler._logger.Error(ex.StackTrace);
            }
        }


        private void OrderSample(Sample sample, string test)
		{
			Patient patient = sample.AnalysisRequest.Patient;
			string formattedSampleCode = sample.FormattedSampleCode;
			List<string> list = new List<string>();
			list.Add("09");
			list.Add("WLAD");
			list.Add("ID " + patient.PatientID.ToString("D16"));
			list.Add("PL " + patient.Nom.Trim().Truncate(16));
			list.Add("PF " + patient.Prenom.Trim().Truncate(16));
			List<string> list2 = list;
			string str = "SX ";
			SexeEnum? patientSexe = patient.PatientSexe;
			SexeEnum sexeEnum = SexeEnum.Masculin;
			list2.Add(str + ((patientSexe.GetValueOrDefault() == sexeEnum & patientSexe != null) ? "M" : "F"));
			list.Add("LN A");
			list.Add(string.Concat(new string[]
			{
				"TS ",
				test,
				",",
				formattedSampleCode,
				","
			}));
			list.Add("SM S\n");
			List<string> list3 = list;
			bool flag = patient.PatientDateNaiss != null;
			if (flag)
			{
				list3.Insert(5, "BD " + patient.PatientDateNaiss.Value.ToShortDateString());
			}
			string msg = list3.Aggregate((string i, string j) => i + "\n" + j);
			this._manager.PutMsgInSendingQueue(msg, 0L);
		}

		private static Logger _logger = LogManager.GetCurrentClassLogger();

		private AstmManager _manager;
	}
}
