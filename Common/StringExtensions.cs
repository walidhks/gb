using System;

namespace GbService.Common
{
	public static class StringExtensions
	{
		public static bool Contains(this string source, string toCheck, StringComparison comp)
		{
			return source != null && source.IndexOf(toCheck, comp) >= 0;
		}

		public static bool Ctn(this string source, string toCheck)
		{
			return source.Contains(toCheck, StringComparison.OrdinalIgnoreCase);
		}

		public static string[] Split(this string source, string s)
		{
			return source.Split(new string[]
			{
				s
			}, StringSplitOptions.RemoveEmptyEntries);
		}

		public static string PadExact(this string source, int l)
		{
			return source.PadRight(l).Substring(0, l);
		}
	}
}
