using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace GbService.Model.Domain
{
	// Token: 0x02000032 RID: 50
	public class InstrumentKind
	{
		// Token: 0x17000017 RID: 23
		// (get) Token: 0x06000119 RID: 281 RVA: 0x0000D7BC File Offset: 0x0000B9BC
		// (set) Token: 0x0600011A RID: 282 RVA: 0x0000D7C4 File Offset: 0x0000B9C4
		[Key]
		[System.ComponentModel.DataAnnotations.Schema.DatabaseGenerated(System.ComponentModel.DataAnnotations.Schema.DatabaseGeneratedOption.Identity)]
		public int InstrumentKindId { get; set; }

		// Token: 0x17000018 RID: 24
		// (get) Token: 0x0600011B RID: 283 RVA: 0x0000D7CD File Offset: 0x0000B9CD
		// (set) Token: 0x0600011C RID: 284 RVA: 0x0000D7D5 File Offset: 0x0000B9D5
		public string InstrumentKindName { get; set; }

		// Token: 0x17000019 RID: 25
		// (get) Token: 0x0600011D RID: 285 RVA: 0x0000D7DE File Offset: 0x0000B9DE
		// (set) Token: 0x0600011E RID: 286 RVA: 0x0000D7E6 File Offset: 0x0000B9E6
		public virtual List<Instrument> Instruments { get; set; }

		// Token: 0x1700001A RID: 26
		// (get) Token: 0x0600011F RID: 287 RVA: 0x0000D7EF File Offset: 0x0000B9EF
		// (set) Token: 0x06000120 RID: 288 RVA: 0x0000D7F7 File Offset: 0x0000B9F7
		public virtual List<AnalysisTypeInstrumentMapping> AnalysisTypeInstrumentMappings { get; set; }

		// Token: 0x06000121 RID: 289 RVA: 0x0000D800 File Offset: 0x0000BA00
		public InstrumentKind()
		{
			this.Instruments = new List<Instrument>();
			this.AnalysisTypeInstrumentMappings = new List<AnalysisTypeInstrumentMapping>();
		}
	}
}
