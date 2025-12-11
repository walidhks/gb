using System;
using System.Globalization;
using GbService.Model.Domain;

namespace GbService.Common
{
	public class Helper
	{
		public static DateTime? GetDateTime(string v, string format)
		{
			string[] formats = new string[]
			{
				"yyyyMMddHHmmss",
				"yyyyMMdd"
			};
			DateTime value;
			return DateTime.TryParseExact(v, formats, CultureInfo.InvariantCulture, DateTimeStyles.None, out value) ? new DateTime?(value) : null;
		}

		public static void ChangeSate(Analysis a, int instrumentId, AnalysisState state)
		{
			a.InstrumentId = new int?(instrumentId);
			a.AnalysisState = AnalysisState.ReçuAutomate;
			a.AnalysisStateChangeDate = DateTime.Now;
		}

		public static int ID;

		public static string Ack;
	}
}
