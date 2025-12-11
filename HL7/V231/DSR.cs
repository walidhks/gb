using System;
using NHapi.Base.Model;
using NHapi.Model.V231.Message;
using NHapi.Model.V231.Segment;

namespace GbService.HL7.V231
{
	internal class DSR : IParser
	{
		public void SetMessage(IMessage msg)
		{
			this.message = (DSR_Q03)msg;
		}

		public string Parse()
		{
			int dsprepetitionsUsed = this.message.DSPRepetitionsUsed;
			for (int i = 0; i < dsprepetitionsUsed; i++)
			{
				DSP dsp = this.message.GetDSP(i);
			}
			Ack ack = new Ack();
			return ack.GetAckMessage(this.message);
		}

		private DSR_Q03 message;
	}
}
