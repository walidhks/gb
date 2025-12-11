using System;
using System.Collections.Generic;
using GbService.Communication.Serial;

namespace GbService.Communication.Common
{
	public interface ILowManager
	{
		event EventHandler<MessageReceivedEventArgs> MessageReceived;

		void HandleMessage(string m);

		bool SendLow(string msg, Coding enc = Coding.Asc);

		void SendLow(byte b);

		void Close();

		void SendLow(List<char> msg);
	}
}
