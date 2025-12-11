using System;
using System.Collections.Generic;
using System.Data.Entity.Infrastructure;
using System.Data.Entity.Validation;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using GbService.Common;
using GbService.Communication;
using GbService.Communication.Common;
using GbService.HL7;
using GbService.HL7.V231;
using GbService.Model.Contexts;
using GbService.Model.Domain;
using GbService.Other;
using NLog;
using NPOI.SS.Formula.Functions;

namespace GbService.ASTM
{
    public class AstmHigh
    {
        public static List<string> Messages(string msg)
        {
            string[] array = msg.Split(new string[]
            {
                "\rH"
            }, StringSplitOptions.RemoveEmptyEntries);
            for (int i = 1; i < array.Length; i++)
            {
                array[i] = "H" + array[i];
            }
            return array.ToList<string>();
        }

        public static bool IsRequest(string msg)
        {
            int num = msg.IndexOf('\r');
            return msg[num + 1] == 'Q';
        }

        public static void Parse(AstmManager manager, string msg)
        {
            msg = msg.Replace("\r\r", '\r'.ToString());
            AstmHigh._logger.Debug(msg);
            // ...
            if (manager.Kind == Jihas.Autobio)
            {
                // ONLY redirect Queries to Upload
                if (msg.Contains("|REQ1|") || msg.Contains("|REQ9|") || msg.Contains("|REQ15|"))
                {
                    AstmHigh.Upload(manager, msg);
                    return;
                }
                // Results (REQ5/REQ7) should NOT return here. They must continue to the list splitting below.
            }
            try
            {
                bool flag = manager.Kind == Jihas.Advia1800;
                if (flag)
                {
                    AstmHigh.ParseAdvia(msg, manager);
                }
                else
                {
                    List<string> list = AstmHigh.Messages(msg);
                    foreach (string msg2 in list)
                    {
                        /* bool flag2 = AstmHigh.IsRequest(msg2);
                         if (flag2)
                         {
                             AstmHigh.Upload(manager, msg2);
                         }
                         else
                         {
                             AstmHigh.ParseResult(msg2, manager.Instrument);
                         }*/
                        if (AstmHigh.IsRequest(msg2)) AstmHigh.Upload(manager, msg2);
                        else AstmHigh.ParseResult(msg2, manager.Instrument); // This parses Results
                    }
                }
            }
            catch (Exception @object)
            {
                AstmHigh._logger.Error(new LogMessageGenerator(@object.ToString));
            }
        }

        public static List<JihazResult> ParseResult(string msg, Instrument instrument)
        {
            ASTM_Message message = Parser.Parse(msg);
            return AstmHigh.HandleResult(message, instrument);
        }

        public static void Upload(AstmManager manager, string msg = null)
        {
            string path = "c:\\test.txt";
            bool flag = File.Exists(path);
            if (flag)
            {
                string text = File.ReadAllText(path);
                text = Ct.Prepare(text, true);
                manager.PutMsgInSendingQueue(text, 0L);
                File.Delete(path);
            }
            else
            {
                try
                {
                    List<string> list = AstmHigh.Query(manager.Instrument, msg, false);
                    foreach (string msg2 in list)
                    {
                        manager.PutMsgInSendingQueue(msg2, 0L);
                    }
                }
                catch (Exception @object)
                {
                    AstmHigh._logger.Error(new LogMessageGenerator(@object.ToString));
                }
            }
        }

        public static void Request(AstmManager manager)
        {
            DateTime dateTime = AstmHigh._endTime.AddSeconds(-5.0);
            AstmHigh._endTime = DateTime.Now;
            string msg = string.Format("H|\\^&|<CR>Q|1|ALL||ALL||{0:yyyyMMddHHmmss}|{1:yyyyMMddHHmmss}<CR>L|1|N<CR>", dateTime, AstmHigh._endTime);
            manager.PutMsgInSendingQueue(msg, 0L);
        }

        public static List<string> Query(Instrument instrument, string msg, bool test = false)
        {
            AstmHigh._logger.Info("h " + msg);
            bool flag = msg == null;
            List<string> list;
            if (flag)
            {
                list = AstmHigh.OrderSamples(AstmHigh.GetSamples(instrument, null), instrument, null, test);
            }
            else
            {
                ASTM_Message astm_Message = Parser.Parse(msg);
                List<ASTM_Request> list2 = astm_Message.requestRecords.ToList<ASTM_Request>();
                bool flag2 = list2.Count == 0;
                if (flag2)
                {
                    return new List<string>();
                }
                List<Sample> samplesList = AstmHigh.GetSamples(instrument, list2);
                if (test)
                {
                    samplesList = new List<Sample>
                    {
                        Ct.Sample(instrument.InstrumentCode)
                    };
                }
                list = AstmHigh.OrderSamples(samplesList, instrument, astm_Message, test);
            }
            AstmHigh._logger.Info<int>(list.Count);
            return (from x in list
                    select x.Replace("<CR>", '\r'.ToString()).Replace(Tu.NL, '\r'.ToString())).ToList<string>();
        }

        public static List<Sample> GetSamplesYonder(long arid)
        {
            LaboContext laboContext = new LaboContext();
            List<Sample> list = (from x in laboContext.Sample
                                 where x.AnalysisRequestId == (long?)arid && (int)x.AnalysisRequest.AnalysisRequestState != 3 && x.TubeType.Color != null
                                 select x).ToList<Sample>();
            AstmHigh._logger.Info(string.Format("count= {0}", list.Count));
            return list;
        }

        public static List<Sample> GetSamplesRange(DateTime start, DateTime end, LaboContext db, Instrument instrument)
        {
            AstmHigh._logger.Info(string.Format("start={0}, end={1}, Std={2}, DataBits={3}", new object[]
            {
                start,
                end,
                instrument.InstrumentStd,
                instrument.InstrumentDataBits
            }));
            start = start.AddDays((double)(-(double)instrument.InstrumentDays));
            end = end.AddDays((double)(-(double)instrument.InstrumentDays));
            List<Sample> list = db.Sample.SqlQuery("EXEC dbo.GetSamplesRange \r\n        @StartDate = {0}, @EndDate = {1}, @InstrumentId = {2},\r\n        @InstrumentStd = {3}, @InstrumentDataBits = {4}, @InstrumentDays = {5},\r\n        @InstrumentCode = {6}, @InstrumentKind = {7}, @InstrumentMode = {8}", new object[]
            {
                start,
                end,
                instrument.InstrumentId,
                instrument.InstrumentStd,
                instrument.InstrumentDataBits,
                instrument.InstrumentDays,
                instrument.InstrumentCode,
                instrument.Kind,
                instrument.Mode
            }).ToList<Sample>();
            AstmHigh._logger.Info("count samples v2 = " + list.Count.ToString());
            return list;
        }

        public static List<Sample> GetSamplesRange1(DateTime start, DateTime end, LaboContext db, Instrument instrument)
        {
            start = start.AddDays((double)(-(double)instrument.InstrumentDays));
            end = end.AddDays((double)(-(double)instrument.InstrumentDays));
            AstmHigh._logger.Info(string.Format("start={0}, end={1}, Std={2}, DataBits={3}", new object[]
            {
                start,
                end,
                instrument.InstrumentStd,
                instrument.InstrumentDataBits
            }));
            string instrumentDataBits = instrument.InstrumentDataBits;
            AnalysisState state = (instrumentDataBits == "C") ? AnalysisState.ÀEnvoyer : AnalysisState.EnCours;
            IQueryable<Analysis> queryable = from x in db.Analysis
                                             where x.CreatedDate >= start && x.CreatedDate <= end && (int)x.AnalysisState == (int)state && (int)x.AnalysisRequest.AnalysisRequestState != 3
                                             select x;
            queryable = ((instrument.InstrumentStd == null) ? (from x in queryable
                                                               where x.InstrumentId == (int?)instrument.InstrumentId
                                                               select x) : queryable);
            AstmHigh._logger.Info(string.Format("count q0= {0}", queryable.Count<Analysis>()));
            IQueryable<Analysis> source = (instrument.Kind == Jihas.Navify && instrument.Mode == 1) ? (from x in queryable
                                                                                                       where x.AnalysisStatusId != x.InstrumentId
                                                                                                       select x) : queryable.Where((Analysis x) => x.AnalysisType.AnalysisTypeInstrumentMappings.Any((AnalysisTypeInstrumentMapping mp) => mp.InstrumentCode == instrument.InstrumentCode && !(mp.AnalysisTypeCode == null || mp.AnalysisTypeCode.Trim() == string.Empty)));
            AstmHigh._logger.Info(string.Format("count q1= {0}", source.Count<Analysis>()));
            IQueryable<Sample> queryable2 = from x in (from x in source
                                                       select x.Sample).Distinct<Sample>()
                                            where x.SampleCode != null
                                            select x;
            List<Sample> list = ((instrumentDataBits == "D") ? (from x in queryable2
                                                                where x.DateReceived != null
                                                                select x) : queryable2).Take(20).ToList<Sample>();
            AstmHigh._logger.Info("count samples= " + list.Count.ToString());
            return list;
        }

        public static List<JihazResult> HandleResult(ASTM_Message message, Instrument instrument)
        {
            char repeater = instrument.Prop.Repeater;
            List<JihazResult> result = AstmHigh.GetResult(message, instrument, repeater);
            foreach (JihazResult jr in result)
            {
                AstmHigh.LoadResults(jr, instrument, new char?(repeater));
            }
            return result;
        }

        public static List<JihazResult> GetResult(ASTM_Message message, Instrument instrument, char repeater)
        {
            bool flag = message == null;
            List<JihazResult> result;
            if (flag)
            {
                result = null;
            }
            else
            {
                List<AstmOrder> list = message.patientRecords.SelectMany((ASTM_Patient x) => x.OrderRecords).ToList<AstmOrder>();
                Jihas kind = instrument.Kind;
                
                bool flag2 = kind == Jihas.Gemini || kind == Jihas.Huma200 || kind == Jihas.Euro || kind == Jihas.ABL800;
                if (flag2)
                {
                    foreach (AstmOrder astmOrder in list)
                    {
                        astmOrder.SampleID = astmOrder.Patient.f_laboratory_id;
                    }
                }
                bool flag3 = (kind == Jihas.Biolis24i && instrument.Mode < 3) || kind == Jihas.V8;
                if (flag3)
                {
                    foreach (AstmOrder astmOrder2 in list)
                    {
                        astmOrder2.SampleID = astmOrder2.Patient.f_practice_id;
                    }
                }
                List<JihazResult> list2 = new List<JihazResult>();
                foreach (AstmOrder astm in from x in list
                                           where x.ResultRecords.Count > 0
                                           select x)
                {
                    JihazResult result2 = AstmHigh.GetResult(astm, instrument, repeater);
                    JihazResult item = AstmHigh.Handle(result2, kind);
                    list2.Add(item);
                }
                result = list2;
            }
            return result;
        }

