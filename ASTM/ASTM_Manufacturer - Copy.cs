using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

namespace GbService.ASTM
{
	public class ASTM_Manufacturer : ASTM_Record
	{
		public override List<ASTM_Record> comments()
		{
			return this.commentRecords.ToList<ASTM_Record>();
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
					result = this.f_mf1;
					break;
				case 4:
					result = this.f_mf2;
					break;
				case 5:
					result = this.f_mf3;
					break;
				case 6:
					result = this.f_mf4;
					break;
				case 7:
					result = this.f_mf5;
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
					this.f_mf1 = text;
					break;
				case 4:
					this.f_mf2 = text;
					break;
				case 5:
					this.f_mf3 = text;
					break;
				case 6:
					this.f_mf4 = text;
					break;
				case 7:
					this.f_mf5 = text;
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

		public string f_mf1;

		public string f_mf2;

		public string f_mf3;

		public string f_mf4;

		public string f_mf5;

		public List<ASTM_Comment> commentRecords = new List<ASTM_Comment>();

		private List<string> fieldNames = new List<string>(new string[]
		{
			"Record Type ID",
			"Sequence Number",
			"Manufacturer Field 1",
			"Manufacturer Field 2",
			"Manufacturer Field 3",
			"Manufacturer Field 4",
			"Manufacturer Field 5"
		});
	}
}
