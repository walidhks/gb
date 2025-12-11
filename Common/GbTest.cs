using System;
using GbService.ASTM;
using GbService.Model.Contexts;
using GbService.Model.Domain;
using NLog;

namespace GbService.Common
{
	internal class GbTest
	{
		public void ServiceThreadBody()
		{
			try
			{
				LaboContext laboContext = new LaboContext();
				GbTest._info.Info("start");
				Instrument instrument = laboContext.Instrument.Find(new object[]
				{
					8
				});
				bool flag = LogHelper.Init(instrument, "");
				AstmHigh.GetSamplesRange(DateTime.Now.AddDays(-2.0), DateTime.Now, laboContext, instrument);
				GbTest._info.Info("end");
			}
			catch
			{
			}
		}

		private static Logger _info = LogManager.GetLogger("Info");
	}
}
