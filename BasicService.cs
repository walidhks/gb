using System;
using System.ServiceProcess;

namespace GbService
{
	internal class BasicService<T> : ServiceBase where T : IService, new()
	{
		protected override void OnStart(string[] args)
		{
			try
			{
				this._service = Activator.CreateInstance<T>();
				this._service.Start();
			}
			catch
			{
				base.ExitCode = 1064;
				throw;
			}
		}

		protected override void OnStop()
		{
			this._service.Dispose();
		}

		private IService _service;
	}
}
