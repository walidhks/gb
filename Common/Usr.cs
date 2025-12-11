using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using GbService.Model.Domain;
using NLog;

namespace GbService.Common
{
	public class Usr
	{
		public static void Restart(Instrument instrument, string user = "admin", string pass = "admin")
		{
			try
			{
				string usrMac = instrument.UsrMac;
				string text = (usrMac != null) ? usrMac.Replace("-", "") : null;
				bool flag = string.IsNullOrWhiteSpace(text);
				if (flag)
				{
					Usr._logger.Info("mac null");
				}
				else
				{
					Usr._logger.Info("USR Try Restart");
					string text2 = user + "\0" + pass + "\0";
					List<byte> list = Usr.HexToByte("02" + text);
					list.Insert(0, (byte)(7 + text2.Length));
					list.AddRange(Encoding.ASCII.GetBytes(text2));
					list.Add(Usr.Convert(list));
					list.Insert(0, byte.MaxValue);
					UdpClient udpClient = new UdpClient(1500);
					string text3 = instrument.InstrumentPortName.Split(new char[]
					{
						','
					})[0] + "p";
					string ipString = text3.Replace(text3.Split(new char[]
					{
						'.'
					})[3], "255");
					IPEndPoint endPoint = new IPEndPoint(IPAddress.Parse(ipString), 1500);
					udpClient.Send(list.ToArray(), list.Count, endPoint);
					byte[] array = udpClient.Receive(ref endPoint);
					string @string = Encoding.ASCII.GetString(array, 0, array.Length);
					Usr._logger.Info("USR Restarted");
				}
			}
			catch (Exception ex)
			{
				Usr._logger.Info(new LogMessageGenerator(ex.ToString));
			}
		}

		public static byte Convert(List<byte> key)
		{
			return (byte)key.Aggregate(0, (int current, byte t) => current + (int)t);
		}

		private static List<byte> HexToByte(string msg)
		{
			msg = msg.Replace(" ", "");
			byte[] array = new byte[msg.Length / 2];
			for (int i = 0; i < msg.Length; i += 2)
			{
				array[i / 2] = System.Convert.ToByte(msg.Substring(i, 2), 16);
			}
			return array.ToList<byte>();
		}

		private static Logger _logger = LogManager.GetCurrentClassLogger();
	}
}