        public static List<string> OrderSamples(List<Sample> samplesList, Instrument instrument, ASTM_Message message = null, bool test = false)
        {
            List<Sample> list = (from x in samplesList
                                 where x != null
                                 select x).ToList<Sample>();
            AstmHigh._logger.Info("samples.Count = " + list.Count.ToString());
            Jihas kind = instrument.Kind;
            List<string> list2 = new List<string>();
            bool flag = kind == Jihas.Bioflash;
            if (flag)
            {
                ASTM_Message astm_Message = AstmHigh.EncodeSamplesBioflash(list, instrument);
                bool flag2 = astm_Message != null;
                if (flag2)
                {
                    list2.Add(astm_Message.EncodeMessage(0, 0, 0));
                }
            }
            else
            {
                bool flag3 = kind == Jihas.BA200_BA400;
                if (flag3)
                {
                    list2.Add(AstmHigh.EncodeSamplesBa200(list, message, instrument));
                }
                //
                //
                else if (kind == Jihas.Autobio) //El3alouche Kind
                {
                    // Extract timestamp from the request to echo it back 
                    string reqDate = (message != null) ? message.f_timestamp : "";
                    string reqCmd = (message != null) ? message.f_processing_id : "";

                    string autobioMsg = AstmHigh.EncodeSamplesAutobio(list, instrument, reqDate, reqCmd);
                    if (!string.IsNullOrEmpty(autobioMsg)) list2.Add(autobioMsg);
                }

                else
                {
                    bool flag4 = Gb.Bid.Contains(kind) || (kind == Jihas.Maglumi && instrument.Mode == 1);
                    if (flag4)
                    {
                        bool flag5 = !instrument.B1;
                        if (flag5)
                        {
                            return list2;
                        }
                        foreach (Sample sample in list)
                        {
                            string text = AstmHigh.EncodeSample(sample, instrument, message, test);
                            bool flag6 = text != null;
                            if (flag6)
                            {
                                list2.Add(text);
                            }
                        }
                    }
                    else
                    {
                        foreach (Sample sample2 in list)
                        {
                            AstmHigh._logger.Info("sampleid hh = " + sample2.SampleId.ToString());
                            ASTM_Message astm_Message2 = AstmHigh.EncodeSampleOld(sample2, instrument);
                            bool flag7 = astm_Message2 == null;
                            if (flag7)
                            {
                                AstmHigh._logger.Info("res == null");
                            }
                            else
                            {
                                bool flag8 = kind == Jihas.Acl;
                                string text2;
                                if (flag8)
                                {
                                    text2 = astm_Message2.EncodeMessage(2, 35, 32);
                                }
                                else
                                {
                                    bool flag9 = kind == Jihas.VitrosEciq;
                                    if (flag9)
                                    {
                                        text2 = astm_Message2.EncodeMessage(1, 0, 0);
                                    }
                                    else
                                    {
                                        text2 = astm_Message2.EncodeMessage(0, 0, 0);
                                    }
                                }
                                AstmHigh._logger.Debug(text2);
                                list2.Add(text2);
                            }
                        }
                    }
                }
            }
            return list2;
        }
        // ---------------------------------------------------------//
        // Custom Encoder for Autobio 3alouche Made in bladi                          //
        // ---------------------------------------------------------//
        private static string EncodeSamplesAutobio(List<Sample> samples, Instrument instrument, string reqDate, string reqCmd)
        {
            if (samples == null || samples.Count == 0) return null;

            //Use the exact date from the request to satisfy the instrument
            string timeToUse = !string.IsNullOrEmpty(reqDate) ? reqDate : DateTime.Now.ToString("yyyyMMddHHmmss");

            // Determine Response Code (REQ1->RSP1, REQ15->RSP15)
            string responseCmd = "RSP1";
            if (!string.IsNullOrEmpty(reqCmd) && reqCmd.StartsWith("REQ"))
            {
                responseCmd = reqCmd.Replace("REQ", "RSP");
            }

            string msg = $"H|\\^&|||BMLIS||0|||||{responseCmd}|1394-97|{timeToUse}\r";

            int seq = 1;
            foreach (Sample sample in samples)
            {
                Patient p = sample.AnalysisRequest.Patient;
                string barcode = sample.FormattedSampleCode;

                // Calculate Age (Field 8)
                string age = "0";
                if (p.PatientDateNaiss.HasValue)
                {
                    int ageVal = DateTime.Now.Year - p.PatientDateNaiss.Value.Year;
                    if (DateTime.Now < p.PatientDateNaiss.Value.AddYears(ageVal)) ageVal--;
                    age = ageVal.ToString();
                    if (age.Length > 3) age = "0";
                }

                string sex = (p.ShortSexe == "M") ? "M" : (p.ShortSexe == "F" ? "F" : "U");

                // 2. Patient Record
                msg += $"P|{seq}||||{p.Nom}^{p.Prenom}|||{sex}||||||||||||||||gynecology^ward1^bed1|||||||||\r";

                string priority = sample.AnalysisRequest.IsEmergency ? "A" : "R";
                string specimen = "0"; // Serum
                if (sample.SampleSource.SampleTypeName.ToLower().Contains("urin")) specimen = "1";

                List<string> tests = AstmHigh.GetTests(sample.SampleId, instrument, false);
                int oSeq = 1;
                foreach (string test in tests)
                {
                    // 3. Order Record
                    // Barcode in Field 4 (Component 2: ^Barcode) matches the Query format
                    // Test ID in Field 5 (Component 1: Test^^^)
                    string universalTestId = $"{test}^^^";

                    msg += $"O|{oSeq}||^{barcode}^^|^{test}^1|{priority}|||||||||{timeToUse}|0|||1|||F|||||||||\r";
                    oSeq++;
                }
                seq++;
            }

            msg += "L|1|N\r";
            return msg;
        }
        public static ASTM_Message EncodeSampleOld(Sample sample, Instrument instrument)
        {
            bool flag = sample == null;
            ASTM_Message result;
            if (flag)
            {
                result = null;
            }
            else
            {
                List<AstmOrder> orderRecords = AstmHigh.GetOrderRecords(sample, instrument);
                Jihas kind = instrument.Kind;
                bool flag2 = orderRecords == null && kind == Jihas.VitrosEciq;
                if (flag2)
                {
                    result = null;
                }
                else
                {
                    ASTM_Message headerMessage = AstmHigh.GetHeaderMessage(instrument);
                    Patient patient = sample.AnalysisRequest.Patient;
                    long? analysisRequestId = sample.AnalysisRequest.AnalysisRequestId2;
                    long? num = analysisRequestId;
                    string name = num.ToString() + patient.Nom + instrument.Prop.Repeater.ToString() + patient.Prenom;
                    bool flag3 = kind == Jihas.StaSatelit || kind == Jihas.StagoStaMax;
                    if (flag3)
                    {
                        name = (patient.PatientNomPrenom ?? "");
                    }
                    AstmHigh._logger.Info("SampleId DD: " + sample.SampleId.ToString());
                    ASTM_Patient astm_Patient = new ASTM_Patient
                    {
                        f_seq = "1",
                        AstmPatientID = analysisRequestId.ToString(),
                        f_practice_id = analysisRequestId.ToString(),
                        f_laboratory_id = sample.FormattedSampleCode,
                        Name = name,
                        Birthdate = patient.PatientDateNaiss
                    };
                    bool flag4 = orderRecords != null;
                    if (flag4)
                    {
                        astm_Patient.OrderRecords.AddRange(orderRecords);
                    }
                    headerMessage.patientRecords.Add(astm_Patient);
                    result = headerMessage;
                }
            }
            return result;
        }

        private static ASTM_Message GetHeaderMessage(Instrument instrument)
        {
            AstmProp prop = instrument.Prop;
            bool flag = instrument.Kind == Jihas.Bioflash;
            if (flag)
            {
                prop.Receiver = instrument.S2;
            }
            return new ASTM_Message
            {
                f_type = "H",
                f_delimeter = prop.Separator.ToString() + prop.Repeater.ToString() + prop.Delimiter.ToString(),
                f_sender = prop.Sender,
                f_address = ((instrument.Kind == Jihas.Bioflash) ? "" : "BM-Tech"),
                f_receiver = prop.Receiver,
                f_processing_id = "P",
                f_version = prop.Version,
                f_timestamp = instrument.Now.ToString("yyyyMMddHHmmss")
            };
        }

        public static ASTM_Message EncodeSamplesBioflash(List<Sample> samples, Instrument instrument)
        {
            bool flag = samples.Count == 0;
            ASTM_Message result;
            if (flag)
            {
                result = null;
            }
            else
            {
                ASTM_Message headerMessage = AstmHigh.GetHeaderMessage(instrument);
                int num = 0;
                foreach (Sample sample in samples)
                {
                    List<AstmOrder> orderRecords = AstmHigh.GetOrderRecords(sample, instrument);
                    bool flag2 = orderRecords == null;
                    if (!flag2)
                    {
                        num++;
                        ASTM_Patient astm_Patient = new ASTM_Patient
                        {
                            f_seq = num.ToString(),
                            AstmPatientID = sample.AnalysisRequest.AnalysisRequestId2.ToString(),
                            f_practice_id = sample.AnalysisRequest.AnalysisRequestId2.ToString(),
                            f_laboratory_id = sample.FormattedSampleCode,
                            Name = "",
                            Birthdate = sample.AnalysisRequest.Patient.PatientDateNaiss
                        };
                        astm_Patient.OrderRecords.AddRange(orderRecords);
                        headerMessage.patientRecords.Add(astm_Patient);
                    }
                }
                result = headerMessage;
            }
            return result;
        }

        private static List<AstmOrder> GetOrderRecords(Sample sample, Instrument instrument)
        {
            AstmProp prop = instrument.Prop;
            List<string> testsPreffix = AstmHigh.GetTestsPreffix(sample, instrument, prop.Repeater, false);
            bool flag = !testsPreffix.Any<string>();
            List<AstmOrder> result;
            if (flag)
            {
                result = null;
            }
            else
            {
                Jihas jihas = instrument.Kind;
                bool flag2 = jihas == Jihas.Acl;
                if (flag2)
                {
                    result = testsPreffix.Select((string x, int i) => AstmHigh.OrderRecord(sample, x, jihas, instrument, i)).ToList<AstmOrder>();
                }
                else
                {
                    string text = string.Join(prop.Separator.ToString(), testsPreffix);
                    bool flag3 = jihas == Jihas.VitrosEciq;
                    if (flag3)
                    {
                        text = new string(prop.Repeater, 3) + "1.000000+" + text;
                    }
                    result = new List<AstmOrder>
                    {
                        AstmHigh.OrderRecord(sample, text, jihas, instrument, 0)
                    };
                }
            }
            return result;
        }

        private static AstmOrder OrderRecord(Sample sample, string test, Jihas kind, Instrument instrument, int seq = 0)
        {
            string[] param = new string[]
            {
                "A",
                "Serum",
                ""
            };
            bool flag = kind == Jihas.Maglumi;
            if (flag)
            {
                param = new string[]
                {
                    "",
                    "",
                    ""
                };
            }
            else
            {
                bool flag2 = kind == Jihas.LiaisonXL;
                if (flag2)
                {
                    param = new string[]
                    {
                        "",
                        "S",
                        "O"
                    };
                }
                else
                {
                    bool flag3 = kind == Jihas.Spa;
                    if (flag3)
                    {
                        param = new string[]
                        {
                            "A",
                            "Serum",
                            "O"
                        };
                    }
                    else
                    {
                        bool flag4 = kind == Jihas.Bioflash;
                        if (flag4)
                        {
                            param = new string[]
                            {
                                "A",
                                "SER",
                                "Q"
                            };
                        }
                        else
                        {
                            bool flag5 = kind == Jihas.CobasE411;
                            if (flag5)
                            {
                                param = new string[]
                                {
                                    "N",
                                    "",
                                    "Q"
                                };
                            }
                            else
                            {
                                bool flag6 = kind == Jihas.DXH800;
                                if (flag6)
                                {
                                    param = new string[]
                                    {
                                        "N",
                                        "Whole blood",
                                        ""
                                    };
                                }
                                else
                                {
                                    bool flag7 = kind == Jihas.VitrosEciq;
                                    if (flag7)
                                    {
                                        param = new string[]
                                        {
                                            "N",
                                            "4",
                                            "F"
                                        };
                                    }
                                }
                            }
                        }
                    }
                }
            }
            return new AstmOrder(sample, test, seq, param, instrument);
        }

        private static List<string> GetFnsTests(long sid, Instrument instrument)
        {
            string str = new string(instrument.Prop.Repeater, 3);
            LaboContext laboContext = new LaboContext();
            List<string> list = new List<string>();
            Sample sample = laboContext.Sample.Find(new object[]
            {
                sid
            });
            bool flag = sample == null;
            List<string> result;
            if (flag)
            {
                result = list;
            }
            else
            {
                string text = null;
                AnalysisTypeInstrumentMapping cd = laboContext.AnalysisTypeInstrumentMappings.FirstOrDefault((AnalysisTypeInstrumentMapping y) => y.InstrumentCode == instrument.InstrumentCode && y.AnalysisTypeCode == "CD");
                AnalysisTypeInstrumentMapping cbc = laboContext.AnalysisTypeInstrumentMappings.FirstOrDefault((AnalysisTypeInstrumentMapping y) => y.InstrumentCode == instrument.InstrumentCode && y.AnalysisTypeCode == "CBC");
                bool flag2 = cbc != null;
                if (flag2)
                {
                    Analysis analysis = sample.Analysis.FirstOrDefault(delegate (Analysis x)
                    {
                        bool result2;
                        if (x.AnalysisTypeId == cbc.AnalysisTypeId && x.AnalysisState == AnalysisState.EnCours)
                        {
                            int? instrumentId = x.InstrumentId;
                            int instrumentId2 = instrument.InstrumentId;
                            result2 = (instrumentId.GetValueOrDefault() == instrumentId2 & instrumentId != null);
                        }
                        else
                        {
                            result2 = false;
                        }
                        return result2;
                    });
                    bool flag3 = analysis != null;
                    if (flag3)
                    {
                        text = "CBC";
                        analysis.AnalysisState = AnalysisState.EnvoyerAutomate;
                        foreach (Analysis analysis2 in analysis.ChildAnalysises)
                        {
                            analysis2.AnalysisState = AnalysisState.EnvoyerAutomate;
                        }
                    }
                }
                bool flag4 = cd != null;
                if (flag4)
                {
                    Analysis analysis3 = sample.Analysis.FirstOrDefault(delegate (Analysis x)
                    {
                        bool result2;
                        if (x.AnalysisTypeId == cd.AnalysisTypeId && x.AnalysisState == AnalysisState.EnCours)
                        {
                            int? instrumentId = x.InstrumentId;
                            int instrumentId2 = instrument.InstrumentId;
                            result2 = (instrumentId.GetValueOrDefault() == instrumentId2 & instrumentId != null);
                        }
                        else
                        {
                            result2 = false;
                        }
                        return result2;
                    });
                    bool flag5 = analysis3 != null;
                    if (flag5)
                    {
                        text = "CD";
                        analysis3.AnalysisState = AnalysisState.EnvoyerAutomate;
                        foreach (Analysis analysis4 in analysis3.ChildAnalysises)
                        {
                            analysis4.AnalysisState = AnalysisState.EnvoyerAutomate;
                        }
                    }
                }
                bool flag6 = text != null;
                if (flag6)
                {
                    list.Add(str + text);
                }
                laboContext.SaveChanges();
                result = list;
            }
            return result;
        }

        public static List<Sample> GetSamples(Instrument instrument, List<ASTM_Request> requests = null)
        {
            LaboContext db = new LaboContext();
            bool flag = requests == null;
            List<Sample> result;
            if (flag)
            {
                int num = (instrument.Kind == Jihas.Navify) ? -9 : -5;
				result = AstmHigh.GetSamplesRange(DateTime.Now.Date.AddDays((double)num), DateTime.Now, db, instrument);
            }
            else
            {
                List<Sample> list = new List<Sample>();
                foreach (ASTM_Request astm_Request in requests)
                {
                    Jihas kind = instrument.Kind;
                    AstmProp prop = instrument.Prop;
                    bool flag2 = kind == Jihas.CobasE411 || kind == Jihas.CobasC311 || kind == Jihas.Macura;
                    if (flag2)
                    {
                        List<Sample> samplesCobas = AstmHigh.GetSamplesCobas(astm_Request, instrument);
                        list.AddRange(samplesCobas);
                    }
                    else
                    {
                        bool flag3 = kind == Jihas.SysmexCA600 || kind == Jihas.Mispa;
                        if (flag3)
                        {
                            string[] array = astm_Request.f_srangeid.Split(new char[]
                            {
                                prop.Repeater
                            });
                            Sample item = (kind == Jihas.SysmexCA600) ? AstmHigh.GetSample(array[2], array[0] + prop.Repeater.ToString() + array[1]) : AstmHigh.GetSample(array[0], array[2] + prop.Repeater.ToString() + array[3]);
                            list.Add(item);
                        }
                        else
                        {
                            string text = astm_Request.f_srangeid.Replace(prop.Repeater.ToString(), "");
                            bool flag4 = text == "ALL";
                            if (flag4)
                            {
                                list = AstmHigh.GetAll(instrument);
                            }
                            else
                            {
                                List<string> source = text.Split(new char[]
                                {
                                    prop.Separator
                                }).ToList<string>();
                                list.AddRange(from id in source
                                              select AstmHigh.GetSample(id, null));
                            }
                        }
                    }
                }
                result = (from x in list
                          where x != null
                          select x).ToList<Sample>();
            }
            return result;
        }

