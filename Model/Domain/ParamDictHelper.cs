using System;
using System.Collections.Generic;
using System.Linq;
using GbService.Model.Contexts;

namespace GbService.Model.Domain
{
	public static class ParamDictHelper
	{
		public static int? SettingInt(ParamDictName paramDictName)
		{
			int value;
			return int.TryParse(ParamDictHelper.Setting2(paramDictName), out value) ? new int?(value) : null;
		}

		public static long? SettingLong(ParamDictName paramDictName)
		{
			long value;
			return long.TryParse(ParamDictHelper.Setting2(paramDictName), out value) ? new long?(value) : null;
		}

		public static string Setting2(ParamDictName paramDictName)
		{
			bool flag = ParamDictHelper.ParamDicts == null || ParamDictHelper.ParamDicts.Count == 0;
			if (flag)
			{
				ParamDictHelper.LoadSettings();
			}
			List<ParamDict> paramDicts = ParamDictHelper.ParamDicts;
			string result;
			if (paramDicts == null)
			{
				result = null;
			}
			else
			{
				ParamDict paramDict = paramDicts.FirstOrDefault((ParamDict x) => x.ParamDictNom == paramDictName);
				result = ((paramDict != null) ? paramDict.ParamDictValeur : null);
			}
			return result;
		}

		public static void SetSetting(ParamDictName paramDictName, string value)
		{
			LaboContext laboContext = new LaboContext();
			ParamDict paramDict = laboContext.ParamDict.SingleOrDefault((ParamDict x) => (int)x.ParamDictNom == (int)paramDictName);
			bool flag = paramDict == null;
			if (!flag)
			{
				paramDict.ParamDictValeur = value;
				laboContext.SaveChanges();
			}
		}

		public static void LoadSettings()
		{
			LaboContext laboContext = new LaboContext();
			ParamDictHelper.ParamDicts = laboContext.ParamDict.ToList<ParamDict>();
		}

		public static string Setting(ParamDictName paramDictName)
		{
			LaboContext laboContext = new LaboContext();
			ParamDict paramDict = laboContext.ParamDict.FirstOrDefault((ParamDict x) => (int)x.ParamDictNom == (int)paramDictName);
			return (paramDict != null) ? paramDict.ParamDictValeur : "";
		}

		public static List<ParamDict> ParamDicts { get; set; } = new List<ParamDict>();

		public static long FnsId = (long)ParamDictHelper.SettingInt(ParamDictName.FnsId).GetValueOrDefault();

		public static long CounterLength = (long)ParamDictHelper.SettingInt(ParamDictName.CounterLenght).GetValueOrDefault(5);

		public static int NumberPositionBarcode = ParamDictHelper.SettingInt(ParamDictName.NumberPositionBarcode).GetValueOrDefault(12);

		public static string Reset = ParamDictHelper.Setting(ParamDictName.AnalysisRequestIdResetInterval);
	}
}
