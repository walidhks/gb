using System;
using System.Collections.Generic;
using GbService.Model.Domain;

namespace GbService.Communication
{
	public class Inf
	{
		public Inf(int i, int j, int k = 0)
		{
			this.I = i;
			this.J = j;
			this.K = k;
		}

		public static Inf Get(Instrument instrument)
		{
			List<int> list = TextUtil.SplitInt(instrument.S3);
			Jihas kind = instrument.Kind;
			bool flag = list != null;
			Inf result;
			if (flag)
			{
				result = new Inf(list[0], list[1], list[2]);
			}
			else
			{
				result = ((kind == Jihas.DiagonDcell_BC3000) ? new Inf(12, 10, 35) : ((kind == Jihas.GenusKT6400) ? new Inf(114, 15, 25) : null));
			}
			return result;
		}

		public int I;

		public int J;

		public int K;
	}
}
