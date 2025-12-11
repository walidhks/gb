using System;
using GbService.Model.Domain;
using NHapi.Base.Model;
using NHapi.Model.V23.Group;
using NHapi.Model.V23.Message;
using NHapi.Model.V23.Segment;
using NLog;

namespace GbService.HL7.V23
{
	internal class OruR01Handler_v23 : IParser
	{
		public OruR01Handler_v23(IMessage message, Instrument instrument)
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
			ORU_R01_RESPONSE response = this._message.GetRESPONSE();
			int order_OBSERVATIONRepetitionsUsed = response.ORDER_OBSERVATIONRepetitionsUsed;
			for (int i = 0; i < order_OBSERVATIONRepetitionsUsed; i++)
			{
				ORU_R01_ORDER_OBSERVATION order_OBSERVATION = response.GetORDER_OBSERVATION(i);
				int observationrepetitionsUsed = order_OBSERVATION.OBSERVATIONRepetitionsUsed;
				for (int j = 0; j < observationrepetitionsUsed; j++)
				{
					ORU_R01_OBSERVATION observation = order_OBSERVATION.GetOBSERVATION(j);
					OBX obx = (observation != null) ? observation.OBX : null;
					bool flag = ((obx != null) ? obx.GetObservationValue() : null) == null;
					if (!flag)
					{
						string variesValue = OruR01Handler_v23.GetVariesValue(obx.GetObservationValue(0));
						string value = obx.ObservationIdentifier.Identifier.Value;
					}
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

		private ORU_R01 _message;

		private Instrument _instrument;

		private static Logger _logger = LogManager.GetCurrentClassLogger();
	}
}
