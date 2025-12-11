using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace GbService.ASTM
{
	public class ASTM_Message : ASTM_Record
	{
		public override List<ASTM_Record> comments()
		{
			return this.commentRecords.ToList<ASTM_Record>();
		}

		public override List<ASTM_Record> manufacturerInfo()
		{
			return this.manufacturerRecords.ToList<ASTM_Record>();
		}

		public override int fieldsCount()
		{
			return this.fieldNames.Count<string>();
		}

		public override string fieldName(int idx)
		{
			idx--;
			bool flag = idx < 0 || idx >= this.fieldNames.Count<string>();
			string result;
			if (flag)
			{
				result = null;
			}
			else
			{
				result = this.fieldNames.ElementAt(idx);
			}
			return result;
		}

		public override string fieldValue(int idx)
		{
			bool flag = idx < 1 || idx > this.fieldNames.Count<string>();
			string result;
			if (flag)
			{
				result = null;
			}
			else
			{
				switch (idx)
				{
				case 1:
					result = this.f_type;
					break;
				case 2:
					result = this.f_delimeter;
					break;
				case 3:
					result = this.f_message_id;
					break;
				case 4:
					result = this.f_password;
					break;
				case 5:
					result = this.f_sender;
					break;
				case 6:
					result = this.f_address;
					break;
				case 7:
					result = this.f_reserved;
					break;
				case 8:
					result = this.f_phone;
					break;
				case 9:
					result = this.f_caps;
					break;
				case 10:
					result = this.f_receiver;
					break;
				case 11:
					result = this.f_comments;
					break;
				case 12:
					result = this.f_processing_id;
					break;
				case 13:
					result = this.f_version;
					break;
				case 14:
					result = this.f_timestamp;
					break;
				default:
					result = null;
					break;
				}
			}
			return result;
		}

		public override bool addComment(string recordData)
		{
			this.commentRecords.Add(new ASTM_Comment());
			return this.commentRecords.Last<ASTM_Comment>().parseData(recordData);
		}

		public override bool addManufacturerInfo(string recordData)
		{
			this.manufacturerRecords.Add(new ASTM_Manufacturer());
			return this.manufacturerRecords.Last<ASTM_Manufacturer>().parseData(recordData);
		}

		public override bool parseData(string recordData)
		{
			string[] array = Regex.Split(recordData, "[|]");
			int num = 1;
			foreach (string text in array)
			{
				switch (num)
				{
				case 1:
					this.f_type = text;
					break;
				case 2:
					this.f_delimeter = text;
					break;
				case 3:
					this.f_message_id = text;
					break;
				case 4:
					this.f_password = text;
					break;
				case 5:
					this.f_sender = text;
					break;
				case 6:
					this.f_address = text;
					break;
				case 7:
					this.f_reserved = text;
					break;
				case 8:
					this.f_phone = text;
					break;
				case 9:
					this.f_caps = text;
					break;
				case 10:
					this.f_receiver = text;
					break;
				case 11:
					this.f_comments = text;
					break;
				case 12:
					this.f_processing_id = text;
					break;
				case 13:
					this.f_version = text;
					break;
				case 14:
					this.f_timestamp = text;
					break;
				default:
					return false;
				}
				num++;
			}
			return true;
		}

		public string EncodeMessage(int mode = 0, int patientFields = 0, int orderFields = 0)
		{
			StringBuilder stringBuilder = new StringBuilder();
			stringBuilder.Append(this.EncodeRecord(0, 0));
			foreach (ASTM_Patient astm_Patient in this.patientRecords)
			{
				stringBuilder.Append(astm_Patient.EncodeRecord(mode, patientFields));
				foreach (AstmOrder astmOrder in astm_Patient.OrderRecords)
				{
					stringBuilder.Append(astmOrder.EncodeRecord(mode, orderFields));
				}
			}
			stringBuilder.Append("L|1|N<CR>");
			return stringBuilder.ToString();
		}

		public string f_type;

		public string f_delimeter;

		public string f_message_id;

		public string f_password;

		public string f_sender;

		public string f_address;

		public string f_reserved;

		public string f_phone;

		public string f_caps;

		public string f_receiver;

		public string f_comments;

		public string f_processing_id;

		public string f_version;

		public string f_timestamp;

		public List<ASTM_Manufacturer> manufacturerRecords = new List<ASTM_Manufacturer>();

		public List<ASTM_Scientific> scientificRecords = new List<ASTM_Scientific>();

		public List<ASTM_Patient> patientRecords = new List<ASTM_Patient>();

		public List<ASTM_Comment> commentRecords = new List<ASTM_Comment>();

		public List<ASTM_Request> requestRecords = new List<ASTM_Request>();

		private List<string> fieldNames = new List<string>(new string[]
		{
			"Record Type ID",
			"Delimiter Definition",
			"Message Control ID",
			"Access Password",
			"Sender Name or ID",
			"Sender Street Address",
			"Reserved Field",
			"Sender Telephone Number",
			"Characteristics of Sender",
			"Receiver ID",
			"Comments",
			"Processing ID",
			"Version Number",
			"Date/Timeof Message"
		});
	}
}
