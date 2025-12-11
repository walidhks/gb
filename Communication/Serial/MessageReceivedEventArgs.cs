using System;

namespace GbService.Communication.Serial
{
	public class MessageReceivedEventArgs : EventArgs
	{
		public string Message { get; set; }
	}
}
