using System;
using System.Data.Entity.Migrations;
using GbService.Model.Contexts;

namespace GbService.Model.Migrations
{
	internal sealed class Configuration : DbMigrationsConfiguration<LaboContext>
	{
		public Configuration()
		{
			base.AutomaticMigrationsEnabled = false;
			base.AutomaticMigrationDataLossAllowed = true;
			base.MigrationsDirectory = "Migrations";
		}

		protected override void Seed(LaboContext context)
		{
		}
	}
}
