using GbService.Common;
using GbService.Communication;
using GbService.Communication.Common;
using GbService.HL7;
using GbService.Model.Contexts;
using GbService.Model.Domain;
using GbService.Other;
using NLog;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Timers;
using System.Xml.Linq;
using static System.Net.Mime.MediaTypeNames;

namespace GbService.ASTM
{
	public class AstmHigh2
	{
		public AstmHigh2(Instrument instrument, string input, string output)
		{
			this.Init(instrument, instrument.InstrumentBaudRate, input, output);
		}

		public AstmHigh2(Instrument instrument)
		{
			string[] array = instrument.InstrumentPortName.Split(new char[]
			{
				','
			});
			Jihas kind = instrument.Kind;
			bool flag = kind == Jihas.Psm || kind == Jihas.VidasKube || (kind == Jihas.Evm && instrument.Mode == 1);
			if (flag)
			{
				this.InitFtp(instrument, array);
			}
			else
			{
				this.Init(instrument, array[0], "\\" + array[1], "\\" + array[2]);
			}
		}

		private void InitFtp(Instrument instrument, string[] j)
		{
			try
			{
				Directory.CreateDirectory("C:\\BM\\temp\\");
				Directory.CreateDirectory(AstmHigh2._inFolder);
				this._instrument = instrument;
				this._ip = j[0];
				this._user = j[1];
				this._pass = j[2];
				AstmHigh2._logger.Info(string.Concat(new string[]
				{
					"ftp ",
					this._ip,
					" ",
					this._user,
					" ",
					this._pass
				}));
				bool flag = instrument.Kind == Jihas.Psm;
				if (flag)
				{
					this._sr = "out";
					this._er = "in";
					this._ext = "RES";
				}
				else
				{
					bool flag2 = instrument.Kind == Jihas.Evm;
					if (flag2)
					{
						this._sr = "retour";
						this._er = "aller";
						this._ext = "HPR";
					}
					else
					{
						this._sr = j[3];
						this._er = j[4];
						this._ext = j[5];
					}
				}
				this.ScheduleRefresh(15, new ElapsedEventHandler(this.RefreshPsm));
			}
			catch (Exception ex)
			{
				AstmHigh2._logger.Error(new LogMessageGenerator(ex.ToString));
			}
		}

		private void Init(Instrument instrument, string path, string input, string output)
		{
			try
			{
				this._instrument = instrument;
				this._entree = path + input;
				this._sortie = path + output;
				AstmHigh2._logger.Info(this._entree + "\n" + this._sortie);
				this.ScheduleRefresh(15, new ElapsedEventHandler(this.Refresh));
			}
			catch (Exception ex)
			{
				AstmHigh2._logger.Error(new LogMessageGenerator(ex.ToString));
			}
		}

		private void ScheduleRefresh(int seconds, ElapsedEventHandler s)
		{
			this._serviceTimer = new Timer
			{
				Enabled = true,
				Interval = (double)(1000 * seconds)
			};
			this._serviceTimer.Elapsed += s;
			this._serviceTimer.Start();
		}

		public List<string> ListDirectory(FtpWebResponse response)
		{
			Stream responseStream = response.GetResponseStream();
			StreamReader streamReader = new StreamReader(responseStream);
			string text = streamReader.ReadToEnd();
			streamReader.Close();
			response.Close();
			return text.Split(new string[]
			{
				"\r\n"
			}, StringSplitOptions.RemoveEmptyEntries).ToList<string>();
		}

		public FtpWebResponse CreateRequest(string method, string fileName = null)
		{
			FtpWebRequest ftpWebRequest = (FtpWebRequest)WebRequest.Create(string.Concat(new string[]
			{
				"ftp://",
				this._ip,
				"/",
				this._sr,
				"/",
				fileName
			}));
			ftpWebRequest.Method = method;
			ftpWebRequest.UsePassive = true;
			ftpWebRequest.Credentials = new NetworkCredential(this._user, this._pass);
			return (FtpWebResponse)ftpWebRequest.GetResponse();
		}

