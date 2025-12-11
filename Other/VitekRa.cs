using System;
using System.Collections.Generic;
using System.Linq;

namespace GbService.Other
{
	internal class VitekRa
	{
		public VitekRa(string s)
		{
			List<string> source = s.Split(new string[]
			{
				"|"
			}, StringSplitOptions.RemoveEmptyEntries).ToList<string>();
			List<string[]> records = (from x in source
			select new string[]
			{
				x.Substring(0, 2),
				x.Substring(2)
			}).ToList<string[]>();
			this.Code = VidasHandler.GetValueById(records, "a1", null);
			this.Name = VidasHandler.GetValueById(records, "a2", null);
			this.ExactValue = VidasHandler.GetValueById(records, "a3", null);
			string valueById = VidasHandler.GetValueById(records, "a4", null);
			this.Value = ((valueById == "S") ? "Sensible" : ((valueById == "R") ? "Résistant" : ((valueById == "I") ? "Intermédiaire" : valueById)));
			this.V3 = VidasHandler.GetValueById(records, "an", null);
		}

		public string Code;

		public string Name;

		public string ExactValue;

		public string Value;

		public string V3;
	}
}
