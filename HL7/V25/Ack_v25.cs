using System;
using System.Threading;
using NHapi.Base.Model;
using NHapi.Base.Parser;
using NHapi.Model.V25.Datatype;
using NHapi.Model.V25.Message;

namespace GbService.HL7.V25
{
	internal class Ack_v25 : IAckMessage
	{
		internal Ack_v25()
		{
			this.message = new ACK();
		}

		public void SetMessage(IMessage msg)
		{
			bool flag = msg is ORU_R01;
			if (flag)
			{
				this.message = (ACK)HL7MessageHelper.MakeACK(msg, "AA", null, "2.5");
			}
		}

		public void SetError(IMessage msg)
		{
			this.message = (ACK)HL7MessageHelper.MakeACK(msg, "AE", "Error", "2.5");
			ELD errorCodeAndLocation = this.message.GetERR().GetErrorCodeAndLocation(0);
			errorCodeAndLocation.CodeIdentifyingError.AlternateIdentifier.Value = "1";
			errorCodeAndLocation.CodeIdentifyingError.AlternateText.Value = "Something went wrong.";
		}

		public string GetAckMessage()
		{
			PipeParser pipeParser = new PipeParser();
			string text = pipeParser.Encode(this.message);
			string[] array = text.Split(new char[]
			{
				'\r'
			});
			string str = array[0] + "\r";
			string str2 = array[1] + "\r";
			return str + str2;
		}

		public string GetAckMessage(string date = null)
		{
			Thread.Sleep(500);
			ST messageControlID = this.message.MSA.MessageControlID;
			string text = string.Format("MSH|$~\\&|SIEMENS_EU$ADVIA560||||20180211203725||ORU$R01|56|P|2.5|24162<CR>MSA|AA|{0}<CR>", messageControlID);
			PipeParser pipeParser = new PipeParser();
			string text2 = pipeParser.Encode(this.message);
			string str = text2.Split(new char[]
			{
				Convert.ToChar("\r")
			})[0] + "|||\r";
			string str2 = text2.Split(new char[]
			{
				Convert.ToChar("\r")
			})[1] + "|\r";
			return str + str2;
		}

		internal string GetAckMessage(IMessage msg)
		{
			this.SetMessage(msg);
			string value = ((ORU_R01)msg).MSH.DateTimeOfMessage.Time.Value;
			return this.GetAckMessage(value);
		}

		private ACK message;
	}
}
