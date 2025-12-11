using System;
using GbService.HL7.V23;
using GbService.HL7.V231;
using GbService.HL7.V24;
using GbService.HL7.V25;
using GbService.Model.Domain;
using NHapi.Base.Model;

namespace GbService.HL7
{
	internal abstract class HL7VersionFactory
	{
		public static IParser GetParserOnHl7Message(IMessage message, Instrument instrument)
		{
			string version = message.Version;
			string a = version;
			IParser result;
			if (!(a == "2.3"))
			{
				if (!(a == "2.3.1"))
				{
					if (!(a == "2.4"))
					{
						if (!(a == "2.5"))
						{
							result = HL7VersionFactory.GetEventMessageParserV231(message, instrument);
						}
						else
						{
							result = HL7VersionFactory.GetEventMessageParserV25(message, instrument);
						}
					}
					else
					{
						result = HL7VersionFactory.GetEventMessageParserV24(message, instrument);
					}
				}
				else
				{
					result = HL7VersionFactory.GetEventMessageParserV231(message, instrument);
				}
			}
			else
			{
				result = HL7VersionFactory.GetEventMessageParserV23(message, instrument);
			}
			return result;
		}

		private static IParser GetEventMessageParserV231(IMessage message, Instrument instrument)
		{
			string structureName = message.Message.GetStructureName();
			string text = structureName;
			string a = text;
			IParser result;
			if (!(a == "ADT_A01"))
			{
				if (!(a == "ORU_R01"))
				{
					if (!(a == "QRY_Q02"))
					{
						if (!(a == "DSR_Q03"))
						{
							throw new NotImplementedException("HL7 event message is not supported.");
						}
						result = new DSR();
					}
					else
					{
						result = new QryQ02Handler(message);
					}
				}
				else
				{
					result = new OruR01Handler(message, instrument);
				}
			}
			else
			{
				result = new A01();
			}
			return result;
		}

		private static IParser GetEventMessageParserV24(IMessage message, Instrument instrument)
		{
			string structureName = message.Message.GetStructureName();
			string text = structureName;
			string a = text;
			if (!(a == "ORU_R01"))
			{
				throw new NotImplementedException("HL7 event message is not supported.");
			}
			return new OruR01Handler_v24(message, instrument);
		}

		private static IParser GetEventMessageParserV25(IMessage message, Instrument instrument)
		{
			string structureName = message.Message.GetStructureName();
			string text = structureName;
			string a = text;
			if (!(a == "ORU_R01"))
			{
				throw new NotImplementedException("HL7 event message is not supported.");
			}
			return new OruR01Handler_v25(message, instrument);
		}

		private static IParser GetEventMessageParserV23(IMessage message, Instrument instrument)
		{
			string structureName = message.Message.GetStructureName();
			string text = structureName;
			string a = text;
			if (!(a == "ORU_R01"))
			{
				throw new NotImplementedException("HL7 event message is not supported.");
			}
			return new OruR01Handler_v23(message, instrument);
		}
	}
}