        public static Sample GetSample(string scode, string info = null)
        {
            AstmHigh._logger.Info(scode);
            long? code = TextUtil.Long(scode);
            bool flag = code == null;
            Sample result;
            if (flag)
            {
                result = null;
            }
            else
            {
                Sample sample = (from x in new LaboContext().Sample
                                 orderby x.DateCreated descending
                                 select x).FirstOrDefault((Sample s) => s.SampleCode == code);
                bool flag2 = sample == null;
                if (flag2)
                {
                    result = null;
                }
                else
                {
                    sample.InstrumentSampleId = info;
                    result = sample;
                }
            }
            return result;
        }

        private static List<Sample> GetSamplesCobas(ASTM_Request request, Instrument instrument)
        {
            List<Sid> list = new List<Sid>();
            string[] array = request.f_srangeid.Split(new char[]
            {
                instrument.Prop.Separator
            });
            foreach (string text in array)
            {
                bool flag = instrument.Kind == Jihas.CobasE411;
                Sid item;
                if (flag)
                {
                    string[] array3 = text.Split(new char[]
                    {
                        instrument.Prop.Repeater
                    }, StringSplitOptions.RemoveEmptyEntries);
                    item = new Sid(array3[0], array3[1]);
                }
                else
                {
                    string text2 = text.Substring(2);
                    int num = text2.IndexOf(instrument.Prop.Repeater);
                    item = new Sid(text2.Substring(0, num), text2.Substring(num + 1));
                }
                list.Add(item);
            }
            return (from i in list
                    select AstmHigh.GetSample(i.Code, i.Info)).ToList<Sample>();
        }

        private static List<Sample> GetAll(Instrument instrument)
        {
            LaboContext laboContext = new LaboContext();
            DateTime lastweek = DateTime.Now.Date.AddDays(-1.0);
            Instrument ins = instrument;
            List<Analysis> list = (ins.InstrumentStd == null) ? (from x in laboContext.Analysis
                                                                 where x.CreatedDate > lastweek && x.InstrumentId == (int?)ins.InstrumentId
                                                                 select x).ToList<Analysis>() : (from x in laboContext.Analysis
                                                                                                 where x.CreatedDate > lastweek
                                                                                                 select x).ToList<Analysis>();
            string instrumentDataBits = ins.InstrumentDataBits;
            IEnumerable<Analysis> enumerable2;
            if (!(instrumentDataBits == "C"))
            {
                if (instrumentDataBits != null)
                {
                    IEnumerable<Analysis> enumerable = list;
                    enumerable2 = enumerable;
                }
                else
                {
                    enumerable2 = from x in list
                                  where x.AnalysisState == AnalysisState.EnCours
                                  select x;
                }
            }
            else
            {
                enumerable2 = from x in list
                              where x.AnalysisState == AnalysisState.ÀEnvoyer
                              select x;
            }
            IEnumerable<Analysis> source = enumerable2;
            IEnumerable<Analysis> source2 = source.Where(x =>
            {
                return x.AnalysisType.AnalysisTypeInstrumentMappings
                    .Any(y => y.InstrumentCode == instrument.InstrumentCode &&
                              !string.IsNullOrWhiteSpace(y.AnalysisTypeCode));
            });
            return (from y in source2
                    select y.Sample).Distinct<Sample>().ToList<Sample>();
        }

        public static string EncodeSample2(Sample sample, Instrument instrument, string ack = "")
        {
            Jihas kind = instrument.Kind;
            string formattedSampleCode = sample.FormattedSampleCode;
            DateTime now = instrument.Now;
            string text = now.ToString("yyyyMMddHHmmssfff");
            string text2 = DateTime.Now.ToString("yyyyMMddHHmmss");
            string text3 = now.ToString("yyyyMMdd");
            Patient patient = sample.AnalysisRequest.Patient;
            bool isEmergency = sample.AnalysisRequest.IsEmergency;
            long? analysisRequestId = sample.AnalysisRequest.AnalysisRequestId2;
            DateTime? dateTime = patient.PatientDateNaiss;
            string text4 = ((patient.PatientDateNaiss != null) ? dateTime.GetValueOrDefault().ToString("yyyyMMdd") : null) ?? "20000101";
            List<string> tests = AstmHigh.GetTests(sample.SampleId, instrument, false);
            string text5 = "";
            string arg = string.Join("^", tests);
            string text6 = sample.SampleSource.SampleTypeName.ToLower();
            string text7 = text6.Contains("urin") ? "UR^^HL70487" : (text6.Contains("sang") ? "WB^^HL70487" : "SERPLAS^^99ROC");
            bool flag = kind == Jihas.Biolis30i;
            string result;
            if (flag)
            {
                for (int i = 0; i < tests.Count; i++)
                {
                    text5 += string.Format("OBX|{0}||{1}^^^^^^^^||||||||||||||||<CR>", i + 1, tests[i]);
                }
                string text8 = string.Concat(new string[]
                {
                    "MSH|^~\\&|^BMLab^||^BiOLiS30i^||",
                    text3,
                    "^||OML^o35^OML_o35|",
                    text,
                    "||2.5^&&&&&&&&^&&&&&&&&||||||ASCII|||<CR>SPM||||SER^^^^^^^^|||||||||||||||||||||||||<CR>SAC|||",
                    formattedSampleCode,
                    "^^^|||||||||||||||||||||||||||||||||||||||||<CR>OBR|||||||||||||||||||||||||||||||||||||||||||||||||<CR>",
                    text5
                });
                result = text8.Replace("<CR>", '\r'.ToString());
            }
            else
            {
                bool flag2 = kind == Jihas.CobasPro;
                if (flag2)
                {
                    int num = Helper.ID++;
                    for (int j = 0; j < tests.Count; j++)
                    {
                        string text9 = tests[j];
                        text5 += string.Format("ORC|NW||||||||{0}\rTQ1|||||||||R^^HL70485\rOBR|{1}|{2}||{3}^^99ROC\rTCD|{4}^^99ROC\r", new object[]
                        {
                            text2,
                            j + 1,
                            formattedSampleCode,
                            text9,
                            text9
                        });
                    }
                    result = string.Concat(new string[]
                    {
                        string.Format("MSH|^~\\&|Host||cobas pro||{0}+0200||OML^O33^OML_O33|{1}|P|2.5.1|||NE|AL||UNICODE UTF-8|||LAB-28R^ROCHE\r", text2, num),
                        string.Format("PID|||{0}||{1}^{2}^^^^^U||{3}|{4}\r", new object[]
                        {
                            patient.PatientID,
                            patient.Nom,
                            patient.Prenom,
                            text4,
                            patient.ShortSexe
                        }),
                        "SPM|1|",
                        formattedSampleCode,
                        "&BARCODE||",
                        text7,
                        "|||||||P^^HL70369||||||",
                        text2,
                        "||||||||||SC^^99ROC\rSAC|||",
                        formattedSampleCode,
                        "^BARCODE|||||||",
                        ack,
                        "\r",
                        text5
                    });
                }
                else
                {
                    bool flag3 = kind == Jihas.Cobas8000;
                    if (flag3)
                    {
                        foreach (string str in tests)
                        {
                            text5 = text5 + "TQ1|1||||||||R\rOBR|1|||" + str + "^|||||||A\r";
                        }
                        result = string.Concat(new string[]
                        {
                            "MSH|^~\\&|BMLab||cobas.8000||",
                            text,
                            "||OML^O33|0851659||2.5||||NE||UNICODE.UTF-8\r",
                            string.Format("PID|1|{0}|||{1}^{2}||{3}|{4}\r", new object[]
                            {
                                patient.PatientID,
                                patient.Nom,
                                patient.Prenom,
                                text4,
                                patient.ShortSexe
                            }),
                            "SPM||",
                            formattedSampleCode,
                            "||S1||||||||||",
                            patient.Nom,
                            "^",
                            patient.Prenom,
                            "^^Plasma^20-05-64|||||||||||||\rSAC||||||||||50002|1\r",
                            text5
                        });
                    }
                    else
                    {
                        bool flag4 = kind == Jihas.C3100;
                        if (flag4)
                        {
                            for (int k = 0; k < tests.Count; k++)
                            {
                                text5 += string.Format("OBX|{0}|NM|{1}||||||||F\r", k + 1, tests[k]);
                            }
                            result = string.Concat(new string[]
                            {
                                "MSH|^~\\&|MPCoagu|Mindray|||||ORR^O02|1|P|2.3.1||||||UNICODE\rMSA|AA|",
                                ack,
                                "||||\r",
                                string.Format("PID|1|{0}|^^^^MR||{1}^{2}||{3}|{4}\r", new object[]
                                {
                                    patient.PatientID,
                                    patient.Nom,
                                    patient.Prenom,
                                    text4,
                                    patient.ShortSexe
                                }),
                                "PV1|1||^^|||||||||||||||||AA\rORC|AF|",
                                formattedSampleCode,
                                "\rOBR|1|",
                                formattedSampleCode,
                                "||00001^Worksheet response^99MRC||",
                                text2,
                                "||||sender||||",
                                text2,
                                "||||||||||HM\r",
                                text5
                            });
                        }
                        else
                        {
                            bool flag5 = kind == Jihas.DiruiCsT180;
                            if (flag5)
                            {
                                int num2 = Helper.ID++;
                                result = string.Concat(new string[]
                                {
                                    "MSH|^~\\&|CS-T180|T180|LIS|T180|",
                                    text2,
                                    "||DSR^Q03|1|P|2.3.1||||0||UNICODE|||\rMSA|OK|1||||0|\rDSP|1||||",
                                    patient.Nom,
                                    "|^||M||||||||||||||||||||||1^Y\r",
                                    string.Format("DSP|2|{0}|{1}|CS-T180^CS6400|N||{2}||1|{3}||N||{4}|0|||||||{5}|||||||||||||||||||||||||\r", new object[]
                                    {
                                        formattedSampleCode,
                                        num2,
                                        text2,
                                        ack,
                                        text2,
                                        text2
                                    }),
                                    string.Format("DSP|3|{0}|{1}|||\r", tests.Count, arg)
                                });
                            }
                            else
                            {
                                bool flag6 = kind == Jihas.Urit8031;
                                if (flag6)
                                {
                                    int num3 = Helper.ID++;
                                    string testsUrit = DsrQ03Handler.GetTestsUrit(tests);
                                    string text10 = isEmergency ? "Y" : "N";
                                    string text11 = text6.Contains("urin") ? "URINE" : "SERUM";
                                    result = string.Format("MSH|^~\\&|urit|8030|||{0}||DSR^Q03|{1}|P|2.3.1||||0||ASCII|||\r\nMSA|AA|{2}|Message accepted|||0|\r\nERR|0|\r\nQAK|SR|OK|\r\nQRD|{3}|R|D|-1|||RD|{4}|OTH|||T|\r\nQRF|8030|{5}000000|{6}235959|||RCT|COR|ALL||\r\nDSP|1||{7}|||\r\nDSP|2||{8}|||\r\nDSP|3||{9}|||\r\nDSP|4||{10} {11}|||\r\nDSP|5||{12}|||\r\nDSP|6||{13}|||\r\nDSP|7||0|||\r\nDSP|8||{14}|||\r\nDSP|9|||||\r\nDSP|10|||||\r\nDSP|11|||||\r\nDSP|12|||||\r\nDSP|13||Admin|||\r\nDSP|14|||||\r\nDSP|15||{15}|||\r\nDSP|16||{16}|||\r\nDSP|17||1|||\r\n{17}DSC|-1|", new object[]
                                    {
                                        text2,
                                        text2,
                                        text2,
                                        text2,
                                        formattedSampleCode,
                                        text3,
                                        text3,
                                        text2,
                                        formattedSampleCode,
                                        text11,
                                        patient.Nom,
                                        patient.Prenom,
                                        patient.ShortSexe,
                                        patient.Age,
                                        analysisRequestId,
                                        text3,
                                        text10,
                                        testsUrit
                                    });
                                }
                                else
                                {
                                    bool flag7 = kind == Jihas.ZybioExc200;
                                    if (flag7)
                                    {
                                        int id = Helper.ID;
                                        string tests2 = DsrQ03Handler.GetTests(tests);
                                        string text12 = isEmergency ? "Y" : "N";
                                        string text13 = text6.Contains("urin") ? "URINE" : "SERUM";
                                        string text14 = string.Format("MSH|^~\\&|||||{0}||DSR^Q03|1|P|2.5||||||UTF-8|||\r\nMSA|AA|{1}|Message accepted|||0\r\nERR|0|||||||||||\r\nQAK|SR|OK||||\r\nQRD|{2}|T|D|4|||RD|{3}||||T\r\nQRF|EXC200|||||RCT|COR|ALL||\r\nDSP|1||||\r\nDSP|2||||\r\nDSP|3||{4} {5}||\r\nDSP|4||{6}^Y||\r\nDSP|5||{7}||\r\nDSP|6||O||\r\nDSP|7||||\r\nDSP|8||||\r\nDSP|9||||\r\nDSP|10||{8}||\r\nDSP|11||^||\r\nDSP|12||{9}||\r\nDSP|13||||\r\nDSP|14||||\r\nDSP|15||||\r\nDSP|16||||\r\nDSP|17||{10}||\r\nDSP|18||||\r\nDSP|19||||\r\nDSP|20||||\r\nDSP|21||{11}||\r\nDSP|22||1||\r\nDSP|23||{12}||\r\nDSP|24||{13}||\r\nDSP|25||||\r\nDSP|26||{14}||\r\nDSP|27||||\r\nDSP|28||||\r\n{15}DSC|\r\n", new object[]
                                        {
                                            text2,
                                            Helper.Ack,
                                            text2,
                                            formattedSampleCode,
                                            patient.Nom,
                                            patient.Prenom,
                                            patient.Age,
                                            patient.ShortSexe,
                                            analysisRequestId,
                                            text2,
                                            text2,
                                            formattedSampleCode,
                                            text2,
                                            text12,
                                            text13,
                                            tests2
                                        });
                                        string text15 = string.Format("MSH|^~\\&|||||{0}||DSR^Q03|{1}|P|2.5||||||UTF-8|||\r", text, id) + "MSA|AA|1|Message accepted|||0\rERR|0||||||||||||\rQAK|SR|OK|||||\r";
                                        text15 = string.Concat(new string[]
                                        {
                                            text15,
                                            "QRD|",
                                            text,
                                            "|R|D|1|||RD|",
                                            formattedSampleCode,
                                            "|||||T\r\nQRF|EXC200|||||RCT|COR|ALL||\r\nDSP|1|||\r\nDSP|2|||\r\nDSP|3||salim||\r\nDSP|4||25^Y||\r\nDSP|5||M||\r\nDSP|6||A||\r\nDSP|7|RhD(+)||\r\nDSP|8||||\r\nDSP|9||||\r\nDSP|10||00117||\r\nDSP|11||^||\r\nDSP|12||",
                                            text,
                                            "||\r\nDSP|13||||\r\nDSP|14||||\r\nDSP|15||||\r\nDSP|16||||\r\nDSP|17||20210926150001||\r\nDSP|18||||\r\nDSP|19||||\r\nDSP|20||||\r\nDSP|21||",
                                            formattedSampleCode,
                                            "||\r\nDSP|22||124||\r\nDSP|23||",
                                            text,
                                            "||\r\nDSP|24||N||\r\nDSP|25||||\r\nDSP|26||SERUM||\r\nDSP|27||||\r\nDSP|28||||\r\n",
                                            tests2,
                                            "DSC|"
                                        });
                                        result = text14.Replace(Tu.NL, "\r");
                                    }
                                    else
                                    {
                                        result = null;
                                    }
                                }
                            }
                        }
                    }
                }
            }
            return result;
        }

