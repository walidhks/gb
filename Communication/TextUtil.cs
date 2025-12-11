using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO.Ports;
using System.Linq;
using System.Text;
using GbService.ASTM;
using GbService.Common;
using GbService.Communication.Common;
using GbService.Communication.Serial;
using GbService.HL7;
using GbService.Model.Contexts;
using GbService.Model.Domain;
using Newtonsoft.Json.Linq;
using NLog;

namespace GbService.Communication
{
    public static class TextUtil
    {
        public static int? GetProp(string info, string prop)
        {
            bool flag = string.IsNullOrEmpty(info);
            int? result;
            if (flag)
            {
                result = null;
            }
            else
            {
                JObject jobject = JObject.Parse(info);
                JToken jtoken = jobject[prop];
                string str = (jtoken != null) ? jtoken.ToString() : null;
                result = TextUtil.GetInt(str);
            }
            return result;
        }

        public static Util GetUtil(Instrument instrument)
        {
            TextUtil._logger.Info(instrument.S3);
            List<string> list = TextUtil.SplitStr(instrument.S3);
            bool flag = list != null && list.Count == 6;
            Util result;
            if (flag)
            {
                result = new Util(list[0], list[1], list[2], list[3], list[4], list[5]);
            }
            else
            {
                result = null;
            }
            return result;
        }

        public static SerialManager SerialManager(Jihas jihas)
        {
            Instrument instrument = new Instrument((int)jihas, "9600,None,One,8,COM1", 0, null);
            bool logLow = LogHelper.Init(instrument, "");
            return new SerialManager(instrument.InstrumentPortName, instrument.Kind, logLow, null, Handshake.None, true, false, false);
        }

        public static AstmManager Instrument(Jihas jihas)
        {
            Instrument instrument = new Instrument((int)jihas, "9600,None,One,8,COM1", 0, null);
            bool logLow = LogHelper.Init(instrument, "");
            SerialManager il = new SerialManager(instrument.InstrumentPortName, instrument.Kind, logLow, null, Handshake.None, true, false, false);
            return new AstmManager(il, instrument);
        }

        public static Sample Sample(LaboContext db, string sCode, Instrument instrument)
        {
            long? sampleCode = TextUtil.Long(sCode);
            bool flag = sampleCode == null;
            Sample result;
            if (flag)
            {
                TextUtil._logger.Error(sCode + " not long");
                result = null;
            }
            else
            {
                DateTime today = DateTime.Today.Date;
                long lastAnalysisId = instrument.LastAnalysisId;
                TextUtil._logger.Info(string.Format("puissance = {0}", lastAnalysisId));
                bool flag2 = lastAnalysisId <= 0L || lastAnalysisId >= (long)ParamDictHelper.NumberPositionBarcode;
                Sample sample;
                if (flag2)
                {
                    sample = (from x in db.Sample
                              orderby x.SampleId descending
                              select x).FirstOrDefault((Sample s) => s.SampleCode == sampleCode);
                }
                else
                {
                    long p = (long)Math.Pow(10.0, (double)lastAnalysisId);
                    IQueryable<Sample> source = from s in db.Sample
                                                where s.SampleCode % (long?)p == sampleCode && s.DateCreated.Month == today.Month && s.DateCreated.Year == today.Year
                                                select s;
                    bool flag3 = ParamDictHelper.Reset == "jour";
                    if (flag3)
                    {
                        source = from s in source
                                 where s.DateCreated.Day == today.Day
                                 select s;
                    }
                    bool b = instrument.B2;
                    if (b)
                    {
                        TextUtil._logger.Info(string.Format("B2 = 1, InstrumentId = {0}", instrument.InstrumentId));
                        source = from s in source
                                 where (from x in s.Analysis
                                        select x.InstrumentId).Contains((int?)instrument.InstrumentId)
                                 select s;
                    }
                    else
                    {
                        long fnsId = ParamDictHelper.FnsId;
                        TextUtil._logger.Info(string.Format("fns = {0}", fnsId));
                        source = from s in source
                                 where (from x in s.Analysis
                                        select x.AnalysisTypeId).Contains(fnsId)
                                 select s;
                    }
                    sample = (from x in source
                              orderby x.SampleId descending
                              select x).FirstOrDefault<Sample>();
                }
                bool flag4 = sample == null;
                if (flag4)
                {
                    TextUtil._logger.Info(string.Format("Sample {0} not found!", sampleCode));
                }
                else
                {
                    TextUtil._logger.Info<long?>("Sample {0} found", sample.SampleCode);
                }
                result = sample;
            }
            return result;
        }

