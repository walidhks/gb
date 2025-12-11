using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.ServiceProcess;
using NLog;

namespace GbService
{
	public static class BasicServiceStarter
	{
		public static void Run<T>(string serviceName) where T : IService, new()
		{
			AppDomain.CurrentDomain.UnhandledException += delegate(object s, UnhandledExceptionEventArgs e)
			{
				Logger logger = BasicServiceStarter._logger;
				string serviceName2 = serviceName;
				string str = " : ";
				object exceptionObject = e.ExceptionObject;
				logger.Error(serviceName2 + str + ((exceptionObject != null) ? exceptionObject.ToString() : null));
				bool flag = EventLog.SourceExists(serviceName);
				if (flag)
				{
					string serviceName3 = serviceName;
					string str2 = "Fatal Exception : ";
					string newLine = Environment.NewLine;
					object exceptionObject2 = e.ExceptionObject;
					EventLog.WriteEntry(serviceName3, str2 + newLine + ((exceptionObject2 != null) ? exceptionObject2.ToString() : null), EventLogEntryType.Error);
				}
			};
			bool userInteractive = Environment.UserInteractive;
			if (userInteractive)
			{
				List<string> list = Environment.GetCommandLineArgs().Skip(1).ToList<string>();
				string text = (list.FirstOrDefault<string>() ?? "").ToLower();
				string text2 = (list.Count > 1) ? list[1] : serviceName;
				string text3 = text;
				string a = text3;
				if (!(a == "i") && !(a == "install"))
				{
					if (!(a == "u") && !(a == "uninstall"))
					{
						using (T t = Activator.CreateInstance<T>())
						{
							t.Start();
							Console.WriteLine("Running {0}, press any key to stop", text2);
							Console.ReadKey();
						}
					}
					else
					{
						Console.WriteLine("Uninstalling {0}", text2);
						BasicServiceInstaller.Uninstall(text2);
					}
				}
				else
				{
					Console.WriteLine("Installing {0}", text2);
					BasicServiceInstaller.Install(text2);
				}
			}
			else
			{
				ServiceBase.Run(new BasicService<T>
				{
					ServiceName = serviceName
				});
			}
		}

		private static Logger _logger = LogManager.GetCurrentClassLogger();
	}
}
