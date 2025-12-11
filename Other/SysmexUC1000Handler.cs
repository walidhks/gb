using System;
using System.Collections.Generic;
using System.Text;
using GbService.Model.Domain;
using NLog;

namespace GbService.Other
{
    // Define Result Class
    public class AnalysisResult
    {
        public string SampleCode { get; set; }
        public string AnalysisCode { get; set; }
        public string Value { get; set; }
        public DateTime Date { get; set; }
    }

    public class SysmexUC1000Handler
    {
        private static Logger logger = LogManager.GetCurrentClassLogger();

        public bool Parse(string input, out List<AnalysisResult> results)
        {
            results = new List<AnalysisResult>();

            if (string.IsNullOrEmpty(input)) return false;

            // 1. Clean and Split by Comma
            string cleanInput = input.Replace("\x02", "").Replace("\x03", "").Replace("\r", "").Replace("\n", "");
            string[] parts = cleanInput.Split(',');

            if (parts.Length < 8) return false;

            try
            {
                // 2. Get Barcode / Sequence
                string barcode = parts[0].Trim(); // "2512020446"
                string seqNo = parts[3].Trim();   // "N00000002"
                string sampleId = string.IsNullOrEmpty(barcode) ? seqNo : barcode;

                // 3. Map Tests (Order based on your log)
                // Log Index 8 starts the results.
                string[] testCodes = { "URO", "BLD", "BIL", "KET", "GLU", "PRO", "PH", "NIT", "LEU", "SG", "CRE", "ALB", "PC", "AC", "SG_REF" };
                int startResultIndex = 8;

                for (int i = 0; i < testCodes.Length; i++)
                {
                    int index = startResultIndex + i;
                    if (index < parts.Length)
                    {
                        string raw = parts[index]; // e.g. "0 normal      93.7"
                        if (raw.Length > 8)
                        {
                            // Parse "0 normal      93.7" -> Qual="normal", Conc="93.7"
                            // Based on fixed width inside the comma-separated block
                            // Skip State(1)+Mark(1) = 2 chars
                            string qual = raw.Substring(2, 6).Trim();
                            string conc = raw.Substring(8, Math.Min(6, raw.Length - 8)).Trim();

                            string val = string.IsNullOrEmpty(qual) ? conc : qual;

                            // Log and Add
                            logger.Info($"Parsed {testCodes[i]}: {val}");

                            results.Add(new AnalysisResult
                            {
                                SampleCode = sampleId,
                                AnalysisCode = testCodes[i],
                                Value = val,
                                Date = DateTime.Now
                            });
                        }
                    }
                }
                return true;
            }
            catch (Exception ex)
            {
                logger.Error("Sysmex Parse Error: " + ex.Message);
                return false;
            }
        }
    }
}