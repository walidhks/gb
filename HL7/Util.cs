using System;
using GbService.Communication;
using GbService.Model.Domain;
//88888
namespace GbService.HL7
{
	public class Util
	{
		public static Util Init(Instrument instrument)
		{
			Util util = TextUtil.GetUtil(instrument);
			return util ?? new Util(instrument);
		}

		public Util(Instrument instrument)
		{
// ---------------------------------------------------------
            // [FIX] URIT3000 MODE 6: Force Pattern OBR.2.OBX.3.5.0
            //* ---------------------------------------------------------
           /* if (instrument.Kind == Jihas.Urit3000 && instrument.Mode == 6)
            {
                this.Obr = "OBR";
                this.Obri = 2;    // Field Index for Sample ID
                this.Obx = "OBX";
                this.Obxi = 3;    // Field Index for Test Code
                this.Obxj = 5;    // Field Index for Result Value
                this.Index = 0;   // Component Index (Take 1st part)
                return;           // Exit immediately to prevent overwrite
            }*/
            // ---------------------------------------------------------
            Jihas kind = instrument.Kind;
			this.Obr = "OBR";
			this.Obri = 3;
			this.Obx = "OBX";
			this.Obxi = 3;
			this.Obxj = 5;
			this.Index = null;
			Jihas jihas = kind;
			Jihas jihas2 = jihas;
			if (jihas2 <= Jihas.Ichroma)
			{
				if (jihas2 == Jihas.Cobas8000)
				{
					this.Obr = "SPM";
					this.Obri = 2;
					return;
				}

				if (jihas2 != Jihas.Evm)
				{
					if (jihas2 != Jihas.Ichroma)
					{
						return;
					}
					this.Obr = "PID";
					this.Obri = 2;
					return;
				}
			}
			else
			{
				if (jihas2 == Jihas.ErbaEc90)
				{
					this.Obxi = 4;
					return;
				}
				if (jihas2 == Jihas.ZybioExc200)
				{
					this.Obri = 2;
					return;
				}
				switch (jihas2)
				{
				case Jihas.Biolis30i:
					this.Obr = (instrument.B1 ? "SAC" : "PID");
					return;
				case Jihas.HumaLytePlus5:
				case Jihas.Vitek:
				case Jihas.YumizenH500:
				case Jihas.Ampilink:
				case Jihas.KenzaMax:
				case Jihas.VirClia:
				case Jihas.Precision:
				case Jihas.Atellica:
					return;
				case Jihas.Urit3000:
				{
					bool flag = instrument.Mode == 1;
					if (flag)
					{
						this.Obri = 4;
					}
					else
					{
						bool flag2 = instrument.Mode == 2;
						if (flag2)
						{
							this.Obri = 2;
						}
					}
					return;
				}
				case Jihas.Urit8031:
					this.Obri = 2;
					this.Obxi = 4;
					return;
				case Jihas.YumizenH550:
					this.Obr = "SPM";
					this.Obri = 2;
					return;
				case Jihas.I15:
					this.Obxi = 4;
					return;
				case Jihas.HumaCount80:
					this.Obr = "PID";
					return;
				case Jihas.F200:
					this.Obr = "PID";
					return;
				case Jihas.Mpl:
					break;
				case Jihas.CobasPro:
					this.Obr = "SAC";
					this.Obri = 3;
					return;
				case Jihas.DiruiCsT180:
					this.Obri = 2;
					this.Obxj = 4;
					return;
				default:
					return;
				}
			}
			this.Obri = 2;
		}

		public Util(string obr, string s, string s1, string s2, string s3, string s4)
		{
			this.Obr = obr;
			this.Obri = TextUtil.GetInt(s).GetValueOrDefault(3);
			this.Obx = s1;
			this.Obxi = TextUtil.GetInt(s2).GetValueOrDefault(3);
			this.Obxj = TextUtil.GetInt(s3).GetValueOrDefault(5);
			this.Index = TextUtil.GetInt(s4);
		}

		public string Obr;

		public string Obx;

		public int Obri;

		public int Obxi;

		public int Obxj;

		public int? Index;
	}
}
