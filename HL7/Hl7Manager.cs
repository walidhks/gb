using System;
using System.Collections.Generic;
using System.Linq;
using System.Timers;
using GbService.ASTM;
using GbService.Common;
using GbService.Communication;
using GbService.Communication.Common;
using GbService.Communication.Serial;
using GbService.HL7.V231;
using GbService.Model.Contexts;
using GbService.Model.Domain;
using NLog;

namespace GbService.HL7
{
    public class Hl7Manager
    {
        public Hl7Manager(ILowManager il, Instrument instrument, bool biolis30Up = false)
        {
            this._il = il;
            this._instrument = instrument;
            this._il.MessageReceived += this.OnMessageReceived;
            if (biolis30Up)
            {
                Hl7Manager.ScheduleRefresh(30, delegate (object sender, ElapsedEventArgs e)
                {
                    this.Upload();
                });
            }
        }

        public void Upload()
        {
            List<Sample> samples = AstmHigh.GetSamples(this._instrument, null);
            foreach (Sample sample in samples)
            {
                string result = AstmHigh.EncodeSample2(sample, this._instrument, "");
                this.SendLowHL7(result);
            }
        }

        private static void ScheduleRefresh(int seconds, Gb.Refresh r)
        {
            Timer timer = new Timer
            {
                Enabled = true,
                Interval = (double)(1000 * seconds)
            };
            timer.Elapsed += delegate (object s, ElapsedEventArgs a)
            {
                r(s, a);
            };
            timer.Start();
        }

        private void OnMessageReceived(object sender, MessageReceivedEventArgs messageReceivedEventArgs)
        {
            this.HandleMessage(messageReceivedEventArgs.Message);
        }

        public static void HandleMessagePro(string s, Instrument instrument)
        {
            JihazResult result = Hl7Manager.GetResult(s, instrument);
            AstmHigh.LoadResults(result, instrument, null);
        }

        public static void HandleHprim(string msg, Instrument instrument)
        {
            Hl7Manager._logger.Debug(msg);
            string[] array = msg.Split(new string[] { "\rOBR|" }, StringSplitOptions.None);
            string[] array2 = array;
            int i = 0;
            while (i < array2.Length)
            {
                string text = array2[i];
                try
                {
                    bool flag = !text.Contains("OBX");
                    if (!flag)
                    {
                        JihazResult result = Hl7Manager.GetResult("OBR|" + text, instrument);
                        AstmHigh.LoadResults(result, instrument, null);
                    }
                }
                catch (Exception ex)
                {
                    Hl7Manager._logger.Error(new LogMessageGenerator(ex.ToString));
                }
                i++;
            }
        }

