using System;
using System.Linq;
using GbService.ASTM;
using GbService.Communication;
using GbService.Model.Domain;

namespace GbService.Other
{
	public class AbxMessageHandler
	{
		public AbxMessageHandler(string message, Instrument instrument)
		{
			this.Message = message;
			this._instrument = instrument;
			this.ParseMessage();
		}

		private void ParseMessage()
		{
			string[] records = this.Message.Split(new char[]
			{
				'\r'
			});
			string valueById = this.GetValueById(records, 'u');
			JihazResult result = this.GetResult(records, valueById);
			AstmHigh.LoadResults(result, this._instrument, null);
		}

		public JihazResult GetResult(string[] records, string sampleCode)
		{
			JihazResult jihazResult = new JihazResult(sampleCode);
			foreach (string text in records)
			{
				bool flag = text.Length < 3;
				if (!flag)
				{
					char c = text[0];
					string text2 = text.Substring(2);
					bool flag2 = text2.Length > 2;
					if (flag2)
					{
						text2 = text2.Remove(text2.Length - 2);
					}
					jihazResult.Results.Add(new LowResult(c.ToString(), text2, null, null, null));
				}
			}
			return jihazResult;
		}

		private string GetValueById(string[] records, char id)
		{
			string text = records.FirstOrDefault((string x) => x.First<char>() == id);
			return (text != null) ? text.Substring(2) : null;
		}

		public string Message;

		private Instrument _instrument;
	}
}
