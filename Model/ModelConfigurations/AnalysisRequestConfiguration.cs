using System;
using System.Data.Entity.ModelConfiguration;
using GbService.Model.Domain;

namespace GbService.Model.ModelConfigurations
{
	internal class AnalysisRequestConfiguration : EntityTypeConfiguration<AnalysisRequest>
	{
		public AnalysisRequestConfiguration()
		{
			base.HasRequired<Patient>((AnalysisRequest e) => e.Patient);
			base.HasMany<Sample>((AnalysisRequest e) => e.Samples).WithOptional((Sample e) => e.AnalysisRequest).HasForeignKey<long?>((Sample e) => e.AnalysisRequestId).WillCascadeOnDelete(true);
		}
	}
}
