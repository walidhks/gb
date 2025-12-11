using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using GbService.ASTM;
using NLog;

namespace GbService.Communication.Common
{
	public class Tu
	{
		public static Tuple<int, string> CleanMsg(string c, Logger logFile, Jihas kind)
		{
			c = c.Replace('\u0002'.ToString(), "");
			int num = c.IndexOf('\u0017');
			int num2 = c.IndexOf('\u0003');
			bool flag = num < 0 && num2 < 0;
			Tuple<int, string> result;
			if (flag)
			{
				logFile.Error("Message not in right format 1");
				result = null;
			}
			else
			{
				c = c.Substring(0, (num >= 0) ? num : num2);
				int item;
				bool flag2 = int.TryParse(c.Substring(0, 1), out item);
				if (flag2)
				{
					string text = c.Substring(1);
					result = ((kind == Jihas.Iris) ? Tuple.Create<int, string>(item, Tu.ToAscii(text)) : Tuple.Create<int, string>(item, text));
				}
				else
				{
					logFile.Error("Message not in right format 2");
					result = null;
				}
			}
			return result;
		}

		public static Tuple<int, string> CleanMsgCobas411(string c, Logger logFile)
		{
			int num = c.IndexOf('\u0002');
			int num2 = c.IndexOf('\u0017');
			int num3 = c.IndexOf('\u0003');
			bool flag = (num2 < 0 && num3 < 0) || num < 0;
			Tuple<int, string> result;
			if (flag)
			{
				logFile.Error("Message not in right format 1 cobas");
				result = null;
			}
			else
			{
				c = c.Substring(num, (num2 >= 0) ? (num2 - num) : (num3 - num));
				c = c.Replace('\u0002'.ToString(), "");
				int item;
				bool flag2 = int.TryParse(c.Substring(0, 1), out item);
				if (flag2)
				{
					string item2 = c.Substring(1);
					result = Tuple.Create<int, string>(item, item2);
				}
				else
				{
					logFile.Error("Message not in right format 2 cobas");
					result = null;
				}
			}
			return result;
		}

		public static string CleanMsgLH780(string c, Logger logFile)
		{
			c = c.Substring(3);
			return c.Substring(0, c.IndexOf('\u0003') - 4);
		}

		public static string CleanMsgMaglumi(string c)
		{
			int num = c.IndexOf('\u0002');
			int num2 = c.IndexOf('\u0017');
			int num3 = c.IndexOf('\u0003');
			bool flag = (num2 < 0 && num3 < 0) || num < 0;
			string result;
			if (flag)
			{
				result = null;
			}
			else
			{
				int num4 = (num2 >= 0) ? num2 : num3;
				result = c.Substring(num + 1, num4 - num - 1);
			}
			return result;
		}

		public static string CleanMsgEc90(string c)
		{
			int num = c.IndexOf('\u0002');
			int num2 = c.IndexOf('\u0003');
			bool flag = num2 < 0 || num < 0;
			string result;
			if (flag)
			{
				result = null;
			}
			else
			{
				result = c.Substring(num + 2, num2 - num - 2);
			}
			return result;
		}

		public static string KermitMessage(string c)
		{
			c = c.Substring(4);
			return c.Substring(0, c.Length - 2);
		}

		public static string ToAscii(string msg)
		{
			string result;
			try
			{
				List<string> list = TextUtil.Split(msg, 4);
				StringBuilder stringBuilder = new StringBuilder();
				foreach (string text in list)
				{
					string s = text.Substring(2) + text.Substring(0, 2);
					int utf = int.Parse(s, NumberStyles.HexNumber);
					string value = char.ConvertFromUtf32(utf);
					stringBuilder.Append(value);
				}
				result = stringBuilder.ToString();
			}
			catch (Exception ex)
			{
				result = msg;
			}
			return result;
		}

		public static string DataToString(char[] ba)
		{
			string text = "";
			for (int i = 0; i < ba.Length; i++)
			{
				byte b = (byte)ba.ElementAt(i);
				bool flag = b < 32;
				if (flag)
				{
					try
					{
						text += Tu.SpecialDict[ba.ElementAt(i)];
					}
					catch (Exception)
					{
						text = text + "[" + b.ToString() + "]";
					}
				}
				else
				{
					text += ba.ElementAt(i).ToString();
				}
			}
			return text;
		}

