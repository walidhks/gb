using System;
using System.Threading;

namespace GbService
{
	internal class GbService : IService, IDisposable
	{
		public void Start()
		{
			ThreadPool.QueueUserWorkItem(delegate(object o)
			{
				this._gb = new Gb();
				this._gb.ServiceThreadBody();
			});
		}

		public void Dispose()
		{
			this._gb.Stop();
		}

		private Gb _gb;
	}
}
