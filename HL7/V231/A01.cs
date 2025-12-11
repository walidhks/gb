using System;
using NHapi.Base.Model;
using NHapi.Model.V231.Message;

namespace GbService.HL7.V231
{
	internal class A01 : IParser
	{
		public void SetMessage(IMessage msg)
		{
			this.message = (ADT_A01)msg;
		}

		public string Parse()
		{
			Ack ack = new Ack();
			return ack.GetAckMessage(this.message);
		}

		private ADT_A01 message;
	}
}
