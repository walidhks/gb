using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using GbService.Model.Common;

namespace GbService.Model.Domain
{
	public class Analysis : EntityBase
	{
		[Key]
		[DatabaseGenerated(DatabaseGeneratedOption.Identity)]
		[Index("IX_ParentAnalysisId", 1, IsClustered = false)]
		[Index("IX_SampleIdAnalysisId", 1, IsClustered = false)]
		public long AnalysisId { get; set; }

		public long? ParentAnalysisId { get; set; }

		public long? AnalysisRequestId { get; set; }

		public virtual AnalysisRequest AnalysisRequest { get; set; }

		public long? SampleId { get; set; }

		public int? InstrumentId { get; set; }

		public long AnalysisTypeId { get; set; }

		public string ResultTxt
		{
			get
			{
				return this._resultTxt;
			}
			set
			{
				base.Set<string>(ref this._resultTxt, value);
				bool flag = string.IsNullOrEmpty(value);
				if (flag)
				{
					this.AnalysisResultDate = new DateTime?(DateTime.Now);
				}
			}
		}

		public AnalysisState AnalysisState { get; set; }

		public int? AnalysisStatusId { get; set; }

		public DateTime AnalysisStateChangeDate { get; set; }

		public string Flag { get; set; }

		public virtual Sample Sample { get; set; }

		public virtual Analysis Parent { get; set; }

		public virtual AnalysisType AnalysisType { get; set; }

		public DateTime? AnalysisResultDate { get; set; }

		public DateTime CreatedDate { get; set; }

		public virtual Instrument Instrument { get; set; }

		public virtual List<Analysis> ChildAnalysises { get; set; }

		public Analysis()
		{
			this.ChildAnalysises = new List<Analysis>();
			this.AnalysisState = AnalysisState.EnCours;
			this.AnalysisStateChangeDate = DateTime.Now;
		}

		private string _resultTxt;
	}
}
