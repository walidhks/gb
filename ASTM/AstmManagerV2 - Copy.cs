using System;
using GbService.Communication.Common;
using GbService.Model.Domain;

namespace GbService.ASTM
{
	public class AstmManagerV2 : AstmManager
	{
		public AstmManagerV2(ILowManager il, Instrument instrument) : base(il, instrument)
		{
		}

		protected override void HandleMessage(string m)
		{
			this.HandleText(m);
		}

		protected override void HandleText(string m)
		{
			AstmHigh.Parse(this, m);
		}
	}
}
