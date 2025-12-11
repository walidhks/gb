using System;
using System.Collections;
using System.Collections.Specialized;
using System.Configuration.Install;
using System.Reflection;
using System.ServiceProcess;

namespace GbService
{
	internal static class BasicServiceInstaller
	{
		public static void Install(string serviceName)
		{
			BasicServiceInstaller.CreateInstaller(serviceName).Install(new Hashtable());
		}

		public static void Uninstall(string serviceName)
		{
			BasicServiceInstaller.CreateInstaller(serviceName).Uninstall(null);
		}

		private static Installer CreateInstaller(string serviceName)
		{
			TransactedInstaller transactedInstaller = new TransactedInstaller();
			transactedInstaller.Installers.Add(new ServiceInstaller
			{
				ServiceName = serviceName,
				DisplayName = serviceName,
				StartType = ServiceStartMode.Automatic,
				DelayedAutoStart = true
			});
			transactedInstaller.Installers.Add(new ServiceProcessInstaller
			{
				Account = ServiceAccount.LocalSystem
			});
			InstallContext installContext = new InstallContext(serviceName + ".install.log", null);
			StringDictionary parameters = installContext.Parameters;
			string key = "assemblypath";
			Assembly entryAssembly = Assembly.GetEntryAssembly();
			parameters[key] = ((entryAssembly != null) ? entryAssembly.Location : null);
			transactedInstaller.Context = installContext;
			return transactedInstaller;
		}
	}
}
