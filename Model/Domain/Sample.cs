using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;

namespace GbService.Model.Domain
{
	public class Sample
	{
		public long SampleId { get; set; }

		public long? SampleCode { get; set; }

        public string FormattedSampleCode
        {
            get
            {
                if (this.SampleCode.HasValue)
                {
                    return this.SampleCode.Value.ToString("D" + ParamDictHelper.NumberPositionBarcode);
                }
                return null;
            }
        }

        [Index("IX_AnalysisRequestIdSampleId", 0)]
		public long? AnalysisRequestId { get; set; }

		[Column(TypeName = "DateTime2")]
		public DateTime DateCreated { get; set; }

		[Column(TypeName = "DateTime2")]
		public DateTime? DateReceived { get; set; }

		public long SampleSourceId { get; set; }

		public long? TubeTypeId { get; set; }

		[ForeignKey("TubeTypeId")]
		public virtual TubeType TubeType { get; set; }

		public virtual SampleSource SampleSource { get; set; }

		public virtual AnalysisRequest AnalysisRequest { get; set; }

		public virtual List<Analysis> Analysis { get; set; }

		public Sample()
		{
			this.DateCreated = DateTime.Now;
			this.Analysis = new List<Analysis>();
		}

		public string InstrumentSampleId;
	}
}
