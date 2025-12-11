using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Ports;
using System.Linq;
using System.Threading;
using System.Timers;
using GbService.ASTM;
using GbService.Common;
using GbService.Communication;
using GbService.Communication.Common;
using GbService.Communication.Serial;
using GbService.Communication.TCP;
using GbService.HL7;
using GbService.Model.Contexts;
using GbService.Model.Domain;
using GbService.Other;
using GbService.Properties;
using NLog;

namespace GbService
{
	public class Gb
	{
		////xxxxxx
		// [FIX 1] Add these missing fields if they aren't there
    // 'g' is the logger for the password file
    private readonly NLog.Logger g = LogManager.GetLogger("Pass");
    
    // 'h' is the Instrument ID from settings
    private int h = Settings.Default.ID;
    
    // 'i' is the Instrument object
    private Instrument i;

    // Backing field for Key
    private static string e;

    // [FIX 2] Ensure Key has a 'set' accessor
    public static string Key
    {
        get { return Gb.e; }
        set { Gb.e = value; } // <--- This SET is required for CS0200
    }
		/*public static string Key
		{
			get
			{
				return "ge``\\rfd67481+5'6r*]@\\rfçè-t?'éèça)àoiçàae";
			}
		}
		*/
		public void Update1(LaboContext db)
		{
			bool flag = this._instrument.L1 > 0L;
			if (!flag)
			{
				Jihas kind = this._instrument.Kind;
				Jihas jihas = kind;
				if (jihas <= Jihas.TosohGx)
				{
					if (jihas != Jihas.AU480)
					{
						if (jihas == Jihas.TosohGx)
						{
							this._instrument.InstrumentStopBits = new int?(this._instrument.InstrumentDays);
						}
					}
					else
					{
						IQueryable<AnalysisTypeInstrumentMapping> queryable = from x in db.AnalysisTypeInstrumentMappings
						where x.InstrumentCode == this._instrument.InstrumentCode
						select x;
						foreach (AnalysisTypeInstrumentMapping analysisTypeInstrumentMapping in queryable)
						{
							bool flag2 = analysisTypeInstrumentMapping.AnalysisTypeCode == null;
							if (!flag2)
							{
								analysisTypeInstrumentMapping.AnalysisTypeCode = analysisTypeInstrumentMapping.AnalysisTypeCode.TrimStart(new char[]
								{
									'0'
								});
							}
						}
					}
				}
				else if (jihas == Jihas.Huma200 || jihas == Jihas.BiosystemA15)
				{
					this._instrument.InstrumentDataBits = "C";
				}
				this._instrument.B1 = (this._instrument.Mode != 2 || Gb.Bid.Contains(this._instrument.Kind));
				this._instrument.L1 = 1L;
				db.SaveChanges();
			}
		}

		public void Update2(LaboContext db)
		{
			bool flag = this._instrument.L1 > 1L;
			if (!flag)
			{
				bool flag2 = this._instrument.Kind == Jihas.AU480;
				if (flag2)
				{
					this._instrument.Mode = ((this._instrument.InstrumentStopBits.GetValueOrDefault() == 3) ? 1 : 0);
				}
				else
				{
					bool flag3 = this._instrument.Kind == Jihas.TosohGx;
					if (flag3)
					{
						this._instrument.Mode = ((this._instrument.InstrumentStopBits.GetValueOrDefault() == 3) ? 1 : 0);
					}
					else
					{
						bool flag4 = this._instrument.Kind == Jihas.DiagonDcell_BC3000;
						if (flag4)
						{
							this._instrument.Mode = 1;
						}
					}
				}
				this._instrument.L1 = 2L;
				db.SaveChanges();
			}
		}

