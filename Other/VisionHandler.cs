using System;
using System.Collections.Generic;
using System.Linq;
using GbService.ASTM;
using GbService.Communication;
using GbService.Communication.Common;
using GbService.Model.Domain;
using NLog;

namespace GbService.Other
{
	public class VisionHandler
	{
		public VisionHandler(Instrument instrument, ILowManager il)
		{
			this._instrument = instrument;
			this._il = il;
		}

		public void HandleMessage(string m)
		{
			try
			{
				List<string> list = m.Split(new char[]
				{
					'\n'
				}, StringSplitOptions.RemoveEmptyEntries).ToList<string>();
				foreach (string m2 in list)
				{
					this.HandleRecord(m2);
				}
			}
			catch (Exception ex)
			{
				VisionHandler._logger.Error(ex.ToString());
			}
		}

		private void HandleRecord(string m)
		{
			string[] array = m.Split(new char[]
			{
				'\t'
			});
			int i = int.Parse(array[0]);
			string a = array[1];
			bool flag = a == "CONNECT";
			if (flag)
			{
				this._count = 0;
				this.Ack(i);
				this._il.SendLow(this._count.ToString() + "\tList\n", Coding.Asc);
				this._count++;
			}
			else
			{
				bool flag2 = a == "RESULTS";
				if (flag2)
				{
					string text = array[2];
					int num = (array.Length == 4) ? (int.Parse(text) - int.Parse(array[3])) : int.Parse(text);
					this.Ack(i);
					this._il.SendLow(string.Concat(new string[]
					{
						this._count.ToString(),
						"\tGet\t",
						num.ToString(),
						"\t",
						text,
						"\n"
					}), Coding.Asc);
					this._count++;
				}
				else
				{
					bool flag3 = a == "RESULT";
					if (flag3)
					{
						this.Ack(i);
						int num2 = (this._instrument.Mode == 1) ? 8 : 9;
						this.LoadOk(array[6], array[num2]);
					}
					else
					{
						bool flag4 = a == "ACK";
						if (flag4)
						{
						}
					}
				}
			}
		}

		private void LoadOk(string sid, string result)
		{
			JihazResult result2 = TextUtil.GetResult(sid, "VS", result);
			AstmHigh.LoadResults(result2, this._instrument, null);
		}

		private void Ack(int i)
		{
			this._il.SendLow(i.ToString() + "\tACK\n", Coding.Asc);
		}

		private static Logger _logger = LogManager.GetCurrentClassLogger();

		private ILowManager _il;

		private Instrument _instrument;

		private int _count;
	}
}
