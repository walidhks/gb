using System;
using NHapi.Base.Model;
using NHapi.Base.Parser;
using NHapi.Model.V24.Datatype;
using NHapi.Model.V24.Message;

namespace GbService.HL7.V24
{
	internal class Ack_v24 : IAckMessage
	{
		internal Ack_v24()
		{
			this.message = new ACK();
		}

		public void SetMessage(IMessage msg)
		{
			bool flag = msg is ORU_R01;
			if (flag)
			{
				this.message = (ACK)HL7MessageHelper.MakeACK(msg, "AA", null, "2.4");
			}
		}

		public void SetError(IMessage msg)
		{
			this.message = (ACK)HL7MessageHelper.MakeACK(msg, "AE", "Error", "2.4");
			ELD errorCodeAndLocation = this.message.ERR.GetErrorCodeAndLocation(0);
			errorCodeAndLocation.CodeIdentifyingError.AlternateIdentifier.Value = "1";
			errorCodeAndLocation.CodeIdentifyingError.AlternateText.Value = "Something went wrong.";
		}

		public string GetAckMessage()
		{
			PipeParser pipeParser = new PipeParser();
			string text = pipeParser.Encode(this.message);
			string str = text.Split(new char[]
			{
				Convert.ToChar("\r")
			})[0] + "|||\r";
			string str2 = text.Split(new char[]
			{
				Convert.ToChar("\r")
			})[1] + "|\r";
			return str + str2;
		}

		internal string GetAckMessage(IMessage msg)
		{
			this.SetMessage(msg);
			return this.GetAckMessage();
		}

		private ACK message;
	}
}