		public void Update3(LaboContext db)
		{
			bool flag = this._instrument.L2 != 0L;
			if (!flag)
			{
				this._instrument.L2 = (long)this._instrument.InstrumentCode;
				Jihas kind = this._instrument.Kind;
				Jihas jihas = kind;
				if (jihas <= Jihas.SysmexKx21N)
				{
					if (jihas <= Jihas.Aia360)
					{
						if (jihas != Jihas.MiniVidas && jihas != Jihas.Aia360)
						{
							goto IL_D1;
						}
						this._instrument.L3 = 1L;
						goto IL_D1;
					}
					else if (jihas != Jihas.CobasE411 && jihas - Jihas.CobasC311 > 1)
					{
						goto IL_D1;
					}
				}
				else if (jihas <= Jihas.TosohG8)
				{
					if (jihas != Jihas.SysmexCA600 && jihas != Jihas.TosohG8)
					{
						goto IL_D1;
					}
				}
				else if (jihas - Jihas.TosohGx > 1 && jihas != Jihas.Spa && jihas != Jihas.CobasC111)
				{
					goto IL_D1;
				}
				this._instrument.L3 = 2L;
				IL_D1:
				db.SaveChanges();
			}
		}

		public static void Test(Instrument instrument)
		{
			string text = File.ReadAllText("c:\\test.txt");
			text = Ct.Prepare(text, true);
			Hl7Manager.HandleHprim(text, instrument);
		}

