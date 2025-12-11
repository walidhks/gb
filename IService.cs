using System;

namespace GbService
{
	public interface IService : IDisposable
	{
		void Start();
	}
}
