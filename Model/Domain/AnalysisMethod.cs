using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace GbService.Model.Domain
{
	// Token: 0x02000036 RID: 54
	public class AnalysisMethod
	{
		// Token: 0x17000031 RID: 49
		// (get) Token: 0x06000150 RID: 336 RVA: 0x0000DA19 File Offset: 0x0000BC19
		// (set) Token: 0x06000151 RID: 337 RVA: 0x0000DA21 File Offset: 0x0000BC21
		[Key]
		[System.ComponentModel.DataAnnotations.Schema.DatabaseGenerated(System.ComponentModel.DataAnnotations.Schema.DatabaseGeneratedOption.Identity)]
		public long AnalysisMethodId { get; set; }

		// Token: 0x17000032 RID: 50
		// (get) Token: 0x06000152 RID: 338 RVA: 0x0000DA2A File Offset: 0x0000BC2A
		// (set) Token: 0x06000153 RID: 339 RVA: 0x0000DA32 File Offset: 0x0000BC32
		public string AnalysisMethodName { get; set; }
	}
}
