using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

namespace GbService.ASTM
{
	public class ASTM_Request : ASTM_Record
	{
		public ASTM_Request()
		{
			this.f_type = "Q";
		}

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
					result = this.f_seq;
					break;
				case 3:
					result = this.f_srangeid;
					break;
				case 4:
					result = this.f_erangeid;
					break;
				case 5:
					result = this.f_utestid;
					break;
				case 6:
					result = this.f_noreqtmlim;
					break;
				case 7:
					result = this.f_begreqresdt;
					break;
				case 8:
					result = this.f_endreqresdt;
					break;
				case 9:
					result = this.f_reqphysname;
					break;
				case 10:
					result = this.f_reqphystel;
					break;
				case 11:
					result = this.f_userfld1;
					break;
				case 12:
					result = this.f_userfld2;
					break;
				case 13:
					result = this.f_statcodes;
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
					this.f_seq = text;
					break;
				case 3:
					this.f_srangeid = text;
					break;
				case 4:
					this.f_erangeid = text;
					break;
				case 5:
					this.f_utestid = text;
					break;
				case 6:
					this.f_noreqtmlim = text;
					break;
				case 7:
					this.f_begreqresdt = text;
					break;
				case 8:
					this.f_endreqresdt = text;
					break;
				case 9:
					this.f_reqphysname = text;
					break;
				case 10:
					this.f_reqphystel = text;
					break;
				case 11:
					this.f_userfld1 = text;
					break;
				case 12:
					this.f_userfld2 = text;
					break;
				case 13:
					this.f_statcodes = text;
					break;
				default:
					return false;
				}
				num++;
			}
			return true;
		}

		public string encode()
		{
			string str = this.fieldValue(1);
			for (int i = 2; i <= this.fieldsCount(); i++)
			{
				str = str + "|" + this.fieldValue(i);
			}
			return str + "<CR>";
		}

		public string f_type;

		public string f_seq;

		public string f_srangeid;

		public string f_erangeid;

		public string f_utestid;

		public string f_noreqtmlim;

		public string f_begreqresdt;

		public string f_endreqresdt;

		public string f_reqphysname;

		public string f_reqphystel;

		public string f_userfld1;

		public string f_userfld2;

		public string f_statcodes;

		public List<ASTM_Manufacturer> manufacturerRecords = new List<ASTM_Manufacturer>();

		public List<ASTM_Comment> commentRecords = new List<ASTM_Comment>();

		private List<string> fieldNames = new List<string>(new string[]
		{
			"Record Type ID",
			"Sequence Number",
			"Starting Range ID Number",
			"Ending Range ID Number",
			"Universal Test ID",
			"Nature of Request Time Limits",
			"Beginning Request Results Date and Time",
			"Ending Request Results Date and Time",
			"Requesting Physician Name",
			"Requesting Physician Telephone Number",
			"User Field No. 1",
			"User Field No. 2",
			"Request Information Status Codes"
		});
	}
}