		private void Refresh(object sender, ElapsedEventArgs e)
		{
			try
			{
				string searchPattern = (this._instrument.Kind == Jihas.PcrReal) ? "*.xml" : "*.*";
				List<string> list = Directory.GetFiles(this._sortie, searchPattern).ToList<string>();
				AstmHigh2._logger.Info<int>(list.Count);
				foreach (string text in list)
				{
					string fileName = Path.GetFileName(text);
					AstmHigh2._logger.Info("File : " + fileName);
					string text2 = File.ReadAllText(text);
					AstmHigh2._logger.Debug("content : \n" + text2);
					try
					{
						bool flag = this._instrument.Kind == Jihas.HumaClot;
						if (flag)
						{
							AstmHigh2.ParseHumaClot(text2, this._instrument, this.Repeater);
						}
						else
						{
							bool flag2 = this._instrument.Kind == Jihas.BiosystemA15;
							if (flag2)
							{
								this.ParseA15(text2);
							}
							else
							{
								bool flag3 = this._instrument.Kind == Jihas.HLA;
								if (flag3)
								{
									AstmHigh2.ParseHLA(text, fileName, this._instrument);
								}
								else
								{
									bool flag4 = this._instrument.Kind == Jihas.Huma200;
									if (flag4)
									{
										AstmHigh2.ParseAstm(text2, this._instrument);
									}
									else
									{
										bool flag5 = this._instrument.Kind == Jihas.Mpl || this._instrument.Kind == Jihas.Evm;
										if (flag5)
										{
											AstmHigh2.ParseHprim(text2, this._instrument);
										}
										else
										{
											bool flag6 = this._instrument.Kind == Jihas.VirClia;
											if (flag6)
											{
												this.ParseVirClia(text2, this._instrument);
											}
											else
											{
												bool flag7 = this._instrument.Kind == Jihas.RotorGene;
												if (flag7)
												{
													AstmHigh2.ParseRotor(text2, this._instrument);
												}
												else
												{
													bool flag8 = this._instrument.Kind == Jihas.PcrReal;
													if (flag8)
													{
														AstmHigh2.ParsePcr(text2, this._instrument);
													}
													else
													{
														bool flag9 = this._instrument.Kind == Jihas.Union;
														if (flag9)
														{
															AstmHigh2.ParseUnion(text2, this._instrument);
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
						AstmHigh2._logger.Error(new LogMessageGenerator(ex.ToString));
					}
					finally
					{
						File.Delete(text);
					}
				}
				bool flag10 = !this._list.Contains(this._instrument.Kind);
				if (!flag10)
				{
					List<Sample> samplesRange = AstmHigh.GetSamplesRange(DateTime.Now.Date.AddDays(-2.0), DateTime.Now.AddMinutes(-5.0), new LaboContext(), this._instrument);
					bool flag11 = samplesRange.Count > 0;
					if (flag11)
					{
						AstmHigh2.EncodeAll(samplesRange, this._instrument, this._entree);
					}
				}
			}
			catch (Exception ex2)
			{
				AstmHigh2._logger.Error(new LogMessageGenerator(ex2.ToString));
			}
		}

		public static void ParseHLA(string file, string name, Instrument instrument)
		{
			HLAExcelParser hlaexcelParser = new HLAExcelParser(file, "AF2", "AJ", "L", 11);
			Dictionary<string, string> dictionary = hlaexcelParser.ParseOldExcel(file);
			bool flag = dictionary == null;
			if (flag)
			{
				AstmHigh2._logger.Error("Invalid file format");
			}
			else
			{
				JihazResult jihazResult = new JihazResult(name);
				foreach (KeyValuePair<string, string> keyValuePair in dictionary)
				{
					jihazResult.Results.Add(new LowResult(keyValuePair.Key, keyValuePair.Value, null, null, null));
				}
				AstmHigh.LoadResults(jihazResult, instrument, null);
			}
		}

		public void RefreshPsm(object sender, ElapsedEventArgs e)
		{
			try
			{
				FtpWebResponse response = this.CreateRequest("NLST", null);
				List<string> list = this.ListDirectory(response);
				Jihas kind = this._instrument.Kind;
				AstmHigh2._logger.Info(string.Format("Result Files count : {0}", list.Count));
				foreach (string text in list)
				{
					string text2 = "C:\\BM\\temp\\" + text;
					try
					{
						using (WebClient webClient = new WebClient())
						{
							webClient.Credentials = new NetworkCredential(this._user, this._pass);
							webClient.DownloadFile(string.Concat(new string[]
							{
								"ftp://",
								this._ip,
								"//",
								this._sr,
								"/",
								text
							}), text2);
						}
						AstmHigh2._logger.Info("File : " + text);
						string text3 = File.ReadAllText(text2);
						AstmHigh2._logger.Debug("content : \n" + text3);
						bool flag = kind == Jihas.Evm;
						if (flag)
						{
							AstmHigh2.ParseHprim(text3, this._instrument);
						}
						else
						{
							bool flag2 = kind == Jihas.VidasKube;
							if (flag2)
							{
								AstmHigh2.ParseKube(text3, this._instrument);
							}
							else
							{
								AstmHigh.ParseResult(text3, this._instrument);
							}
						}
					}
					catch (WebException ex)
					{
						AstmHigh2._logger.Info(((FtpWebResponse)ex.Response).StatusDescription);
					}
					catch (Exception ex2)
					{
						AstmHigh2._logger.Error(new LogMessageGenerator(ex2.ToString));
					}
					finally
					{
						this.CreateRequest("DELE", text);
					}
				}
				List<Sample> samplesRange = AstmHigh.GetSamplesRange(DateTime.Now.Date.AddDays(-2.0), DateTime.Now, new LaboContext(), this._instrument);
				bool flag3 = samplesRange.Count == 0;
				if (!flag3)
				{
					bool flag4 = kind == Jihas.VidasKube;
					if (flag4)
					{
						foreach (Sample sample in samplesRange)
						{
							this.EncodeSampleKube(sample, this._instrument);
						}
					}
					else
					{
						string pt = AstmHigh2.EncodeAll(samplesRange, this._instrument, AstmHigh2._inFolder);
						this.SendFtp(pt, kind);
					}
				}
			}
			catch (Exception ex3)
			{
				AstmHigh2._logger.Error(new LogMessageGenerator(ex3.ToString));
			}
		}

		private void SendFtp(string pt, Jihas k)
		{
			using (WebClient webClient = new WebClient())
			{
				webClient.Credentials = new NetworkCredential(this._user, this._pass);
				string str = string.Concat(new string[]
				{
					"ftp://",
					this._ip,
					"//",
					this._er,
					"/",
					pt
				});
				string str2 = "C:\\BM\\temp1\\" + pt;
				webClient.UploadFile(str + "." + this._ext, "STOR", str2 + "." + this._ext);
				bool flag = k == Jihas.Psm;
				if (flag)
				{
					webClient.UploadFile(str + ".OK", "STOR", str2 + ".OK");
				}
			}
		}

		public static void ParseKube(string msg, Instrument instrument)
		{
			XDocument xml = XDocument.Parse(msg);
			string code = AstmHigh2.Value(xml, "testIdentifier");
			string value = AstmHigh2.Value(xml, "value").Replace("|", "");
			AstmHigh.LoadResults(new JihazResult(AstmHigh2.Value(xml, "specimenIdentifier"))
			{
				Results = 
				{
					new LowResult(code, value, null, null, null)
				}
			}, instrument, null);
		}

		private static string Value(XDocument xml, string elm)
		{
			XElement xelement = xml.Descendants(elm).FirstOrDefault<XElement>();
			return (xelement != null) ? xelement.Value : null;
		}

		private void ParseVirClia(string msg, Instrument instrument)
		{
			string[] array = msg.Split(new char[]
			{
				'\n'
			});
		}

		private void ParseA15(string msg)
		{
			string[] array = msg.Split(Tu.NL);
			foreach (string text in array)
			{
				string[] array3 = text.Split(new char[]
				{
					'\t'
				});
				AstmHigh.LoadResults(new JihazResult(array3[0])
				{
					Results = 
					{
						new LowResult(array3[1], array3[3], null, null, null)
					}
				}, this._instrument, new char?(this.Repeater));
			}
		}

		public static void ParsePcr(string msg, Instrument instrument)
		{
			XDocument xdocument = XDocument.Parse(msg);
			var tests = xdocument.Root.Descendants("test").Select(delegate(XElement c)
			{
				XAttribute xattribute = c.Attribute("id");
				string sc = (xattribute != null) ? xattribute.Value : null;
				XAttribute xattribute2 = c.Attribute("value");
				string value2 = (xattribute2 != null) ? xattribute2.Value : null;
				XElement parent = c.Parent;
				string id;
				if (parent == null)
				{
					id = null;
				}
				else
				{
					XAttribute xattribute3 = parent.Attribute("name");
					id = ((xattribute3 != null) ? xattribute3.Value : null);
				}
				return new
				{
					sc = sc,
					Value = value2,
					id = id
				};
			});
            foreach (var test in tests)
            {
                JihazResult jihazResult = new JihazResult(test.id);

                // Safely parse decimal
                decimal? parsedValue = TextUtil.Decimal(test.Value);

                string value = parsedValue?.ToString() ?? "<26";

                jihazResult.Results.Add(new LowResult(test.sc, value, null, null, null));

                AstmHigh.LoadResults(jihazResult, instrument, null);
            }
            string[] array = msg.Split(new char[]
			{
				'\r'
			}, StringSplitOptions.RemoveEmptyEntries);
			foreach (string text in array)
			{
				string[] array3 = text.Split(new char[]
				{
					';'
				});
			}
		}

		public static void ParseRotor(string msg, Instrument instrument)
		{
			msg = msg.Substring(msg.IndexOf("\"No.\";", StringComparison.Ordinal)).Replace("\"", "");
			string[] array = msg.Split(new char[]
			{
				'\r'
			}, StringSplitOptions.RemoveEmptyEntries);
			foreach (string text in array)
			{
				string[] array3 = text.Split(new char[]
				{
					';'
				});
				JihazResult jihazResult = new JihazResult(array3[2]);
				decimal? num = TextUtil.Decimal(array3[4]);
				decimal d = 35;
				string value = (num.GetValueOrDefault() < d & num != null) ? "POSITIF" : "NEGATIF";
				jihazResult.Results.Add(new LowResult("Cov19", value, null, array3[4], null));
				AstmHigh.LoadResults(jihazResult, instrument, null);
			}
		}

        public static void ParseUnion(string msg, Instrument instrument)
        {
            XDocument xdocument = XDocument.Parse(msg);

            var results = xdocument.Root.Descendants("Result").Select(c =>
            {
                string sc = c.Attribute("sampleCode")?.Value;
                string code = c.Attribute("testCode")?.Value;
                string val = c.Attribute("value")?.Value;

                return new
                {
                    sc,
                    code,
                    val
                };
            });

            foreach (var result in results)
            {
                AstmHigh2._logger.Info($"{result.sc} {result.code} {result.val}");

                JihazResult jihazResult = new JihazResult(result.sc);
                jihazResult.Results.Add(new LowResult(result.code, result.val, null, null, null));

                AstmHigh.LoadResults(jihazResult, instrument, null);
            }
        }

        public static void ParseHumaClot(string msg, Instrument instrument, char repeater)
		{
			string[] array = msg.Split(Tu.NL);
			bool flag = !array.Contains("START") || !array.Contains("END");
			if (flag)
			{
				AstmHigh2._logger.Error("no Start, no End");
			}
			else
			{
				Dictionary<string, string> dictionary = new Dictionary<string, string>();
				foreach (string source in array)
				{
					string[] array3 = source.Split(";");
					bool flag2 = array3.Length > 1;
					if (flag2)
					{
						dictionary.Add(array3[0], array3[1]);
					}
				}
				JihazResult jr = AstmHigh2.ParseHumaClotLine(dictionary);
				AstmHigh.LoadResults(jr, instrument, new char?(repeater));
			}
		}

		private static JihazResult ParseHumaClotLine(Dictionary<string, string> dict)
		{
			JihazResult jihazResult = new JihazResult(dict["sample_name  "]);
			string at = dict["method_abbrev"].Trim();
			AstmHigh2.GetResult(jihazResult, at, dict, "clot      ");
			AstmHigh2.GetResult(jihazResult, at, dict, "clot-conv1");
			AstmHigh2.GetResult(jihazResult, at, dict, "clot-conv2");
			return jihazResult;
		}

		private static void GetResult(JihazResult jr, string at, Dictionary<string, string> dict, string key)
		{
			try
			{
				string source;
				bool flag = dict.TryGetValue(key, out source);
				bool flag2 = !flag;
				if (!flag2)
				{
					string[] array = source.Split(" ");
					jr.Results.Add(new LowResult(at + ";" + array[2], array[1].Trim(), null, null, null));
				}
			}
			catch (Exception ex)
			{
				AstmHigh2._logger.Error(new LogMessageGenerator(ex.ToString));
			}
		}

		public static string EncodeAll(List<Sample> samples, Instrument instrument, string inFolder)
		{
			string text = string.Format("{0:yyyyMMdd_HHmmss_fff}", DateTime.Now);
			string text2 = "";
			for (int i = 0; i < samples.Count; i++)
			{
				Sample sample = samples[i];
				text2 += AstmHigh2.EncodeSample(sample, instrument);
			}
			bool flag = string.IsNullOrWhiteSpace(text2);
			string result;
			if (flag)
			{
				result = null;
			}
			else
			{
				bool flag2 = instrument.Kind == Jihas.BiosystemA15;
				string path;
				if (flag2)
				{
					path = "import.txt";
				}
				else
				{
					bool flag3 = instrument.Kind == Jihas.Mpl;
					if (flag3)
					{
						path = text + ".AST";
						string text3 = instrument.Now.ToString("yyyyMMddHHmmss");
						text2 = string.Concat(new string[]
						{
							"H|^~\\&|||BMLab||ORM|||MPL|||A2.2|",
							text3,
							"\n",
							text2,
							"L|1|N"
						});
					}
					else
					{
						bool flag4 = instrument.Kind == Jihas.Psm;
						if (flag4)
						{
							path = text + ".RES";
							string text4 = instrument.Now.ToString("yyyyMMddHHmmss");
							text2 = string.Concat(new string[]
							{
								"H|^~\\&|||BMLab||ORM|||MPL||P|H2.2|",
								text4,
								"\n",
								text2,
								"L|1|N"
							});
						}
						else
						{
							bool flag5 = instrument.Kind == Jihas.Evm;
							if (flag5)
							{
								path = text + ".HPR";
								string text5 = instrument.Now.ToString("yyyyMMddHHmmss");
								text2 = string.Concat(new string[]
								{
									"H|^~\\&|||LIS||ORM|||EVM||P|H2.1|",
									text5,
									"\r\n",
									text2,
									"L|1|F\r\n"
								});
							}
							else
							{
								path = string.Format("{0:dd_HHmmss}.astm", DateTime.Now);
							}
						}
					}
				}
				string text6 = Path.Combine(inFolder, path);
				File.AppendAllText(text6, text2);
				bool flag6 = instrument.Kind == Jihas.Mpl || instrument.Kind == Jihas.Psm;
				if (flag6)
				{
					File.AppendAllText(Path.Combine(inFolder, text) + ".OK", ".OK");
				}
				AstmHigh2._logger.Info("envoyé automate : " + text6);
				AstmHigh2._logger.Debug(text2);
				result = text;
			}
			return result;
		}

		public static void ParseAstm(string msg, Instrument instrument)
		{
			try
			{
				msg = msg.Replace(Tu.NL, '\r'.ToString());
				msg = msg.Replace(" ", "");
				AstmHigh2._logger.Debug(msg);
				ASTM_Message message = Parser.Parse(msg);
				AstmHigh.HandleResult(message, instrument);
			}
			catch (Exception ex)
			{
				AstmHigh2._logger.Error(new LogMessageGenerator(ex.ToString));
			}
		}

		public static void ParseHprim(string msg, Instrument instrument)
		{
			try
			{
				msg = msg.Replace(Tu.NL, '\r'.ToString());
				AstmHigh2._logger.Debug(msg);
				Hl7Manager.HandleHprim(msg, instrument);
			}
			catch (Exception ex)
			{
				AstmHigh2._logger.Error(new LogMessageGenerator(ex.ToString));
			}
		}

		private void EncodeSampleKube(Sample sample, Instrument instrument)
		{
			Patient patient = sample.AnalysisRequest.Patient;
			DateTime? patientDateNaiss = patient.PatientDateNaiss;
			bool flag = patientDateNaiss == null || patientDateNaiss.Value.Date == DateTime.Today;
			if (flag)
			{
				patientDateNaiss = new DateTime?(DateTime.Today.AddDays(-1.0));
			}
			string text = patientDateNaiss.Value.ToString("yyyyMMdd");
			string text2 = sample.DateCreated.ToString("yyyyMMddHHmmss");
			string formattedSampleCode = sample.FormattedSampleCode;
			List<string> tests = AstmHigh.GetTests(sample.SampleId, instrument, false);
			string text3 = sample.AnalysisRequest.IsEmergency ? "A" : "R";
			bool flag2 = tests.Count == 0;
			if (flag2)
			{
				AstmHigh2._logger.Info("tests.Count == 0");
			}
			else
			{
				foreach (string text4 in tests)
				{
					string text5 = (instrument.Mode == 0) ? string.Format("{0:yyyyMMdd_HHmmss_fff}", DateTime.Now) : string.Format("BM_{0:yyMMdd_HHmmss_f}", DateTime.Now);
					string path = text5 + ".xml";
					string path2 = Path.Combine(AstmHigh2._inFolder, path);
					string contents = string.Format("<?xml version=\"1.0\" encoding=\"UTF-8\"?>\r\n<lisMessage xmlns:xsi=\"http://www.w3.org/2001/XMLSchema-instance\" xsi:noNamespaceSchemaLocation=\"ASTM.xsd\">\r\n<header>\r\n<version>1394-97</version>\r\n<dateTime>{0}</dateTime>\r\n</header>\r\n<request>\r\n<patientInformation>\r\n<patientIdentifier>{1}</patientIdentifier>\r\n<lastName>{2}</lastName>\r\n<firstName>{3}</firstName>\r\n<birthdate>{4}</birthdate>\r\n<sex>{5}</sex>\r\n<physicianIdentifier>BMLab</physicianIdentifier>\r\n<location>4</location>\r\n</patientInformation>\r\n<testOrder>\r\n<specimen>\r\n<specimenIdentifier>{6}</specimenIdentifier>\r\n</specimen>\r\n<test>\r\n<universalIdentifier>\r\n<testIdentifier>{7}</testIdentifier>\r\n<dilution>1/1</dilution>\r\n</universalIdentifier>\r\n</test>\r\n</testOrder>\r\n</request>\r\n</lisMessage>", new object[]
					{
						text2,
						patient.PatientID,
						patient.Nom,
						patient.Prenom,
						text,
						patient.ShortSexe,
						formattedSampleCode,
						text4
					});
					File.AppendAllText(path2, contents);
					this.SendFtp(text5, instrument.Kind);
				}
			}
		}

		private static string EncodeSample(Sample sample, Instrument instrument)
		{
			Patient patient = sample.AnalysisRequest.Patient;
			DateTime? patientDateNaiss = patient.PatientDateNaiss;
			bool flag = patientDateNaiss == null || patientDateNaiss.Value.Date == DateTime.Today;
			if (flag)
			{
				patientDateNaiss = new DateTime?(DateTime.Today.AddDays(-1.0));
			}
			string text = patientDateNaiss.Value.ToString("yyyyMMdd");
			string text2 = sample.DateCreated.ToString("yyyyMMddHHmmss");
			string formattedSampleCode = sample.FormattedSampleCode;
			List<string> tests = AstmHigh.GetTests(sample.SampleId, instrument, false);
			string text3 = string.Join("~", tests.Distinct<string>());
			string text4 = string.Join("\\", from x in tests.Distinct<string>()
			select "^^^" + x);
			string text5 = sample.AnalysisRequest.IsEmergency ? "A" : "R";
			bool flag2 = tests.Count == 0;
			string result;
			if (flag2)
			{
				AstmHigh2._logger.Info("tests.Count == 0");
				result = null;
			}
			else
			{
				string text6 = "";
				Jihas kind = instrument.Kind;
				bool flag3 = kind == Jihas.Huma200;
				if (flag3)
				{
					string text7 = string.Join(Tu.NL, tests.Select((string x, int i) => string.Format("O|{0}|||{1}|False||||||||||Serum|||||||||||||||", i + 1, x)));
					text6 = string.Concat(new string[]
					{
						"H|\\^&|||HSX00^V1.0|||||Host||P|1|20110117\r\nP|1||",
						formattedSampleCode,
						"||",
						patient.Nom,
						"|",
						patient.Prenom,
						"|",
						text,
						"||||||||||||||||||||||||||\r\nC|1|||\r\n",
						text7,
						"\r\nL||N\r\n"
					});
				}
				else
				{
					bool flag4 = kind == Jihas.BiosystemA15;
					if (flag4)
					{
						string source = TextUtil.GetSource(sample.SampleSource.SampleSourceCode);
						text6 = string.Join(Tu.NL, tests.Select((string x, int i) => string.Format("N\t{0}\t{1}\t{2}\tT15", source, sample.SampleCode, x))) + Tu.NL;
					}
					else
					{
						bool flag5 = kind == Jihas.Mpl;
						if (flag5)
						{
							text6 = string.Concat(new string[]
							{
								string.Format("P|1|{0}|||{1}^{2}||{3}|{4}||||||||||||||||||\r\n", new object[]
								{
									patient.PatientID,
									patient.Nom,
									patient.Prenom,
									text,
									patient.ShortSexe
								}),
								"OBR|1|",
								formattedSampleCode,
								"||",
								text3,
								"|",
								text5,
								"||",
								text2,
								"||||A|||",
								text2,
								"||a1||BMLab||a3||a4|||\r\nC|1|L|",
								sample.AnalysisRequest.Remark,
								"\r\n"
							});
						}
						else
						{
							bool flag6 = kind == Jihas.Psm;
							if (flag6)
							{
								text6 = string.Concat(new string[]
								{
									string.Format("P|1|{0}|||{1}^{2}||{3}|CN||^^^^|||^&&&&&|||||||||||||||||||||\r\n", new object[]
									{
										patient.PatientID,
										patient.Nom,
										patient.Prenom,
										text
									}),
									"O|1|",
									formattedSampleCode,
									"||",
									text4,
									"|R||||||A|||",
									text2,
									"||^||||||||||||||||\r\n"
								});
							}
							else
							{
								bool flag7 = kind == Jihas.Evm;
								if (flag7)
								{
									text6 = string.Concat(new string[]
									{
										string.Format("P|1|{0}|||{1}^{2}||{3}|{4}|||||^BMLab||||||||||||BMLab\r\n", new object[]
										{
											patient.PatientID,
											patient.Nom,
											patient.Prenom,
											text,
											patient.ShortSexe
										}),
										"OBR|1|",
										formattedSampleCode,
										"^",
										formattedSampleCode,
										"||",
										text3,
										"|",
										text5,
										"||",
										text2,
										"||||A|||",
										text2,
										"|||||||||||O\r\n"
									});
								}
							}
						}
					}
				}
				AstmHigh2._logger.Debug(text6);
				result = text6;
			}
			return result;
		}

		private List<Jihas> _list = new List<Jihas>
		{
			Jihas.BiosystemA15,
			Jihas.Huma200,
			Jihas.Mpl,
			Jihas.Evm
		};

		private static Logger _logger = LogManager.GetCurrentClassLogger();

		private Instrument _instrument;

		public char Repeater;

		private Timer _serviceTimer;

		private string _sortie;

		private string _entree;

		private string _ip;

		private string _user;

		private string _pass;

		private string _sr;

		private string _ext;

		private string _er;

		private static string _inFolder = "C:\\BM\\temp1\\";
	}
}
