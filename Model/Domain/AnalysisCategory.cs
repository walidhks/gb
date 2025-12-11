using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace GbService.Model.Domain
{
	// Token: 0x02000035 RID: 53
	public class AnalysisCategory
	{
		// Token: 0x1700002F RID: 47
		// (get) Token: 0x0600014A RID: 330 RVA: 0x0000D9D5 File Offset: 0x0000BBD5
		// (set) Token: 0x0600014B RID: 331 RVA: 0x0000D9DD File Offset: 0x0000BBDD
		[Key]
		[System.ComponentModel.DataAnnotations.Schema.DatabaseGenerated(System.ComponentModel.DataAnnotations.Schema.DatabaseGeneratedOption.Identity)]
		public long AnalysisCategoryId { get; set; }

		// Token: 0x17000030 RID: 48
		// (get) Token: 0x0600014C RID: 332 RVA: 0x0000D9E6 File Offset: 0x0000BBE6
		// (set) Token: 0x0600014D RID: 333 RVA: 0x0000D9EE File Offset: 0x0000BBEE
		public string AnalysisCategoryName { get; set; }

		// Token: 0x0600014E RID: 334 RVA: 0x0000D9F8 File Offset: 0x0000BBF8
		public override string ToString()
		{
			return this.AnalysisCategoryName;
		}
	}
}
