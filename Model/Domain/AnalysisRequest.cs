using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text;

namespace GbService.Model.Domain
{
	public class AnalysisRequest
	{
		[Key]
		[DatabaseGenerated(DatabaseGeneratedOption.Identity)]
		public long AnalysisRequestId { get; set; }

		public long? AnalysisRequestId2 { get; set; }

		[Range(1.0, 9.223372036854776E+18, ErrorMessage = "Champ obligatoire")]
		public long PatientId { get; set; }

		public bool IsEmergency { get; set; }

		public bool IsBlocked { get; set; }

		public string Remark { get; set; }
		//added medecin string
		public string MedecinString { get; set; }

        public string MedecinAdressant { get; set; }
        public long? PartnerId { get; set; }  // Add this line

        // Optional: Add navigation property if you use Entity Framework relationships
        public virtual Partner Partner { get; set; }

        [Column(TypeName = "DateTime2")]
		public DateTime DateRequested { get; set; }

		public AnalysisRequestState AnalysisRequestState
		{
			get
			{
				return this._analysisRequestState;
			}
			set
			{
				this._analysisRequestState = value;
				this.AnalysisRequestDateState = new DateTime?(DateTime.Now);
			}
		}

		public DateTime? AnalysisRequestDateState { get; set; }

		public virtual Patient Patient { get; set; }

		public virtual List<Sample> Samples { get; set; }

		public string WebAccountPassword { get; set; }

		public virtual List<Analysis> Analysis { get; set; }

		public AnalysisRequest()
		{
			this.AnalysisRequestState = AnalysisRequestState.Crée;
			this.Analysis = new List<Analysis>();
			this.DateRequested = DateTime.Now;
			this.Samples = new List<Sample>();
			this.WebAccountPassword = AnalysisRequest.CreatePassword(5);
		}

		public override string ToString()
		{
			string str = this.AnalysisRequestId2.ToString();
			string str2 = " ";
			Patient patient = this.Patient;
			return str + str2 + ((patient != null) ? patient.ToString() : null);
		}

		public static string CreatePassword(int length)
		{
			StringBuilder stringBuilder = new StringBuilder();
			Random random = new Random();
			while (0 < length--)
			{
				stringBuilder.Append("1234567890"[random.Next("1234567890".Length)]);
			}
			return stringBuilder.ToString();
		}

		private AnalysisRequestState _analysisRequestState;
	}
}
