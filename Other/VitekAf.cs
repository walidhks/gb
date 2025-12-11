using System;
using System.Collections.Generic;
using System.Linq;

namespace GbService.Other
{
	internal class VitekAf
	{
		public VitekAf(string s)
		{
			List<string> list = s.Split(new string[]
			{
				"|ap"
			}, StringSplitOptions.RemoveEmptyEntries).ToList<string>();
			this.Code = list[0];
			list.Remove(this.Code);
			this.Values = list;
		}

		public string Code;

		public List<string> Values;
	}
}
