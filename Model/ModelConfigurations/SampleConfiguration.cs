using System;
using System.Data.Entity.ModelConfiguration;
using GbService.Model.Domain;

namespace GbService.Model.ModelConfigurations
{
	internal class SampleConfiguration : EntityTypeConfiguration<Sample>
	{
		public SampleConfiguration()
		{
			base.HasRequired<SampleSource>((Sample e) => e.SampleSource).WithMany().HasForeignKey<long>((Sample e) => e.SampleSourceId);
			base.HasMany<Analysis>((Sample e) => e.Analysis).WithOptional((Analysis e) => e.Sample).HasForeignKey<long?>((Analysis e) => e.SampleId).WillCascadeOnDelete(true);
		}
	}
}
