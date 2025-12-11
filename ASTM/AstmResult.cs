using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

namespace GbService.ASTM
{
	public class AstmResult : ASTM_Record
	{
		public string Test { get; set; }

		public string Value { get; set; }

		public string Units { get; set; }

		public string References { get; set; }

		public string AbnormalFlag { get; set; }

		public string AbnormalityNature { get; set; }

		public string Status { get; set; }

		public string Operator { get; set; }

		public string StartedAt { get; set; }

		public string CompletedAt { get; set; }

		public string Instrument { get; set; }

		public override List<ASTM_Record> comments()
		{
			return this.CommentRecords.ToList<ASTM_Record>();
		}

		public override List<ASTM_Record> manufacturerInfo()
		{
			return this.ManufacturerRecords.ToList<ASTM_Record>();
		}

		public override int fieldsCount()
		{
			return this._fieldNames.Count<string>();
		}

		public override string fieldName(int idx)
		{
			idx--;
			bool flag = idx < 0 || idx >= this._fieldNames.Count<string>();
			string result;
			if (flag)
			{
				result = null;
			}
			else
			{
				result = this._fieldNames.ElementAt(idx);
			}
			return result;
		}

		public override string fieldValue(int idx)
		{
			bool flag = idx < 1 || idx > this._fieldNames.Count<string>();
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
					result = this.Type;
					break;
				case 2:
					result = this.Seq;
					break;
				case 3:
					result = this.Test;
					break;
				case 4:
					result = this.Value;
					break;
				case 5:
					result = this.Units;
					break;
				case 6:
					result = this.References;
					break;
				case 7:
					result = this.AbnormalFlag;
					break;
				case 8:
					result = this.AbnormalityNature;
					break;
				case 9:
					result = this.Status;
					break;
				case 10:
					result = this.NormsChangedAt;
					break;
				case 11:
					result = this.Operator;
					break;
				case 12:
					result = this.StartedAt;
					break;
				case 13:
					result = this.CompletedAt;
					break;
				case 14:
					result = this.Instrument;
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
			this.CommentRecords.Add(new ASTM_Comment());
			return this.CommentRecords.Last<ASTM_Comment>().parseData(recordData);
		}

		public override bool addManufacturerInfo(string recordData)
		{
			this.ManufacturerRecords.Add(new ASTM_Manufacturer());
			return this.ManufacturerRecords.Last<ASTM_Manufacturer>().parseData(recordData);
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
					this.Type = text;
					break;
				case 2:
					this.Seq = text;
					break;
				case 3:
					this.Test = text;
					break;
				case 4:
					this.Value = text;
					break;
				case 5:
					this.Units = text;
					break;
				case 6:
					this.References = text;
					break;
				case 7:
					this.AbnormalFlag = text;
					break;
				case 8:
					this.AbnormalityNature = text;
					break;
				case 9:
					this.Status = text;
					break;
				case 10:
					this.NormsChangedAt = text;
					break;
				case 11:
					this.Operator = text;
					break;
				case 12:
					this.StartedAt = text;
					break;
				case 13:
					this.CompletedAt = text;
					break;
				case 14:
					this.Instrument = text;
					break;
				default:
					return false;
				}
				num++;
			}
			return true;
		}

		public AstmResult(string lib, string res, string unite = null)
		{
			this.Test = lib;
			this.Value = res;
			this.Units = unite;
		}

		public AstmResult()
		{
		}

		public string Type;

		public string Seq;

		public string NormsChangedAt;

		public List<ASTM_Manufacturer> ManufacturerRecords = new List<ASTM_Manufacturer>();

		public List<ASTM_Comment> CommentRecords = new List<ASTM_Comment>();

		private List<string> _fieldNames = new List<string>(new string[]
		{
			"Record Type ID",
			"Sequence Number",
			"Universal Test ID",
			"Data or Measurement Value",
			"Units",
			"Reference Ranges",
			"Result Abnormal Flags",
			"Nature of Abnormal Testing",
			"Results Status",
			"Date of Changein Instrument",
			"Operator Identification",
			"Date/Time Test Started",
			"Date/Time Test Complete",
			"Instrument Identification"
		});
	}
}