		public virtual void ServiceThreadBody()
		{
			try
			{
				LaboContext laboContext = new LaboContext();
				Gb._info.Info("Starting Service... instrumentId=" + this._instrumentId.ToString());
				this._instrument = laboContext.Instrument.Find(new object[]
				{
					this._instrumentId
				});
				Gb._info.Info("instrumentId=" + this._instrumentId.ToString());
				this.Update1(laboContext);
				this.Update2(laboContext);
				this.Update3(laboContext);
				Helper.ID = 1;
				bool flag = this._instrument == null;
				if (flag)
				{
					Gb._info.Error("instrument not found");
				}
				else
				{
					Gb._info.Info(string.Format("Kind= {0}, Code= {1}, Mode= {2}", this._instrument.Kind, this._instrument.InstrumentCode, this._instrument.Mode));
					string fileName = DateTime.Now.ToString(string.Format("{0}_yyyyMMdd_HHmmss", this._instrumentId));
					Jihas kind = this._instrument.Kind;
					string instrumentPortName = this._instrument.InstrumentPortName;
					bool flag2 = LogHelper.Init(this._instrument, fileName);
					bool checkEnd = true;
					Handshake hs = this._hs.Contains(kind) ? Handshake.RequestToSend : Handshake.None;
					Dictionary<Jihas, string> dictionary = new Dictionary<Jihas, string>
					{
						{
							Jihas.Micros60,
							"03"
						},
						{
							Jihas.DiagonDcell_BC3000,
							"1A"
						},
						{
							Jihas.SfriH18,
							"26"
						},
						{
							Jihas.GenusKT6400,
							"26"
						},
						{
							Jihas.TosohG8,
							"0D"
						},
						{
							Jihas.TosohG7,
							"0D"
						},
						{
							Jihas.SysmexKx21N,
							"03"
						},
						{
							Jihas.CellTacMek6500,
							"03"
						}
					};

					string text = dictionary.ContainsKey(kind) ? dictionary[kind] : null;
					bool rts = this._instrument.L3 == 1L || this._instrument.L3 == 3L;
					bool dtr = this._instrument.L3 == 2L || this._instrument.L3 == 3L;
					bool flag3 = this._instrument.L3 > 0L;
					if (flag3)
					{
						hs = Handshake.RequestToSend;
					}
					bool flag4 = kind == Jihas.CellTacMek6500 && this._instrument.Mode == 1;
					if (flag4)
					{
						hs = Handshake.XOnXOff;
					}
					ILowManager il;
					if (!(this._instrument.InstrumentBaudRate == "0"))
					{
						if (!(this._instrument.InstrumentBaudRate == "1"))
						{
							ILowManager lowManager = (this._instrument.InstrumentBaudRate == "2") ? new TcpManager(instrumentPortName, kind, flag2, text, checkEnd) : null;
							il = lowManager;
						}
						else
						{
							ILowManager lowManager = new TcpClient(instrumentPortName, kind, flag2, text, checkEnd);
							il = lowManager;
						}
					}
					else
					{
						ILowManager lowManager = new SerialManager(instrumentPortName, kind, flag2, text, hs, checkEnd, dtr, rts);
						il = lowManager;
					}
					this._il = il;
					Jihas jihas = kind;
					Jihas jihas2 = jihas;
					if (jihas2 <= Jihas.LH780)
					{
						VidasHandler high;
						if (jihas2 <= Jihas.Kenza240)
						{
							if (jihas2 <= Jihas.Evm)
							{
								if (jihas2 == Jihas.RotorGene)
								{
									goto IL_854;
								}
								if (jihas2 != Jihas.Vidas)
								{
									if (jihas2 != Jihas.Evm)
									{
										goto IL_876;
									}
									goto IL_854;
								}
							}
							else if (jihas2 <= Jihas.Union)
							{
								switch (jihas2)
								{
								case Jihas.Psm:
									goto IL_854;
								case Jihas.Medconn:
								case Jihas.SwelabSmall:
									goto IL_876;
								case Jihas.Sysmex_Xt2000i:
									goto IL_745;
								case Jihas.Yonder:
									HttpServer.Handle("http://+:" + instrumentPortName + "/");
									goto IL_9CD;
								default:
									if (jihas2 != Jihas.Union)
									{
										goto IL_876;
									}
									goto IL_854;
								}
							}
							else
							{
								if (jihas2 == Jihas.LH780U)
								{
									LH780Manager lh780Manager = new LH780Manager(this._il, this._instrument);
									goto IL_9CD;
								}
								if (jihas2 != Jihas.Kenza240)
								{
									goto IL_876;
								}
								Kenza240Manager manager6 = new Kenza240Manager(this._il, this._instrument);
								Kenza240Handler high2 = new Kenza240Handler(manager6);
								Gb.ScheduleRefresh(30, delegate(object sender, ElapsedEventArgs e)
								{
									high2.Upload();
								});
								goto IL_9CD;
							}
						}
						else if (jihas2 <= Jihas.Mpl)
						{
							if (jihas2 == Jihas.V8)
							{
								goto IL_745;
							}
							if (jihas2 != Jihas.Vitek)
							{
								if (jihas2 != Jihas.Mpl)
								{
									goto IL_876;
								}
								new AstmHigh2(this._instrument, "\\presq", "\\resu");
								goto IL_9CD;
							}
						}
						else if (jihas2 <= Jihas.PcrReal)
						{
							if (jihas2 == Jihas.VidasKube)
							{
								goto IL_854;
							}
							if (jihas2 != Jihas.PcrReal)
							{
								goto IL_876;
							}
							new AstmHigh2(this._instrument, "\\presq", "");
							goto IL_9CD;
						}
						else
						{
							if (jihas2 == Jihas.HLA)
							{
								goto IL_854;
							}
							if (jihas2 != Jihas.LH780)
							{
								goto IL_876;
							}
							LH780Manager manager2 = new LH780Manager(this._il, this._instrument);
							LH780MessageHandler high3 = new LH780MessageHandler(manager2);
							Gb.ScheduleRefresh(20, delegate(object sender, ElapsedEventArgs e)
							{
								high3.Upload();
							});
							goto IL_9CD;
						}
						VidasManager manager3 = new VidasManager(this._il, this._instrument);
						high = new VidasHandler(manager3);
						Gb.ScheduleRefresh(15, delegate(object sender, ElapsedEventArgs e)
						{
							high.Upload();
						});
						goto IL_9CD;
						IL_745:
						new AstmManagerV2(this._il, this._instrument);
						goto IL_9CD;
						IL_854:
						new AstmHigh2(this._instrument);
						goto IL_9CD;
					}
					if (jihas2 <= Jihas.Cobas400Plus)
					{
						if (jihas2 <= Jihas.Vitros350)
						{
							if (jihas2 == Jihas.AU480)
							{
								new AU480Manager(this._il, this._instrument);
								goto IL_9CD;
							}
							if (jihas2 == Jihas.VitrosEciq)
							{
								AstmManager manager = new AstmManager(this._il, this._instrument);
								Gb.ScheduleRefresh(30, delegate(object sender, ElapsedEventArgs e)
								{
									AstmHigh.Upload(manager, null);
								});
								goto IL_9CD;
							}
							if (jihas2 == Jihas.Vitros350)
							{
								KermitManager manager4 = new KermitManager(this._il, this._instrument);
								KermitHigh high = new KermitHigh(manager4);
								Gb.ScheduleRefresh(30, delegate(object sender, ElapsedEventArgs e)
								{
									high.Upload();
								});
								goto IL_9CD;
							}
						}
						else if (jihas2 <= Jihas.SwingSaxo)
						{
							if (jihas2 == Jihas.Advia2120)
							{
								Advia2120MessageHandler advia2120MessageHandler = new Advia2120MessageHandler(this._il, this._instrument);
								goto IL_9CD;
							}
							if (jihas2 == Jihas.SwingSaxo)
							{
								new AstmHigh1(this._instrument, '\\', '^');
								goto IL_9CD;
							}
						}
						else
						{
							if (jihas2 == Jihas.Capillarys)
							{
								new FileHigh(this._instrument);
								goto IL_9CD;
							}
							if (jihas2 == Jihas.Cobas400Plus)
							{
								this._il.MessageReceived += this.OnMessageReceived;
								new Cobas400PlusMessageHandler(this._il, this._instrument);
								goto IL_9CD;
							}
						}
					}
					else if (jihas2 <= Jihas.Pictus200)
					{
						if (jihas2 == Jihas.SelectraE)
						{
							this._il.MessageReceived += this.OnMessageReceived;
							new SelectraeHandler(this._il, this._instrument);
							goto IL_9CD;
						}
						if (jihas2 == Jihas.Huma200)
						{
							new AstmHigh2(this._instrument, "\\Input Worklist", "\\Output Worklist");
							goto IL_9CD;
						}
						if (jihas2 == Jihas.Pictus200)
						{
							AstmManager manager = new AstmManager(this._il, this._instrument);
							Gb.ScheduleRefresh(30, delegate(object sender, ElapsedEventArgs e)
							{
								AstmHigh.Upload(manager, null);
								Thread.Sleep(15000);
								AstmHigh.Request(manager);
							});
							goto IL_9CD;
						}
					}
					else if (jihas2 <= Jihas.BiosystemA15)
					{
						if (jihas2 == Jihas.HumaClot)
						{
							new AstmHigh2(this._instrument, "\\host", "\\host");
							goto IL_9CD;
						}
						if (jihas2 == Jihas.BiosystemA15)
						{
							new AstmHigh2(this._instrument, "\\Import", "\\Export");
							goto IL_9CD;
						}
					}
					else
					{
						if (jihas2 == Jihas.Targa)
						{
							TargaManager manager = new TargaManager(this._il, this._instrument);
							TargaHandler high = new TargaHandler(manager);
							Gb.ScheduleRefresh(30, delegate(object sender, ElapsedEventArgs e)
							{
								high.Upload();
								manager.Download();
							});
							goto IL_9CD;
						}
						if (jihas2 == Jihas.BeckmanCX9)
						{
							BeckmanCX9Manager manager5 = new BeckmanCX9Manager(this._il, this._instrument);
							BeckmanCX9Handler beckmanCX9Handler = new BeckmanCX9Handler(manager5);
							goto IL_9CD;
						}
					}
					IL_876:
					bool flag5 = this._astm.Contains(kind) || Gb.Bid.Contains(kind);
					if (flag5)
					{
						AstmManager manager = new AstmManager(this._il, this._instrument);
						string instrumentDataBits = this._instrument.InstrumentDataBits;
						bool flag6 = instrumentDataBits == "C" || instrumentDataBits == "B" || instrumentDataBits == "D";
						if (flag6)
						{
							Gb.ScheduleRefresh(15, delegate(object sender, ElapsedEventArgs e)
							{
								AstmHigh.Upload(manager, null);
							});
						}
					}
					else
					{
						bool flag7 = TextUtil._hl7.Contains(kind) || TextUtil._hl7_2.Contains(kind);
						if (flag7)
						{
							bool flag8 = kind == Jihas.Biolis30i;
							if (flag8)
							{
								string tcp = this._instrument.S1 ?? "0.0.0.0,55001";
								TcpManager il2 = new TcpManager(tcp, kind, flag2, null, checkEnd);
								new Hl7Manager(il2, this._instrument, false);
								bool b = this._instrument.B1;
								if (b)
								{
									new Hl7Manager(this._il, this._instrument, true);
								}
							}
							else
							{
								new Hl7Manager(this._il, this._instrument, false);
							}
						}
						else
						{
							this._il.MessageReceived += this.OnMessageReceived;
						}
					}
					IL_9CD:;
				}
			}
			catch (Exception ex)
			{
				Gb._info.Error(new LogMessageGenerator(ex.ToString));
			}
		}

