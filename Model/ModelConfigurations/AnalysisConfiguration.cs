using System;
using System.Data.Entity.ModelConfiguration;
using GbService.Model.Domain;

namespace GbService.Model.ModelConfigurations
{
	public class AnalysisConfiguration : EntityTypeConfiguration<Analysis>
	{
		public AnalysisConfiguration()
		{
			base.HasRequired<AnalysisType>((Analysis e) => e.AnalysisType).WithMany().HasForeignKey<long>((Analysis e) => e.AnalysisTypeId);
			base.HasOptional<Instrument>((Analysis e) => e.Instrument).WithMany().HasForeignKey<int?>((Analysis e) => e.InstrumentId);
			base.HasOptional<Analysis>((Analysis e) => e.Parent).WithMany((Analysis e) => e.ChildAnalysises).HasForeignKey<long?>((Analysis e) => e.ParentAnalysisId);
		}
	}
}