        public static string Prepare(string m)
        {
            m = m.Replace("<CR>", "\r");
            m = m.Replace("<SB>", '\v'.ToString());
            m = m.Replace("<EB>", '\u001c'.ToString());
            m = m.Replace(Tu.NL, "\r");
            return m;
        }

        public static string GetSource(long src)
        {
            List<long> list = new List<long>
            {
                8L,
                9L,
                10L
            };
            return (src == 7L) ? "WBL" : (list.Contains(src) ? "URI" : "SER");
        }

        public static int? GetInt(string str)
        {
            int value;
            return int.TryParse(str, out value) ? new int?(value) : null;
        }

        public static long? Long(string str)
        {
            long value;
            return long.TryParse((str != null) ? str.Replace("-", "") : null, out value) ? new long?(value) : null;
        }

        public static decimal? Decimal(string str)
        {
            decimal value;
            return decimal.TryParse((str != null) ? str.Replace(',', '.') : null, NumberStyles.Any, CultureInfo.InvariantCulture, out value) ? new decimal?(value) : null;
        }

        public static double? Double(string str)
        {
            double value;
            return double.TryParse((str != null) ? str.Replace(',', '.') : null, out value) ? new double?(value) : null;
        }

        public static string Result(string str)
        {
            str = str.Replace(',', '.');
            decimal num;
            return decimal.TryParse(str, NumberStyles.Any, CultureInfo.InvariantCulture, out num) ? num.ToString(new CultureInfo("en-US")) : str;
        }

        public static string Result(string str, Jihas kind)
        {
            str = str.Replace(',', '.');
            decimal d;
            return decimal.TryParse(str, NumberStyles.Any, CultureInfo.InvariantCulture, out d) ? decimal.Round(d, 3, MidpointRounding.AwayFromZero).ToString(new CultureInfo("en-US")) : (TextUtil.IgnoreList.Contains(kind) ? null : str);
        }

        public static IEnumerable<string> ChunksUpto(string str, int maxChunkSize)
        {
            for (int i = 0; i < str.Length; i += maxChunkSize)
            {
                yield return str.Substring(i, Math.Min(maxChunkSize, str.Length - i));
            }
            yield break;
        }

        public static List<string> Split(string str, int chunkSize)
        {
            return (from i in Enumerable.Range(0, str.Length / chunkSize)
                    select str.Substring(i * chunkSize, chunkSize)).ToList<string>();
        }

        public static List<int> SplitInt(string str)
        {
            List<int> result;
            try
            {
                str = str.Replace(',', '.');
                result = str.Split(new char[]
                {
                    '.'
                }).Select(new Func<string, int>(int.Parse)).ToList<int>();
            }
            catch (Exception ex)
            {
                result = null;
            }
            return result;
        }

        public static List<string> SplitStr(string str)
        {
            List<string> result;
            try
            {
                str = str.Replace(',', '.');
                result = str.Split(new char[]
                {
                    '.'
                }).ToList<string>();
            }
            catch (Exception ex)
            {
                result = null;
            }
            return result;
        }

        public static string UnicodeRepresentation(string s)
        {
            StringBuilder stringBuilder = new StringBuilder();
            foreach (char c in s)
            {
                int num = (int)c;
                string text = num.ToString("X4");
                stringBuilder.Append(text.Substring(2));
                stringBuilder.Append(text.Substring(0, 2));
            }
            return stringBuilder.ToString().ToUpper();
        }

        public static string Truncate(this string str, int maxLength)
        {
            return str.Substring(0, Math.Min(str.Length, maxLength));
        }

        public static DateTime Truncate(this DateTime dateTime, TimeSpan timeSpan)
        {
            return (timeSpan == TimeSpan.Zero) ? dateTime : dateTime.AddTicks(-(dateTime.Ticks % timeSpan.Ticks));
        }

