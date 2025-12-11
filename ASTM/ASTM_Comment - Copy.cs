using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

namespace GbService.ASTM
{
	public class ASTM_Comment : ASTM_Record
	{
		public override List<ASTM_Record> comments()
		{
			return null;
		}

		public override List<ASTM_Record> manufacturerInfo()
		{
			return null;
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
					result = this.f_source;
					break;
				case 4:
					result = this.f_data;
					break;
				case 5:
					result = this.f_ctype;
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
			return false;
		}

		public override bool addManufacturerInfo(string recordData)
		{
			return false;
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
					this.f_source = text;
					break;
				case 4:
					this.f_data = text;
					break;
				case 5:
					this.f_ctype = text;
					break;
				default:
					return false;
				}
				num++;
			}
			return true;
		}

		private string f_type;

		private string f_seq;

		public string f_source;

		public string f_data;

		public string f_ctype;

		public List<ASTM_Manufacturer> manufacturerRecords = new List<ASTM_Manufacturer>();

		private List<string> fieldNames = new List<string>(new string[]
		{
			"Record Type ID",
			"Sequence Number",
			"Comment Source",
			"Comment Text",
			"Comment Type"
		});
	}
}