		public static string GetCheckSumValue(string frame)
		{
			string text = "00";
			int num = 0;
			bool flag = false;
			foreach (char value in frame)
			{
				int num2 = Convert.ToInt32(value);
				int num3 = num2;
				int num4 = num3;
				if (num4 != 2)
				{
					if (num4 != 3 && num4 != 23)
					{
						num += num2;
					}
					else
					{
						num += num2;
						flag = true;
					}
				}
				else
				{
					num = 0;
				}
				bool flag2 = flag;
				if (flag2)
				{
					break;
				}
			}
			bool flag3 = num > 0;
			if (flag3)
			{
				text = Convert.ToString(num % 256, 16).ToUpper();
			}
			return (text.Length == 1) ? ("0" + text) : text;
		}

		public static List<Frame> GetMatches(string input)
		{
			List<Frame> list = new List<Frame>();
			char c = '\u0005';
			char c2 = '\u0004';
			string text = string.Format("{0}.+{1}..{2}{3}", new object[]
			{
				'\u0002',
				'\u0017',
				'\r',
				'\n'
			});
			string text2 = string.Format("{0}.+{1}..{2}{3}", new object[]
			{
				'\u0002',
				'\u0003',
				'\r',
				'\n'
			});
			string text3 = "^\u0006+$";
			string text4 = "^\u0015+$";
			string text5 = string.Format("({0})|({1})|({2})|({3})|({4})|({5})", new object[]
			{
				c,
				c2,
				text,
				text2,
				text3,
				text4
			});
			bool flag = !Regex.IsMatch(input, "(" + text5 + ")+");
			List<Frame> result;
			if (flag)
			{
				result = null;
			}
			else
			{
				foreach (object obj in Regex.Matches(input, text5))
				{
					Match match = (Match)obj;
					FrameKind kind = Regex.IsMatch(match.Value, c.ToString()) ? FrameKind.Enq : (Regex.IsMatch(match.Value, c2.ToString()) ? FrameKind.Eot : (Regex.IsMatch(match.Value, text) ? FrameKind.Etb : (Regex.IsMatch(match.Value, text2) ? FrameKind.Etx : (Regex.IsMatch(match.Value, text3) ? FrameKind.Ack : FrameKind.Nak))));
					list.Add(new Frame(kind, match.Value));
				}
				result = list;
			}
			return result;
		}

		private static Dictionary<char, string> SpecialDict = new Dictionary<char, string>
		{
			{
				'\u0002',
				"<STX>"
			},
			{
				'\u0003',
				"<ETX>"
			},
			{
				'\u0017',
				"<ETB>"
			},
			{
				'\u0005',
				"<ENQ>"
			},
			{
				'\u0004',
				"<EOT>"
			},
			{
				'\u0006',
				"<ACK>"
			},
			{
				'\u0015',
				"<NAK>"
			},
			{
				'\r',
				'\r'.ToString()
			},
			{
				'\n',
				'\n'.ToString()
			},
			{
				'\v',
				"<SB>"
			},
			{
				'\u001c',
				"<EB>"
			},
			{
				'\u001d',
				"<GS>"
			},
			{
				'\u001e',
				"<RS>"
			}
		};

		public const char NUL = '\0';

		public const char SOH = '\u0001';

		public const char STX = '\u0002';

		public const char ETX = '\u0003';

		public const char EOT = '\u0004';

		public const char ENQ = '\u0005';

		public const char ACK = '\u0006';

		public const char SYN = '\u0016';

		public const char ETB = '\u0017';

		public const char TAB = '\t';

		public const char NAK = '\u0015';

		public const char LF = '\n';

		public const char CR = '\r';

		public const char SB = '\v';

		public const char DLE = '\u0010';

		public const char EB = '\u001c';

		public const char GS = '\u001d';

		public const char RS = '\u001e';

		public const char Zero = '0';

		public static string NL = "\r\n";

		public const byte _ENQ = 5;

		public const byte _ACK = 6;

		public const byte _NAK = 21;

		public const byte _EOT = 4;

		public const byte _ETX = 3;

		public const byte _SYN = 22;

		public const byte _ETB = 23;

		public const byte _STX = 2;

		public const byte _CR = 13;

		public const byte _LF = 10;

		public const byte NEWLINE = 10;

		public static byte[] ACK_BUFF = new byte[]
		{
			6
		};

		public static byte[] ENQ_BUFF = new byte[]
		{
			5
		};

		public static byte[] NAK_BUFF = new byte[]
		{
			21
		};

		public static byte[] EOT_BUFF = new byte[]
		{
			4
		};
	}
}
