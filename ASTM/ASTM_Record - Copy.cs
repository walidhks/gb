using System;
using System.Collections.Generic;
using System.Text;

namespace GbService.ASTM
{
	public abstract class ASTM_Record
	{
		public abstract bool parseData(string recordData);

		public abstract bool addComment(string recordData);

		public abstract bool addManufacturerInfo(string recordData);

		public abstract string fieldName(int idx);

		public abstract string fieldValue(int idx);

		public abstract int fieldsCount();

		public abstract List<ASTM_Record> comments();

		public abstract List<ASTM_Record> manufacturerInfo();

		public virtual string EncodeRecord(int mode = 0, int requiredFields = 0)
		{
			StringBuilder stringBuilder = new StringBuilder();
			stringBuilder.Append(this.fieldValue(1));
			int num = this.fieldsCount();
			for (int i = 2; i <= num; i++)
			{
				stringBuilder.Append("|" + this.fieldValue(i));
			}
			bool flag = mode == 0;
			string result;
			if (flag)
			{
				StringBuilder stringBuilder2 = stringBuilder;
				result = ((stringBuilder2 != null) ? stringBuilder2.ToString() : null) + "<CR>";
			}
			else
			{
				string str = stringBuilder.ToString().TrimEnd(new char[]
				{
					'|'
				});
				bool flag2 = mode == 1;
				if (flag2)
				{
					result = str + "<CR>";
				}
				else
				{
					StringBuilder stringBuilder3 = (requiredFields > num) ? stringBuilder.Append(new string('|', requiredFields - num)) : stringBuilder;
					StringBuilder stringBuilder4 = stringBuilder3;
					result = ((stringBuilder4 != null) ? stringBuilder4.ToString() : null) + "<CR>";
				}
			}
			return result;
		}
	}
}
