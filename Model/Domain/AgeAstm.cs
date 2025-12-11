using System;

namespace GbService.Model.Domain
{
	public class AgeAstm
	{
		public AgeEnum Type { get; set; }

		public int Value { get; set; }

		public AgeAstm(int v, AgeEnum t)
		{
			this.Type = t;
			this.Value = v;
		}
	}
}
