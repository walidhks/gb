using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using GbService.ASTM;
using GbService.Communication;
using GbService.Model.Contexts;
using GbService.Model.Domain;
using NLog;

namespace GbService.Other
{
	public class VidasHandler
	{
		public VidasHandler(VidasManager manager)
		{
			this._manager = manager;
		}
        public string EncodeSampleVitek(Sample sample)
        {
            if (sample == null) return null;

            // Get tests mapped to Vitek (e.g., "GN", "GP", "AST-N233")
            // FIXED: Set L2 instead (which backs the Kind property)
            List<string> tests = AstmHigh.GetTests(sample.SampleId, new Instrument { L2 = (long)Jihas.Vitek, InstrumentCode = 0 }, false);
            // Note: You might need to pass the actual instrument object if available

            if (tests.Count == 0) return null;

            Patient p = sample.AnalysisRequest.Patient;
            string barcode = sample.FormattedSampleCode;
            string dob = (p.PatientDateNaiss ?? DateTime.Today).ToString("MM/dd/yyyy");
            string sex = (p.ShortSexe == "M") ? "M" : "F";

            StringBuilder sb = new StringBuilder();

            // Vitek expects one "mtmpr" message per test card type
            foreach (string testCode in tests)
            {
                // Format per BCI Manual Page 3-15
                // mtmpr | pi {PatID} | pn {Name} | pb {DOB} | ps {Sex} | si | ci {Barcode} | ta | rt {CardType} | zz

                sb.Append("mtmpr");
                sb.Append($"|pi{p.PatientID}");
                sb.Append($"|pn{p.Nom}, {p.Prenom}");
                sb.Append($"|pb{dob}|ps{sex}");
                sb.Append("|si");              // Specimen Separator (Empty)
                sb.Append($"|ci{barcode}");    // Culture ID / Barcode
                sb.Append("|ta");              // Test Separator
                sb.Append($"|rt{testCode}");   // Test Type (Card Code)
                sb.Append("|zz|");             // Terminator
                sb.Append("\r");
            }

            return sb.ToString();
        }
        public static void Parse(string message, Instrument instrument)
		{
			string sampleCode = null;
			string analysisTypeCode = null;
			string qn = null;
			string unit = null;
			string ql = null;
			bool flag = message == "mtbis";
			if (!flag)
			{
				string[] array = Regex.Split(message, "[|]");
				foreach (string text in array)
				{
					bool flag2 = string.IsNullOrEmpty(text) || text.Length <= 2;
					if (!flag2)
					{
						string a = text.Substring(0, 2);
						string text2 = text.Substring(2);
						bool flag3 = a == "ci";
						if (flag3)
						{
							sampleCode = text2;
						}
						else
						{
							bool flag4 = a == "rt";
							if (flag4)
							{
								analysisTypeCode = text2;
							}
							else
							{
								bool flag5 = a == "qn";
								if (flag5)
								{
									qn = text2.Replace(" 3 IS", "");
								}
								else
								{
									bool flag6 = a == "ql";
									if (flag6)
									{
										ql = text2;
									}
									else
									{
										bool flag7 = a == "y3";
										if (flag7)
										{
											unit = text2;
										}
									}
								}
							}
						}
					}
				}
				VidasHandler.LoadMessage(sampleCode, analysisTypeCode, qn, ql, unit, instrument);
			}
		}

		private static void LoadMessage(string sampleCode, string analysisTypeCode, string qn, string ql, string unit, Instrument instrument)
		{
			JihazResult jihazResult = new JihazResult(sampleCode);
			bool flag = string.IsNullOrWhiteSpace(qn);
			string text;
			if (flag)
			{
				text = ql;
			}
			else
			{
				text = ((unit != null) ? qn.Replace(unit, "") : qn);
				text = text.Trim();
				int num = text.LastIndexOf(" ", StringComparison.Ordinal);
				bool flag2 = num > 0 && TextUtil.Decimal(text.Substring(num)) == null;
				if (flag2)
				{
					text = text.Substring(0, num);
				}
			}
			jihazResult.Results.Add(new LowResult(analysisTypeCode, text, unit, null, null));
			AstmHigh.LoadResults(jihazResult, instrument, null);
		}

		public void OrderSamples(List<Sample> samples)
		{
			foreach (Sample sample in samples)
			{
				this.EncodeSample(sample);
			}
		}

		private void EncodeSample(Sample sample)
		{
			bool flag = sample == null;
			if (!flag)
			{
				List<string> tests = this.GetTests(sample.SampleId);
				bool flag2 = tests == null || tests.Count == 0;
				if (!flag2)
				{
					Patient p = sample.AnalysisRequest.Patient;
					string bd = (p.PatientDateNaiss ?? DateTime.Today).ToString("yyyyMMdd");
					long? code = sample.SampleCode;
					string msg = tests.Aggregate("", delegate(string c, string i)
					{
						string str = string.Format("mtmpr|pi{0}|pn{1},{2}|pb{3}|ps{4}|", new object[]
						{
							code,
							p.Nom,
							p.Prenom,
							bd,
							p.ShortSexe
						});
						string str2 = str + ((this._manager.Instrument.Mode == 0) ? string.Format("si|ci{0}|rt{1}|qd1|{2}", code, i, '\r') : string.Format("so|si|ci{0}|rt{1}|qd{2}", code, i, '\r'));
						return c + str2;
					});
					this._manager.PutMsgInSendingQueue(msg, sample.SampleId);
				}
			}
		}

