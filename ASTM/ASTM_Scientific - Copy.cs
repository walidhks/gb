using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

namespace GbService.ASTM
{
	public class ASTM_Scientific : ASTM_Record
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
					result = this.f_seq;
					break;
				case 3:
					result = this.f_anmeth;
					break;
				case 4:
					result = this.f_instr;
					break;
				case 5:
					result = this.f_reagents;
					break;
				case 6:
					result = this.f_unitofmeas;
					break;
				case 7:
					result = this.f_qc;
					break;
				case 8:
					result = this.f_spcmdescr;
					break;
				case 9:
					result = this.f_resrvd;
					break;
				case 10:
					result = this.f_container;
					break;
				case 11:
					result = this.f_spcmid;
					break;
				case 12:
					result = this.f_analyte;
					break;
				case 13:
					result = this.f_result;
					break;
				case 14:
					result = this.f_resunts;
					break;
				case 15:
					result = this.f_collctdt;
					break;
				case 16:
					result = this.f_resdt;
					break;
				case 17:
					result = this.f_anlprocstp;
					break;
				case 18:
					result = this.f_patdiagn;
					break;
				case 19:
					result = this.f_patbd;
					break;
				case 20:
					result = this.f_patsex;
					break;
				case 21:
					result = this.f_patrace;
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
					this.f_anmeth = text;
					break;
				case 4:
					this.f_instr = text;
					break;
				case 5:
					this.f_reagents = text;
					break;
				case 6:
					this.f_unitofmeas = text;
					break;
				case 7:
					this.f_qc = text;
					break;
				case 8:
					this.f_spcmdescr = text;
					break;
				case 9:
					this.f_resrvd = text;
					break;
				case 10:
					this.f_container = text;
					break;
				case 11:
					this.f_spcmid = text;
					break;
				case 12:
					this.f_analyte = text;
					break;
				case 13:
					this.f_result = text;
					break;
				case 14:
					this.f_resunts = text;
					break;
				case 15:
					this.f_collctdt = text;
					break;
				case 16:
					this.f_resdt = text;
					break;
				case 17:
					this.f_anlprocstp = text;
					break;
				case 18:
					this.f_patdiagn = text;
					break;
				case 19:
					this.f_patbd = text;
					break;
				case 20:
					this.f_patsex = text;
					break;
				case 21:
					this.f_patrace = text;
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

		public string f_anmeth;

		public string f_instr;

		public string f_reagents;

		public string f_unitofmeas;

		public string f_qc;

		public string f_spcmdescr;

		public string f_resrvd;

		public string f_container;

		public string f_spcmid;

		public string f_analyte;

		public string f_result;

		public string f_resunts;

		public string f_collctdt;

		public string f_resdt;

		public string f_anlprocstp;

		public string f_patdiagn;

		public string f_patbd;

		public string f_patsex;

		public string f_patrace;

		public List<ASTM_Manufacturer> manufacturerRecords = new List<ASTM_Manufacturer>();

		public List<ASTM_Comment> commentRecords = new List<ASTM_Comment>();

		private List<string> fieldNames = new List<string>(new string[]
		{
			"Record Type ID",
			"Sequence Number",
			"Analytical Method",
			"Instrumentation",
			"Reagents",
			"Units of Measure",
			"Quality Control",
			"Specimen Descriptor",
			"Reserved Field",
			"Container",
			"Specimen ID",
			"Analyte",
			"Result",
			"Result Units",
			"Collection Date and Time",
			"Result Date and Time",
			"Analytical Preprocessing Steps",
			"Patient Diagnosis",
			"Patient Birthdate",
			"Patient Sex",
			"Patient Race"
		});
	}
}