        public void HandleMessage(string s)
        {
            try
            {
                Jihas kind = this._instrument.Kind;

               
                List<Jihas> list = new List<Jihas>
                {
                    Jihas.Biolis30i,
                    Jihas.Urit8031,
                    Jihas.I15,
					Jihas.Urit3000, 
					Jihas.YumizenH550,
                    Jihas.HumaCount80,
                    Jihas.F200,
                    Jihas.Ichroma,
                    Jihas.C3100
                };

                string text = this._instrument.Now.ToString("yyyyMMddHHmmss");
                bool flag = kind == Jihas.LabExpert;
                if (flag)
                {
                    bool flag2 = s.Contains("||ORU^R01|");
                    if (flag2)
                    {
                        Hl7Manager.HandleMessagePro(s, this._instrument);
                    }
                }
                bool flag3 = kind == Jihas.CobasPro;
                if (flag3)
                {
                    string field = Hl7Manager.GetField(s, this._instrument, "MSH", 9, true);
                    bool flag4 = s.Contains("|ESU^U01^ESU_U01|");
                    if (flag4)
                    {
                        this.SendLowHL7(string.Format("MSH|^~\\&|Host||cobas pure||{0}+0900||ACK^U01^ACK|{1}|P|2.5.1|||NE|AL||UNICODE UTF-8|||ROC-02^ROCHE{2}MSA|AA|{3}{4}", new object[]
                        {
                            text,
                            field,
                            '\r',
                            field,
                            '\r'
                        }));
                    }
                    else
                    {
                        bool flag5 = s.Contains("|INU^U05^INU_U05|");
                        if (flag5)
                        {
                            this.SendLowHL7(string.Format("MSH|^~\\&|Host||cobas pure||{0}+0900||ACK^U05^ACK|{1}|P|2.5.1|||NE|AL||UNICODE UTF-8|||ROC-04^ROCHE{2}MSA|AA|{3}{4}", new object[]
                            {
                                text,
                                field,
                                '\r',
                                field,
                                '\r'
                            }));
                        }
                        else
                        {
                            bool flag6 = s.Contains("||QBP^Q11^QBP_Q11|");
                            if (flag6)
                            {
                                this.HandleRequest(s, this._instrument);
                            }
                            else
                            {
                                bool flag7 = s.Contains("||OUL^R22^OUL_R22|");
                                if (flag7)
                                {
                                    Hl7Manager.HandleMessagePro(s, this._instrument);
                                    this.SendLowHL7(string.Format("MSH|^~\\&|Host||cobas pro||{0}+0100||ACK^R22^ACK|abcd|P|2.5.1|||||||UNICODE UTF-8||LAB-27R^ROCHE{1}MSA|AA|2{2}", text, '\r', '\r'));
                                }
                            }
                        }
                    }
                }
                else
                {
                    bool flag8 = kind == Jihas.DiruiCsT180 || kind == Jihas.ZybioExc200 ||kind==Jihas.Urit8031;
                    if (flag8)
                    {
                        bool flag9 = s.Contains("|QRY^Q01|") || s.Contains("|QRY^Q02|");
                        if (flag9)
                        {
                            string text2 = Helper.Ack = Hl7Manager.GetField(s, this._instrument, "MSH", 9, true);
                            this.SendLowHL7((kind == Jihas.ZybioExc200) ? string.Format("MSH|^~\\&|||||{0}||QCK^Q02|{1}|P|2.5||||||UTF-8|||{2}MSA|AA|{3}|Message accepted|||0{4}ERR|0||||||||||||{5}QAK|SR|OK|||||{6}", new object[]
                            {
                                text,
                                text2,
                                '\r',
                                text2,
                                '\r',
                                '\r',
                                '\r'
                            }) : string.Format("MSH|^~\\&|GbServiceBMACK||Autobio||{0}||ACK^R01|1|P|2.3.1||||||UNICODE UTF-8{1}MSA|AA|{2}|Message accepted{3}",
                                                        DateTime.Now.ToString("yyyyMMddHHmmss"), '\r', 1, '\r'));
                            this.HandleRequestSimple(s, this._instrument);
                        }
                        else
                        {
                            bool flag10 = s.Contains("|ORU^R01|");
                            if (flag10)
                            {
                                Hl7Manager.HandleMessagePro(s, this._instrument);
                            }
                        }
                    }
                    else
                    {
                        bool flag11 = kind == Jihas.Cobas8000;
                        if (flag11)
                        {
                            bool flag12 = s.Contains("\rQPD|TSREQ|");
                            if (flag12)
                            {
                                this.HandleRequest(s, this._instrument);
                            }
                            else
                            {
                                bool flag13 = s.Contains("||OUL^R22|");
                                if (flag13)
                                {
                                    Hl7Manager.HandleMessagePro(s, this._instrument);
                                    this.SendLowHL7(string.Format("MSH|^~\\&|BMLab||cobas.8000||{0}||ACK|0857666||2.5||||NE||UNICODE.UTF-8{1}MSA|AA|2{2}", text, '\r', '\r'));
                                }
                            }
                        }
                        else
                        {
                            bool flag14 = kind == Jihas.C3100;
                            if (flag14)
                            {
                                Hl7Manager._logger.Info(s);
                                bool flag15 = s.Contains("|ORM^O01|");
                                if (flag15)
                                {
                                    this.HandleRequestSimple(s, this._instrument);
                                }
                                else
                                {
                                    bool flag16 = s.Contains("|ORU^R01|");
                                    if (flag16)
                                    {
                                        Hl7Manager.HandleMessagePro(s, this._instrument);
                                        this.SendLowHL7(string.Format("MSH|^~\\&|||Mindray||{0}||ACK^R01|2|P|2.3.1||||0||ASCII|||{1}MSA|AA|2||||0|{2}", text, '\r', '\r'));
                                    }
                                }
                            }
                            else
                            {
                                bool flag17 = kind == Jihas.MindrayCL1000i;
                                if (flag17)
                                {
                                    bool flag18 = s.Contains("ORU^R01");
                                    if (flag18)
                                    {
                                        Hl7Manager.HandleMessagePro(s, this._instrument);
                                    }
                                }

                                bool flag19 = list.Contains(kind);
                                // This flag19 block traps Autobio 
                                if (flag19)
                                {
                                    Hl7Manager.HandleMessagePro(s, this._instrument);
                                    bool flag20 = kind == Jihas.Biolis30i;
                                    if (!flag20)
                                    {
                                        bool flag21 = kind == Jihas.F200;
                                        if (flag21)
                                        {
                                            this.SendLowHL7(string.Format("MSH|^~\\&|||FA20EAITG3967^70b3d573724018f7^EUI-64||{0}||ACK^R01|BM_1|P|2.6||||||UNICODE UTF-8{1}MSA|AA|2|Message accepted|||0|{2}", text, '\r', '\r'));
                                        }
                                        else
                                        {
                                            bool flag22 = kind == Jihas.Urit3000 && this._instrument.Mode == 4;
                                            if (flag22)
                                            {
                                                this.SendLowHL7(string.Format("MSH|^~\\&|Zybio|BMLab|Zybio|Z6|{0}||ACK^R01|{1}|P|2.3.1||||||UNICODE|||{2}MSA|AA|{3}|Message accepted|||0|{4}", new object[]
                                                {
                                                    text,
                                                    text,
                                                    '\r',
                                                    text,
                                                    '\r'
                                                }));
                                            }
                                            else
                                            {
                                                bool flag23 = kind == Jihas.Urit3000 && this._instrument.Mode == 8;
                                                if (flag23)
                                                {
                                                    this.SendLowHL7(string.Format("MSH|^~\\&|||Mindray|BS-330E|{0}||ACK^R01|164|P|2.3.1||||0||ASCII|||<CR>MSA|AA|164|Message accepted|||0|{1}", text, '\r'));
                                                }
                                                else
                                                {
                                                    this.SendLowHL7(string.Format("MSH|^~\\&|||BM850^HL7MW||{0}||ACK^R01|BM_1|P|2.7||||||UNICODE UTF-8{1}MSA|AA|2|Message accepted|||0|{2}", text, '\r', '\r'));
                                                }
                                            }
                                        }
                                    }
                                }
                                else
                                {
                                    bool flag24 = kind == Jihas.Chemray240;
                                    if (flag24)
                                    {
                                        s = s.Replace("P|2.5||||||UNICODE\r", "P|2.3.1||||||UNICODE\r");
                                    }
                                    bool flag25 = kind == Jihas.Rt7600S;
                                    if (flag25)
                                    {
                                        s = s.Replace("MSH|||^~\\&", "MSH|^~\\&");
                                        s = s.Replace('\u001c', '\r');
                                        s = s.Replace("\r\r", "\u001c\r");
                                    }
                                    bool flag26 = kind == Jihas.Medonic;
                                    if (flag26)
                                    {
                                        s = s.Replace("|P|2.7||||||UNICODE UTF-8", "|P|2.3.1||||||UNICODE UTF-8");
                                    }
                                    bool flag27 = kind == Jihas.BC5150 || kind == Jihas.Kt6610 || kind == Jihas.BC5380 || kind == Jihas.MindrayH50P;
                                    if (flag27)
                                    {
                                        string[] array = s.Split(new string[]
                                        {
                                            "\u001c\r"
                                        }, StringSplitOptions.RemoveEmptyEntries);
                                        foreach (string text3 in array)
                                        {
                                            int num = text3.IndexOf('\v');
                                            int length = text3.Length;
                                            bool flag28 = num > -1 && length > -1;
                                            if (flag28)
                                            {
                                                string text4 = text3.Substring(num + 1);
                                                text4 = text4.Substring(0, length - num - 1);
                                                text4 = text4.Replace("|IS|", "|NM|");
                                                this.Parse(text4);
                                            }
                                            else
                                            {
                                                Hl7Manager._logger.Fatal("Fatal Error");
                                            }
                                        }
                                    }
                                    else
                                    {
                                        bool flag29 = kind == Jihas.MindrayBs200 || kind == Jihas.Rt7600S || kind == Jihas.MindrayCL1000i || kind == Jihas.Bs240 || kind == Jihas.Chemray240 || kind == Jihas.Icubio || kind == Jihas.Bs300 || kind == Jihas.Medonic || kind == Jihas.Autobio ||kind == Jihas.CelltacMEK9100 ;
                                        if (flag29)
                                        {
                                            string text5 = s;
                                            int num2 = text5.IndexOf('\v');
                                            int num3 = text5.IndexOf('\u001c');

                                            if (num2 >= 0 && num3 > num2)
                                            {
                                                text5 = text5.Substring(num2 + 1);
                                                text5 = text5.Substring(0, num3 - num2 - 1);
                                            }
                                            // -----------------------------------------------------------
                                            // CELLTAC MEK-9100 LOGIC (Updated for OUL^R22)
                                            // -----------------------------------------------------------
                                            if (kind == Jihas.CelltacMEK9100)
                                            {
                                                // 1. Handle Query (QBP^Q11)
                                                if (text5.Contains("QBP^Q11"))
                                                {
                                                    // ... (Keep your existing Query Logic here) ...
                                                    string msgControlId = Hl7Manager.GetField(text5, this._instrument, "MSH", 9, false);
                                                    string queryTag = Hl7Manager.GetField(text5, this._instrument, "QPD", 2, false);
                                                    string barcodeField = Hl7Manager.GetField(text5, this._instrument, "QPD", 3, false);
                                                    string barcode = barcodeField.Split('^')[0];

                                                    Sample sample = AstmHigh.GetSample(barcode, null);

                                                    if (sample != null)
                                                    {
                                                        string response = AstmHigh.EncodeSample2(sample, this._instrument, msgControlId + "|" + queryTag);
                                                        this.SendLowHL7(response);
                                                    }
                                                }
                                                // 2. Handle Results (ORU^R01 OR OUL^R22)
                                                // [FIX] Added check for OUL^R22
                                                else if (text5.Contains("ORU^R01") || text5.Contains("OUL^R22"))
                                                {
                                                    // Clean up the message: MEK-9100 sends "" for empty fields, which breaks parsing
                                                    string cleanMsg = text5.Replace("\"\"", "");

                                                    // 1. Parse and Save Results
                                                    // HandleMessagePro uses GetResult. We pass the cleaned message.
                                                    // Note: If Sample ID is missing in the message, this will fail to save to DB.
                                                    Hl7Manager.HandleMessagePro(cleanMsg, this._instrument);

                                                    // 2. Send ACK
                                                    string msgId = Hl7Manager.GetField(cleanMsg, this._instrument, "MSH", 9, false);
                                                    string ack = string.Format("MSH|^~\\&|GbService|LIS|MEK9100||{0}||ACK^R22|1|P|2.3.1\rMSA|AA|{1}\r",
                                                        DateTime.Now.ToString("yyyyMMddHHmmss"), msgId);

                                                    this.SendLowHL7(ack);
                                                }
                                            }
                                            // [FIX] Autobio Logic (Query & Result)
                                            if (kind == Jihas.Autobio)
                                            {
                                                if (text5.Contains("QRY^Q01"))
                                                {
                                                    try
                                                    {
                                                        // 1. Extract Barcode
                                                        string barcode = Hl7Manager.GetField(text5, this._instrument, "QRD", 8, true);
                                                        // 2. Find Sample
                                                        Sample sample = AstmHigh.GetSample(barcode, null);
                                                        if (sample != null)
                                                        {
                                                            // 3. Send Order
                                                        
                                                            string orderMessage = AstmHigh.EncodeSample2(sample, this._instrument, text5);
                                                            this.SendLowHL7(orderMessage);
                                                        }
                                                    }
                                                    catch (Exception ex) { Hl7Manager._logger.Error(ex.ToString()); }
                                                }
                                                else
                                                {
                                                    // 1. Parse Result (Save to DB)
                                                    Hl7Manager.HandleMessagePro(text5, this._instrument);

                                                    // 2. Send ACK
                                                    string msgId = Hl7Manager.GetField(text5, this._instrument, "MSH", 9, false);
                                                    string ack = string.Format("MSH|^~\\&|GbServiceBMACK||Autobio||{0}||ACK^R01|1|P|2.3.1||||||UNICODE UTF-8{1}MSA|AA|{2}|Message accepted{3}",
                                                        DateTime.Now.ToString("yyyyMMddHHmmss"), '\r', msgId, '\r');
                                                    this.SendLowHL7(ack);
                                                }
                                            }
                                            else
                                            {
                                                this.Parse(text5);
                                            }
                                        }



                                        else
                                        {
                                            // [FIX] Urit3000 Mode 3 Logic
                                            bool flag33 = kind == Jihas.Urit3000 && this._instrument.Mode == 3;
                                            if (flag33)
                                            {
                                                if (s.Contains("QRY^Q01"))
                                                {
                                                    try
                                                    {
                                                        string barcode = Hl7Manager.GetField(s, this._instrument, "QRD", 8, true);
                                                        Sample sample = AstmHigh.GetSample(barcode, null);
                                                        if (sample != null)
                                                        {
                                                            string orderMessage = AstmHigh.EncodeSample2(sample, this._instrument, "text5");
                                                            this.SendLowHL7(orderMessage);
                                                        }
                                                    }
                                                    catch (Exception ex) { Hl7Manager._logger.Error(ex.ToString()); }
                                                }
                                                else
                                                {
                                                    Hl7Manager.HandleMessagePro(s, this._instrument);
                                                    string msgId = Hl7Manager.GetField(s, this._instrument, "MSH", 9, false);
                                                    string ack = string.Format("MSH|^~\\&|GbService||Autobio||{0}||ACK^R01|1|P|2.3.1||||||UNICODE UTF-8{1}MSA|AA|{2}|Message accepted{3}",
                                                        DateTime.Now.ToString("yyyyMMddHHmmss"), '\r', msgId, '\r');
                                                    this.SendLowHL7(ack);
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
            catch (Exception ex)
            {
                Hl7Manager._logger.Error(ex.ToString());
            }
        }

        public void HandleRequest(string s, Instrument instrument)
        {
            string arg = this._instrument.Now.ToString("yyyyMMddHHmmss");
            string field = Hl7Manager.GetField(s, instrument, "QPD", 2, true);
            string field2 = Hl7Manager.GetField(s, instrument, "QPD", 3, true);
            string field3 = Hl7Manager.GetField(s, instrument, "QPD", 4, true);
            string field4 = Hl7Manager.GetField(s, instrument, "QPD", 5, true);
            string line = Hl7Manager.GetLine(s, instrument, "QPD");
            bool flag = instrument.Kind == Jihas.CobasPro;
            if (flag)
            {
                string text = string.Format("MSH|^~\\&|Host||cobas pro||{0}+0200||RSP^K11^RSP_K11|1235|P|2.5.1|||||UNICODE UTF-8|||LAB-27R^ROCHE{1}MSA|AA|2{2}", arg, '\r', '\r');
                text = string.Concat(new string[]
                {
                    text,
                    "QAK|",
                    field,
                    "|OK|INIBAR^^99ROC\r",
                    line,
                    "\r"
                });
                this.SendLowHL7(text);
            }
            Sample sample = AstmHigh.GetSample(field2, null);
            string result = AstmHigh.EncodeSample2(sample, instrument, field3 + "|" + field4);
            this.SendLowHL7(result);
        }

        public void HandleRequestSimple(string s, Instrument instrument)
        {
            Talab talab = new Talab(instrument);
            string field = Hl7Manager.GetField(s, instrument, talab.Code, talab.Codei, true);
            Sample sample = AstmHigh.GetSample(field, null);
            bool flag = sample == null;
            if (!flag)
            {
                string field2 = Hl7Manager.GetField(s, instrument, talab.Info, talab.Infoi, false);
                string result = AstmHigh.EncodeSample2(sample, instrument, field2);
                this.SendLowHL7(result);
            }
        }

        public static JihazResult GetResult(string s, Instrument instrument)
        {
            Hl7Manager._logger.Debug(s);
            if (instrument.Kind == Jihas.F200)
            {
                s = s.Replace("\rNTE|", "|");
            }
            Util util = Util.Init(instrument);
            s = s.Replace(Tu.NL, "\r");
            s = s.Replace("\n\n", "\r");
            s = s.Replace('\n'.ToString(), "\r");
            List<string[]> source = (from x in s.Split(new char[]
            {
                '\r'
            })
                                     select x.Split(new char[]
                                     {
                '|'
                                     })).ToList<string[]>();
            int num = source.Count((string[] x) => x[0] == util.Obr);
            if (num > 1)
            {
                Hl7Manager._logger.Info<int>(num);
                Hl7Manager._logger.Info(s);
                foreach (string[] array in from x in source
                                           where x[0] == util.Obr
                                           select x)
                {
                    Hl7Manager._logger.Info(array[0] + "::" + array[1]);
                }
            }
            string[] array2 = source.SingleOrDefault((string[] x) => x[0] == util.Obr);
            JihazResult result;
            if (array2 == null)
            {
                result = null;
            }
            else
            {
                Jihas kind = instrument.Kind;
                string text = Hl7Manager.ConvertScode(array2[util.Obri], kind);
                if (instrument.Kind == Jihas.Urit3000 && instrument.Mode == 5)
                {
                    text = source.SingleOrDefault((string[] x) => x[0] == "OBX" && x[3] == "ID1")[5];
                }
                if (instrument.Kind == Jihas.Mpl)
                {
                    text = text.Split(new char[]
                    {
                        '^'
                    })[0];
                }
                if (instrument.Kind == Jihas.Evm)
                {
                    text = text.Split(new char[]
                    {
                        '~'
                    })[0];
                }
                JihazResult jihazResult = new JihazResult(text);
                foreach (string[] array3 in from x in source
                                            where x[0] == util.Obx
                                            select x)
                {
                    try
                    {
                        Hl7Manager._logger.Info(string.Format("util.Obxi = {0}, util.Obxj = {1}", util.Obxi, util.Obxj));
                        string text2 = Hl7Manager.ConvertCode(array3[util.Obxi], instrument, util);
                        string[] array4 = array3[util.Obxj].Split(new char[]
                        {
                            '^'
                        });
                        string text3 = (array4.Length > 1) ? array4[1] : array4[0];

                        // [FIX] Result Value Cleaning (Remove ~)
                        if (kind == Jihas.Autobio)
                        {
                            text3 = text3.Replace("~", "").Trim();
                        }

                        int? num2 = (kind == Jihas.Navify && instrument.Mode == 1) ? Hl7Manager.GetMplInstrument(array3[13]) : new int?(0);
                        if (kind == Jihas.MindrayCL1000i && !string.IsNullOrEmpty(array3[9]))
                        {
                            text3 = array3[9];
                        }
                        if (kind == Jihas.C3100)
                        {
                            text2 = text2 + "-" + array3[6];
                        }
                        if (kind == Jihas.F200)
                        {
                            if (text2.StartsWith("SDB-06"))
                            {
                                text3 = array3[22].Replace("Cut Off Index,Value=", "");
                            }
                            if (text2.StartsWith("SDB-0690"))
                            {
                                text2 = array3[5].Split(new char[]
                                {
                                    '^'
                                })[0];
                            }
                        }
                        else if (kind == Jihas.Ichroma)
                        {
                            if (text2 == "COVID-19 Ab" || text2 == "IgM" || text2 == "IgG")
                            {
                                string[] array5 = text3.Split(" ");
                                text2 = array5[0];
                                text3 = array5[1];
                            }
                        }
                        else if (kind == Jihas.YumizenH550)
                        {
                            text2 = text2.Split(new char[]
                            {
                                '^'
                            })[0];
                        }
                        if (kind == Jihas.Urit3000 && instrument.Mode == 2)
                        {
                            List<string> list = text3.Split(new char[]
                            {
                                '^'
                            }).ToList<string>();
                            for (int i = 0; i < list.Count; i++)
                            {
                                jihazResult.Results.Add(new LowResult(text2 + "-" + (i + 1).ToString(), list[i], null, null, null));
                            }
                        }
                        else if (!text3.StartsWith("-268435455"))
                        {
                            List<LowResult> results = jihazResult.Results;
                            string code = text2;
                            string value = text3;
                            string unit = null;
                            string flag = null;
                            int? num3 = num2;
                            results.Add(new LowResult(code, value, unit, flag, (num3 != null) ? new long?((long)num3.GetValueOrDefault()) : null));
                        }
                    }
                    catch (Exception ex)
                    {
                        Hl7Manager._logger.Error(new LogMessageGenerator(ex.ToString));
                    }
                }
                result = jihazResult;
            }
            return result;
        }

        public static int? GetNavifyInstrument(string id)
        {
            bool flag = id == "8000";
            int? result;
            if (flag)
            {
                result = new int?(9);
            }
            else
            {
                bool flag2 = id == "6000";
                if (flag2)
                {
                    result = new int?(28);
                }
                else
                {
                    result = null;
                }
            }
            return result;
        }

        public static int? GetMplInstrument(string id)
        {
            return new int?(new LaboContext().Database.SqlQuery<int>("select InstrumentId from InstrumentMap where MapId = " + id, new object[0]).FirstOrDefault<int>());
        }

        public static string GetField(string s, Instrument instrument, string name, int index, bool convert = true)
        {
            s = s.Replace('\v'.ToString(), "");
            string[] source = s.Split(new char[]
            {
                '\r'
            });
            List<string[]> source2 = (from x in source
                                      select x.Split(new char[]
                                      {
                '|'
                                      })).ToList<string[]>();
            string[] array = source2.SingleOrDefault((string[] x) => x[0] == name);
            bool flag = array == null;
            string result;
            if (flag)
            {
                result = null;
            }
            else
            {
                Jihas kind = instrument.Kind;
                bool flag2 = index >= array.Length;
                if (flag2)
                {
                    result = null;
                }
                else
                {
                    string text = convert ? Hl7Manager.ConvertScode(array[index], kind) : array[index];
                    result = text;
                }
            }
            return result;
        }

        public static string GetLine(string s, Instrument instrument, string name)
        {
            string[] source = s.Split(new char[]
            {
                '\r'
            });
            return source.FirstOrDefault((string x) => x.StartsWith(name + "|"));
        }

        private static string ConvertScode(string s, Jihas jih)
        {
            return s.Split(new char[]
            {
                '^'
            })[0];
        }

        private static string ConvertValue(string s, Jihas jih)
        {
            return s.Split(new char[]
            {
                '^'
            })[0];
        }

        /*  private static string ConvertCode(string s, Instrument i, Util util)
          {
              Hl7Manager._logger.Info("code = " + s);
              Hl7Manager._logger.Info(s.Split(new char[]
              {
                  '^'
              })[0]);
              bool flag = util.Index != null;
              string result;
              if (flag)
              {
                  result = s.Split(new char[]
                  {
                      '^'
                  })[util.Index.Value];
              }
              else
              {
                  bool flag2 = Hl7Manager._list.Contains(i.Kind) || (i.Kind == Jihas.Urit3000 && i.Mode == 3);
                  if (flag2)
                  {
                      result = s.Split(new char[]
                      {
                          '^'
                      })[0];
                  }
                  else
                  {
                      bool flag3 = i.Kind == Jihas.LabExpert || (i.Kind == Jihas.Urit3000 && i.Mode == 4);
                      if (flag3)
                      {
                          result = s.Split(new char[]
                          {
                              '^'
                          })[1];
                      }

                          else
                      {
                          bool flag300 = Hl7Manager._list.Contains(i.Kind) || (i.Kind == Jihas.Urit3000 && i.Mode == 6);
                          if (flag300)
                          {
                              /* result = s.Split(new char[]
                               {
                               '_'
                               })[1];
                           }*/
        /* result = s.Replace("UD_", "XXXXXXXXXX");
     }
     else
     {
         result = s;
     }
 }
}

}
return result;
}*/
        private static string ConvertCode(string s, Instrument i, Util util)
        {
            Hl7Manager._logger.Info("code = " + s);
            string result;

            // ---------------------------------------------------------
            // 1. Extraction Logic (Determine WHICH part of ^ to take)
            // ---------------------------------------------------------
            if (util.Index != null)
            {
                // If S3 is used, use the index from S3
                result = s.Split(new char[] { '^' })[util.Index.Value];
            }
            else
            {
                // Default fallbacks if S3 is null
                if (Hl7Manager._list.Contains(i.Kind) || (i.Kind == Jihas.Urit3000 && i.Mode == 3))
                {
                    result = s.Split(new char[] { '^' })[0];
                }
                else if (i.Kind == Jihas.LabExpert || (i.Kind == Jihas.Urit3000 && i.Mode == 4))
                {
                    result = s.Split(new char[] { '^' })[1];
                }
                else
                {
                    result = s;
                }
            }

            // ---------------------------------------------------------
            // 2. Cleaning Logic (Run this AFTER extraction)
            // [FIX] Urit3000 Mode 6: Remove "UD_" prefix for urit us1000
            // This now runs even if S3 is used.
            // ---------------------------------------------------------
            if (i.Kind == Jihas.Urit3000 && i.Mode == 6 && !string.IsNullOrEmpty(result))
            {
                result = result.Replace("UD_", "");
            }

            Hl7Manager._logger.Info(result);
            return result;
        }

        public static string ReplaceHL7(string s, string pid, string obr, int i, int j)
        {
            string[] source = s.Split(new char[]
            {
                '\r'
            });
            List<string[]> source2 = (from x in source
                                      select x.Split(new char[]
                                      {
                '|'
                                      })).ToList<string[]>();
            string[] array = source2.FirstOrDefault((string[] x) => x[0] == pid);
            string[] array2 = source2.FirstOrDefault((string[] x) => x[0] == obr);
            array2[j] = array[i];
            return string.Join("\r", from x in source2
                                     select string.Join("|", x));
        }

        public void Parse(string m)
        {
            bool flag = this._instrument.Kind == Jihas.Rt7600S;
            if (flag)
            {
                m = Hl7Manager.ReplaceHL7(m, "PID", "OBR", 2, 2);
            }
            MessageHandler messageHandler = new MessageHandler(m, this._instrument);
            bool flag2 = messageHandler.MessageType == "ORU_R01";
            if (flag2)
            {
                string result = messageHandler.ParseMessage();
                this.SendLowHL7(result);
            }
            bool flag3 = messageHandler.MessageType == "QRY_Q02";
            if (flag3)
            {
                string result2 = messageHandler.ParseMessage();
                this.SendLowHL7(result2);
                QryQ02Handler qryQ02Handler = (QryQ02Handler)messageHandler.MessageParser;
                bool flag4 = qryQ02Handler.IsSampleExist();
                if (flag4)
                {
                    string dsrMessage = messageHandler.GetDsrMessage(qryQ02Handler.SampleCode, "1", null, null);
                    this.SendLowHL7(dsrMessage);
                }
                else
                {
                    bool isAllQuery = qryQ02Handler.IsAllQuery;
                    if (isAllQuery)
                    {
                        LaboContext db = new LaboContext();
                        DateTime start = qryQ02Handler.StartDateTime.AddDays(-30.0);
                        DateTime endDateTime = qryQ02Handler.EndDateTime;
                        List<Sample> samplesRange = AstmHigh.GetSamplesRange(start, endDateTime, db, this._instrument);
                        int num = 1;
                        foreach (Sample sample in samplesRange)
                        {
                            Hl7Manager._logger.Info("SampleCode : " + sample.SampleCode.ToString());
                            long? sampleCode = sample.SampleCode;
                            bool flag5 = sampleCode == null;
                            if (!flag5)
                            {
                                string dsrMessage2 = messageHandler.GetDsrMessage(sampleCode.Value, num.ToString(), (num != samplesRange.Count) ? num.ToString() : "", null);
                                this.SendLowHL7(dsrMessage2);
                                num++;
                            }
                        }
                    }
                }
            }
        }

        public void SendLowHL7(string result)
        {
            Coding enc = (this._instrument.Kind == Jihas.DiruiCsT180) ? Coding.Unicode : Coding.Asc;
            this._il.SendLow("\v" + result + "\u001c\r", enc);
        }

        public void Close()
        {
            this._il.Close();
        }

        private ILowManager _il;

        private static Logger _logger = LogManager.GetCurrentClassLogger();

        private Instrument _instrument;

        private static List<Jihas> _list = new List<Jihas>
        {
            Jihas.Biolis30i,
            Jihas.F200,
            Jihas.CobasPro,
            Jihas.ZybioExc200,
            Jihas.DiruiCsT180,
            Jihas.SysmexSuit
			
		};
    }
}