		public List<string> GetTests(long sampleID)
		{
			return AstmHigh.GetTests(sampleID, this._manager.Instrument, false);
		}

		public void Upload()
		{
			try
			{
              //List<Sample> samplesRange = AstmHigh.GetSamplesRange(DateTime.Now.Date.AddDays(-2.0), DateTime.Now, new LaboContext(), this._manager.Instrument);
                List<Sample> samplesRange = AstmHigh.GetSamplesRange1(DateTime.Now.Date.AddDays(-2.0), DateTime.Now, new LaboContext(), this._manager.Instrument);
                this.OrderSamples(samplesRange);
			}
			catch (Exception ex)
			{
				VidasHandler._logger.Error(new LogMessageGenerator(ex.ToString));
			}
		}

        /*public static void ParseVitek(string message, Instrument instrument)
		{
			int num = message.IndexOf("|af", StringComparison.Ordinal);
			string text = message.Substring(0, num);
			int num2 = message.IndexOf("|ra|", StringComparison.Ordinal);
			string m = message.Substring(num, num2 - num);
			int num3 = message.IndexOf("|zz|", StringComparison.Ordinal);
			string m2 = message.Substring(num2, num3 - num2);
			string[] source = text.Split(new char[]
			{
				'|'
			}, StringSplitOptions.RemoveEmptyEntries);
			List<string[]> records = (from x in source
			select new string[]
			{
				x.Substring(0, 2),
				x.Substring(2)
			}).ToList<string[]>();
			string valueById = VidasHandler.GetValueById(records, "ci", null);
			JihazResult jihazResult = new JihazResult(valueById);
			IEnumerable<VitekAf> afs = VidasHandler.GetAfs(m);
			IEnumerable<VitekRa> ras = VidasHandler.GetRas(m2);
			foreach (VitekRa vitekRa in ras)
			{
				jihazResult.Results.Add(new LowResult(vitekRa.Code, vitekRa.Value, null, vitekRa.ExactValue, null));
			}
			AstmHigh.LoadResults(jihazResult, instrument, null);
		}*/
        // [FIXED] ParseVitek (Handles mtrsl messages)
        // [FIXED] Robust Vitek Result Parser
        public static void ParseVitek(string message, Instrument instrument)
        {
            try
            {
                // Split by '|' to get fields
                string[] parts = message.Split(new char[] { '|' }, StringSplitOptions.RemoveEmptyEntries);

                string barcode = null;

                JihazResult result = null;

                // 1. Find Barcode (ci field)
                foreach (string part in parts)
                {
                    if (part.StartsWith("ci"))
                    {
                        barcode = part.Substring(2);
                        result = new JihazResult(barcode);
                        break;
                    }
                }

                if (result == null) return; // No barcode found

                // 2. Extract Data
                for (int i = 0; i < parts.Length; i++)
                {
                    string field = parts[i];

                    // Organism (o2 = Organism Name)
                    if (field.StartsWith("o2"))
                    {
                        string orgName = field.Substring(2);
                        result.Results.Add(new LowResult("Organism", orgName, null, null, null));
                    }
                    // Antibiotic (a1 = Code, a3 = MIC, a4 = Interpretation)
                    else if (field.StartsWith("a1"))
                    {
                        string drugCode = field.Substring(2);
                        string mic = "";
                        string interp = "";

                        // Look ahead for MIC and INT
                        // Vitek sends them in order: a1..a2..a3..a4
                        for (int k = i + 1; k < i + 5 && k < parts.Length; k++)
                        {
                            if (parts[k].StartsWith("a3")) mic = parts[k].Substring(2);
                            if (parts[k].StartsWith("a4")) interp = parts[k].Substring(2);
                            if (parts[k].StartsWith("a1")) break; // Next drug started
                        }

                        if (!string.IsNullOrEmpty(drugCode))
                        {
                            result.Results.Add(new LowResult(drugCode, mic, "MIC", interp, null));
                        }
                    }
                }

                // 3. Save
                if (result.Results.Count > 0)
                {
                    AstmHigh.LoadResults(result, instrument, null);
                    _logger.Info($"Vitek: Saved {result.Results.Count} results for {barcode}");
                }
            }
            catch (Exception ex)
            {
                _logger.Error("Vitek Parse Error: " + ex.ToString());
            }
        }
    

// Helper to find values like "|ci12345|"
private static string GetValue(string msg, string tag)
        {
            int index = msg.IndexOf(tag);
            if (index == -1) return null;

            int start = index + tag.Length;
            int end = msg.IndexOf('|', start);
            if (end == -1) return msg.Substring(start);

            return msg.Substring(start, end - start);
        }
        private static IEnumerable<VitekAf> GetAfs(string m)
		{
			string[] source = m.Split(new string[]
			{
				"|af"
			}, StringSplitOptions.RemoveEmptyEntries);
			return from x in source
			select new VitekAf(x);
		}

		private static IEnumerable<VitekRa> GetRas(string m)
		{
			string[] source = m.Split(new string[]
			{
				"|ra"
			}, StringSplitOptions.RemoveEmptyEntries);
			return from x in source
			select new VitekRa(x);
		}

		public static string GetValueById(IEnumerable<string[]> records, string id, Analysis analysis = null)
		{
			string[] array = records.FirstOrDefault((string[] x) => x[0] == id);
			return (array != null) ? array[1] : null;
		}

		private static Logger _logger = LogManager.GetCurrentClassLogger();

		private VidasManager _manager;
	}
}
