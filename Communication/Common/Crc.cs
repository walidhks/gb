using System;
using System.Linq;
using System.Text;

namespace GbService.Communication.Common
{
	public class Crc
	{
		public static string Kenza(string s)
		{
			string text = ((byte)(Crc.AsciiSum(s) % 256)).ToString("x");
			return (text.Length == 1) ? ("0" + text) : text;
		}

		public static string calc_crc(string ss)
		{
			byte[] bytes = Encoding.ASCII.GetBytes(ss);
			byte b = byte.MaxValue;
			byte b2 = byte.MaxValue;
			foreach (byte b3 in bytes)
			{
				int num = (int)(b3 ^ b);
				num ^= num / 16;
				b = (byte)((int)b2 ^ num / 8 ^ num * 16);
				b2 = (byte)(num ^ num * 32);
			}
			b ^= byte.MaxValue;
			b2 ^= byte.MaxValue;
			string text = b.ToString("x");
			string text2 = b2.ToString("x");
			string str = (text.Length == 1) ? ("0" + text) : text;
			string str2 = (text2.Length == 1) ? ("0" + text2) : text2;
			return (str + str2).ToUpper();
		}

		public static string exclusiveOR(string s)
		{
			byte[] bytes = Encoding.ASCII.GetBytes(s);
			byte b = bytes[0];
			for (int i = 1; i < bytes.Length; i++)
			{
				b ^= bytes[i];
			}
			bool flag = b == 3;
			if (flag)
			{
				b = 127;
			}
			return Encoding.ASCII.GetString(new byte[]
			{
				b
			});
		}

		public static string Kermit(string s)
		{
			byte[] bytes = Encoding.ASCII.GetBytes(s);
			byte b = bytes[0];
			for (int i = 1; i < bytes.Length; i++)
			{
				b += bytes[i];
			}
			int num = (int)(b + (b & 192) / 64 & 63);
			byte b2 = (byte)(num + 32);
			return Encoding.ASCII.GetString(new byte[]
			{
				b2
			});
		}

		public static string Vidas(string s)
		{
			int num = Crc.AsciiSum(s);
			string text = ((byte)num).ToString("x");
			return (text.Length == 1) ? ("0" + text) : text;
		}

		public static string Targa(string s)
		{
			return (Crc.AsciiSum(s) % 256).ToString().PadLeft(3);
		}

		public static string BeckmanCx(string s)
		{
			string text = ((byte)(256 - Crc.AsciiSum(s) % 256)).ToString("x");
			return (text.Length == 1) ? ("0" + text) : text;
		}

		private static int AsciiSum(string s)
		{
			return Encoding.ASCII.GetBytes(s).Aggregate(0, (int current, byte t) => current + (int)t);
		}
	}
}
