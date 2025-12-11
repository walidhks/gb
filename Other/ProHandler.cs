using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Xml;
using GbService.ASTM;
using GbService.Common;
using GbService.Communication;
using GbService.Communication.Common;
using GbService.Model.Domain;
using NLog;

namespace GbService.Other
{
	public class ProHandler
	{
		public ProHandler(Instrument instrument)
		{
			this._instrument = instrument;
		}

		public void Parse(string m, ILowManager il)
		{
			try
			{
				Jihas kind = this._instrument.Kind;
				bool flag = kind == Jihas.TosohG8;
				if (flag)
				{
					ProHandler.ParseG8(m, this._instrument);
				}
				else
				{
					bool flag2 = kind == Jihas.TosohG7;
					if (flag2)
					{
						this.ParseG7(m);
					}
					else
					{
						bool flag3 = kind == Jihas.Medconn;
						if (flag3)
						{
							this.ParseMedconn(m);
						}
						else
						{
							bool flag4 = kind == Jihas.LabNovation;
							if (flag4)
							{
								this.ParseLabNovation(m);
							}
							else
							{
								bool flag5 = kind == Jihas.SwelabSmall;
								if (flag5)
								{
									this.ParseSwelabSmall(m);
								}
								else
								{
									bool flag6 = kind == Jihas.EasyLite;
									if (flag6)
									{
										ProHandler.ParseEasyLite(m, this._instrument);
									}
									else
									{
										bool flag7 = kind == Jihas.ABL800;
										if (flag7)
										{
											this.ParseABL800(m);
										}
										else
										{
											bool flag8 = kind == Jihas.Ge300;
											if (flag8)
											{
												ProHandler.ParseGe300(m, this._instrument);
											}
											else
											{
												bool flag9 = kind == Jihas.Roller;
												if (flag9)
												{
													ProHandler.ParseRoller(m, this._instrument);
												}
												else
                                                { // NEW: independent UC‑1000 handling
                                                    bool flagUC = kind == Jihas.SysmexUC1000;
                                                if (flagUC)
                                                {
                                                    ProHandler.ParseUC1000(m, this._instrument);
                                                }
                                                else
                                                {
													bool flag10 = kind == Jihas.Esr;
													if (flag10)
													{
														ProHandler.ParseEsr(m, this._instrument);
													}
													else
													{
														bool flag11 = kind == Jihas.Precision;
														if (flag11)
														{
															ProHandler.ParsePrecision(m, this._instrument);
														}
														else
														{
															bool flag12 = kind == Jihas.KenzaMax;
															if (flag12)
															{
																ProHandler.ParseKenzaMax(m, this._instrument);
															}
															else
															{
																bool flag13 = kind == Jihas.HumaLytePlus5;
																if (flag13)
																{
																	ProHandler.HumaLytePlus5(m, this._instrument);
																}
																else
																{
																	bool flag14 = kind == Jihas.Xpand;
																	if (flag14)
																	{
																		ProHandler.ParseXpand(m, this._instrument);
																	}
																	else
																	{
																		bool flag15 = kind == Jihas.TosohGx;
																		if (flag15)
																		{
																			bool flag16 = m == '\u0004'.ToString();
																			if (!flag16)
																			{
																				this.ParseGx(m);
																				il.SendLow(6);
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

            catch (Exception ex)
			{
				ProHandler._logger.Error(new LogMessageGenerator(ex.ToString));
			}
		}

		public void ParseLabNovation(string s)
		{
			Dictionary<string, string> dictionary = ProHandler.ParseCodeValuePairs(s);
			string sampleId = ProHandler.GetSampleId(s);
			JihazResult jihazResult = new JihazResult(sampleId);
			foreach (KeyValuePair<string, string> keyValuePair in dictionary)
			{
				jihazResult.Results.Add(new LowResult(keyValuePair.Key, keyValuePair.Value, null, null, null));
			}
			AstmHigh.LoadResults(jihazResult, this._instrument, null);
		}

		public static Dictionary<string, string> ParseCodeValuePairs(string input)
		{
			Dictionary<string, string> dictionary = new Dictionary<string, string>();
			Match match = Regex.Match(input, "<R>(.*?)</R>");
			bool flag = !match.Success;
			Dictionary<string, string> result;
			if (flag)
			{
				result = dictionary;
			}
			else
			{
				string value = match.Groups[1].Value;
				List<string> source = new List<string>
				{
					"HbA1a",
					"HbA1b",
					"HbF",
					"L-A1c",
					"HbA1c",
					"HbA0",
					"p3",
					"HbS",
					"A2",
					"eAG"
				};
				foreach (string text in from x in source
				where !string.IsNullOrWhiteSpace(x)
				select x)
				{
					int length = (text == "HbA0") ? 4 : 3;
					dictionary[text] = value.Substring(value.IndexOf(text, StringComparison.Ordinal) + text.Length + 1, length);
				}
				result = dictionary;
			}
			return result;
		}

		public static string GetSampleId(string input)
		{
			Match match = Regex.Match(input, "<I>(.*?)</I>");
			bool flag = !match.Success;
			string result;
			if (flag)
			{
				result = null;
			}
			else
			{
				string value = match.Groups[1].Value;
				string[] array = value.Split(new char[]
				{
					'|'
				});
				result = ((array.Length >= 4) ? array[3] : null);
			}
			return result;
		}

		public static void ParseXpand(string x, Instrument instrument)
		{
			string[] array = x.Split(new string[]
			{
				"\r\n",
				"\n"
			}, StringSplitOptions.RemoveEmptyEntries);
			foreach (string text in array)
			{
				bool flag = text.Length != 60;
				if (!flag)
				{
					string scode = text.Substring(20, 8);
					string code = text.Substring(33, 4);
					string value = text.Substring(42, 7);
					AstmHigh.LoadResults(new JihazResult(scode)
					{
						Results = 
						{
							new LowResult(code, value, null, null, null)
						}
					}, instrument, null);
				}
			}
		}

		public static void ParseG8(string m, Instrument instrument)
		{
			ProHandler._logger.Info("ahm : g8, mode = " + instrument.Mode.ToString());
			m = m.Remove(m.IndexOf('\r'));
			Inf inf = Inf.Get(instrument);
			bool flag = inf != null;
			if (flag)
			{
				string text = m.Substring(inf.I).Trim();
				double? num = TextUtil.Double(m.Substring(inf.J, inf.K));
				double? num2;
				if (instrument.Mode != 0)
				{
					num2 = num;
				}
				else
				{
					double? num3 = num;
					double num4 = 0.0915;
					num2 = ((num3 != null) ? new double?(num3.GetValueOrDefault() * num4 + 2.15) : null);
				}
				double? num5 = num2;
				string value = (num5 != null) ? num5.GetValueOrDefault().ToString().Replace(',', '.') : null;
				ProHandler.LoadOk(text, value, instrument);
			}
			else
			{
				bool flag2 = instrument.Mode == 0;
				if (flag2)
				{
					string text = m.Substring(62).Trim();
					string value = m.Substring(31, 5);
					ProHandler.LoadOk(text, value, instrument);
				}
				else
				{
					string text = m.Substring(72).Trim();
					JihazResult jihazResult = new JihazResult(text);
					string str = m.Substring(34, 6);
					double? num3 = TextUtil.Double(str);
					double num4 = 0.0915;
					double? num6 = (num3 != null) ? new double?(num3.GetValueOrDefault() * num4 + 2.15) : null;
					string value = (num6 != null) ? num6.GetValueOrDefault().ToString().Replace(',', '.') : null;
					jihazResult.Results.Add(new LowResult("HbA1c", value, null, null, null));
					jihazResult.Results.Add(new LowResult("0", m.Substring(46, 6), null, null, null));
					jihazResult.Results.Add(new LowResult("1", m.Substring(52, 6), null, null, null));
					jihazResult.Results.Add(new LowResult("2", m.Substring(58, 6), null, null, null));
					AstmHigh.LoadResults(jihazResult, instrument, null);
				}
			}
		}

		public void ParseMedconn(string m)
		{
			ProHandler._logger.Info("ahm : Medconn");
			string[] array = m.Split(Tu.NL);
			string item = ProHandler.GetItem(array, "sample", 3);
			JihazResult jihazResult = new JihazResult(item);
			foreach (string text in array)
			{
				bool flag = !text.Contains("|");
				if (!flag)
				{
					string[] array3 = text.Split(new char[]
					{
						'|'
					});
					jihazResult.Results.Add(new LowResult(array3[0], array3[1], null, null, null));
				}
			}
			AstmHigh.LoadResults(jihazResult, this._instrument, null);
		}

		public void ParseABL800(string m)
		{
			m = m.Replace('\u0001'.ToString(), "").Replace('\u0004'.ToString(), "");
			AstmHigh.ParseResult(m, this._instrument);
		}

		public void ParseSwelabSmall(string m)
		{
			XmlDocument xmlDocument = new XmlDocument();
			xmlDocument.LoadXml(m);
			string innerText = xmlDocument.SelectSingleNode("/sample/smpinfo/p/n[.='ID']").ParentNode["v"].InnerText;
			XmlNodeList xmlNodeList = xmlDocument.SelectNodes("/sample/smpresults/p");
			JihazResult jihazResult = new JihazResult(innerText);
			foreach (object obj in xmlNodeList)
			{
				XmlNode xmlNode = (XmlNode)obj;
				jihazResult.Results.Add(new LowResult(xmlNode["n"].InnerText, xmlNode["v"].InnerText, null, null, null));
			}
			AstmHigh.LoadResults(jihazResult, this._instrument, null);
		}

		public static void ParseGe300(string m, Instrument instrument)
		{
			List<string> list = m.Split(new string[]
			{
				" "
			}, StringSplitOptions.RemoveEmptyEntries).ToList<string>();
			string scode = list[list.IndexOf("ID") + 1];
			string text = "K";
			string text2 = "Na";
			string text3 = "Cl";
			string value = list[list.IndexOf(text) + 1];
			string value2 = list[list.IndexOf(text2) + 1];
			string value3 = list[list.IndexOf(text3) + 1];
			AstmHigh.LoadResults(new JihazResult(scode)
			{
				Results = 
				{
					new LowResult(text, value, null, null, null),
					new LowResult(text2, value2, null, null, null),
					new LowResult(text3, value3, null, null, null)
				}
			}, instrument, null);
		}

		public static void HumaLytePlus5(string m, Instrument instrument)
		{
			List<string> list = m.Split(new string[]
			{
				" "
			}, StringSplitOptions.RemoveEmptyEntries).ToList<string>();
			string scode = list[1];
			AstmHigh.LoadResults(new JihazResult(scode)
			{
				Results = 
				{
					new LowResult("K", list[3], null, null, null),
					new LowResult("Na", list[4], null, null, null),
					new LowResult("Cl", list[5], null, null, null),
					new LowResult("Ca", list[6], null, null, null)
				}
			}, instrument, null);
		}

		public static void ParseKenzaMax(string m, Instrument instrument)
		{
			int num = m.IndexOf("\nID:", StringComparison.Ordinal);
			bool flag = num == -1;
			if (!flag)
			{
				string scode = m.Substring(num + 3, 4);
				string code = "VS";
				string value = m.Substring(26, 3);
				AstmHigh.LoadResults(new JihazResult(scode)
				{
					Results = 
					{
						new LowResult(code, value, null, null, null)
					}
				}, instrument, null);
			}
		}

		public static void ParseEsr(string m, Instrument instrument)
		{
			m = m.Substring(10).Substring(0, m.Length - 20);
			List<string> list = m.Split(new char[]
			{
				'\n'
			}).ToList<string>();
			foreach (string text in list)
			{
				List<string> list2 = text.Split(new string[]
				{
					" "
				}, StringSplitOptions.RemoveEmptyEntries).ToList<string>();
				AstmHigh.LoadResults(new JihazResult(list2[2])
				{
					Results = 
					{
						new LowResult("VS1", list2[3], null, null, null),
						new LowResult("VS2", list2[4], null, null, null)
					}
				}, instrument, null);
			}
		}

		public static void ParseEasyLite(string m, Instrument instrument)
		{
			List<string> list = m.Split("ID#").ToList<string>();
			foreach (string text in list)
			{
				int num = text.IndexOf("\rNa ");
				bool flag = num < 0;
				if (!flag)
				{
					AstmHigh.LoadResults(new JihazResult(text.Substring(1, 15))
					{
						Results = 
						{
							new LowResult("Na", text.Substring(num + 4, 5), null, null, null),
							new LowResult("K", text.Substring(num + 12, 5), null, null, null),
							new LowResult("Cl", text.Substring(num + 21, 5), null, null, null)
						}
					}, instrument, null);
				}
			}
		}
       
            public static void ParseRoller(string m, Instrument instrument)
		{
			bool flag = instrument.Mode == 2;
			if (flag)
			{
				ProHandler.ParseBK5000P(m, instrument);
			}
			else
			{
				bool flag2 = instrument.Mode == 3;
				if (flag2)
				{
					ProHandler.ParseUC1000(m, instrument);
				}
                else
                {
                    int numberPositionBarcode = ParamDictHelper.NumberPositionBarcode;
                    string scode = m.Substring(4, numberPositionBarcode);

                    // ---------------------------------------------------------
                    // [FIX] MODE 4: Lifotronic H9 (Uses LifotronicHandler)
                    // ---------------------------------------------------------
                    if (instrument.Mode == 4)
                    {
                        // This calls the class you just fixed in the previous step
                        // Make sure LifotronicHandler.cs is saved!
                        LifotronicHandler.Parse(m, instrument);
                        return;
                    }
                    // ---------------------------------------------------------

                    // Mode 1: Old Roller Logic (Single Result via S3)
                    bool flag3 = instrument.Mode == 1;
                    string code = "VS";
                    string value = m.Substring(26, 3);

                    if (flag3)
                    {
                        // Extract ID (Start at 10)
                        scode = m.Substring(10, numberPositionBarcode);

                        // Extract Value (Using S3 or default 145,4)
                        List<int> list = TextUtil.SplitInt(instrument.S3);
                        value = ((list == null) ? m.Substring(145, 4) : m.Substring(list[0], list[1]));
                        code = "HbA1c";
                    }

                    // Save Mode 0/1 Results
                    AstmHigh.LoadResults(new JihazResult(scode)
                    {
                        Results =
                        {
                            new LowResult(code, value, null, null, null)
                        }
                    }, instrument, null);
                }
            
        
        /* else
         {
             // Default Sample ID extraction (can be overwritten inside the mode block)
             int numberPositionBarcode = ParamDictHelper.NumberPositionBarcode;
             string scode = m.Substring(4, numberPositionBarcode);

             // ---------------------------------------------------------
             // [FIX] MODE 4: Lifotronic H9 (All Variants)
             // ---------------------------------------------------------
             if (instrument.Mode == 4)
             {
                 try
                 {
                     // 1. Clean message
                     string cleanMsg = m.Replace("\u0002", "").Replace("\u0003", "").Trim();

                     // 2. Dynamic Parsing (Safer than hardcoded numbers)
                     int cursor = 1; // Skip 'S'
                     cursor += 6;    // Skip Header info

                     // Get ID Length
                     string lenStr = cleanMsg.Substring(cursor, 2);
                     int idLen = int.Parse(lenStr);
                     if (idLen == 0) idLen = 12; // Fallback
                     cursor += 2;

                     // Get Sample ID
                     scode = cleanMsg.Substring(cursor, idLen).Trim();
                     cursor += idLen;

                     // Create Result Object
                     JihazResult result = new JihazResult(scode);

                     // Skip Demographics (23), Times (12), Absorbance (36), Areas (42)
                     // Total skip = 113 chars
                     cursor += 119;

                     // 3. Extract Ratios (The % values for variants)
                     // Format: 5 chars each (e.g. "01.70")
                     string[] variants = { "HbA1a", "HbA1b", "HbF", "LA1c", "HbA1c", "HbA0" };

                     foreach (string v in variants)
                     {
                         if (cursor + 5 > cleanMsg.Length) break;
                         string val = cleanMsg.Substring(cursor, 5);
                         result.Results.Add(new LowResult(v, val.Trim(), "%", null, null));
                         cursor += 5;
                     }

                     // 4. Extract Final Results (after the ratios)
                     // HbA1c IFCC (5 chars)
                     if (cursor + 5 <= cleanMsg.Length)
                     {
                         string ifcc = cleanMsg.Substring(cursor, 5);
                         result.Results.Add(new LowResult("HbA1c_IFCC", ifcc.Trim(), "mmol/mol", null, null));
                         cursor += 5;
                     }

                     // eAG (4 chars)
                     if (cursor + 4 <= cleanMsg.Length)
                     {
                         string eag = cleanMsg.Substring(cursor, 4);
                         result.Results.Add(new LowResult("eAG", eag.Trim(), "mmol/L", null, null));
                     }

                     // 5. Save
                     AstmHigh.LoadResults(result, instrument, null);
                 }
                 catch (Exception ex)
                 {
                     // Log error if parsing fails
                     // _logger.Error(ex.ToString());
                 }
             }
             // ---------------------------------------------------------
             // Mode 1 (Old Roller Logic)
             // ---------------------------------------------------------
             else if (instrument.Mode == 1)
             {
                 scode = m.Substring(10, numberPositionBarcode);
                 List<int> list = TextUtil.SplitInt(instrument.S3);
                 string value = ((list == null) ? m.Substring(145, 4) : m.Substring(list[0], list[1]));

                 AstmHigh.LoadResults(new JihazResult(scode)
                 {
                     Results = { new LowResult("HbA1c", value, null, null, null) }
                 }, instrument, null);
             }
             // ---------------------------------------------------------
             // Default (Mode 0 / Others)
             // ---------------------------------------------------------
             else
             {
                 string code = "VS";
                 string value = m.Substring(26, 3);
                 AstmHigh.LoadResults(new JihazResult(scode)
                 {
                     Results = { new LowResult(code, value, null, null, null) }
                 }, instrument, null);
             }
         }*/

    }
		}

		private static void ParseBK5000P(string m, Instrument instrument)
		{
			string id = m.Substring(19, 21);
			string[] source = m.Substring(234, m.Length - 235).Split(new char[]
			{
				' '
			}, StringSplitOptions.RemoveEmptyEntries);
			List<string[]> records = (from s in source
			select s.Split(new char[]
			{
				'|'
			})).ToList<string[]>();
			JihazResult result = TextUtil.GetResult(records, id, 1);
			AstmHigh.LoadResults(result, instrument, null);
		}

        /*  public static void ParseUC1000(string m, Instrument instrument)
          {
              // ... (Keep the code inside exactly as it is) ...
              string scode = m.Substring(1, 14);
              JihazResult jihazResult = new JihazResult(scode);
              string[] array = m.Substring(70).Split(new char[] { ',' }, StringSplitOptions.RemoveEmptyEntries);

              for (int i = 0; i < 15; i++)
              {
                  // ...
              }
              AstmHigh.LoadResults(jihazResult, instrument, null);
          }*/

        // inside ProHandler class
        public static void ParseUC1000(string m, Instrument instrument)
        {
            var handler = new SysmexUC1000Handler();

            // Step 1: parse the raw message with our UC1000 handler
            if (!handler.Parse(m, out var parsedResults) || parsedResults == null || parsedResults.Count == 0)
                return;

            // Step 2: group by SampleCode (usually only one sample per frame)
            foreach (var grp in parsedResults.GroupBy(r => r.SampleCode))
            {
                var jihazResult = new JihazResult(grp.Key);

                // Step 3: convert each AnalysisResult into LowResult
                foreach (var r in grp)
                {
                    jihazResult.Results.Add(new LowResult(
                        r.AnalysisCode,  // e.g. "URO"
                        r.Value,         // e.g. "1+" or "100"
                        null, null, null
                    ));
                }

                // Step 4: send to the common result loader
                AstmHigh.LoadResults(jihazResult, instrument, null);
            }
        }

        public static void ParsePrecision(string m, Instrument instrument)
		{
			List<string> list = m.Split(new char[]
			{
				'|'
			}).ToList<string>();
			int num = list.IndexOf("COVID-19 IgM");
			int num2 = list.IndexOf("COVID-19 IgG");
			ProHandler._logger.Info(string.Format("{0} {1}", num, num2));
			AstmHigh.LoadResults(new JihazResult(list[4].Split(":")[1])
			{
				Results = 
				{
					new LowResult("COVID-19 IgM", list[num + 3], null, null, null),
					new LowResult("COVID-19 IgG", list[num2 + 3], null, null, null)
				}
			}, instrument, null);
		}

		private static string GetItem(string[] list, string param, int position)
		{
			string text = list.FirstOrDefault((string x) => x.StartsWith(param));
			return text.Split(new char[]
			{
				'|'
			})[position];
		}

		public void ParseG7(string m)
		{
			ProHandler._logger.Info("ahm : g7");
			m = m.Remove(m.IndexOf('\r'));
			string sid = m.Substring(62).Trim();
			string str = m.Substring(55, 5);
			string str2 = m.Substring(10, 5);
			string str3 = m.Substring(15, 5);
			decimal? num = TextUtil.Decimal(str) - TextUtil.Decimal(str2) - TextUtil.Decimal(str3);
			bool flag = num != null;
			if (flag)
			{
				ProHandler.LoadOk(sid, num.ToString(), this._instrument);
			}
		}

		public void ParseGx(string m)
		{
			ProHandler._logger.Info("ahm : gx");
			int num = (this._instrument.Mode == 0) ? 0 : 3;
			m = m.Remove(m.IndexOf('\u0003'));
			Inf inf = Inf.Get(this._instrument);
			bool flag = !m.Contains('\u0002');
			if (flag)
			{
				ProHandler._logger.Warn("no stx");
			}
			else
			{
				bool flag2 = inf == null;
				if (flag2)
				{
					m = m.Substring(m.LastIndexOf('\u0002') + 1);
					string sid = m.Substring(156 + num).Trim();
					string value = m.Substring(45 + num, 6);
					ProHandler.LoadOk(sid, value, this._instrument);
				}
				else
				{
					m = m.Substring(m.LastIndexOf('\u0002') + 1);
					string sid2 = m.Substring(inf.I + num).Trim();
					string value2 = m.Substring(inf.J + num, inf.K);
					ProHandler.LoadOk(sid2, value2, this._instrument);
				}
			}
		}

		public static void LoadOk(string sid, string value, Instrument instrument)
		{
			JihazResult result = TextUtil.GetResult(sid, "HbA1c", value);
			AstmHigh.LoadResults(result, instrument, null);
		}

		private Instrument _instrument;

		private static Logger _logger = LogManager.GetCurrentClassLogger();
	}
}
