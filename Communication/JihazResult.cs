using System;
using System.Collections.Generic;

namespace GbService.Communication
{
	public class JihazResult
	{
		public JihazResult(string scode)
		{
			this.Scode = ((scode != null) ? scode.Trim() : null);
			this.Results = new List<LowResult>();
		}

		public string Scode { get; set; }

		public List<LowResult> Results { get; set; }
	}
}
