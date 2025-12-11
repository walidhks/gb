using System;

namespace GbService.Communication
{
	public class LowResult
	{
		public LowResult(string code, string value, string unit = null, string flag = null, long? order = null)
		{
			this.Code = code;
			this.Value = value;
			this.Flag = flag;
			this.Unit = unit;
			this.Order = order;
		}

		public string Code { get; set; }

		public string Value { get; set; }

		public string Unit { get; set; }

		public string Flag { get; set; }

		public long? Order { get; set; }
	}
}
