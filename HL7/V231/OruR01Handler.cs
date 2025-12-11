using System;
using GbService.ASTM;
using GbService.Communication;
using GbService.Model.Domain;
using NHapi.Base.Model;
using NHapi.Model.V231.Group;
using NHapi.Model.V231.Message;
using NHapi.Model.V231.Segment;
using NLog;

namespace GbService.HL7.V231
{
	internal class OruR01Handler : IParser
	{
		public OruR01Handler(IMessage message, Instrument instrument)
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
			Jihas kind = this._instrument.Kind;
			OruR01Handler._logger.Info(kind.ToString() + ", mode = " + this._instrument.Mode.ToString());
			ORU_R01_PATIENT_RESULT patient_RESULT = this._message.GetPATIENT_RESULT();
			int order_OBSERVATIONRepetitionsUsed = patient_RESULT.ORDER_OBSERVATIONRepetitionsUsed;
			for (int i = 0; i < order_OBSERVATIONRepetitionsUsed; i++)
			{
				ORU_R01_ORDER_OBSERVATION order_OBSERVATION = patient_RESULT.GetORDER_OBSERVATION(i);
				bool flag = this._instrument.Kind == Jihas.BC5150 || this._instrument.Kind == Jihas.BC5380 || this._instrument.Kind == Jihas.MindrayH50P || this._instrument.Kind == Jihas.Chemray240;
				string value;
				if (flag)
				{
					value = order_OBSERVATION.OBR.FillerOrderNumber.EntityIdentifier.Value;
				}
				else
				{
					bool flag2 = this._instrument.Kind == Jihas.Medonic;
					if (flag2)
					{
						value = order_OBSERVATION.OBR.UniversalServiceID.Identifier.Value;
					}
					else
					{
						bool flag3 = this._instrument.Kind == Jihas.Kt6610;
						if (flag3)
						{
							value = patient_RESULT.PATIENT.PID.GetPatientIdentifierList()[0].ID.Value;
						}
						else
						{
							value = order_OBSERVATION.OBR.PlacerOrderNumber.EntityIdentifier.Value;
						}
					}
				}
				OruR01Handler._logger.Debug("sid=" + value);
				JihazResult jihazResult = new JihazResult(value);
				int observationrepetitionsUsed = order_OBSERVATION.OBSERVATIONRepetitionsUsed;
				for (int j = 0; j < observationrepetitionsUsed; j++)
				{
					ORU_R01_OBSERVATION observation = order_OBSERVATION.GetOBSERVATION(j);
					OBX obx = (observation != null) ? observation.OBX : null;
					bool flag4 = ((obx != null) ? obx.GetObservationValue() : null) == null;
					if (flag4)
					{
						OruR01Handler._logger.Warn("obx.GetObservationValue is null");
					}
					else
					{
						string text = (kind == Jihas.Bs300) ? obx.UserDefinedAccessChecks.Value : OruR01Handler.GetVariesValue(obx.GetObservationValue(0));
						string value2 = obx.ObservationIdentifier.Identifier.Value;
						bool flag5 = kind == Jihas.Kt6610 || ((kind == Jihas.BC5380 || kind == Jihas.BC5150) && this._instrument.Mode == 1);
						if (flag5)
						{
							value2 = obx.ObservationIdentifier.Text.Value;
						}
						bool flag6 = text.StartsWith("-268435455");
						if (!flag6)
						{
							jihazResult.Results.Add(new LowResult(value2, text, null, null, null));
						}
					}
				}
				AstmHigh.LoadResults(jihazResult, this._instrument, null);
			}
			return this.CreateAckMessage();
		}

		private string CreateAckMessage()
		{
			Ack ack = new Ack();
			return ack.GetAckMessage(this._message);
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
