using System;
using GbService.ASTM;
using GbService.Communication;
using GbService.Model.Domain;
using NHapi.Base.Model;
using NHapi.Model.V24.Group;
using NHapi.Model.V24.Message;
using NHapi.Model.V24.Segment;

namespace GbService.HL7.V24
{
	internal class OruR01Handler_v24 : IParser
	{
		public OruR01Handler_v24(IMessage message, Instrument instrument)
		{
			this._message = (ORU_R01)message;
			this._instrument = instrument;
		}

		public void SetMessage(IMessage msg)
		{
			this._message = (ORU_R01)msg;
		}

		public string Parse()
		{
			ORU_R01_PATIENT_RESULT patient_RESULT = this._message.GetPATIENT_RESULT();
			int order_OBSERVATIONRepetitionsUsed = patient_RESULT.ORDER_OBSERVATIONRepetitionsUsed;
			for (int i = 0; i < order_OBSERVATIONRepetitionsUsed; i++)
			{
				ORU_R01_ORDER_OBSERVATION order_OBSERVATION = patient_RESULT.GetORDER_OBSERVATION(i);
				JihazResult jihazResult = new JihazResult(order_OBSERVATION.OBR.PlacerOrderNumber.EntityIdentifier.Value);
				int observationrepetitionsUsed = order_OBSERVATION.OBSERVATIONRepetitionsUsed;
				for (int j = 0; j < observationrepetitionsUsed; j++)
				{
					ORU_R01_OBSERVATION observation = order_OBSERVATION.GetOBSERVATION(j);
					OBX obx = (observation != null) ? observation.OBX : null;
					bool flag = ((obx != null) ? obx.GetObservationValue() : null) == null;
					if (!flag)
					{
						string variesValue = OruR01Handler_v24.GetVariesValue(obx.GetObservationValue(0));
						string value = obx.ObservationIdentifier.Identifier.Value;
						jihazResult.Results.Add(new LowResult(value, variesValue, null, null, null));
						AstmHigh.LoadResults(jihazResult, this._instrument, null);
					}
				}
			}
			return this.CreateAckMessage();
		}

		private string CreateAckMessage()
		{
			Ack_v24 ack_v = new Ack_v24();
			return ack_v.GetAckMessage(this._message);
		}

		protected static string GetVariesValue(Varies value)
		{
			string text;
			if (value == null)
			{
				text = null;
			}
			else
			{
				IType data = value.Data;
				text = ((data != null) ? data.ToString() : null);
			}
			return text ?? "";
		}

		private ORU_R01 _message;

		private Instrument _instrument;
	}
}
