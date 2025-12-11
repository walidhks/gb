using System;
using System.Collections.Generic;
using System.Globalization;   // NEW: for DateTime parsing
using System.Text;
using GbService.Model.Domain;
using NLog;

namespace GbService.Other
{
    // This is just a simple container we use inside the parser
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

        /// <summary>
        /// Parse one UC-1000 frame (one sample).
        /// input  = raw text from the instrument (may contain STX/ETX/CR/LF)
        /// results = list of (sample, testCode, value, date)
        /// returns true if parsing succeeded.
        /// </summary>
        public bool Parse(string input, out List<AnalysisResult> results)
        {
            results = new List<AnalysisResult>();

            if (string.IsNullOrWhiteSpace(input))
                return false;

            // 1) Remove STX / ETX / CR / LF  (same as before)
            string cleanInput = input
                .Replace("\x02", "")
                .Replace("\x03", "")
                .Replace("\r", "")
                .Replace("\n", "");

            // 2) Split by comma – this gives us:
            // [0] Barcode
            // [1] Rack
            // [2] Tube position
            // [3] Analysis series + number (e.g. "N00000001")
            // [4] Error code
            // [5] Strip name
            // [6] Date  (YYYY/MM/DD)
            // [7] Time  (HH:MM:SS)
            // [8] First result block (URO)
            string[] parts = cleanInput.Split(',');

            if (parts.Length < 8)
                return false;

            try
            {
                // 3) Decide sample ID (keep your logic)
                string barcode = parts[0].Trim();
                string seriesAndNo = parts[3].Trim(); // e.g. "N00000002"
                string sampleId = string.IsNullOrEmpty(barcode) ? seriesAndNo : barcode;

                // 4) Parse instrument date/time  (NEW)
                DateTime measureTime = DateTime.Now; // default fallback

                try
                {
                    string datePart = parts[6].Trim(); // "2015/04/02"
                    string timePart = parts[7].Trim(); // "14:17:50"

                    // Format from manual: YYYY/MM/DD and HH:MM:SS
                    // If parsing fails we just keep DateTime.Now
                    measureTime = DateTime.ParseExact(
                        datePart + " " + timePart,
                        "yyyy/MM/dd HH:mm:ss",
                        CultureInfo.InvariantCulture
                    );
                }
                catch (Exception dtEx)
                {
                    logger.Warn("UC1000 date/time parse failed. Using DateTime.Now. " + dtEx.Message);
                }

                // 5) Correct test order (this was the big issue)  // CHANGED
                // Order from the manual:
                // URO, BLD, BIL, KET, GLU, PRO, pH, NIT, LEU,
                // S.G (strip), CRE, ALB, P/C, A/C, S.G (refractometer)
                string[] testCodes =
                {
                    "URO",
                    "BLD",
                    "BIL",
                    "KET",
                    "GLU",
                    "PRO",
                    "PH",
                    "NIT",
                    "LEU",
                    "SG",      // strip S.G.
                    "CRE",
                    "ALB",
                    "PC",      // P/C ratio
                    "AC",      // A/C ratio
                    "SG_REF"   // refractometer S.G.
                };

                int startResultIndex = 8;

                // 6) Loop all test blocks
                for (int i = 0; i < testCodes.Length; i++)
                {
                    int index = startResultIndex + i;
                    if (index >= parts.Length)
                        break;

                    string raw = parts[index];

                    // We expect a fixed-width block at least long enough
                    if (string.IsNullOrWhiteSpace(raw) || raw.Length < 9)
                        continue;

                    // ---- NEW: check analysis state ("0" = analyzed) ----
                    char state = raw[0];
                    if (state != '0')
                    {
                        // Not analyzed – skip this test
                        continue;
                    }

                    // char[1] = comment mark, we ignore for now
                    // chars[2..7] = qualitative value (F)
                    // chars[8..13] = concentration (C)
                    string qual = raw.Substring(2, 6).Trim();
                    string conc = raw.Substring(8, Math.Min(6, raw.Length - 8)).Trim();

                    // If qualitative is empty, fall back to numeric
                    string val = !string.IsNullOrEmpty(qual) ? qual : conc;

                    if (string.IsNullOrEmpty(val))
                        continue;

                    logger.Info($"UC1000: Sample={sampleId}, Test={testCodes[i]}, Value={val}");

                    results.Add(new AnalysisResult
                    {
                        SampleCode = sampleId,
                        AnalysisCode = testCodes[i],
                        Value = val,
                        Date = measureTime   // CHANGED: real instrument time
                    });
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
