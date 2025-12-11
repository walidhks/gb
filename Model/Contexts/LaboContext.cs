using System;
using System.Data.Entity;
using System.Data.Entity.ModelConfiguration.Conventions;
using GbService.Model.Common;
using GbService.Model.Domain;
using GbService.Model.ModelConfigurations;

namespace GbService.Model.Contexts
{
	public class LaboContext : BaseContext<LaboContext>
	{
		public virtual DbSet<Analysis> Analysis { get; set; }

		public virtual DbSet<ParamDict> ParamDict { get; set; }

		public virtual DbSet<Sample> Sample { get; set; }

		public virtual DbSet<AnalysisRequest> AnalysisRequest { get; set; }

		public virtual DbSet<Instrument> Instrument { get; set; }

		public virtual DbSet<Patient> Patient { get; set; }

		public virtual DbSet<LabMessage> LabMessage { get; set; }

		public virtual DbSet<AnalysisTypeInstrumentMapping> AnalysisTypeInstrumentMappings { get; set; }
        public virtual DbSet<Partner> Partner { get; set; }
        protected override void OnModelCreating(DbModelBuilder modelBuilder)
		{
			modelBuilder.Configurations.Add<AnalysisRequest>(new AnalysisRequestConfiguration());
			modelBuilder.Configurations.Add<AnalysisType>(new AnalysisTypeConfiguration());
			modelBuilder.Configurations.Add<Analysis>(new AnalysisConfiguration());
			modelBuilder.Configurations.Add<Sample>(new SampleConfiguration());
			modelBuilder.Conventions.Remove<OneToManyCascadeDeleteConvention>();
			modelBuilder.Conventions.Remove<PluralizingTableNameConvention>();
			modelBuilder.Entity<AnalysisTypeInstrumentMapping>().Property<int>((AnalysisTypeInstrumentMapping b) => b.InstrumentCode).HasColumnName("InstrumentId");
		}
	}
}
