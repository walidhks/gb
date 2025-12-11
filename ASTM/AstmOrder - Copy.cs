using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Text.RegularExpressions;
using GbService.Common;
using GbService.Communication;
using GbService.Model.Domain;

namespace GbService.ASTM
{
	public class AstmOrder : ASTM_Record, INotifyPropertyChanged
	{
		public string SampleID { get; set; }

		public string Instrument { get; set; }

		public string Test { get; set; }

		public string Priority { get; set; }

		public DateTime? CreatedAt { get; set; }

		public bool Valide
		{
			get
			{
				return this._valide;
			}
			set
			{
				this._valide = value;
				this.RaisePropertyChanged("Valide");
			}
		}

		public ASTM_Patient Patient { get; set; }

		public List<AstmResult> ResultRecords { get; set; }

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
					result = this.SampleID;
					break;
				case 4:
					result = this.Instrument;
					break;
				case 5:
					result = this.Test;
					break;
				case 6:
					result = this.Priority;
					break;
				case 7:
					result = this.CreatedAt.ToString();
					break;
				case 8:
					result = this.f_sampled_at;
					break;
				case 9:
					result = this.f_collected_at;
					break;
				case 10:
					result = this.f_volume;
					break;
				case 11:
					result = this.f_collector;
					break;
				case 12:
					result = this.f_action_code;
					break;
				case 13:
					result = this.f_danger_code;
					break;
				case 14:
					result = this.f_clinical_info;
					break;
				case 15:
					result = this.f_delivered_at;
					break;
				case 16:
					result = this.f_biomaterial;
					break;
				case 17:
					result = this.f_physician;
					break;
				case 18:
					result = this.f_physician_phone;
					break;
				case 19:
					result = this.f_user_field_1;
					break;
				case 20:
					result = this.f_user_field_2;
					break;
				case 21:
					result = this.f_laboratory_field_1;
					break;
				case 22:
					result = this.f_laboratory_field_2;
					break;
				case 23:
					result = this.f_modified_at;
					break;
				case 24:
					result = this.f_instrument_charge;
					break;
				case 25:
					result = this.f_instrument_section;
					break;
				case 26:
					result = this.f_report_type;
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
					this.SampleID = text;
					break;
				case 4:
					this.Instrument = text;
					break;
				case 5:
					this.Test = text.Replace("^^^", "");
					break;
				case 6:
					this.Priority = text;
					break;
				case 7:
					this.CreatedAt = Helper.GetDateTime(text, "yyyyMMdd");
					break;
				case 8:
					this.f_sampled_at = text;
					break;
				case 9:
					this.f_collected_at = text;
					break;
				case 10:
					this.f_volume = text;
					break;
				case 11:
					this.f_collector = text;
					break;
				case 12:
					this.f_action_code = text;
					break;
				case 13:
					this.f_danger_code = text;
					break;
				case 14:
					this.f_clinical_info = text;
					break;
				case 15:
					this.f_delivered_at = text;
					break;
				case 16:
					this.f_biomaterial = text;
					break;
				case 17:
					this.f_physician = text;
					break;
				case 18:
					this.f_physician_phone = text;
					break;
				case 19:
					this.f_user_field_1 = text;
					break;
				case 20:
					this.f_user_field_2 = text;
					break;
				case 21:
					this.f_laboratory_field_1 = text;
					break;
				case 22:
					this.f_laboratory_field_2 = text;
					break;
				case 23:
					this.f_modified_at = text;
					break;
				case 24:
					this.f_instrument_charge = text;
					break;
				case 25:
					this.f_instrument_section = text;
					break;
				case 26:
					this.f_report_type = text;
					break;
				default:
					return false;
				}
				num++;
			}
			return true;
		}

		public AstmOrder()
		{
			this.f_type = "O";
			this.ResultRecords = new List<AstmResult>();
		}

		public AstmOrder(Sample sample, string test, int seq, string[] param, Instrument instrument) : this()
		{
			this.f_seq = (seq + 1).ToString();
			this.SampleID = sample.FormattedSampleCode;
			this.Instrument = sample.InstrumentSampleId;
			this.Priority = "R";
			this.f_sampled_at = TextUtil.GetDate(sample, instrument).ToString("yyyyMMddHHmmss");
			this.f_action_code = param[0];
			this.f_biomaterial = param[1];
			this.f_report_type = param[2];
			this.Test = test;
		}

		//[DebuggerBrowsable(DebuggerBrowsableState.Never)]
		public event PropertyChangedEventHandler PropertyChanged;

		private void RaisePropertyChanged(string propertyName)
		{
			PropertyChangedEventHandler propertyChanged = this.PropertyChanged;
			if (propertyChanged != null)
			{
				propertyChanged(this, new PropertyChangedEventArgs(propertyName));
			}
		}

		private string f_type;

		public string f_seq;

		public string f_sampled_at;

		public string f_collected_at;

		public string f_volume;

		public string f_collector;

		public string f_action_code;

		public string f_danger_code;

		public string f_clinical_info;

		public string f_delivered_at;

		public string f_biomaterial;

		public string f_physician;

		public string f_physician_phone;

		public string f_user_field_1;

		public string f_user_field_2;

		public string f_laboratory_field_1;

		public string f_laboratory_field_2;

		public string f_modified_at;

		public string f_instrument_charge;

		public string f_instrument_section;

		public string f_report_type;

		public List<ASTM_Manufacturer> manufacturerRecords = new List<ASTM_Manufacturer>();

		public List<ASTM_Comment> commentRecords = new List<ASTM_Comment>();

		private List<string> fieldNames = new List<string>(new string[]
		{
			"Record Type ID",
			"Sequence Number",
			"Specimen ID",
			"Instrument Specimen ID",
			"Universal Test ID",
			"Priority",
			"Requested/Ordered Date/Time",
			"Specimen Collection Date/Time",
			"Collection End Time",
			"Collection Volume",
			"Collector ID",
			"Action Code",
			"Danger Code",
			"Relevant Information",
			"Date/Time Specimen Received",
			"Specimen Descriptor",
			"Ordering Physician",
			"Physician's Telephone Number",
			"User Field No. 1",
			"User Field No. 2",
			"Laboratory Field No. 1",
			"Laboratory Field No. 2",
			"Date/Time Reported",
			"Instrument Charge",
			"Instrument Section ID",
			"Report Type"
		});

		private bool _valide;
	}
}
