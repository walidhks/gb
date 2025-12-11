using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace GbService.Model.Domain
{
	public class AnalysisTypeInstrumentMapping
	{
		public AnalysisTypeInstrumentMapping()
		{
		}

		public AnalysisTypeInstrumentMapping(int instrumentCode, string s)
		{
			this.InstrumentCode = instrumentCode;
			this.AnalysisTypeCode = s;
		}

		[Key]
		[DatabaseGenerated(DatabaseGeneratedOption.Identity)]
		public long MappingId { get; set; }

		public long AnalysisTypeId { get; set; }

		public int InstrumentCode { get; set; }

		public string AnalysisTypeCode { get; set; }

		public bool IsDefault { get; set; }

		public virtual AnalysisType AnalysisType { get; set; }
	}
}
