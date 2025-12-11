using System;
using System.ComponentModel;
using System.Net;
using System.Runtime.InteropServices;

namespace GbService.Common
{
	public class NetworkConnection : IDisposable
	{
		public NetworkConnection(string networkName, NetworkCredential credentials)
		{
			this._networkName = networkName;
			NetResource netResource = new NetResource
			{
				Scope = ResourceScope.GlobalNetwork,
				ResourceType = ResourceType.Disk,
				DisplayType = ResourceDisplaytype.Share,
				RemoteName = networkName
			};
			string username = string.IsNullOrEmpty(credentials.Domain) ? credentials.UserName : string.Format("{0}\\{1}", credentials.Domain, credentials.UserName);
			int num = NetworkConnection.WNetAddConnection2(netResource, credentials.Password, username, 0);
			bool flag = num != 0;
			if (flag)
			{
				throw new Win32Exception(num);
			}
		}

		~NetworkConnection()
		{
			this.Dispose(false);
		}

		public void Dispose()
		{
			this.Dispose(true);
			GC.SuppressFinalize(this);
		}

		protected virtual void Dispose(bool disposing)
		{
			NetworkConnection.WNetCancelConnection2(this._networkName, 0, true);
		}

		[DllImport("mpr.dll")]
		private static extern int WNetAddConnection2(NetResource netResource, string password, string username, int flags);

		[DllImport("mpr.dll")]
		private static extern int WNetCancelConnection2(string name, int flags, bool force);

		private string _networkName;
	}
}
