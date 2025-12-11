using System;

namespace GbService.ASTM
{
	internal class Sid
	{
		public Sid(string code, string info)
		{
			this.Code = code;
			this.Info = info;
		}

		public string Info { get; set; }

		public string Code { get; set; }
	}
}
