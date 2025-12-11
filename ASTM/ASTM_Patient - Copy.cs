using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using GbService.Common;

namespace GbService.ASTM
{
	public class ASTM_Patient : ASTM_Record
	{
		public ASTM_Patient()
		{
			this.f_type = "P";
		}

		public string AstmPatientID { get; set; }

		public string Name { get; set; }

		public DateTime? Birthdate { get; set; }

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
			return this.fieldNames.Count;
		}

		public override string fieldName(int idx)
		{
			idx--;
			bool flag = idx < 0 || idx >= this.fieldNames.Count;
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
            bool flag = idx < 1 || idx > this.fieldNames.Count;
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
                        result = this.f_practice_id;
                        break;
                    case 4:
                        result = this.f_laboratory_id;
                        break;
                    case 5:
                        result = this.AstmPatientID;
                        break;
                    case 6:
                        result = this.Name;
                        break;
                    case 7:
                        result = this.f_maiden_name;
                        break;
                    case 8:
                        {
                            result = this.Birthdate?.ToString("yyyyMMdd") ?? "";
                            break;
                        }
                    case 9:
                        result = this.f_sex;
                        break;
                    case 10:
                        result = this.f_race;
                        break;
                    case 11:
                        result = this.f_address;
                        break;
                    case 12:
                        result = this.f_reserved;
                        break;
                    case 13:
                        result = this.f_phone;
                        break;
                    case 14:
                        result = this.f_physician_id;
                        break;
                    case 15:
                        result = this.f_special_1;
                        break;
                    case 16:
                        result = this.f_special_2;
                        break;
                    case 17:
                        result = this.f_height;
                        break;
                    case 18:
                        result = this.f_weight;
                        break;
                    case 19:
                        result = this.f_diagnosis;
                        break;
                    case 20:
                        result = this.f_medication;
                        break;
                    case 21:
                        result = this.f_diet;
                        break;
                    case 22:
                        result = this.f_practice_field_1;
                        break;
                    case 23:
                        result = this.f_practice_field_2;
                        break;
                    case 24:
                        result = this.f_admission_date;
                        break;
                    case 25:
                        result = this.f_admission_status;
                        break;
                    case 26:
                        result = this.f_location;
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
					this.f_practice_id = text;
					break;
				case 4:
					this.f_laboratory_id = text;
					break;
				case 5:
					this.AstmPatientID = text;
					break;
				case 6:
					this.Name = text;
					break;
				case 7:
					this.f_maiden_name = text;
					break;
				case 8:
					this.Birthdate = Helper.GetDateTime(text, "yyyyMMdd");
					break;
				case 9:
					this.f_sex = text;
					break;
				case 10:
					this.f_race = text;
					break;
				case 11:
					this.f_address = text;
					break;
				case 12:
					this.f_reserved = text;
					break;
				case 13:
					this.f_phone = text;
					break;
				case 14:
					this.f_physician_id = text;
					break;
				case 15:
					this.f_special_1 = text;
					break;
				case 16:
					this.f_special_2 = text;
					break;
				case 17:
					this.f_height = text;
					break;
				case 18:
					this.f_weight = text;
					break;
				case 19:
					this.f_diagnosis = text;
					break;
				case 20:
					this.f_medication = text;
					break;
				case 21:
					this.f_diet = text;
					break;
				case 22:
					this.f_practice_field_1 = text;
					break;
				case 23:
					this.f_practice_field_2 = text;
					break;
				case 24:
					this.f_admission_date = text;
					break;
				case 25:
					this.f_admission_status = text;
					break;
				case 26:
					this.f_location = text;
					break;
				default:
					return false;
				}
				num++;
			}
			return true;
		}

		public override string ToString()
		{
			return this.Name + " " + this.Birthdate.ToString();
		}

		private string f_type;

		public string f_seq;

		public string f_practice_id;

		public string f_laboratory_id;

		public string f_maiden_name;

		public string f_sex;

		public string f_race;

		public string f_address;

		public string f_reserved;

		public string f_phone;

		public string f_physician_id;

		public string f_special_1;

		public string f_special_2;

		public string f_height;

		public string f_weight;

		public string f_diagnosis;

		public string f_medication;

		public string f_diet;

		public string f_practice_field_1;

		public string f_practice_field_2;

		public string f_admission_date;

		public string f_admission_status;

		public string f_location;

		public List<ASTM_Manufacturer> manufacturerRecords = new List<ASTM_Manufacturer>();

		public List<AstmOrder> OrderRecords = new List<AstmOrder>();

		public List<ASTM_Comment> commentRecords = new List<ASTM_Comment>();

		private List<string> fieldNames = new List<string>(new string[]
		{
			"Record Type ID",
			"Sequence Number",
			"Practice Assigned Patient ID",
			"Laboratory Assigned Patient ID",
			"Patient ID",
			"Patient Name",
			"Mother?s Maiden Name",
			"Birthdate",
			"Patient Sex",
			"Patient Race-, Ethnic Origin",
			"Patient Address",
			"Reserved Field",
			"Patient Telephone Number",
			"Attending Physician ID",
			"Special Field No. 1",
			"Special Field No. 2",
			"Patient Height",
			"Patient Weight",
			"Patient's Known Diagnosis",
			"Patient?s Active Medication",
			"Patient's Diet",
			"Practice Field No. 1",
			"Practice Field No. 2",
			"Admission/Discharge Dates",
			"Admission Status",
			"Location"
		});
	}
}
