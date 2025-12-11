using System;
using NHapi.Base.Model;

namespace GbService.HL7
{
	internal interface IAckMessage
	{
		void SetMessage(IMessage msg);

		void SetError(IMessage msg);

		string GetAckMessage();
	}
}
