using System;
using System.ComponentModel.DataAnnotations.Schema;

namespace GbService.Model.Domain
{
	public class PanelRelationship
	{
		[Index("IX_ParentChildAnalysisTypeId", 2, IsClustered = true)]
		public long PanelRelationshipId { get; set; }

		[Index("IX_ParentChildAnalysisTypeId", 1, IsClustered = true)]
		public long ChildAnalysisTypeId { get; set; }

		[Index("IX_ParentChildAnalysisTypeId", 0, IsClustered = true)]
		public long ParentAnalysisTypeId { get; set; }

		public long SortOrder { get; set; }

		[ForeignKey("ChildAnalysisTypeId")]
		public virtual AnalysisType ChildAnalysisType { get; set; }

		[ForeignKey("ParentAnalysisTypeId")]
		public virtual AnalysisType ParentAnalysisType { get; set; }
	}
}