        public static string GetRhesus(string s)
        {
            return (s == "POS") ? "+" : ((s == "NEG") ? "-" : "NULL");
        }

        public static string GetABO(string s)
        {
            return (s == "0") ? "O" : s;
        }

        public static JihazResult GetResult(string sid, string code, string value)
        {
            return new JihazResult(sid)
            {
                Results =
                {
                    new LowResult(code, TextUtil.Result(value), null, null, null)
                }
            };
        }

        public static JihazResult GetResult(List<string[]> records, string id, int index)
        {
            JihazResult jihazResult = new JihazResult(id);
            foreach (string[] array in records)
            {
                string text = array[0];
                bool flag = array.Length <= index;
                if (!flag)
                {
                    string text2 = TextUtil.Result(array[index]);
                    TextUtil._logger.Info(text + " : " + text2);
                    jihazResult.Results.Add(new LowResult(text, text2, null, null, null));
                }
            }
            return jihazResult;
        }

        public static string RemoveDiacritics(string text)
        {
            string text2 = text.Normalize(NormalizationForm.FormD);
            StringBuilder stringBuilder = new StringBuilder(text2.Length);
            foreach (char c in text2)
            {
                UnicodeCategory unicodeCategory = CharUnicodeInfo.GetUnicodeCategory(c);
                bool flag = unicodeCategory != UnicodeCategory.NonSpacingMark;
                if (flag)
                {
                    stringBuilder.Append(c);
                }
            }
            return stringBuilder.ToString().Normalize(NormalizationForm.FormC);
        }

        public static List<Mapping> Mappings(IEnumerable<string> codes, char sep)
        {
            List<Mapping> list = new List<Mapping>();
            foreach (string text in codes)
            {
                bool flag = text == null || !text.Contains(sep);
                if (!flag)
                {
                    string[] split = text.Split(new char[]
                    {
                        sep
                    });
                    int? num = TextUtil.Element(split, 0);
                    int? num2 = TextUtil.Element(split, 1);
                    bool flag2 = num == null || num2 == null;
                    if (!flag2)
                    {
                        list.Add(new Mapping(text, num.Value, num2.Value, TextUtil.Element(split, 2), TextUtil.Element(split, 3)));
                    }
                }
            }
            return (from x in list
                    orderby x.Order
                    select x).ToList<Mapping>();
        }

        private static int? Element(IList<string> split, int i)
        {
            return (split.Count > i) ? TextUtil.GetInt(split[i]) : null;
        }

        public static bool SampleContainResults(string scode, JihazResult expected)
        {
            return true;
        }

        public static DateTime GetDate(Sample sample, Instrument instrument)
        {
            return sample.DateCreated.AddDays((double)instrument.InstrumentDays);
        }

        public static DateTime Now
        {
            get
            {
                return DateTime.Now;
            }
        }

        public static List<Jihas> _hl7 = new List<Jihas>
        {
            Jihas.MindrayBs200,
            Jihas.Bs300,
            Jihas.Bs240,
            Jihas.Chemray240,
            Jihas.Icubio,
            Jihas.MindrayCL1000i,
            Jihas.Kt6610,
            Jihas.Medonic,
            Jihas.Rt7600S,
            Jihas.BC5380,
            Jihas.Biolis30i,
            Jihas.Urit8031,
            Jihas.ZybioExc200,
            Jihas.Urit3000,
            Jihas.YumizenH550,
            Jihas.I15,
            Jihas.HumaCount80,
            Jihas.F200,
            Jihas.CobasPro,
            Jihas.Cobas8000,
            Jihas.LabExpert,
            Jihas.C3100,
            Jihas.DiruiCsT180
            //jihas.autobio
        };

        public static List<Jihas> _hl7_2 = new List<Jihas>
        {
            Jihas.BC5150,
            Jihas.MindrayH50P,
            Jihas.Ichroma
        };

        private static Logger _logger = LogManager.GetCurrentClassLogger();

        private static List<Jihas> IgnoreList = new List<Jihas>
        {
            Jihas.Vitros350
        };

        public static DateTime _now = new DateTime(2020, 3, 25);
    }
}
