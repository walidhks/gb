using System;

namespace GbService.Communication
{
	public class Mapping
	{
		public Mapping(string code, int i, int j, int? k, int? element)
		{
			this.Code = code;
			this.Order = i;
			this.P1 = j;
			this.P2 = k;
			this.P3 = element;
		}

		public int? P3 { get; set; }

		public string Code { get; set; }

		public int Order { get; set; }

		public int P1 { get; set; }

		public int? P2 { get; set; }

		public long AnalysisTypeId { get; set; }
	}
}
