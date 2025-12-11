using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using GbService.ASTM;
using GbService.Communication;

namespace GbService.Model.Domain
{
	public class Instrument
	{
		public Instrument()
		{
		}

		public Instrument(int instrumentCode, string port, int mode = 0, string instrumentBaudRate = null)
		{
			this.InstrumentId = instrumentCode;
			this.InstrumentCode = instrumentCode;
			this.L2 = (long)instrumentCode;
			this.InstrumentPortName = port;
			this.InstrumentBaudRate = instrumentBaudRate;
			this.Mode = mode;
			this.InstrumentParity = new int?(0);
		}

		[Key]
		[DatabaseGenerated(DatabaseGeneratedOption.Identity)]
		public int InstrumentId { get; set; }

		public string InstrumentName { get; set; }

		public string InstrumentDescription { get; set; }

		public string InstrumentBaudRate { get; set; }

		public string InstrumentPortName { get; set; }

		public int? InstrumentParity { get; set; }

		public string InstrumentDataBits { get; set; }

		public long LastAnalysisId { get; set; }

		public int InstrumentDays { get; set; }

		public string InstrumentStd { get; set; }

		public int InstrumentCode { get; set; }

		public bool B1 { get; set; }

		public string S1 { get; set; }

		public string S2 { get; set; }

		public string S3 { get; set; }

		public string UsrMac { get; set; }

		public long L1 { get; set; }

		public int Mode { get; set; }

		public long L2 { get; set; }

		public long L3 { get; set; }

		public int? InstrumentStopBits { get; set; }

		public bool B2 { get; set; }

		public bool B3 { get; set; }

		public Jihas Kind
		{
			get
			{
				return (Jihas)this.L2;
			}
		}

		public AstmProp Prop
		{
			get
			{
				AstmProp result;
				if ((result = Instrument._prop) == null)
				{
					result = (Instrument._prop = AstmProp.Create(this.Kind));
				}
				return result;
			}
		}

		public DateTime Now
		{
			get
			{
				return DateTime.Now.AddDays((double)this.InstrumentDays);
			}
		}

		private static AstmProp _prop;
	}
}
