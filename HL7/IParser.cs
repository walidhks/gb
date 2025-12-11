using System;
using NHapi.Base.Model;

namespace GbService.HL7
{
	public interface IParser
	{
		void SetMessage(IMessage msg);

		string Parse();
	}
}
