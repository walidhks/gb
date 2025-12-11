using System;

namespace GbService.Communication
{
	public class GraphMap
	{
		public GraphMap(int i, int id, int order, string code)
		{
			this.InstrumentKindId = i;
			this.AnalysisTypeId = (long)id;
			this.Order = order;
			this.AnalysisTypeCode = code;
		}

		public int InstrumentKindId { get; set; }

		public long AnalysisTypeId { get; set; }

		public int Order { get; set; }

		public string AnalysisTypeCode { get; set; }
	}
}
