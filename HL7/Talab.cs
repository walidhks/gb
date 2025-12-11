using System;
using GbService.Communication;
using GbService.Model.Domain;

namespace GbService.HL7
{
	public class Talab
	{
		public Talab(Instrument instrument)
		{
			bool flag = instrument.Kind == Jihas.DiruiCsT180 || instrument.Kind == Jihas.ZybioExc200;
			if (flag)
			{
				this.Code = (this.Info = "QRD");
				this.Codei = 8;
				this.Infoi = 10;
			}
			else
			{
				this.Code = "ORC";
				this.Info = "MSH";
				this.Codei = 3;
				this.Infoi = 9;
			}
		}

		public string Code;

		public string Info;

		public int Codei;

		public int Infoi;
	}
}
