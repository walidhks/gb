using System;
using NHapi.Base.Model;
using NHapi.Model.V23.Datatype;
using NHapi.Model.V23.Message;
using NHapi.Model.V23.Segment;

namespace GbService.HL7.V23
{
	internal class A01_v23 : IParser
	{
		public A01_v23(IMessage message, int instrumentId)
		{
			this._message = (ADT_A01)message;
			this._instrumentId = instrumentId;
		}

		public void SetMessage(IMessage msg)
		{
			this._message = (ADT_A01)msg;
		}

		public string Parse()
		{
			PID pid = this._message.PID;
			CX patientIDExternalID = pid.PatientIDExternalID;
			int obxrepetitionsUsed = this._message.OBXRepetitionsUsed;
			for (int i = 0; i < obxrepetitionsUsed; i++)
			{
				OBX obx = this._message.GetOBX(i);
				bool flag = ((obx != null) ? obx.GetObservationValue() : null) == null;
				if (!flag)
				{
					string variesValue = A01_v23.GetVariesValue(obx.GetObservationValue(0));
					string value = obx.ObservationIdentifier.Identifier.Value;
				}
			}
			return this.CreateAckMessage();
		}

		private string CreateAckMessage()
		{
			Ack_v23 ack_v = new Ack_v23();
			return ack_v.GetAckMessage(this._message);
		}

		protected static string GetVariesValue(Varies value)
		{
			string result = string.Empty;
			IType type = (value != null) ? value.Data : null;
			bool flag = type != null;
			if (flag)
			{
				result = type.ToString();
			}
			return result;
		}

		private ADT_A01 _message;

		private int _instrumentId;
	}
}
