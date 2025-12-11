using System;
using System.Runtime.InteropServices;

namespace GbService.Common
{
	[StructLayout(LayoutKind.Sequential)]
	public class NetResource
	{
		public ResourceScope Scope;

		public ResourceType ResourceType;

		public ResourceDisplaytype DisplayType;

		public int Usage;

		[MarshalAs(UnmanagedType.LPWStr)]
		public string LocalName;

		[MarshalAs(UnmanagedType.LPWStr)]
		public string RemoteName;

		[MarshalAs(UnmanagedType.LPWStr)]
		public string Comment;

		[MarshalAs(UnmanagedType.LPWStr)]
		public string Provider;
	}
}
