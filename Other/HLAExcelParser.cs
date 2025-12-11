using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.CompilerServices;
using System.Text.RegularExpressions;
using NLog;
using NPOI.HSSF.UserModel;
using NPOI.SS.UserModel;
using NPOI.XSSF.UserModel;

namespace GbService.Other
{
	public class HLAExcelParser
	{
		public static Logger Logger
		{
			get
			{
				return LogManager.GetCurrentClassLogger();
			}
		}

		public HLAExcelParser(string path, string SampleIdColOrValue, string MappingCell, string DataCell, int dataStartsAt = 0)
		{
			this.path = path;
			this.SampleIdCol = SampleIdColOrValue;
			this.MappingCell = MappingCell;
			this.DataCell = DataCell;
			this.DataStartsAt = dataStartsAt;
		}

		public Dictionary<string, Dictionary<string, string>> Parse(bool deleteFile = false)
		{
			string[] files = Directory.GetFiles(this.path, "*.xls");
			Dictionary<string, Dictionary<string, string>> dictionary = new Dictionary<string, Dictionary<string, string>>();
			foreach (string text in files)
			{
				Dictionary<string, string> dictionary2 = this.ParseOldExcel(text);
				bool flag = dictionary2 == null;
				if (flag)
				{
					HLAExcelParser.Logger.Error("Invalid file format");
				}
				dictionary.Add(dictionary2["SampleId"] + " " + text, dictionary2);
				if (deleteFile)
				{
					File.Delete(text);
					HLAExcelParser.Logger.Info(text + " deleted");
				}
			}
			return dictionary;
		}

        public ValueTuple<int, int> GetPosition()
        {
            return (3, 5);
        }
        private static ValueTuple<int, int> ParseCellReference(string cellRef)
		{
			Match match = Regex.Match(cellRef, "^([A-Z]+)(\\d+)$");
			bool flag = !match.Success;
			if (flag)
			{
				throw new ArgumentException("Invalid cell reference format.");
			}
			string value = match.Groups[1].Value;
			int num = int.Parse(match.Groups[2].Value);
			return new ValueTuple<int, int>(HLAExcelParser.ColumnLetterToIndex(value), num - 1);
		}

		private static string ColumnIndexToLetter(int index)
		{
			string text = "";
			while (index >= 0)
			{
				text = ((char)(65 + index % 26)).ToString() + text;
				index = index / 26 - 1;
			}
			return text;
		}

		private static int ColumnLetterToIndex(string column)
		{
			int num = 0;
			foreach (char c in column.ToUpper())
			{
				num = num * 26 + (int)(c - 'A' + '\u0001');
			}
			return num - 1;
		}

        private static string GetCellValue(ICell cell, IFormulaEvaluator evaluator)
        {
            if (cell == null)
                return "";

            switch (cell.CellType)
            {
                case CellType.Numeric:
                    return cell.NumericCellValue.ToString();

                case CellType.String:
                    return cell.StringCellValue;

                case CellType.Formula:
                    return evaluator.Evaluate(cell).FormatAsString();

                case CellType.Blank:
                    return "";

                case CellType.Boolean:
                    return cell.BooleanCellValue.ToString();

                case CellType.Error:
                    return FormulaError.ForInt(cell.ErrorCellValue).String;

                default:
                    return "Unsupported Cell Type";
            }
        }


        public Dictionary<string, string> ParseOldExcel(string xpath)
		{
			Dictionary<string, string> result;
			using (FileStream fileStream = new FileStream(xpath, FileMode.Open, FileAccess.Read, FileShare.Read))
			{
				bool flag = xpath.EndsWith("xls");
				IWorkbook workbook;
				if (flag)
				{
					workbook = new HSSFWorkbook(fileStream);
				}
				else
				{
					workbook = new XSSFWorkbook(fileStream);
				}
				ISheet sheetAt = workbook.GetSheetAt(0);
				IFormulaEvaluator evaluator = workbook.GetCreationHelper().CreateFormulaEvaluator();
				Dictionary<string, string> dictionary = new Dictionary<string, string>();
				ValueTuple<int, int> valueTuple = HLAExcelParser.ParseCellReference(this.SampleIdCol);
				string stringCellValue = sheetAt.GetRow(valueTuple.Item2).GetCell(valueTuple.Item1).StringCellValue;
				dictionary.Add("SampleId", string.IsNullOrEmpty(stringCellValue) ? this.SampleIdCol : stringCellValue);
				for (int i = this.DataStartsAt; i < sheetAt.LastRowNum; i++)
				{
					int num = HLAExcelParser.ColumnLetterToIndex(this.MappingCell);
					int num2 = HLAExcelParser.ColumnLetterToIndex(this.DataCell);
					string cellValue = HLAExcelParser.GetCellValue(sheetAt.GetRow(i).GetCell(num), evaluator);
					string text = (cellValue != null) ? cellValue.Replace(" ", "").Replace("-", "").Trim(new char[]
					{
						','
					}) : null;
					bool flag2 = string.IsNullOrEmpty(text);
					if (flag2)
					{
						HLAExcelParser.Logger.Warn("Empty mapping found in row " + i.ToString());
					}
					else
					{
						text = Regex.Replace(text, ",+", ",");
						string cellValue2 = HLAExcelParser.GetCellValue(sheetAt.GetRow(i).GetCell(num2), evaluator);
						bool flag3 = !dictionary.ContainsKey(text);
						if (flag3)
						{
							dictionary.Add(text, cellValue2);
						}
					}
				}
				result = dictionary;
			}
			return result;
		}

		private readonly string path;

		private readonly string SampleIdCol = "A1";

		private readonly string MappingCell = "LA";

		private readonly string DataCell = "FA";

		private readonly int DataStartsAt = 1;
	}
}
