/*using System;
using GbService.Common.Cipher; // Ensure this namespace exists
using GbService.Model.Domain;
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
                // 1. Register the Encryption Wrapper (Uses FastCipher now)
                try
                {
                    ConfigurationItemFactory.Default.LayoutRenderers.RegisterDefinition("Encrypt", typeof(EncryptLayoutRendererWrapper));
                }
                catch { }

                if (LogManager.Configuration != null)
                {
                    LogManager.Configuration = LogManager.Configuration.Reload();
                }

                LogManager.ReconfigExistingLoggers();
                LoggingConfiguration loggingConfiguration = new LoggingConfiguration();

                // ---------------------------------------------------------
                // TARGET 1: ENCRYPTED FILE (For Raw ASTM Data)
                // ---------------------------------------------------------
                // This file (e.g. 5028_2025.txt) will contain scrambled text
                FileTarget fileTargetEncrypted = new FileTarget()
                {
                    Name = "target2",
                    FileName = "${basedir}/" + fileName + ".txt",
                    // Use ${Encrypt} wrapper here
                    Layout = "${longdate} ${level:uppercase=true} ${callsite:fileName=true} ${Encrypt:${message}} ${exception}"
                };

                // ---------------------------------------------------------
                // TARGET 2: PLAIN TEXT FILE (For Info.txt)
                // ---------------------------------------------------------
                // This file will contain readable errors and status messages
                FileTarget fileTargetInfo = new FileTarget()
                {
                    Name = "target1",
                    FileName = "${basedir}/Info.txt",
                    // Normal layout, no encryption
                    Layout = "${longdate} ${level:uppercase=true} ${callsite:fileName=true} ${message} ${exception}"
                };

                loggingConfiguration.AddTarget("target2", fileTargetEncrypted);
                loggingConfiguration.AddTarget("target1", fileTargetInfo);

                // ---------------------------------------------------------
                // RULES (The Critical Part)
                // ---------------------------------------------------------

                // Rule 1: Send TRACE and DEBUG (Low Level) to the Encrypted File
                // We use '*' to catch everything, but you can limit it if needed.
                LoggingRule ruleEncrypted = new LoggingRule("*", fileTargetEncrypted);
                ruleEncrypted.EnableLoggingForLevel(LogLevel.Trace);
                ruleEncrypted.EnableLoggingForLevel(LogLevel.Debug);
                // Optional: You can also send Info/Error here if you want a full encrypted backup
                ruleEncrypted.EnableLoggingForLevel(LogLevel.Info);
                ruleEncrypted.EnableLoggingForLevel(LogLevel.Error);
                ruleEncrypted.EnableLoggingForLevel(LogLevel.Fatal);
                loggingConfiguration.LoggingRules.Add(ruleEncrypted);

                // Rule 2: Send INFO, WARN, ERROR to Info.txt (Plain Text)
                // IMPORTANT: We explicitly DISABLE Trace and Debug here so raw data doesn't leak
                LoggingRule ruleInfo = new LoggingRule("*", fileTargetInfo);
                ruleInfo.DisableLoggingForLevel(LogLevel.Trace); // <--- BLOCK RAW DATA
                ruleInfo.DisableLoggingForLevel(LogLevel.Debug); // <--- BLOCK RAW DATA
                ruleInfo.EnableLoggingForLevel(LogLevel.Info);
                ruleInfo.EnableLoggingForLevel(LogLevel.Warn);
                ruleInfo.EnableLoggingForLevel(LogLevel.Error);
                ruleInfo.EnableLoggingForLevel(LogLevel.Fatal);

                loggingConfiguration.LoggingRules.Add(ruleInfo);

                LogManager.Configuration = loggingConfiguration;

                int? num = log;
                int num2 = 1;
                result = (num.GetValueOrDefault() <= num2 & num != null);
            }
            return result;
        }

        public static string Sp = "|";
    }
}*/
using System;
using GbService.Model.Domain;
using NLog;
using NLog.Config;
using NLog.Targets;

namespace GbService.Common
{
    public class LogHelper
    {
        public static bool Init(Instrument instrument, string fileName = "")
        {
            // Pass through to the main Init method
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
                // [RESET] Clear configuration
                if (LogManager.Configuration != null)
                {
                    LogManager.Configuration = LogManager.Configuration.Reload();
                }

                LogManager.ReconfigExistingLoggers();
                LoggingConfiguration loggingConfiguration = new LoggingConfiguration();

                // ---------------------------------------------------------
                // TARGET: Info.txt (Plain Text Only)
                // ---------------------------------------------------------
                // [FIX 1] Use parameterless constructor for NLog 4 compatibility
                FileTarget fileTarget = new FileTarget()
                {
                    Name = "target1", // Set Name property here explicitly

                    // Force filename to Info.txt (Ignoring 'fileName' variable)
                    FileName = "${basedir}/Info.txt",

                    // Clean layout with Pipe separator
                    Layout = "${longdate} ${level:uppercase=true} ${callsite:fileName=true} ${message} ${exception}"
                };

                // [FIX 2] Use the 2-argument AddTarget method (Name, Target)
                loggingConfiguration.AddTarget("target1", fileTarget);

                // ---------------------------------------------------------
                // RULE: Log EVERYTHING to Info.txt
                // ---------------------------------------------------------
                LoggingRule rule1 = new LoggingRule("*", fileTarget);

                // Enable ALL levels
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

        public static string Sp = "|";
    }
}