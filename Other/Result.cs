using System;

namespace GbService.Other
{
	public class Result
	{
		public Result(string code, string value, string flag)
		{
			this.Code = code.Trim().PadLeft(3, '0');
			this.Value = value;
			this.Flag = flag;
		}

		public override string ToString()
		{
			return string.Concat(new string[]
			{
				this.Code,
				" ",
				this.Value,
				" ",
				this.Flag
			});
		}

		public string Code;

		public string Value;

		public string Flag;
	}
}
