using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using GbService.Communication;

namespace GbService.Model.Domain
{
	public class Patient
	{
		public Patient()
		{
			this.AnalysisRequests = new List<AnalysisRequest>();
			this.PatientCreation = DateTime.Now;
		}

		[Index("IX_Patient_NomComplet", 2)]
		public long PatientID { get; set; }

		[StringLength(50)]
		[Required]
		public string PatientNom { get; set; }

		public string Nom
		{
			get
			{
				return this.PatientNom.Truncate(20);
			}
		}

		[StringLength(50)]
		[Required]
		public string PatientPrenom { get; set; }

		public string Prenom
		{
			get
			{
				return this.PatientPrenom.Truncate(20);
			}
		}
        public string PatientTelMob { get; set; }


        public bool IsAgeGroup { get; }

		public string PatientNomPrenom
		{
			get
			{
				return this.Nom + " " + this.Prenom;
			}
		}

		public string ShortSexe
		{
			get
			{
				SexeEnum? patientSexe = this.PatientSexe;
				SexeEnum sexeEnum = SexeEnum.Masculin;
				return (patientSexe.GetValueOrDefault() == sexeEnum & patientSexe != null) ? "M" : "F";
			}
		}

		public DateTime? PatientDateNaiss { get; set; }

		public DateTime? PatientDateDeces { get; set; }

		[NotMapped]
		public AgeGroup AgeGroupe
		{
			get
			{
				return (this.AgeDays <= 28) ? AgeGroup.NouveauNé : ((this.Age < 15) ? AgeGroup.Enfant : AgeGroup.Adulte);
			}
		}

		[NotMapped]
		public int Age
		{
			get
			{
				bool flag = this.PatientDateNaiss == null;
				int result;
				if (flag)
				{
					result = 0;
				}
				else
				{
					result = (this.PatientDateDeces ?? DateTime.Today).Year - this.PatientDateNaiss.Value.Year;
				}
				return result;
			}
		}

		[NotMapped]
		public int AgeDays
		{
			get
			{
				bool flag = this.PatientDateNaiss == null;
				int result;
				if (flag)
				{
					result = 0;
				}
				else
				{
					DateTime d = this.PatientDateDeces ?? DateTime.Today;
					int days = (d - this.PatientDateNaiss.Value).Days;
					result = days;
				}
				return result;
			}
		}

		[NotMapped]
		public int AgeMonths
		{
			get
			{
				bool flag = this.PatientDateNaiss == null;
				int result;
				if (flag)
				{
					result = 0;
				}
				else
				{
					DateTime d = this.PatientDateDeces ?? DateTime.Today;
					int num = (d - this.PatientDateNaiss.Value).Days / 30;
					result = num;
				}
				return result;
			}
		}

		public AgeAstm AgeAstm
		{
			get
			{
				return (this.AgeDays < 30) ? new AgeAstm(this.AgeDays, AgeEnum.D) : ((this.AgeDays >= 30 && this.AgeDays < 365) ? new AgeAstm(this.AgeMonths, AgeEnum.M) : new AgeAstm(this.Age, AgeEnum.Y));
			}
		}

		[Required]
		public SexeEnum? PatientSexe { get; set; }

		public DateTime PatientCreation { get; set; }

		public virtual List<AnalysisRequest> AnalysisRequests { get; set; }
	}
}
