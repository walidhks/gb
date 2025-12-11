using System;
using System.Collections.Generic;
using GbService.HL7.V231;
using GbService.Model.Domain;
using NHapi.Base;
using NHapi.Base.Model;
using NHapi.Base.Parser;
using NHapi.Model.V231.Message;
using NLog;

namespace GbService.HL7
{
	public class MessageHandler
	{
		public string MessageType { get; set; }

		public MessageHandler(string message, Instrument instrument)
		{
			this._instrument = instrument;
			PipeParser pipeParser = new PipeParser();
			this.Hl7Message = pipeParser.Parse(message);
			this.MessageType = this.Hl7Message.Message.GetStructureName();
		}

		public string ParseMessage()
		{
			try
			{
				this.MessageParser = HL7VersionFactory.GetParserOnHl7Message(this.Hl7Message, this._instrument);
				this.result = this.MessageParser.Parse();
			}
			catch (HL7Exception ex)
			{
				MessageHandler._logger.Error<HL7Exception>(ex);
				Ack ack = new Ack();
				ack.SetError(this.Hl7Message);
				return ack.GetAckMessage();
			}
			return this.result;
		}

		public string GetDsrMessage(long sampleCode, string messageControlId, string dsc = null, List<AnalysisType> analysisTypes = null)
		{
			DsrQ03Handler dsrQ03Handler = new DsrQ03Handler((QRY_Q02)this.Hl7Message, this._instrument);
			return dsrQ03Handler.GetMessage(sampleCode, messageControlId, this._instrument, dsc, analysisTypes);
		}

		public IMessage Hl7Message;

		public IParser MessageParser;

		private string result;

		private Instrument _instrument;

		private static Logger _logger = LogManager.GetCurrentClassLogger();
	}
}