        /*	private void OnMessageReceived(object sender, MessageReceivedEventArgs messageReceivedEventArgs)
            {
                try
                {
                    string text = messageReceivedEventArgs.Message;
                    Jihas kind = this._instrument.Kind;
                    Jihas jihas = kind;
                    Jihas jihas2 = jihas;
                    if (jihas2 <= Jihas.Minicap)
                    {
                        if (jihas2 <= Jihas.MiniVidas)
                        {
                            if (jihas2 == Jihas.Micros60)
                            {
                                new AbxMessageHandler(text, this._instrument);
                                goto IL_28E;
                            }
                            if (jihas2 != Jihas.MiniVidas)
                            {
                                goto IL_274;
                            }
                            new MiniVidasMessageHandler(this._instrument).Basic(text, this._il);
                            goto IL_28E;
                        }
                        else if (jihas2 != Jihas.DiagonDcell_BC3000 && jihas2 != Jihas.GenusKT6400)
                        {
                            if (jihas2 != Jihas.Minicap)
                            {
                                goto IL_274;
                            }
                            new MiniCapHandler(this._il, this._instrument).Handle(text);
                            goto IL_28E;
                        }
                    }
                    else if (jihas2 <= Jihas.SysmexKx21N)
                    {
                        switch (jihas2)
                        {
                        case Jihas.Vision:
                            new VisionHandler(this._instrument, this._il).HandleMessage(text);
                            goto IL_28E;
                        case (Jihas)2019:
                        case Jihas.Dialab:
                        case Jihas.Vitros350:
                            goto IL_274;
                        case Jihas.Mythic:
                            new MyticMessageHandler(this._instrument).Parse(text);
                            goto IL_28E;
                        case Jihas.Cyan:
                            new CyanStripMiniMessageHandler(this._instrument).Parse(text);
                            goto IL_28E;
                        case Jihas.Advia560:
                            this._il.SendLow("ACK", Coding.Asc);
                            new Advia560Handler(this._instrument).Parse(text);
                            goto IL_28E;
                        default:
                            if (jihas2 != Jihas.SysmexKx21N)
                            {
                                goto IL_274;
                            }
                            SysmexKX21NMessageHandler.Parse(this._instrument, text);
                            goto IL_28E;
                        }
                    }
                    else
                    {
                        if (jihas2 == Jihas.Cobas400Plus)
                        {
                            Cobas400PlusMessageHandler.Parse(text, this._instrument);
                            goto IL_28E;
                        }
                        switch (jihas2)
                        {
                        case Jihas.SelectraE:
                            SelectraeHandler.Parse(text, this._instrument);
                            goto IL_28E;
                        case Jihas.TosohGx:
                        case Jihas.Chemray240:
                        case Jihas.Spa:
                        case Jihas.MindrayCL1000i:
                            goto IL_274;
                        case Jihas.CellTacMek6500:
                            new CelltakHandler(this._instrument).Parse(text);
                            goto IL_28E;
                        case Jihas.HumaCount5:
                        {
                            text = text.Substring(text.IndexOf('\u0001'));
                            this._il.SendLow("\u0006 " + text.Substring(1, 1), Coding.Asc);
                            bool flag = text.Substring(2, 1) == "D";
                            if (flag)
                            {
                                HumaCount5Handler.Parse5(text, this._instrument);
                            }
                            goto IL_28E;
                        }
                        case Jihas.SfriH18:
                            break;
                        case Jihas.HumaCount60:
                            HumaCount5Handler.Parse60(text, this._instrument);
                            goto IL_28E;
                        default:
                            if (jihas2 != Jihas.Gh900)
                            {
                                goto IL_274;
                            }
                            HumaCount5Handler.ParseGh900(text, this._instrument);
                            goto IL_28E;
                        }
                    }
                    DiagonDCell60MessageHandler.Parse(this._instrument, text);
                    goto IL_28E;
                    IL_274:
                    new ProHandler(this._instrument).Parse(text, this._il);
                    IL_28E:;
                }
                catch (Exception ex)
                {
                    Gb._info.Error(new LogMessageGenerator(ex.ToString));
                }
            }*/
        private void OnMessageReceived(object sender, MessageReceivedEventArgs messageReceivedEventArgs)
        {
            try
            {
                string text = messageReceivedEventArgs.Message;
                Jihas kind = this._instrument.Kind;
                Jihas jihas = kind;
                Jihas jihas2 = jihas;

                if (jihas2 <= Jihas.Minicap)
                {
                    if (jihas2 <= Jihas.MiniVidas)
                    {
                        if (jihas2 == Jihas.Micros60)
                        {
                            new AbxMessageHandler(text, this._instrument);
                            goto IL_28E;
                        }
                        if (jihas2 != Jihas.MiniVidas)
                        {
                            goto IL_274;
                        }
                        new MiniVidasMessageHandler(this._instrument).Basic(text, this._il);
                        goto IL_28E;
                    }
                    else if (jihas2 != Jihas.DiagonDcell_BC3000 && jihas2 != Jihas.GenusKT6400)
                    {
                        if (jihas2 != Jihas.Minicap)
                        {
                            goto IL_274;
                        }
                        new MiniCapHandler(this._il, this._instrument).Handle(text);
                        goto IL_28E;
                    }
                }
                else if (jihas2 <= Jihas.SysmexKx21N)
                {
                    switch (jihas2)
                    {
                        case Jihas.Vision:
                            new VisionHandler(this._instrument, this._il).HandleMessage(text);
                            goto IL_28E;
                        case (Jihas)2019:
                        case Jihas.Dialab:
                        case Jihas.Vitros350:
                            goto IL_274;
                        case Jihas.Mythic:
                            new MyticMessageHandler(this._instrument).Parse(text);
                            goto IL_28E;
                        case Jihas.Cyan:
                            new CyanStripMiniMessageHandler(this._instrument).Parse(text);
                            goto IL_28E;
                        case Jihas.Advia560:
                            this._il.SendLow("ACK", Coding.Asc);
                            new Advia560Handler(this._instrument).Parse(text);
                            goto IL_28E;
                        default:
                            if (jihas2 != Jihas.SysmexKx21N)
                            {
                                goto IL_274;
                            }
                            SysmexKX21NMessageHandler.Parse(this._instrument, text);
                            goto IL_28E;
                    }
                }
                else
                {
                    if (jihas2 == Jihas.Cobas400Plus)
                    {
                        Cobas400PlusMessageHandler.Parse(text, this._instrument);
                        goto IL_28E;
                    }
                    switch (jihas2)
                    {
                        case Jihas.SelectraE:
                            SelectraeHandler.Parse(text, this._instrument);
                            goto IL_28E;
                        case Jihas.TosohGx:
                        case Jihas.Chemray240:
                        case Jihas.Spa:
                        case Jihas.MindrayCL1000i:
                            goto IL_274;

                        // [FIX] ADDED ROLLER CASE HERE
                        case Jihas.Roller:
                            // Use the LifotronicHandler for Roller (H9)
                            LifotronicHandler.Parse(text, this._instrument);
                            goto IL_28E;
                        // Inside OnMessageReceived switch statement:

                        case Jihas.SysmexUC1000: // Kind 74
                                                 // [FIX] Add 'ProHandler.' before the method name
                            ProHandler.ParseUC1000(text, this._instrument);
                            goto IL_28E;
                        case Jihas.CellTacMek6500:
                            new CelltakHandler(this._instrument).Parse(text);
                            goto IL_28E;
                        case Jihas.HumaCount5:
							{
								text = text.Substring(text.IndexOf('\u0001'));
								this._il.SendLow("\u0006 " + text.Substring(1, 1), Coding.Asc);
								bool flag = text.Substring(2, 1) == "D";
								if (flag)
								{
									HumaCount5Handler.Parse5(text, this._instrument);
								}
								goto IL_28E;
							}
                        //case Jihas.SysmexUC1000:
                            // Create Serial Manager with the new handler
                           // SysmexUC1000Handler.Parse(text, this._instrument);
                        case Jihas.SfriH18:
                            break;
                        case Jihas.HumaCount60:
                            HumaCount5Handler.Parse60(text, this._instrument);
                            goto IL_28E;
                        default:
                            if (jihas2 != Jihas.Gh900)
                            {
                                goto IL_274;
                            }
                            HumaCount5Handler.ParseGh900(text, this._instrument);
                            goto IL_28E;
                    }
                }
                DiagonDCell60MessageHandler.Parse(this._instrument, text);
                goto IL_28E;
            IL_274:
                new ProHandler(this._instrument).Parse(text, this._il);
            IL_28E:;
            }
            catch (Exception ex)
            {
                Gb._info.Error(new LogMessageGenerator(ex.ToString));
            }
        }
        public void Stop()
		{
			try
			{
				bool flag = this._instrument.Kind == Jihas.AU480;
				if (flag)
				{
					new LaboContext().Database.ExecuteSqlCommand(string.Format("delete from LabMessage where InstrumentId = {0}", this._instrumentId), new object[0]);
				}
				this._il.Close();
				Gb._info.Info("Service Stopped");
			}
			catch (Exception ex)
			{
				Gb._info.Error(new LogMessageGenerator(ex.ToString));
			}
		}

