using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace GbService.Model.Domain
{
	public class AnalysisType
	{
		[Key]
		[DatabaseGenerated(DatabaseGeneratedOption.Identity)]
		public long AnalysisTypeId { get; set; }

		public int? InstrumentId { get; set; }

		public bool NotifyIfOutOfRange { get; set; }

		[Required(ErrorMessage = "Champ obligatoire", AllowEmptyStrings = false)]
		public string AnalysisTypeName { get; set; }

		public string AnalysisTypeShortName { get; set; }

		public long? AnalysisCategoryId { get; set; }

		public long? SampleSourceId { get; set; }

		public bool RespectDefaultInstrument { get; set; }

		public long? AnalysisMethodId { get; set; }

		public int DecimalPlacesNumber { get; set; }

		public virtual SampleSource SampleSource { get; set; }

		public virtual ICollection<Analysis> Analysis { get; set; }

		public virtual List<PanelRelationship> ParentPanelRelationships { get; set; }

		public virtual List<PanelRelationship> ChildPanelRelationships { get; set; }

		public virtual List<AnalysisTypeInstrumentMapping> AnalysisTypeInstrumentMappings { get; set; }

		public virtual Instrument Instrument { get; set; }

		public TimeSpan WaitingTime { get; set; }

		public int SamplingHoure { get; set; }

		public override string ToString()
		{
			return this.AnalysisTypeName;
		}

		public AnalysisType()
		{
			this.Analysis = new List<Analysis>();
			this.AnalysisTypeInstrumentMappings = new List<AnalysisTypeInstrumentMapping>();
			this.ChildPanelRelationships = new List<PanelRelationship>();
			this.ParentPanelRelationships = new List<PanelRelationship>();
			this.DecimalPlacesNumber = 2;
			this.NotifyIfOutOfRange = true;
		}
	}
}
