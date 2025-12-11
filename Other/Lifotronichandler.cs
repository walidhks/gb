/*using System;
using System.Collections.Generic;
using GbService.ASTM;
using GbService.Communication;
using GbService.Model.Domain;
using NLog;

namespace GbService.Other
{
    public class LifotronicHandler
    {
        private static Logger _logger = LogManager.GetCurrentClassLogger();

        public static void Parse(string msg, Instrument instrument)
        {
            try
            {
                // 1. Clean Framing (STX 0x02, ETX 0x03)
                string cleanMsg = msg.Replace("\u0002", "").Replace("\u0003", "").Trim();
                // _logger.Info("H9 Raw: " + cleanMsg);

                 // Validate Start (S = Sample) [cite: 1821]
                if (string.IsNullOrEmpty(cleanMsg) || cleanMsg[0] != 'S') return;

                int cursor = 1; // Skip 'S'

                 // Skip: Version(2) + NumParams(2) + Format(2) = 6 chars [cite: 1821]
                cursor += 6;

                 // --- 2. Get Sample ID Length (2 chars) --- [cite: 1821]
                string lenStr = cleanMsg.Substring(cursor, 2);
                int idLen = 0;
                int.TryParse(lenStr, out idLen);
                cursor += 2;

                // [FIX] Force 12 characters if parsing fails or is zero, as per your request
                if (idLen == 0) idLen = 12;

                 // --- 3. Get Sample ID --- [cite: 1821]
                string sampleId = cleanMsg.Substring(cursor, idLen).Trim();
                cursor += idLen;

                _logger.Info("H9 Parsed ID: " + sampleId); // CORRECT
                JihazResult result = new JihazResult(sampleId);

                 // --- 4. Skip Demographics --- [cite: 1821]
                // RackNo(4) + Pos(2) + RunningNo(4) + BloodType(1) = 11
                // Date: Year(2)+Month(2)+Day(2)+Hour(2)+Min(2)+Sec(2) = 12
                // Total Skip = 23 chars
                cursor += 23;

                // Define Variants in Order (Manual C.3 Page 82-83)
                string[] variants = { "HbA1a", "HbA1b", "HbF", "LA1c", "HbA1c", "HbA0" };

                // --- 5. Parse Appearance Times (6 fields * 2 chars) --- [cite: 1821]
                // Manual says ## (2 chars) for each
                foreach (string v in variants)
                {
                    string val = cleanMsg.Substring(cursor, 2);
                    result.Results.Add(new LowResult(v + "_Time", val, "s", null, null));
                    cursor += 2;
                }

                // --- 6. Parse Absorbance (6 fields * 6 chars) --- [cite: 1821]
                // Manual says #.#### (6 chars)
                foreach (string v in variants)
                {
                    string val = cleanMsg.Substring(cursor, 6);
                    result.Results.Add(new LowResult(v + "_Abs", val.Trim(), "Abs", null, null));
                    cursor += 6;
                }

                // --- 7. Parse Peak Area (6 fields * 7 chars) --- [cite: 1821]
                // Manual says ###.### (7 chars)
                foreach (string v in variants)
                {
                    string val = cleanMsg.Substring(cursor, 7);
                    result.Results.Add(new LowResult(v + "_Area", val.Trim(), null, null, null));
                    cursor += 7;
                }

                // --- 8. Parse Peak Area Ratio (6 fields * 5 chars) --- [cite: 1821]
                // Manual says ##.## (5 chars)
                foreach (string v in variants)
                {
                    string val = cleanMsg.Substring(cursor, 5);
                    result.Results.Add(new LowResult(v + "_Ratio", val.Trim(), "%", null, null));
                    cursor += 5;
                }

                // --- 9. Main Results --- [cite: 1828]
                // HbA1c IFCC (###.# = 5 chars)
                string ifcc = cleanMsg.Substring(cursor, 5);
                result.Results.Add(new LowResult("HbA1c_IFCC", ifcc.Trim(), "mmol/mol", null, null));
                cursor += 5;

                // Average Glucose mmol/L (##.# = 4 chars) - Manual Page 83
                string gluMmol = cleanMsg.Substring(cursor, 4);
                result.Results.Add(new LowResult("eAG_mmol", gluMmol.Trim(), "mmol/L", null, null));
                cursor += 4;

                // Average Glucose mg/dl (#### = 4 chars) - Manual Page 83
                string gluMg = cleanMsg.Substring(cursor, 4);
                result.Results.Add(new LowResult("eAG", gluMg.Trim(), "mg/dL", null, null));
                cursor += 4;

                // NOTE: The main HbA1c NGSP % is often derived from the HbA1c Ratio parsed in Step 8.
                // If there is a specific NGSP field not listed or in a different spot on your firmware version,
                // you can map "HbA1c_Ratio" to your HbA1c test in the database.
                // Or check if one of the "eAG" fields is actually NGSP on your screen.

                // Save All
                AstmHigh.LoadResults(result, instrument, null);
            }
            catch (Exception ex)
            {
                _logger.Error("H9 Parse Error: " + ex.Message);
            }
        }
    }
}*/
/*using System;
using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;
using GbService.ASTM;
using GbService.Communication;
using GbService.Model.Domain;
using NLog;

namespace GbService.Other
{
    public class LifotronicHandler
    {
        private static Logger _logger = LogManager.GetCurrentClassLogger();

        public static void Parse(string msg, Instrument instrument)
        {
            try
            {
                // 1. Clean Framing
                string cleanMsg = msg.Replace("\u0002", "").Replace("\u0003", "").Trim();

                // Validate Start
                if (string.IsNullOrEmpty(cleanMsg) || cleanMsg[0] != 'S') return;

                int cursor = 1; // Skip 'S'
                cursor += 6;    // Skip Header Info

                // --- 2. Get Sample ID ---
                // Read ID Length (2 chars)
                string lenStr = cleanMsg.Substring(cursor, 2);
                int idLen = 0;
                int.TryParse(lenStr, out idLen);
                if (idLen == 0) idLen = 12; // Fallback
                cursor += 2;

                string sampleId = cleanMsg.Substring(cursor, idLen).Trim();
                cursor += idLen;

                _logger.Info("H9 Parsed ID: " + sampleId);
                JihazResult result = new JihazResult(sampleId);

                // --- 3. Skip to Results Section ---
                // Demographics (23) + Times (12) + Absorbance (36) + Area (42)
                // Total Skip = 113 chars
                cursor += 113;

                // Safety check
                if (cursor >= cleanMsg.Length) return;

                // Get the Results Substring (The part with all the ratios)
                // Example: 01.200.801.201.806.488.6046.40
                string resultData = cleanMsg.Substring(cursor);

                // --- 4. Extract Values using Regex ---
                // This finds numbers like "1.20", "0.80", "88.60" automatically
                MatchCollection matches = Regex.Matches(resultData, @"\d+\.\d+");

                // We expect at least 7 values (Variants + IFCC)
                if (matches.Count >= 7)
                {
                    // 1. HbA1a (Index 0)
                    result.Results.Add(new LowResult("HbA1a", matches[0].Value, "%", null, null));

                    // 2. HbA1b (Index 1)
                    result.Results.Add(new LowResult("HbA1b", matches[1].Value, "%", null, null));

                    // 3. HbF (Index 2)
                    result.Results.Add(new LowResult("HbF", matches[2].Value, "%", null, null));

                    // 4. LA1c (Index 3)
                    result.Results.Add(new LowResult("LA1c", matches[3].Value, "%", null, null));

                    // 5. HbA1c (Index 4) - The Main Result
                    result.Results.Add(new LowResult("HbA1c", matches[4].Value, "%", null, null));

                    // 6. HbA0 (Index 5)
                    result.Results.Add(new LowResult("HbA0", matches[5].Value, "%", null, null));

                    // 7. IFCC (Index 6)
                    result.Results.Add(new LowResult("HbA1c_IFCC", matches[6].Value, "mmol/mol", null, null));
                }

                // 8. eAG (Index 7? - Optional check)
                if (matches.Count > 7)
                {
                    result.Results.Add(new LowResult("eAG", matches[7].Value, "mg/dL", null, null));
                }

                // Save to Database
                AstmHigh.LoadResults(result, instrument, null);
            }
            catch (Exception ex)
            {
                _logger.Error("H9 Parse Error: " + ex.Message);
            }
        }
    }
}*/