		private static void ScheduleRefresh(int seconds, Gb.Refresh r)
		{
			System.Timers.Timer timer = new System.Timers.Timer
			{
				Enabled = true,
				Interval = (double)(1000 * seconds)
			};
			timer.Elapsed += delegate(object s, ElapsedEventArgs a)
			{
				r(s, a);
			};
			timer.Start();
		}

		public static List<Jihas> Bid = new List<Jihas>
		{
			Jihas.Architect,
			Jihas.SysmexCA600,
			Jihas.Mispa,
			Jihas.OrthoVision,
			Jihas.Immulite2000,
			Jihas.CobasC111,
			Jihas.Aia360,
			Jihas.Huma600,
			Jihas.StaRMAX,
			Jihas.Vitros4600,
			Jihas.Vitros3600,
			Jihas.Biolis24i,
			Jihas.CobasC311,
			Jihas.Gemini,
			Jihas.Pictus200,
			Jihas.Iflash,
			Jihas.Euro,
			Jihas.OrthoIM,
			Jihas.Macura,
			Jihas.Bioplex,
			Jihas.Atellica,
			Jihas.MaglumiX8,
			Jihas.SysmexSuit,
			Jihas.Advia1800,
			Jihas.Navify,
			Jihas.EUROLabOffice,
			Jihas.Indiko,

            Jihas.Autobio
        };