        public static string EncodeSample(Sample sample, Instrument instrument, ASTM_Message message, bool test = false)
        {
            Jihas kind = instrument.Kind;
            AstmProp prop = instrument.Prop;
            string formattedSampleCode = sample.FormattedSampleCode;
            string text = sample.SampleSource.SampleTypeName.ToLower();
            string text2 = text.Contains("urin") ? "2" : (text.Contains("sang") ? "4" : "1");
            bool isEmergency = sample.AnalysisRequest.IsEmergency;
            string text3 = TextUtil.GetDate(sample, instrument).ToString("yyyyMMddHHmmss");
            string text4 = instrument.Now.ToString("yyyyMMddHHmmss");
            string text5 = instrument.Now.ToString("yyyyMMddHHmm");
            string text6 = instrument.Now.ToString("yyyyMMdd");
            Patient patient = sample.AnalysisRequest.Patient;
            DateTime? patientDateNaiss = patient.PatientDateNaiss;
            string text7 = ((patientDateNaiss != null) ? patientDateNaiss.GetValueOrDefault().ToString("yyyyMMdd") : null) ?? "20000101";
            patientDateNaiss = patient.PatientDateNaiss;
            string text8 = ((patientDateNaiss != null) ? patientDateNaiss.GetValueOrDefault().ToString("dd-MM-yy") : null) ?? "01-01-00";
            patientDateNaiss = patient.PatientDateNaiss;
            string text9 = ((patientDateNaiss != null) ? patientDateNaiss.GetValueOrDefault().ToString("yyyyMMddHHmmss") : null) ?? "20000101";
            List<string> testsPreffix = AstmHigh.GetTestsPreffix(sample, instrument, prop.Repeater, test);
            string text10 = string.Join(prop.Separator.ToString(), testsPreffix.Distinct<string>());
            AgeAstm ageAstm = patient.AgeAstm;
            bool flag = kind == Jihas.OrthoVision;
            string result;
            if (flag)
            {
                bool flag2 = testsPreffix.Count == 0;
                if (flag2)
                {
                    result = null;
                }
                else
                {
                    foreach (string str in testsPreffix)
                    {
                        AstmHigh._logger.Info("test: " + str);
                    }
                    string text11 = "";
                    for (int i = 0; i < testsPreffix.Count; i++)
                    {
                        text11 += string.Format("O|{0}|{1}||{2}|N|{3}|||||||||CENTBLOOD||||||||||X|||||<CR>", new object[]
                        {
                            i + 1,
                            formattedSampleCode,
                            testsPreffix[i],
                            text4
                        });
                    }
                    result = string.Concat(new string[]
                    {
                        "H|\\^&|||OCD^VISION^4.8.0.45887^JNumber|||||||P|LIS2-A|",
                        text4,
                        "<CR>P|1|||",
                        formattedSampleCode,
                        "|",
                        patient.Nom,
                        "^",
                        patient.Prenom,
                        "^B|||U||||||||||||||||||||||||||<CR>",
                        text11,
                        "L||<CR>"
                    });
                }
            }
            else
            {
                bool flag3 = kind == Jihas.Navify;
                if (flag3)
                {
                    result = string.Concat(new string[]
                    {
                        "H|\\^&|||ASTM-BMLab|||||PSM||P|ORD|",
                        text4,
                        "<CR>",
                        string.Format("P|1|{0}|{1}||{2} {3}||{4}|{5}|||||||||||||||{6}||||||||||<CR>", new object[]
                        {
                            formattedSampleCode,
                            patient.PatientID,
                            patient.Nom,
                            patient.Prenom,
                            text7,
                            patient.ShortSexe,
                            text4
                        }),
                        "O|1|",
                        formattedSampleCode,
                        "||",
                        text10,
                        "|R|",
                        text4,
                        "|||||A||||||||||||||O<CR>L|1|N<CR>"
                    });
                }
                else
                {
                    bool flag4 = kind == Jihas.Mispa;
                    if (flag4)
                    {
                        string text12 = "";
                        for (int j = 0; j < testsPreffix.Count; j++)
                        {
                            text12 += string.Format("O|{0}|{1}^^{2}^N||{3}|R|{4}|||||||||1||||||||||O<CR>", new object[]
                            {
                                j + 1,
                                formattedSampleCode,
                                sample.InstrumentSampleId,
                                testsPreffix[j],
                                text4
                            });
                        }
                        result = "H|\\^&<CR>" + string.Format("P|1||{0}||{1} {2}|||{3}||||||<CR>", new object[]
                        {
                            patient.PatientID,
                            patient.Nom,
                            patient.Prenom,
                            patient.ShortSexe
                        }) + text12 + "L|1|N<CR>";
                    }
                    else
                    {
                        bool flag5 = kind == Jihas.Macura;
                        if (flag5)
                        {
                            result = string.Concat(new string[]
                            {
                                "H|\\^&|||I1000|||||host|TSDWN^REAL|P|1|<CR>P|1|||||||U||||||||||||||||||||||||||<CR>O|1|",
                                formattedSampleCode,
                                "|",
                                sample.InstrumentSampleId,
                                "|",
                                text10,
                                "|R||||||||||0|||||||||||||||<CR>C|1|L|^^^^|I<CR>L|1|N<CR>"
                            });
                        }
                        else
                        {
                            bool flag6 = kind == Jihas.OrthoIM;
                            if (flag6)
                            {
                                result = string.Format("H|\\^&||||||||||P|1|{0}\r\nP|1|{1}|||{2}^{3}^||{4}|{5}|||||79^CTS^Pr. Frigaa^M||||||||||||^Fac^Ward^Room^Bed^^|\r\nO|1|{6}||{7}|R||{8}||||A||||SER||||||||||F\r\nL|1|N", new object[]
                                {
                                    text4,
                                    patient.PatientID,
                                    patient.Nom,
                                    patient.Prenom,
                                    text7,
                                    patient.ShortSexe,
                                    formattedSampleCode,
                                    text10,
                                    text4
                                });
                            }
                            else
                            {
                                bool flag7 = kind == Jihas.Iflash;
                                if (flag7)
                                {
                                    result = string.Format("H|\\^&|||^^|||||||QA|1394-97|{0}\r\nP|0||{1}||{2}|{3}^^|{4}|||B|||||||||||||||||||||||\r\nO|1|{5}^|{6}|{7}|R|{8}|{9}||1.0|||||{10}||||1.000000|||||||Q|||||\r\nL|1|N", new object[]
                                    {
                                        text4,
                                        patient.PatientID,
                                        patient.PatientNomPrenom,
                                        text7,
                                        patient.ShortSexe,
                                        formattedSampleCode,
                                        formattedSampleCode,
                                        text10,
                                        text4,
                                        text4,
                                        text4
                                    });
                                }
                                else
                                {
                                    bool flag8 = kind == Jihas.Mindray680;
                                    if (flag8)
                                    {
                                        result = string.Concat(new string[]
                                        {
                                            "H|\\^&|||Mindray^^|||||||SA|1394-97|",
                                            text4,
                                            "\r\nP|1||",
                                            formattedSampleCode,
                                            "||^SALAH HASSINAT^||19760302^49^Y|M||||||||||||||||||||||||||\r\nO|1|",
                                            formattedSampleCode,
                                            "|",
                                            formattedSampleCode,
                                            "|3^UREA^^\\17^Fe^^|R|",
                                            text4,
                                            "|",
                                            text4,
                                            "|||||||",
                                            text4,
                                            "|other|Dr.|||Dr.Zibouche||||||F|||||\r\nL|1|N"
                                        });
                                    }
                                    else
                                    {
                                        bool flag9 = kind == Jihas.SysmexSuit;
                                        if (flag9)
                                        {
                                            string text13 = isEmergency ? "S" : "";
                                            text10 = string.Join("~", testsPreffix.Distinct<string>());
                                            result = string.Format("H|^~\\&|||||||||||SUITplus1.0|{0}\r\nP|1|{1}|||{2}^{3}||{4}|{5}||||||||||||||||||||||||\r\nOBR|1|{6}||{7}|{8}||{9}||||L||^|{10}|^|Pr Frigaa^CTS|||||||||||\r\nL|1||1|4", new object[]
                                            {
                                                text4,
                                                patient.PatientID,
                                                patient.Nom,
                                                patient.Prenom,
                                                text7,
                                                patient.ShortSexe,
                                                formattedSampleCode,
                                                text10,
                                                text13,
                                                text3,
                                                text3
                                            });
                                        }
                                        else
                                        {
                                            bool flag10 = kind == Jihas.Bioplex;
                                            if (flag10)
                                            {
                                                result = string.Format("H|\\^&|||BioPlex|||||||P||{0}\r\nP|1|{1}|||{2}^{3}^M^^||{4}|{5}\r\nO|1|{6}||{7}|R|{8}|{9}\r\nL|1|N", new object[]
                                                {
                                                    text4,
                                                    patient.PatientID,
                                                    patient.Nom,
                                                    patient.Prenom,
                                                    text7,
                                                    patient.ShortSexe,
                                                    formattedSampleCode,
                                                    text10,
                                                    text4,
                                                    text4
                                                });
                                            }
                                            else
                                            {
                                                bool flag11 = kind == Jihas.Atellica;
                                                if (flag11)
                                                {
                                                    result = string.Format("H|\\^&||||||||||P|1<CR>\r\nP|1|{0}|{1}||{2}^{3}||{4}|{5}||||||||||||||||OP<CR>\r\nO|1|{6}||{7}|R|{8}|{9}||||||||Serum||||||||||O<CR>\r\nL|1|F<CR>", new object[]
                                                    {
                                                        patient.PatientID,
                                                        patient.PatientID,
                                                        patient.Nom,
                                                        patient.Prenom,
                                                        text7,
                                                        patient.ShortSexe,
                                                        formattedSampleCode,
                                                        text10,
                                                        text4,
                                                        text4
                                                    });
                                                }
                                                else
                                                {
                                                    bool flag12 = kind == Jihas.Architect;
                                                    if (flag12)
                                                    {
                                                        result = string.Format("H|\\^&|||^BMLab^Computer|||||ARCHITECT^9.00^F3456490464||P|1|{0}\r\nP|1||{1}||{2}^{3}||{4}|M|||||J^B^^^Dr\r\nO|1|{5}||{6}|S|{7}|||||A|||||M^J^^^Dr|||||||||Q\r\nL|1|F", new object[]
                                                        {
                                                            text6,
                                                            patient.PatientID,
                                                            patient.Nom,
                                                            patient.Prenom,
                                                            text7,
                                                            formattedSampleCode,
                                                            text10,
                                                            text4
                                                        });
                                                    }
                                                    else
                                                    {
                                                        bool flag13 = kind == Jihas.SelectraProM;
                                                        if (flag13)
                                                        {
                                                            result = string.Concat(new string[]
                                                            {
                                                                "H|\\^&|||5555|BM-Tech||||1111||P|LIS2-A|",
                                                                text4,
                                                                "\r",
                                                                string.Format("P|1|{0}|{1}|{2}|{3}^{4}||{5}|{6}|||||||||||||||||\r", new object[]
                                                                {
                                                                    formattedSampleCode,
                                                                    formattedSampleCode,
                                                                    patient.PatientID,
                                                                    patient.Nom,
                                                                    patient.Prenom,
                                                                    text7,
                                                                    patient.ShortSexe
                                                                }),
                                                                "O|1|",
                                                                formattedSampleCode,
                                                                "|",
                                                                formattedSampleCode,
                                                                "|",
                                                                text10,
                                                                "|R||",
                                                                text4,
                                                                "||||A||||Serum||||||||||\rL|1|N"
                                                            });
                                                        }
                                                        else
                                                        {
                                                            bool flag14 = kind == Jihas.Indiko;
                                                            if (flag14)
                                                            {
                                                                result = string.Concat(new string[]
                                                                {
                                                                    "H|^&&|||^LIS host^1.0|||||||P\r",
                                                                    string.Format("P|1|{0}|||{1}|||U\r", patient.PatientID, patient.Nom),
                                                                    "O|1|",
                                                                    formattedSampleCode,
                                                                    "||",
                                                                    text10,
                                                                    "|S||",
                                                                    text4,
                                                                    "||||||Test field||3||||||||||O\rL|1|F\r"
                                                                });
                                                            }
                                                            else
                                                            {
                                                                bool flag15 = kind == Jihas.Maglumi || kind == Jihas.MaglumiX8;
                                                                if (flag15)
                                                                {
                                                                    string text14 = "";
                                                                    for (int k = 0; k < testsPreffix.Count; k++)
                                                                    {
                                                                        text14 += string.Format("O|{0}|{1}||{2}|R<CR>", k + 1, formattedSampleCode, testsPreffix[k]);
                                                                    }
                                                                    result = string.Concat(new string[]
                                                                    {
                                                                        "H|\\^&||PSWD|MAGLUMI X8|||||Lis||P|E1394-97|",
                                                                        text6,
                                                                        "<CR>P|1<CR>",
                                                                        text14,
                                                                        "L|1|N<CR>"
                                                                    });
                                                                }
                                                                else
                                                                {
                                                                    bool flag16 = kind == Jihas.Immulite2000;
                                                                    if (flag16)
                                                                    {
                                                                        string text15 = "";
                                                                        bool flag17 = instrument.Mode == 0;
                                                                        if (flag17)
                                                                        {
                                                                            for (int l = 0; l < testsPreffix.Count; l++)
                                                                            {
                                                                                text15 += string.Format("O|{0}|{1}||{2}||{3}|||||||||||||||||||O<CR>", new object[]
                                                                                {
                                                                                    l + 1,
                                                                                    formattedSampleCode,
                                                                                    testsPreffix[l],
                                                                                    text6
                                                                                });
                                                                            }
                                                                            return string.Concat(new string[]
                                                                            {
                                                                                "H|\\^&||DPC|Receiver|Algiers|||N81|Sender||P|1|",
                                                                                text4,
                                                                                "<CR>",
                                                                                string.Format("P|1|{0}|||{1}^{2}||{3}|{4}|||||<CR>", new object[]
                                                                                {
                                                                                    patient.PatientID,
                                                                                    patient.Nom,
                                                                                    patient.Prenom,
                                                                                    text7,
                                                                                    patient.ShortSexe
                                                                                }),
                                                                                text15,
                                                                                "L|1|F<CR>"
                                                                            });
                                                                        }
                                                                        bool flag18 = instrument.Mode == 1;
                                                                        if (flag18)
                                                                        {
                                                                            text10 = string.Join("+", testsPreffix.Distinct<string>());
                                                                            return string.Format("H|\\^&|{0}||Mindray^LabXpert^||||||Worksheet response^00011|P|LIS2-A2|{1}\r\nP|1|||{2}|{3}^{4}||{5}^{6}^{7}|{8}||||||||||||||||Internal medicine|A - 501^1002\r\nO|1|{9}|||||{10}|||Reception||||20090307103100|Venous blood^||||||||||Q\r\nR|1|^Test Mode^^08003|{11}||^|^^^^^^\r\nR|2|^Charge type^^01015|Public||^|^^^^^^\r\nR|3|^Patient type^^01016|Outpatient||^|^^^^^^\r\nL|1|N", new object[]
                                                                            {
                                                                                message.f_message_id,
                                                                                text4,
                                                                                patient.PatientID,
                                                                                patient.Nom,
                                                                                patient.Prenom,
                                                                                text9,
                                                                                ageAstm.Value,
                                                                                ageAstm.Type,
                                                                                patient.ShortSexe,
                                                                                formattedSampleCode,
                                                                                text4,
                                                                                text10
                                                                            });
                                                                        }
                                                                        bool flag19 = instrument.Mode == 2;
                                                                        if (flag19)
                                                                        {
                                                                            return string.Format("H|\\^&|||Winlab 3.0^LABM LALAOUI|||||EVM^PLATEAU TECHNIQUE LALAOUI||P|1|{0:yyyyMMddHHmm}\r\nP|1|{1}|||{2}||{3}|{4}\r\nO|1|{5}|^^01^^SAMPLE^NORMAL|{6}|R||||||A||||||||||||||O\r\nL|1|F", new object[]
                                                                            {
                                                                                instrument.Now,
                                                                                patient.PatientID,
                                                                                patient.PatientNomPrenom,
                                                                                text7,
                                                                                patient.ShortSexe,
                                                                                formattedSampleCode,
                                                                                text10
                                                                            });
                                                                        }
                                                                        bool flag20 = instrument.Mode == 3;
                                                                        if (flag20)
                                                                        {
                                                                            return string.Format("H|\\^&|||30^Host^7.2.1|||||||P||{0}\r\nP|1|{1}|||{2}|||Adult|||||||||||||||||||||||||\r\nO|1|{3}||{4}|R||||||X||||1|||||||||1|Q\\O\r\nL|1|F", new object[]
                                                                            {
                                                                                text4,
                                                                                patient.PatientID,
                                                                                patient.PatientNomPrenom,
                                                                                formattedSampleCode,
                                                                                text10
                                                                            });
                                                                        }
                                                                        bool flag21 = instrument.Mode == 4;
                                                                        if (flag21)
                                                                        {
                                                                            return string.Format("H|\\^&||||||||||P\r\nP|1|||{0}|{1}^{2}^Q||{3}|{4}|||||||||||||||||p\r\nO|1|{5}||{6}|||||||A||||||||||||||O\r\nC|1|L|Order comment.|G\r\nL|1", new object[]
                                                                            {
                                                                                patient.PatientID,
                                                                                patient.Nom,
                                                                                patient.Prenom,
                                                                                text7,
                                                                                patient.ShortSexe,
                                                                                formattedSampleCode,
                                                                                text10
                                                                            });
                                                                        }
                                                                    }
                                                                    bool flag22 = kind == Jihas.CobasC111;
                                                                    if (flag22)
                                                                    {
                                                                        result = string.Concat(new string[]
                                                                        {
                                                                            "H|\\^&|||BMLAB|||||C111|TSDWN^REPLY|P|1|",
                                                                            text4,
                                                                            "<CR>P|1<CR>O|1|",
                                                                            formattedSampleCode,
                                                                            "||",
                                                                            text10,
                                                                            "|R||||||A||||||||||||||O\\Q<CR>L|1|N<CR>"
                                                                        });
                                                                    }
                                                                    else
                                                                    {
                                                                        bool flag23 = kind == Jihas.Vitros3600 || kind == Jihas.Vitros4600;
                                                                        if (flag23)
                                                                        {
                                                                            result = string.Concat(new string[]
                                                                            {
                                                                                "H|\\^&|||BM|||||||||",
                                                                                text4,
                                                                                "<CR>",
                                                                                string.Format("P|1|{0}|||{1}^{2}^||{3}|M||^|||^^|||||||||||||<CR>", new object[]
                                                                                {
                                                                                    patient.PatientID,
                                                                                    patient.Nom,
                                                                                    patient.Prenom,
                                                                                    text7
                                                                                }),
                                                                                "O|1|",
                                                                                formattedSampleCode,
                                                                                "^^||^^^1.000+",
                                                                                text10,
                                                                                "|R||",
                                                                                text3,
                                                                                "||||A||||5||||||||||O<CR>L|1|N<CR>"
                                                                            });
                                                                        }
                                                                        else
                                                                        {
                                                                            bool flag24 = kind == Jihas.Huma600;
                                                                            if (flag24)
                                                                            {
                                                                                result = string.Concat(new string[]
                                                                                {
                                                                                    "H|\\^&|<CR>P|1|",
                                                                                    formattedSampleCode,
                                                                                    "|||",
                                                                                    patient.PatientNomPrenom,
                                                                                    "||",
                                                                                    text7,
                                                                                    "|||||||||||||||||<CR>O|1|",
                                                                                    formattedSampleCode,
                                                                                    "||",
                                                                                    text10,
                                                                                    "|||||||A||||||||||||||Q<CR>L|1|c<CR>"
                                                                                });
                                                                            }
                                                                            else
                                                                            {
                                                                                bool flag25 = kind == Jihas.SysmexCA600;
                                                                                if (flag25)
                                                                                {
                                                                                    result = string.Concat(new string[]
                                                                                    {
                                                                                        "H|\\^&|||BM^^^^|||||CA-600<CR>P|1<CR>O|1|",
                                                                                        sample.InstrumentSampleId,
                                                                                        "^",
                                                                                        formattedSampleCode,
                                                                                        "^B||",
                                                                                        text10,
                                                                                        "|R|",
                                                                                        text3,
                                                                                        "|||||N<CR>L|1|N<CR>"
                                                                                    });
                                                                                }
                                                                                else
                                                                                {
                                                                                    bool flag26 = kind == Jihas.CobasC311;
                                                                                    if (flag26)
                                                                                    {
                                                                                        string instrumentSampleId = sample.InstrumentSampleId;
                                                                                        Regex regex = new Regex("\\^\\^S(\\d+)\\^");
                                                                                        string text16 = (instrument.Mode == 1) ? "1" : regex.Match(instrumentSampleId).Groups[1].Value;
                                                                                        bool flag27 = instrument.S3 == "t";
                                                                                        if (flag27)
                                                                                        {
                                                                                            result = "H|\\^&|||ASTM_SERVICE|||||||P||20240808155607\r\nP|1||1234||Patient Name^Patient Surname||19630101|M|||||||||| |||||20240808155607||||||||||\r\nO|1|1000024301||^^^CHOL\\^^^CREJ2\\^^^HDLC4\\^^^K_S\\^^^NA_S\\^^^TRIG^^^^^^^COBAS_PRO|||||||A||||Serum||||||||||O\r\nL|1|N";
                                                                                        }
                                                                                        else
                                                                                        {
                                                                                            bool flag28 = instrument.S3 == "BEO";
                                                                                            if (flag28)
                                                                                            {
                                                                                                result = string.Concat(new string[]
                                                                                                {
                                                                                                    "H|\\^&|||",
                                                                                                    prop.Sender,
                                                                                                    "|||||",
                                                                                                    prop.Receiver,
                                                                                                    "|TSDWN^REPLY|P|1<CR>P|1<CR>O|1|",
                                                                                                    formattedSampleCode,
                                                                                                    "|",
                                                                                                    instrumentSampleId,
                                                                                                    "|",
                                                                                                    text10,
                                                                                                    "|R||||||A||||",
                                                                                                    text16,
                                                                                                    "||||||||||O<CR>L|1|N<CR>"
                                                                                                });
                                                                                            }
                                                                                            else
                                                                                            {
                                                                                                string text17 = (instrument.S3 == "S") ? "S" : "R";
                                                                                                result = string.Concat(new string[]
                                                                                                {
                                                                                                    "H|\\^&|||",
                                                                                                    prop.Sender,
                                                                                                    "|||||c^1|TSDWN^REPLY|P|1|",
                                                                                                    text5,
                                                                                                    "|<CR>",
                                                                                                    string.Format("P|1||{0}|||||{1}||||||{2}^{3}<CR>", new object[]
                                                                                                    {
                                                                                                        patient.PatientID,
                                                                                                        patient.ShortSexe,
                                                                                                        ageAstm.Value,
                                                                                                        ageAstm.Type
                                                                                                    }),
                                                                                                    "O|1|",
                                                                                                    formattedSampleCode,
                                                                                                    "|0^5000^0^^S",
                                                                                                    text2,
                                                                                                    "^|",
                                                                                                    text10,
                                                                                                    "|",
                                                                                                    text17,
                                                                                                    "||",
                                                                                                    text5,
                                                                                                    "||||A||||",
                                                                                                    text2,
                                                                                                    "||||||||||O||||||<CR>C|1|L|",
                                                                                                    patient.PatientNom,
                                                                                                    "^",
                                                                                                    patient.PatientNomPrenom,
                                                                                                    "^^^",
                                                                                                    text8,
                                                                                                    "|G<CR>L|1|N<CR>"
                                                                                                });
                                                                                            }
                                                                                        }
                                                                                    }
                                                                                    else
                                                                                    {
                                                                                        bool flag29 = kind == Jihas.EUROLabOffice;
                                                                                        if (flag29)
                                                                                        {
                                                                                            result = string.Format("H|\\^&||||||||||P|1|{0}\r\nP|1|{1}|||{2}^{3}||{4}|{5}\r\nO|1|{6}||{7}||{8}||||||||{9}||||||20630|||||O\r\nL|1|N", new object[]
                                                                                            {
                                                                                                text4,
                                                                                                patient.PatientID,
                                                                                                patient.Nom,
                                                                                                patient.Prenom,
                                                                                                text7,
                                                                                                patient.ShortSexe,
                                                                                                formattedSampleCode,
                                                                                                text10,
                                                                                                text4,
                                                                                                text4
                                                                                            });
                                                                                        }
                                                                                        else
                                                                                        {
                                                                                            bool flag30 = kind == Jihas.Gemini || kind == Jihas.Euro;
                                                                                            if (flag30)
                                                                                            {
                                                                                                result = string.Concat(new string[]
                                                                                                {
                                                                                                    "H|\\^&|||<CR>P|1||",
                                                                                                    formattedSampleCode,
                                                                                                    "<CR>O|1|",
                                                                                                    formattedSampleCode,
                                                                                                    "||",
                                                                                                    text10,
                                                                                                    "||20170913114719<CR>L|1|N<CR>"
                                                                                                });
                                                                                            }
                                                                                            else
                                                                                            {
                                                                                                bool flag31 = kind == Jihas.Aia360;
                                                                                                if (flag31)
                                                                                                {
                                                                                                    result = string.Concat(new string[]
                                                                                                    {
                                                                                                        "H|\\^&||| |||||||||",
                                                                                                        text3,
                                                                                                        "<CR>P|1|",
                                                                                                        formattedSampleCode,
                                                                                                        "<CR>O|1|",
                                                                                                        formattedSampleCode,
                                                                                                        "| ^ |",
                                                                                                        text10,
                                                                                                        "|||||||||||2<CR>L|1<CR>"
                                                                                                    });
                                                                                                }
                                                                                                else
                                                                                                {
                                                                                                    bool flag32 = kind == Jihas.Autobio;
                                                                                                    if (flag32)
                                                                                                    {
                                                                                                        result = "H|\\^&|||Autobio BMlab||0|||||RSP1|1394-97|20211128235521\rP|1||36523|||||||||||||||||||||||||||||||\rO|1||^" + formattedSampleCode + "^^|^103^1|R||||||||||0|||1|||F|||||||||\rL|1|N";
                                                                                                    }
                                                                                                    else
                                                                                                    {
                                                                                                        bool flag33 = kind == Jihas.StaRMAX;
                                                                                                        if (flag33)
                                                                                                        {
                                                                                                            bool flag34 = instrument.Mode == 1;
                                                                                                            if (flag34)
                                                                                                            {
                                                                                                                result = string.Concat(new string[]
                                                                                                                {
                                                                                                                    "H|\\^&|||99^2.00|||||||P|1.00|",
                                                                                                                    text4,
                                                                                                                    "<CR>P|1<CR>O|1|",
                                                                                                                    formattedSampleCode,
                                                                                                                    "||",
                                                                                                                    text10,
                                                                                                                    "|R<CR>L|1|N<CR>"
                                                                                                                });
                                                                                                            }
                                                                                                            else
                                                                                                            {
                                                                                                                result = string.Concat(new string[]
                                                                                                                {
                                                                                                                    "H|\\^&|||",
                                                                                                                    prop.Receiver,
                                                                                                                    "|||||||P|LIS2-A2|",
                                                                                                                    text4,
                                                                                                                    "<CR>",
                                                                                                                    string.Format("P|1|{0}|||{1}^{2}^^^||{3}|{4}<CR>", new object[]
                                                                                                                    {
                                                                                                                        patient.PatientID,
                                                                                                                        patient.Nom,
                                                                                                                        patient.Prenom,
                                                                                                                        text7,
                                                                                                                        patient.ShortSexe
                                                                                                                    }),
                                                                                                                    "O|1|",
                                                                                                                    formattedSampleCode,
                                                                                                                    "||",
                                                                                                                    text10,
                                                                                                                    "|R||",
                                                                                                                    text3,
                                                                                                                    "||||N<CR>L|1|N<CR>"
                                                                                                                });
                                                                                                            }
                                                                                                        }
                                                                                                        else
                                                                                                        {
                                                                                                            bool flag35 = kind == Jihas.Biolis24i;
                                                                                                            if (flag35)
                                                                                                            {
                                                                                                                string text18 = instrument.S1 ?? "HOST^P_1";
                                                                                                                string text19 = (instrument.Mode == 0) ? "BIOLIS NEO^SYSTEM1" : "Prestige24i^SYSTEM1";
                                                                                                                string text20 = instrument.S2 ?? text19;
                                                                                                                string text21 = (instrument.InstrumentDataBits == "C") ? string.Format("^1^{0}", Helper.ID++) : "";
                                                                                                                result = string.Concat(new string[]
                                                                                                                {
                                                                                                                    "H|\\^&|||",
                                                                                                                    text18,
                                                                                                                    "|||||",
                                                                                                                    text20,
                                                                                                                    "||P|1|",
                                                                                                                    text4,
                                                                                                                    "<CR>P|1|",
                                                                                                                    formattedSampleCode,
                                                                                                                    "|||",
                                                                                                                    patient.Nom,
                                                                                                                    " ",
                                                                                                                    patient.Prenom,
                                                                                                                    "|||",
                                                                                                                    patient.ShortSexe,
                                                                                                                    "|||||<CR>O|1|",
                                                                                                                    formattedSampleCode,
                                                                                                                    "|",
                                                                                                                    text21,
                                                                                                                    "|",
                                                                                                                    text10,
                                                                                                                    "|R||||||A||||Serum||||||||||O<CR>L|1|N<CR>"
                                                                                                                });
                                                                                                            }
                                                                                                            else
                                                                                                            {
                                                                                                                bool flag36 = kind == Jihas.Pictus200;
                                                                                                                if (flag36)
                                                                                                                {
                                                                                                                    result = string.Concat(new string[]
                                                                                                                    {
                                                                                                                        "H|\\^&|<CR>P|1|",
                                                                                                                        formattedSampleCode,
                                                                                                                        "|||",
                                                                                                                        patient.Nom,
                                                                                                                        " ",
                                                                                                                        patient.Prenom,
                                                                                                                        "||",
                                                                                                                        text7,
                                                                                                                        "|F<CR>O|1|",
                                                                                                                        formattedSampleCode,
                                                                                                                        "||",
                                                                                                                        text10,
                                                                                                                        "|",
                                                                                                                        text3,
                                                                                                                        "||1||||||||||||||||||Q<CR>L|1|N<CR>"
                                                                                                                    });
                                                                                                                }
                                                                                                                else
                                                                                                                {
                                                                                                                    result = null;
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
                                }
                            }
                        }
                    }
                }
            }
            return result;
        }

        public static List<string> GetTestsPreffix(Sample sample, Instrument instrument, char repeater, bool test = false)
        {
            Jihas kind = instrument.Kind;
            string suffix = (kind == Jihas.SysmexCA600) ? "^^100" : null;
            string preffix = new string(repeater, 3);
            bool flag = kind == Jihas.OrthoVision || (kind == Jihas.Immulite2000 && instrument.Mode == 1);
            if (flag)
            {
                preffix = "";
            }
            bool flag2 = kind == Jihas.CobasC311;
            if (flag2)
            {
                suffix = "^";
            }
            bool flag3 = kind == Jihas.Macura;
            if (flag3)
            {
                suffix = "^1";
            }
            bool flag4 = kind == Jihas.Architect;
            if (flag4)
            {
                suffix = "^";
            }
            bool flag5 = kind == Jihas.Spa;
            if (flag5)
            {
                suffix = "^^0";
            }
            bool flag6 = kind == Jihas.SysmexSuit;
            if (flag6)
            {
                preffix = "";
            }
            bool flag7 = kind == Jihas.Iflash;
            if (flag7)
            {
                preffix = "";
                suffix = "^^1.000000^";
            }
            bool flag8 = kind == Jihas.VitrosEciq;
            if (flag8)
            {
                preffix = "";
                suffix = "+1";
            }
            bool flag9 = kind == Jihas.Vitros3600 || kind == Jihas.Vitros4600;
            if (flag9)
            {
                preffix = "";
                suffix = "+1.0";
            }
            List<string> tests = AstmHigh.GetTests(sample.SampleId, instrument, test);
            return (from x in tests
                    select preffix + x + suffix).ToList<string>();
        }

        public static void LoadResults(JihazResult jr, Instrument instrument, char? repeater = null)
        {
            LaboContext laboContext = new LaboContext();
            string text = "Scode = " + (((jr != null) ? jr.Scode : null) ?? "NULL") + "\n";
            foreach (LowResult lowResult in jr.Results)
            {
                text = string.Concat(new string[]
                {
                    text,
                    lowResult.Code,
                    "::",
                    lowResult.Value,
                    "\n"
                });
            }
            AstmHigh._logger.Info(text);
            bool flag = ((jr != null) ? jr.Scode : null) == null;
            if (!flag)
            {
                Sample sample = TextUtil.Sample(laboContext, jr.Scode, instrument);
                bool flag2 = sample == null;
                if (!flag2)
                {
                    Jihas kind = instrument.Kind;
                    bool flag3 = kind == Jihas.Navify && instrument.Mode == 1;
                    List<LowResult> list = jr.Results;
                    bool flag4 = kind == Jihas.Biolis24i;
                    if (flag4)
                    {
                        list = (from x in jr.Results
                                group x by x.Code into x
                                select (from y in x
                                        orderby y.Order descending
                                        select y).First<LowResult>()).ToList<LowResult>();
                    }
                    foreach (LowResult lowResult2 in list)
                    {
                        bool flag5 = string.IsNullOrWhiteSpace(lowResult2.Code);
                        if (!flag5)
                        {
                            AstmHigh._logger.Info(string.Format("Looking for {0}, value = {1}; order = {2}; mpl = {3}", new object[]
                            {
                                lowResult2.Code,
                                lowResult2.Value,
                                lowResult2.Order,
                                flag3
                            }));
                            Instrument instrument2 = flag3 ? laboContext.Instrument.Find(new object[]
                            {
                                lowResult2.Order
                            }) : instrument;
                            bool flag6 = instrument2 == null;
                            if (flag6)
                            {
                                AstmHigh._logger.Info(lowResult2.Code + ": Instrument Not Found");
                            }
                            else
                            {
                                List<long> listTestIds = AstmHigh.GetListTestIds(lowResult2.Code, instrument2);
                                Logger logger = AstmHigh._logger;
                                string format = "listTestIds.Count = {0}";
                                List<long> listTestIds2 = listTestIds;
                                logger.Info(string.Format(format, (listTestIds2 != null) ? new int?(listTestIds2.Count) : null));
                                bool flag7 = listTestIds == null || listTestIds.Count == 0;
                                if (flag7)
                                {
                                    AstmHigh._logger.Info(lowResult2.Code + ": map Not Found");
                                }
                                else
                                {
                                    AstmHigh._logger.Info(string.Join<long>(",", listTestIds));
                                    Analysis analysis = sample.Analysis.FirstOrDefault((Analysis x) => listTestIds.Contains(x.AnalysisTypeId) && (x.AnalysisState <= AnalysisState.EnvoyerAutomate || x.AnalysisState == AnalysisState.NonConforme));
                                    bool flag8 = analysis == null;
                                    if (flag8)
                                    {
                                        Analysis analysis2 = sample.Analysis.FirstOrDefault((Analysis x) => listTestIds.Contains(x.AnalysisTypeId));
                                        AstmHigh._logger.Warn(string.Format("analysis == null,  b == {0}", (analysis2 != null) ? new long?(analysis2.AnalysisTypeId) : null));
                                    }
                                    else
                                    {
                                        AstmHigh._logger.Info(string.Format("found aid = {0}, atid={1}, {2}", analysis.AnalysisId, analysis.AnalysisTypeId, analysis.AnalysisType.AnalysisTypeName));
                                        string text2 = lowResult2.Value;
                                        List<Jihas> list2 = new List<Jihas>
                                        {
                                            Jihas.CobasE411,
                                            Jihas.CobasC311,
                                            Jihas.CobasC111
                                        };
                                        bool flag9 = repeater != null;
                                        if (flag9)
                                        {
                                            string[] array = lowResult2.Value.Split(new char[]
                                            {
                                                repeater.Value
                                            });
                                            text2 = ((list2.Contains(kind) && array.Length > 1) ? array[1] : array[0]);
                                        }
                                        analysis.Flag = lowResult2.Flag;
                                        bool flag10 = string.IsNullOrWhiteSpace(text2) || text2 == "No Result" || text2 == "Noresult" || text2 == "NA";
                                        if (flag10)
                                        {
                                            AstmHigh._logger.Warn("string.IsNullOrWhiteSpace(value) || value == No Result || value == NA");
                                        }
                                        else
                                        {
                                            text2 = TextUtil.Result(text2, kind);
                                            bool flag11 = kind == Jihas.Vitros3600;
                                            if (flag11)
                                            {
                                                bool flag12 = lowResult2.Flag.StartsWith("^5^");
                                                if (flag12)
                                                {
                                                    text2 = "<" + text2;
                                                }
                                                else
                                                {
                                                    bool flag13 = lowResult2.Flag.StartsWith("^4^");
                                                    if (flag13)
                                                    {
                                                        text2 = ">" + text2;
                                                    }
                                                }
                                            }
                                            else
                                            {
                                                bool flag14 = kind == Jihas.Urilyser;
                                                if (flag14)
                                                {
                                                    text2 = text2.Replace("norm", "Absence").Replace("neg", "Absence");
                                                }
                                            }
                                            analysis.ResultTxt = text2;
                                            AstmHigh._logger.Info(" a.ResultTxt=" + analysis.ResultTxt);
                                            analysis.AnalysisState = AnalysisState.ReçuAutomate;
                                            int value = flag3 ? ((int)lowResult2.Order.Value) : instrument.InstrumentId;
                                            analysis.InstrumentId = new int?(value);
                                            bool flag15 = analysis.Parent != null;
                                            if (flag15)
                                            {
                                                analysis.Parent.InstrumentId = new int?(value);
                                            }
                                        }
                                    }
                                }
                            }
                        }
                    }
                    laboContext.SaveChanges();
                }
            }
        }

        public static List<long> GetListTestIds(string id, Instrument instrument)
        {
            LaboContext laboContext = new LaboContext();
            IQueryable<AnalysisTypeInstrumentMapping> source = AstmHigh._aclLike.Contains(instrument.Kind) ? (from m in laboContext.AnalysisTypeInstrumentMappings
                                                                                                              where m.InstrumentCode == instrument.InstrumentCode && m.AnalysisTypeCode.EndsWith("-" + id)
                                                                                                              select m) : laboContext.AnalysisTypeInstrumentMappings.Where((AnalysisTypeInstrumentMapping m) => m.InstrumentCode == instrument.InstrumentCode && m.AnalysisTypeCode == id);
            return (from x in source
                    select x.AnalysisTypeId).ToList<long>();
        }

        private static void AddFileToRtf(long analysisTypeId, string file)
        {
        }

        private static string GetFile(string value)
        {
            return null;
        }

        public static string GetCodePro(string test, string units, Instrument i, char repeater)
        {
            Jihas kind = i.Kind;
            // -----------------------------------------------------------
            // [FIX] AUTOBIO: Extract Test Code from 2nd component (e.g. 610^123^)
            // -----------------------------------------------------------
            if (kind == Jihas.Autobio)
            {
                string[] parts = test.Split('^');
                return (parts.Length > 1) ? parts[1] : parts[0];
            }
            // -----------------------------------------------------------
            bool flag = kind == Jihas.Architect && !test.EndsWith("^^F");
            string result;
            if (flag)
            {
                result = null;
            }
            else
            {
                bool flag2 = kind == Jihas.CobasC311 || kind == Jihas.CobasE411 || kind == Jihas.Macura;
                if (flag2)
                {
                    test = test.Split(new char[]
                    {
                        '/'
                    })[0];
                }
                bool flag3 = kind == Jihas.Access2 && i.Mode == 1;
                if (flag3)
                {
                    result = test.Split(new char[]
                    {
                        repeater
                    })[3];
                }
                else
                {
                    bool flag4 = kind == Jihas.Access2 && i.Mode == 5;
                    if (flag4)
                    {
                        result = test.Split(new char[]
                        {
                            repeater
                        })[6];
                    }
                    else
                    {
                        bool flag5 = kind == Jihas.Access2 && i.Mode == 6;
                        if (flag5)
                        {
                            result = test.Split(new char[]
                            {
                                repeater
                            })[6];
                        }
                        else
                        {
                            string[] array = test.Split(new char[]
                            {
                                repeater
                            }, StringSplitOptions.RemoveEmptyEntries);
                            AstmHigh._logger.Info("test : " + test);
                            bool flag6 = kind == Jihas.SwingSaxo || kind == Jihas.Biorad10;
                            string text;
                            if (flag6)
                            {
                                text = test;
                            }
                            else
                            {
                                bool flag7 = kind == Jihas.V8;
                                if (flag7)
                                {
                                    text = array[1] + ((array.Length > 2) ? ("^" + array[2]) : "");
                                }
                                else
                                {
                                    bool flag8 = kind == Jihas.Bioplex;
                                    if (flag8)
                                    {
                                        text = array[0] + "^" + array[1];
                                    }
                                    else
                                    {
                                        bool flag9 = kind == Jihas.Atellica;
                                        if (flag9)
                                        {
                                            text = ((i.Mode == 0) ? (array[0] + "^" + array[2]) : array[1]);
                                        }
                                        else
                                        {
                                            text = array[0];
                                        }
                                    }
                                }
                            }
                            bool flag10 = kind == Jihas.Acl;
                            if (flag10)
                            {
                                text = text + "-" + units;
                            }
                            AstmHigh._logger.Info("code  : " + text);
                            result = text;
                        }
                    }
                }
            }
            return result;
        }

        public static string GetValuePro(AstmResult val, Instrument i, char repeater)
        {
            string[] array = val.Value.Split(new char[]
            {
                repeater
            });

            // -----------------------------------------------------------
            // [FIX] AUTOBIO: Extract Value from 2nd component
            // -----------------------------------------------------------
            if (i.Kind == Jihas.Autobio)
            {
                string[] parts = val.Value.Split('^');
                return (parts.Length > 1) ? parts[1] : parts[2];
            }
            // -----------------------------------------------------------
            bool flag = i.Kind == Jihas.Iflash;
            string result;
            if (flag)
            {
                result = array[0].Split(new char[]
                {
                    ','
                })[0];
            }
            else
            {
                bool flag2 = i.Kind == Jihas.Mindray680 || (i.Kind == Jihas.Access2 && i.Mode == 2);
                if (flag2)
                {
                    result = array[0];
                }
                else
                {
                    bool flag3 = i.Kind == Jihas.Huma200;
                    if (flag3)
                    {
                        result = ((i.Mode == 0) ? val.AbnormalFlag : val.Status);
                    }
                    else
                    {
                        result = val.Value;
                    }
                }
            }
            return result;
        }

        private static JihazResult Handle(JihazResult jr, Jihas kind)
        {
            bool flag = kind != Jihas.OrthoVision;
            JihazResult result;
            if (flag)
            {
                result = jr;
            }
            else
            {
                JihazResult jihazResult = new JihazResult(jr.Scode);
                List<LowResult> results = jr.Results;
                LowResult lowResult = results.Find((LowResult x) => x.Code == "ABO");
                LowResult lowResult2 = results.Find((LowResult x) => x.Code == "Rh");
                bool flag2 = lowResult != null && lowResult2 != null;
                if (flag2)
                {
                    jihazResult.Results.Add(new LowResult("AboRh", lowResult.Value + TextUtil.GetRhesus(lowResult2.Value), null, null, null));
                }
                LowResult lowResult3 = results.Find((LowResult x) => x.Code == "Pheno");
                bool flag3 = lowResult3 != null && lowResult3.Value.Length == 4;
                if (flag3)
                {
                    jihazResult.Results.Add(new LowResult("C", (lowResult3.Value[0] == 'C') ? "+" : "-", null, null, null));
                    jihazResult.Results.Add(new LowResult("c", (lowResult3.Value[1] == 'c') ? "+" : "-", null, null, null));
                    jihazResult.Results.Add(new LowResult("E", (lowResult3.Value[2] == 'E') ? "+" : "-", null, null, null));
                    jihazResult.Results.Add(new LowResult("e", (lowResult3.Value[3] == 'e') ? "+" : "-", null, null, null));
                }
                LowResult lowResult4 = results.Find((LowResult x) => x.Code == "Kell");
                bool flag4 = lowResult4 != null;
                if (flag4)
                {
                    jihazResult.Results.Add(new LowResult("K", (lowResult4.Value == "NEG") ? "-" : "+", null, null, null));
                }
                LowResult lowResult5 = results.Find((LowResult x) => x.Code == "IgG");
                bool flag5 = lowResult5 != null;
                if (flag5)
                {
                    jihazResult.Results.Add(new LowResult("IgG", (lowResult5.Value == "NEG") ? "Négatif" : "Positif", null, null, null));
                }
                LowResult lowResult6 = results.Find((LowResult x) => x.Code == "C3");
                bool flag6 = lowResult6 != null;
                if (flag6)
                {
                    jihazResult.Results.Add(new LowResult("C3", (lowResult6.Value == "NEG") ? "Négatif" : "Positif", null, null, null));
                }
                result = jihazResult;
            }
            return result;
        }

        public static JihazResult GetResult(AstmOrder astm, Instrument instrument, char repeater)
        {
            Jihas kind = instrument.Kind;
            string[] source = astm.SampleID.Split(new char[]
            {
                repeater
            });
            
            string text = (kind == Jihas.SysmexCA600 && instrument.Mode == 0) ? astm.Instrument.Split(new char[]
            {
                repeater
            })[2] : source.FirstOrDefault<string>();


            if (kind == Jihas.Autobio)
            {
                string[] parts = astm.Instrument.Split('^');
                if (parts.Length > 1)
                {
                    text = parts[1]; // Extract "000038001947"
                }
            }
            bool flag = kind == Jihas.SysmexXS_XN || kind == Jihas.SysmexUf1000 || (kind == Jihas.Sysmex_Xt2000i && instrument.Mode == 0);
            if (flag)
            {
                text = astm.Instrument.Split(new char[]
                {
                    repeater
                })[2];
            }
            bool flag2 = kind == Jihas.Sysmex_Xt2000i && instrument.Mode == 1;
            if (flag2)
            {
                text = astm.Patient.f_laboratory_id;
            }
            bool flag3 = kind == Jihas.CobasC111 || kind == Jihas.Mindray680 || (kind == Jihas.Iflash && instrument.Mode == 0);
            if (flag3)
            {
                text = astm.Instrument.Split(new char[]
                {
                    repeater
                })[0];
            }
            bool flag4 = kind == Jihas.Arkray;
            if (flag4)
            {
                text = ((text != null) ? text.Replace("-", "") : null);
            }
            bool flag5 = kind == Jihas.Spa && string.IsNullOrEmpty(text);
            if (flag5)
            {
                text = astm.Patient.f_practice_id;
            }
            AstmHigh._logger.Info("scode=" + text);
            JihazResult jihazResult = new JihazResult(text);
            foreach (AstmResult astmResult in astm.ResultRecords)
            {
                string text2 = astmResult.Test;
                bool flag6 = kind == Jihas.VitrosEciq || kind == Jihas.Vitros3600 || kind == Jihas.Vitros4600;
                if (flag6)
                {
                    text2 = text2.Split(new char[]
                    {
                        '+'
                    })[1];
                }
                string codePro = AstmHigh.GetCodePro(text2, astmResult.Units, instrument, repeater);
                string text3 = AstmHigh.GetValuePro(astmResult, instrument, repeater);
                bool flag7 = kind == Jihas.Ampilink;
                if (flag7)
                {
                    text3 = text3.Replace(" (", " log:(");
                    text3 = text3.Replace("E+", "*10^");
                    text3 = text3.Replace("Target Not Detected", "Négatif");
                }
                AstmHigh._logger.Info(string.Concat(new string[]
                {
                    text2,
                    " : ",
                    codePro,
                    " : ",
                    text3,
                    " : ",
                    astmResult.AbnormalFlag,
                    " : ",
                    astmResult.CompletedAt,
                    " : ",
                    astmResult.Instrument
                }));
                long? num;
                if (kind != Jihas.Navify || instrument.Mode != 1)
                {
                    num = TextUtil.Long(astmResult.CompletedAt);
                }
                else
                {
                    int? navifyInstrument = Hl7Manager.GetNavifyInstrument(astmResult.Instrument);
                    num = ((navifyInstrument != null) ? new long?((long)navifyInstrument.GetValueOrDefault()) : null);
                }
                long? order = num;
                jihazResult.Results.Add(new LowResult(codePro, text3, astmResult.Units, astmResult.AbnormalFlag, order));
            }
            return jihazResult;
        }

        public static List<string> GetTests(long sid, Instrument ins, bool test = false)
        {
            Logger logger = AstmHigh._logger;
            string format = "InstrumentId :{0}, Std: {1}";
            Instrument ins2 = ins;
            object arg = (ins2 != null) ? new int?(ins2.InstrumentId) : null;
            Instrument ins3 = ins;
            logger.Info(string.Format(format, arg, (ins3 != null) ? ins3.InstrumentStd : null));
            bool flag = ins == null;
            List<string> result;
            if (flag)
            {
                result = null;
            }
            else
            {
                string text = (ins.Kind == Jihas.Vitros350) ? "A" : ins.InstrumentDataBits;
                List<Jihas> list = new List<Jihas>
                {
                    Jihas.Acl,
                    Jihas.SysmexCA600,
                    Jihas.OrthoVision,
                    Jihas.C3100,
                    Jihas.CobasC311
                };
                bool flag2 = ins.Kind == Jihas.DXH800;
                if (flag2)
                {
                    result = AstmHigh.GetFnsTests(sid, ins);
                }
                else
                {
                    LaboContext laboContext = new LaboContext();
                    Jihas kind = ins.Kind;
                    Sample sample = test ? Ct.Sample(ins.InstrumentCode) : laboContext.Sample.Find(new object[]
                    {
                        sid
                    });
                    AstmHigh._logger.Info("SampleCode: " + ((sample != null) ? sample.SampleCode : null).ToString());
                    bool flag3 = sample == null;
                    if (flag3)
                    {
                        result = new List<string>();
                    }
                    else
                    {
                        List<string> list2 = new List<string>();
                        string instrumentStd = ins.InstrumentStd;
                        List<Analysis> list3 = (instrumentStd == null) ? sample.Analysis.Where(delegate (Analysis x)
                        {
                            int? instrumentId2 = x.InstrumentId;
                            int instrumentId3 = ins.InstrumentId;
                            return instrumentId2.GetValueOrDefault() == instrumentId3 & instrumentId2 != null;
                        }).ToList<Analysis>() : sample.Analysis.ToList<Analysis>();
                        AstmHigh._logger.Info("toSend: " + (text ?? "null"));
                        IEnumerable<Analysis> enumerable;
                        if (!(text == "C"))
                        {
                            if (text != null)
                            {
                                enumerable = from x in list3
                                             where x.AnalysisState == AnalysisState.EnCours || x.AnalysisState == AnalysisState.EnvoyerAutomate
                                             select x;
                            }
                            else
                            {
                                enumerable = from x in list3
                                             where x.AnalysisState == AnalysisState.EnCours
                                             select x;
                            }
                        }
                        else
                        {
                            enumerable = from x in list3
                                         where x.AnalysisState == AnalysisState.ÀEnvoyer
                                         select x;
                        }
                        IEnumerable<Analysis> source = enumerable;
                        bool flag4 = ins.Kind == Jihas.OrthoVision;
                        if (flag4)
                        {
                            source = list3.Where(delegate (Analysis x)
                            {
                                if (x.AnalysisState != AnalysisState.EnCours)
                                {
                                    if (x.AnalysisState == AnalysisState.EnvoyerAutomate)
                                    {
                                        int? instrumentId2 = x.InstrumentId;
                                        int instrumentId3 = ins.InstrumentId;
                                        if (!(instrumentId2.GetValueOrDefault() == instrumentId3 & instrumentId2 != null))
                                        {
                                            goto IL_44;
                                        }
                                    }
                                    return x.AnalysisState == AnalysisState.NonConforme;
                                }
                            IL_44:
                                return true;
                            }).ToList<Analysis>();
                        }
                        AstmHigh._logger.Info("list Count :" + list3.Count.ToString());
                        bool flag5 = kind == Jihas.Navify && ins.Mode == 1;
                        bool flag6 = flag5;
                        if (flag6)
                        {
                            source = source.Where(delegate (Analysis x)
                            {
                                int? analysisStatusId = x.AnalysisStatusId;
                                int? instrumentId2 = x.InstrumentId;
                                return !(analysisStatusId.GetValueOrDefault() == instrumentId2.GetValueOrDefault() & analysisStatusId != null == (instrumentId2 != null));
                            });
                        }
                        List<Analysis> list4 = source.ToList<Analysis>();
                        AstmHigh._logger.Info("list3 Count :" + list4.Count.ToString());
                        foreach (Analysis analysis in list3)
                        {
                            Logger logger2 = AstmHigh._logger;
                            AnalysisType analysisType = analysis.AnalysisType;
                            logger2.Info(((analysisType != null) ? analysisType.AnalysisTypeName : null) + " " + analysis.AnalysisState.ToString());
                        }
                        foreach (Analysis analysis2 in list4)
                        {
                            AstmHigh._logger.Info("AnalysisTypeName: " + analysis2.AnalysisType.AnalysisTypeName);
                            if (!(instrumentStd == "B"))
                            {
                                goto IL_43C;
                            }
                            int? instrumentId = analysis2.InstrumentId;
                            int num = ins.InstrumentId;
                            if (instrumentId.GetValueOrDefault() == num & instrumentId != null)
                            {
                                goto IL_43C;
                            }
                            bool flag7 = analysis2.AnalysisType.RespectDefaultInstrument;
                        IL_43D:
                            bool flag8 = flag7;
                            if (flag8)
                            {
                                AstmHigh._logger.Info("std == B");
                                continue;
                            }
                            string text2 = null;
                            bool flag9 = flag5;
                            if (flag9)
                            {
                                instrumentId = analysis2.InstrumentId;
                                num = 5;
                                bool flag10 = instrumentId.GetValueOrDefault() == num & instrumentId != null;
                                if (flag10)
                                {
                                    text2 = "TRI_ALINITY";
                                }
                                else
                                {
                                    instrumentId = analysis2.InstrumentId;
                                    num = 50007;
                                    bool flag11 = instrumentId.GetValueOrDefault() == num & instrumentId != null;
                                    if (flag11)
                                    {
                                        text2 = "TRI_ALINITY2";
                                    }
                                    else
                                    {
                                        instrumentId = analysis2.InstrumentId;
                                        num = 46;
                                        bool flag12 = instrumentId.GetValueOrDefault() == num & instrumentId != null;
                                        if (flag12)
                                        {
                                            text2 = "TRI_SMART";
                                        }
                                        else
                                        {
                                            instrumentId = analysis2.InstrumentId;
                                            num = 2;
                                            bool flag13 = instrumentId.GetValueOrDefault() == num & instrumentId != null;
                                            if (flag13)
                                            {
                                                text2 = "TRI_ALEGRIA";
                                            }
                                            else
                                            {
                                                instrumentId = analysis2.InstrumentId;
                                                num = 9;
                                                bool flag14;
                                                if (!(instrumentId.GetValueOrDefault() == num & instrumentId != null))
                                                {
                                                    instrumentId = analysis2.InstrumentId;
                                                    num = 28;
                                                    flag14 = (instrumentId.GetValueOrDefault() == num & instrumentId != null);
                                                }
                                                else
                                                {
                                                    flag14 = true;
                                                }
                                                bool flag15 = flag14;
                                                if (flag15)
                                                {
                                                    AnalysisTypeInstrumentMapping analysisTypeInstrumentMapping = AstmHigh.FindMap(analysis2.Instrument, analysis2);
                                                    text2 = ((analysisTypeInstrumentMapping != null) ? analysisTypeInstrumentMapping.AnalysisTypeCode : null);
                                                }
                                            }
                                        }
                                    }
                                }
                                analysis2.AnalysisStatusId = analysis2.InstrumentId;
                            }
                            else
                            {
                                AnalysisTypeInstrumentMapping analysisTypeInstrumentMapping2 = AstmHigh.FindMap(ins, analysis2);
                                AstmHigh._logger.Info("MappingId :" + ((analysisTypeInstrumentMapping2 != null) ? new long?(analysisTypeInstrumentMapping2.MappingId) : null).ToString());
                                text2 = ((analysisTypeInstrumentMapping2 != null) ? analysisTypeInstrumentMapping2.AnalysisTypeCode : null);
                            }
                            AstmHigh._logger.Info("AnalysisTypeCode:" + text2);
                            bool flag16 = text2 == null;
                            if (flag16)
                            {
                                AstmHigh._logger.Info("code == null");
                                bool flag17 = text == "C";
                                if (flag17)
                                {
                                    analysis2.AnalysisState = AnalysisState.EnvoyerAutomate;
                                }
                                continue;
                            }
                            bool flag18 = list.Contains(kind);
                            if (flag18)
                            {
                                string str = text2.Split(new char[]
                                {
                                    '-'
                                }).Last<string>();
                                text2 = text2.Replace("-" + str, "");
                            }
                            bool flag19 = kind == Jihas.Bioplex || kind == Jihas.Atellica;
                            if (flag19)
                            {
                                text2 = text2.Split(new char[]
                                {
                                    '^'
                                }).First<string>();
                            }
                            bool flag20 = kind == Jihas.Immulite2000 && ins.Mode == 1;
                            if (flag20)
                            {
                                bool flag21 = text2.Contains("-");
                                if (!flag21)
                                {
                                    analysis2.AnalysisState = AnalysisState.EnvoyerAutomate;
                                    continue;
                                }
                                text2 = text2.Split(new char[]
                                {
                                    '-'
                                })[1];
                            }
                            bool flag22 = string.IsNullOrWhiteSpace(text2);
                            if (flag22)
                            {
                                bool flag23 = kind == Jihas.Vitros350 && text2 == " ";
                                if (!flag23)
                                {
                                    bool flag24 = text == "C";
                                    if (flag24)
                                    {
                                        analysis2.AnalysisState = AnalysisState.EnvoyerAutomate;
                                    }
                                    continue;
                                }
                                AstmHigh._logger.Info("glycemie espace");
                            }
                            AstmHigh._logger.Info("Code:" + text2);
                            list2.Add(text2);
                            bool flag25 = flag5;
                            if (flag25)
                            {
                                bool flag26 = text2.StartsWith("TRI_");
                                if (!flag26)
                                {
                                    analysis2.AnalysisState = AnalysisState.EnvoyerAutomate;
                                }
                            }
                            else
                            {
                                AstmHigh._logger.Info("x.AnalysisId:" + analysis2.AnalysisId.ToString());
                                analysis2.AnalysisState = AnalysisState.EnvoyerAutomate;
                                analysis2.InstrumentId = new int?(ins.InstrumentId);
                            }
                            continue;
                        IL_43C:
                            flag7 = false;
                            goto IL_43D;
                        }
                        try
                        {
                            laboContext.SaveChanges();
                        }
                        catch (DbUpdateException ex)
                        {
                            Ct.LogError("DbUpdateException during SaveChanges", ex);
                            throw;
                        }
                        catch (DbEntityValidationException ex2)
                        {
                            Ct.LogValidationError("DbEntityValidationException during SaveChanges", ex2);
                            throw;
                        }
                        catch (Exception ex3)
                        {
                            Ct.LogError("Unexpected error during SaveChanges", ex3);
                            throw;
                        }
                        result = list2.Distinct<string>().ToList<string>();
                    }
                }
            }
            return result;
        }

        private static AnalysisTypeInstrumentMapping FindMap(Instrument ins, Analysis x)
        {
            return (from m in x.AnalysisType.AnalysisTypeInstrumentMappings
                    where m.InstrumentCode == ins.InstrumentCode
                    select m into y
                    orderby y.IsDefault descending
                    select y).FirstOrDefault<AnalysisTypeInstrumentMapping>();
        }

        private static string EncodeSamplesBa200(List<Sample> samples, ASTM_Message msg, Instrument instrument)
        {
            bool flag = samples.Count == 0;
            string result;
            if (flag)
            {
                result = null;
            }
            else
            {
                string text = instrument.Now.ToString("yyyyMMddHHmmss");
                string text2 = string.Concat(new string[]
                {
                    "H|\\^&|",
                    msg.f_message_id,
                    "||Host|||||BA200||P|LIS2A|",
                    text,
                    "<CR>"
                });
                int num = 1;
                foreach (Sample sample in samples)
                {

                    string source = TextUtil.GetSource(sample.SampleSource.SampleSourceCode);
                    string formattedSampleCode = sample.FormattedSampleCode;
                    Patient patient = sample.AnalysisRequest.Patient;
                    DateTime? dateTime = patient.PatientDateNaiss;

                    string text3 = ((patient.PatientDateNaiss != null) ? dateTime.GetValueOrDefault().ToString("yyyyMMdd") : null) ?? "20000101";
                    List<string> testsPreffix = AstmHigh.GetTestsPreffix(sample, instrument, instrument.Prop.Repeater, false);
                    bool flag2 = testsPreffix.Count > 0;
                    if (flag2)
                    {
                        text2 += string.Format("P|{0}||{1}||{2}^{3}||{4}|{5}<CR>", new object[]
                        {
                            num,
                            formattedSampleCode,
                            patient.Nom,
                            patient.Prenom,
                            text3,
                            patient.ShortSexe
                        });
                        for (int i = 0; i < testsPreffix.Count; i++)
                        {
                            text2 += string.Format("O|{0}|{1}||{2}|R||||||A||||{3}||||||||||O<CR>", new object[]
                            {
                                i + 1,
                                formattedSampleCode,
                                testsPreffix[i],
                                source
                            });
                        }
                    }
                    else
                    {
                        text2 += string.Format("P|{0}<CR>", num);
                        text2 = text2 + "O|1|" + formattedSampleCode + "|||R||||||||||||||||||||Y\\Q<CR>";
                    }
                    num++;
                }
                text2 += "L|1|N<CR>";
                result = text2;
            }
            return result;
        }

        private static void ParseAdvia(string msg, AstmManager manager)
        {
            bool flag = msg.StartsWith("R ");
            if (flag)
            {
                msg = "\r" + msg;
                string[] array = msg.Split(new string[]
                {
                    "\rR "
                }, StringSplitOptions.RemoveEmptyEntries);
                for (int i = 0; i < array.Length; i++)
                {
                    AstmHigh.LoadRes(array[i], manager, i);
                }
            }
            else
            {
                bool flag2 = msg.StartsWith("Q ");
                if (flag2)
                {
                    List<string> list = TextUtil.Split(msg.Substring(9), 13).ToList<string>();
                    string text = "";
                    foreach (string rid in list)
                    {
                        text += AstmHigh.HandleRequest(rid, manager.Instrument);
                    }
                    bool flag3 = text != "";
                    if (flag3)
                    {
                        manager.PutMsgInSendingQueue(text, 0L);
                    }
                }
            }
        }

        public static void LoadRes(string msg, AstmManager manager, int i)
        {
            try
            {
                string text = msg.Substring(23, 12);
                AstmHigh._logger.Info(text);
                string str = msg.Substring((i == 0) ? 93 : 43);
                AstmHigh._logger.Debug("msg= " + msg);
                List<string> list = TextUtil.Split(str, 19).ToList<string>();
                AstmHigh._logger.Debug<int>(list.Count);
                List<string[]> records = (from x in list
                                          select new string[]
                                          {
                    x.Substring(0, 3),
                    x.Substring(4, 9),
                    x.Substring(13, 3)
                                          }).ToList<string[]>();
                JihazResult result = AU480MessageHandler.GetResult(records, text);
                AstmHigh.LoadResults(result, manager.Instrument, null);
            }
            catch (Exception @object)
            {
                AstmHigh._logger.Error(new LogMessageGenerator(@object.ToString));
            }
        }

        public static string HandleRequest(string rid, Instrument instrument)
        {
            LaboContext laboContext = new LaboContext();
            AstmHigh._logger.Info(rid);
            long code = 0;
            bool flag = rid.Length != 13 || !long.TryParse(rid, out code);
            string result;
            if (flag)
            {
                AstmHigh._logger.Error(rid + " not long");
                result = null;
            }
            else
            {
                Sample sample = laboContext.Sample.FirstOrDefault((Sample x) => x.SampleCode == (long?)code);
                bool flag2 = sample == null;
                if (flag2)
                {
                    AstmHigh._logger.Error(string.Format("{0} sample not found ", code));
                    result = null;
                }
                else
                {
                    Patient patient = sample.AnalysisRequest.Patient;
                    string text = (patient.Nom + " " + patient.Prenom).PadRight(32).Truncate(32);
                    string text2 = instrument.Now.ToString("yyyyMMdd");
                    List<string> tests = AstmHigh.GetTests(sample.SampleId, instrument, false);
                    bool flag3 = tests.Count == 0;
                    if (flag3)
                    {
                        result = "";
                    }
                    else
                    {
                        string text3 = tests.Count.ToString().PadLeft(3, '0');
                        string text4 = tests.Aggregate("", (string current, string a) => current + a.PadLeft(3, ' ') + "M");
                        result = string.Format("O 0101{0}N0{1}       {2}{3}{4:D3}{5} 1.011{6}{7}", new object[]
                        {
                            text3,
                            rid,
                            text,
                            patient.ShortSexe,
                            patient.Age,
                            text2,
                            text4,
                            '\r'
                        });
                    }
                }
            }
            return result;
        }

        private static Logger _logger = LogManager.GetCurrentClassLogger();

        private static DateTime _endTime = DateTime.Now.Date;

        private static List<Jihas> _aclLike = new List<Jihas>
        {
            Jihas.SysmexCA600,
            Jihas.OrthoVision,
            Jihas.CobasC311
        };
    }
}
