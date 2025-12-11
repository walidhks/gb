using System;
/*using GbService.Model.Domain;
using NLog;
using NLog.Config;
using NLog.Targets;

namespace GbService.Common
{
    public class LogHelper
    {
        public static bool Init(Instrument instrument, string fileName = "")
        {
            bool result = LogHelper.Init(instrument.InstrumentId, fileName, instrument.InstrumentParity);
            Usr.Restart(instrument, "admin", "admin");
            return result;
        }

        public static bool Init(int instrumentId, string fileName, int? log = null)
        {
            bool flag = log == null;
            bool result;
            if (flag)
            {
                LogManager.Configuration = null;
                result = false;
            }
            else
            {
                // [REMOVED] Encryption registration is gone

                if (LogManager.Configuration != null)
                {
                    LogManager.Configuration = LogManager.Configuration.Reload();
                }

                LogManager.ReconfigExistingLoggers();
                LoggingConfiguration loggingConfiguration = new LoggingConfiguration();

                // Target: Info.txt (Plain Text)
                FileTarget fileTarget = new FileTarget()
                {
                    Name = "target1",
                    FileName = "${basedir}/Info.txt",
                    // Simple layout: Date, Level, File, Message
                    Layout = "${longdate} ${level} ${callsite:fileName=true} ${message} ${exception}"
                };

                loggingConfiguration.AddTarget("target1", fileTarget);

                // Rule: Log everything (*) to Info.txt
                LoggingRule rule1 = new LoggingRule("*", fileTarget);
                // Manually enable levels for NLog 4 compatibility
                rule1.EnableLoggingForLevel(LogLevel.Trace);
                rule1.EnableLoggingForLevel(LogLevel.Debug);
                rule1.EnableLoggingForLevel(LogLevel.Info);
                rule1.EnableLoggingForLevel(LogLevel.Warn);
                rule1.EnableLoggingForLevel(LogLevel.Error);
                rule1.EnableLoggingForLevel(LogLevel.Fatal);

                loggingConfiguration.LoggingRules.Add(rule1);

                LogManager.Configuration = loggingConfiguration;

                int? num = log;
                int num2 = 1;
                result = (num.GetValueOrDefault() <= num2 & num != null);
            }
            return result;
        }

        public static string Sp = "\t";
    }
}*/