using System;
using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;
using GbService.ASTM;
using GbService.Communication;
using GbService.Model.Domain;
using NLog;

namespace GbService.Other
{
    public class LifotronicHandler
    {
        private static Logger _logger = LogManager.GetCurrentClassLogger();

public static void Parse(string msg, Instrument instrument)
{
    try
    {
        // 1. Clean Framing
        string cleanMsg = msg.Replace("\u0002", "").Replace("\u0003", "").Trim();
        if (string.IsNullOrEmpty(cleanMsg) || cleanMsg[0] != 'S') return;

        int cursor = 1; // Skip 'S'
        cursor += 6;    // Header Info

        // --- 2. Get Sample ID ---
        // Read Length (2 chars)
        string lenStr = cleanMsg.Substring(cursor, 2);
        int idLen = 0;
        int.TryParse(lenStr, out idLen);
        if (idLen == 0) idLen = 12; // Fallback
        cursor += 2;

        string sampleId = cleanMsg.Substring(cursor, idLen).Trim();
        cursor += idLen;

        _logger.Info("H9 ID: " + sampleId);
        JihazResult result = new JihazResult(sampleId);

        // --- 3. Skip Demographics ---
        // Rack(4)+Pos(2)+Run(4)+Type(1)+Date(12) = 23 chars
        cursor += 23;

        string[] variants = { "HbA1a", "HbA1b", "HbF", "LA1c", "HbA1c" };

        // --- 4. Skip Times (6 variants * 2 chars) ---
        cursor += 12;

        // --- 5. Skip Absorbance (6 variants * 6 chars) ---
        // Data: 0.0025...
        cursor += 36;

        // --- 6. Skip Peak Area (6 variants * 6 chars) ---
        // Data: 00.161... (Note: Manual said 7, but log shows 6)
        cursor += 36;

        // --- 7. Parse Peak Area Ratios (VARIANTS) ---
        // First 5 variants are 4 chars each (e.g. "01.6", "09.6")
        foreach (string v in variants)
        {
            if (cursor + 4 > cleanMsg.Length) break;
            string val = cleanMsg.Substring(cursor, 4);
            result.Results.Add(new LowResult(v, val.Trim(), "%", null, null));
            cursor += 4;
        }

        // HbA0 is 5 chars (e.g. "84.00")
        if (cursor + 5 <= cleanMsg.Length)
        {
            string valA0 = cleanMsg.Substring(cursor, 5);
            result.Results.Add(new LowResult("HbA0", valA0.Trim(), "%", null, null));
            cursor += 5;
        }

        // --- 8. Parse Main Results ---

        // HbA1c IFCC (5 chars) - e.g. "081.4"
        if (cursor + 5 <= cleanMsg.Length)
        {
            string ifcc = cleanMsg.Substring(cursor, 5);
            result.Results.Add(new LowResult("HbA1c_IFCC", ifcc.Trim(), "mmol/mol", null, null));
            cursor += 5;
        }

        // Average Glucose mmol/L (4 chars) - e.g. "12.6"
        if (cursor + 4 <= cleanMsg.Length)
        {
            string gluMmol = cleanMsg.Substring(cursor, 4);
            result.Results.Add(new LowResult("eAG_mmol/l", gluMmol.Trim(), "mmol/L", null, null));
            cursor += 4;
        }

        // Average Glucose mg/dL (4 chars) - e.g. "228."
        if (cursor + 4 <= cleanMsg.Length)
        {
            string gluMg = cleanMsg.Substring(cursor, 4);
            result.Results.Add(new LowResult("eAG_Mg/dl", gluMg.Trim(), "mg/dL", null, null));
            cursor += 4;
        }
                if (cursor + 3 <= cleanMsg.Length)
                {
                    string pointsCountStr = cleanMsg.Substring(cursor, 3);
                    cursor += 3;
                    int pointsCount = 0;
                    int.TryParse(pointsCountStr, out pointsCount);

                    if (pointsCount > 0)
                    {
                        StringBuilder graphData = new StringBuilder();
                        graphData.Append("Points:\t");

                        // Point width is 7 chars (e.g. 0.00300)
                        int pointWidth = 7;

                        for (int i = 0; i < pointsCount; i++)
                        {
                            if (cursor + pointWidth > cleanMsg.Length) break;

                            string rawVal = cleanMsg.Substring(cursor, pointWidth);
                            cursor += pointWidth;

                            if (decimal.TryParse(rawVal, out decimal val))
                            {
                                int intVal = (int)(val * 10000);
                                graphData.Append(intVal + "\t");
                            }
                            else
                            {
                                graphData.Append("0\t");
                            }
                        }
                        result.Results.Add(new LowResult("Graph", graphData.ToString(), null, null, null));
                    }
                }

                // --- 11. TEST ERROR CODE (New) ---
                 // [cite: 1840] "Test error code #" (1 char)
                // This is the last character before ETX
                if (cursor + 1 <= cleanMsg.Length)
                {
                    string errCode = cleanMsg.Substring(cursor, 1);
                    result.Results.Add(new LowResult("Error_Code", errCode, null, null, null));
                    // _logger.Info($"H9 Error Code: {errCode}");
                }
                else if (cleanMsg.Length > 0)
                {
                    // Fallback: Grab the very last character if cursor calculation drifted
                    string lastChar = cleanMsg.Substring(cleanMsg.Length - 1);
                    result.Results.Add(new LowResult("Error_Code", lastChar, null, null, null));
                }
                // Save to Database
                AstmHigh.LoadResults(result, instrument, null);
    }
    catch (Exception ex)
    {
        _logger.Error("H9 Parse Error: " + ex.Message);
            }
        }
    }
}