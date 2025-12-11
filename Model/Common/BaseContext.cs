using System;
using System.Configuration;
using System.Data.Entity;

namespace GbService.Model.Common
{
	public class BaseContext<TContext> : DbContext where TContext : DbContext
	{
		static BaseContext()
		{
			Database.SetInitializer<TContext>(null);
		}

		protected BaseContext() : base(ConfigurationManager.ConnectionStrings["ClinModel"].ConnectionString + "Application Name=MyApp")
		{
		}
	}
}
