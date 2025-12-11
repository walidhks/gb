using System;
using System.Collections.Generic;
using GbService.Communication;
using GbService.Model.Domain;

namespace GbService.HL7
{
	public class Util2
	{
		public static Util2 Init(Instrument instrument)
		{
			return Util2.GetUtil(instrument) ?? new Util2(instrument);
		}

		public static Util2 GetUtil(Instrument instrument)
		{
			List<string> list = TextUtil.SplitStr(instrument.S3);
			Util2 result;
			if (list != null && list.Count == 6)
			{
				result = new Util2(list[0], list[1], list[2], list[3], list[4], list[5]);
			}
			else
			{
				result = null;
			}
			return result;
		}

		public Util2(Instrument instrument)
		{
			Jihas kind = instrument.Kind;
			this.Obr = "OBR";
			this.Obri = 3;
			this.Obx = "OBX";
			this.Obxi = 3;
			this.Obxj = 5;
			this.Index = null;
		}

		public Util2(string obr, string s, string s1, string s2, string s3, string s4)
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

		public int Obxk = 1;
	}
}