		private List<Jihas> _astm = new List<Jihas>
		{
			Jihas.VitrosEciq,
			Jihas.Access2,
			Jihas.SelectraProM,
			Jihas.Acl,
			Jihas.Arkray,
			Jihas.Maglumi,
			Jihas.Response,
			Jihas.Acl9000,
			Jihas.SysmexXS_XN,
			Jihas.DXH800,
			Jihas.LiaisonXL,
			Jihas.CobasE411,
			Jihas.ErbaEc90,
			Jihas.Bioflash,
			Jihas.Iris,
			Jihas.Spa,
			Jihas.StaSatelit,
			Jihas.StagoStaMax,
			Jihas.Ismart,
			Jihas.Biorad10,
			Jihas.BA200_BA400,
			Jihas.Urilyser,
			Jihas.SysmexUf1000,
			Jihas.YumizenH500,
			Jihas.Ampilink,
			Jihas.Urit8031,
			Jihas.Autobio,
            Jihas.Vitek


        };

		private List<Jihas> _hs = new List<Jihas>
		{
			Jihas.Vitros350,
			Jihas.Biolis24i,
			Jihas.TosohG7,
			Jihas.SysmexKx21N,
			Jihas.SysmexUC1000
		};

		private static Logger _info = LogManager.GetLogger("Info");

		private readonly Logger _pass = LogManager.GetLogger("Pass");

		private int _instrumentId = Settings.Default.ID;

		private Instrument _instrument;

		private ILowManager _il;

		public delegate void Refresh(object sender, ElapsedEventArgs e);
	}
}
