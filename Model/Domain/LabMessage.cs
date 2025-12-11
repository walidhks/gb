using System;

namespace GbService.Model.Domain
{
	public class LabMessage
	{
		public LabMessage()
		{
		}

		public LabMessage(string msg, long sid, int instrumentId)
		{
			this.LabMessageValue = msg;
			this.SampleId = sid;
			this.LabMessageDate = DateTime.Now;
			this.InstrumentId = new int?(instrumentId);
		}

		public long LabMessageID { get; set; }

		public string LabMessageValue { get; set; }

		public int LabMessageRetry { get; set; }

		public DateTime LabMessageDate { get; set; }

		public long SampleId { get; set; }

		public int? InstrumentId { get; set; }

		public LabMessageState LabMessageStatus { get; set; }

		public Instrument Instrument { get; set; }
	}
}
