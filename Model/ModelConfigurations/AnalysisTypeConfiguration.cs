using System;
using System.Data.Entity.ModelConfiguration;
using GbService.Model.Domain;

namespace GbService.Model.ModelConfigurations
{
	internal class AnalysisTypeConfiguration : EntityTypeConfiguration<AnalysisType>
	{
		public AnalysisTypeConfiguration()
		{
			base.HasMany<Analysis>((AnalysisType e) => e.Analysis).WithRequired((Analysis e) => e.AnalysisType).HasForeignKey<long>((Analysis e) => e.AnalysisTypeId);
			base.HasOptional<SampleSource>((AnalysisType e) => e.SampleSource).WithMany().HasForeignKey<long?>((AnalysisType e) => e.SampleSourceId).WillCascadeOnDelete(false);
			base.HasMany<PanelRelationship>((AnalysisType e) => e.ChildPanelRelationships).WithRequired((PanelRelationship e) => e.ParentAnalysisType).HasForeignKey<long>((PanelRelationship e) => e.ParentAnalysisTypeId).WillCascadeOnDelete();
			base.HasMany<AnalysisTypeInstrumentMapping>((AnalysisType e) => e.AnalysisTypeInstrumentMappings).WithRequired((AnalysisTypeInstrumentMapping e) => e.AnalysisType).HasForeignKey<long>((AnalysisTypeInstrumentMapping e) => e.AnalysisTypeId).WillCascadeOnDelete();
			base.HasMany<PanelRelationship>((AnalysisType e) => e.ParentPanelRelationships).WithRequired((PanelRelationship e) => e.ChildAnalysisType).HasForeignKey<long>((PanelRelationship e) => e.ChildAnalysisTypeId);
		}
	}
}
