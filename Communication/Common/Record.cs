using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using CsvHelper;

namespace GbService.Communication.Common
{
	public class Record
	{
		public static void ParseCsv()
		{
			using (StreamReader streamReader = new StreamReader("path\\to\\file.csv"))
			{
				using (CsvReader csvReader = new CsvReader(streamReader, CultureInfo.InvariantCulture, false))
				{
					IEnumerable<Record> records = csvReader.GetRecords<Record>();
				}
			}
		}

		public string Name;

		public string Age;
	}
}
