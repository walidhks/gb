
/*
using System;
using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;
using GbService.ASTM;
using GbService.Communication;
using GbService.Model.Domain;
using NHapi.Base;
using NLog;
using static System.Runtime.CompilerServices.RuntimeHelpers;

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
using System.Globalization;
using System.Text;
using GbService.ASTM;
using GbService.Communication;
using GbService.Model.Domain;
using NLog;

namespace GbService.Other
{
    public class LifotronicHandler
    {
        private static readonly Logger _logger = LogManager.GetCurrentClassLogger();

        /// <summary>
        /// Parse one Lifotronic H9 result message.
        /// </summary>
        public static void Parse(string msg, Instrument instrument)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(msg))
                    return;

                // 1) Remove STX (0x02) and ETX (0x03) framing characters
                string cleanMsg = msg
                    .Replace("\u0002", string.Empty)
                    .Replace("\u0003", string.Empty)
                    .Trim();

                // Message must start with 'S' (sample record)
                if (string.IsNullOrEmpty(cleanMsg) || cleanMsg[0] != 'S')
                    return;

                int cursor = 1; // skip 'S'

                // 2) Skip header: Version(2) + ParamCount(2) + Format(2) = 6 chars
                if (cursor + 6 > cleanMsg.Length)
                    return;
                cursor += 6;

                // 3) Sample ID length (2 chars)
                if (cursor + 2 > cleanMsg.Length)
                    return;

                string lenStr = cleanMsg.Substring(cursor, 2);
                int idLen;
                if (!int.TryParse(lenStr, out idLen) || idLen <= 0)
                    idLen = 12; // fallback if something is wrong

                cursor += 2;

                // 4) Sample ID (idLen chars)
                if (cursor + idLen > cleanMsg.Length)
                    return;

                string sampleId = cleanMsg.Substring(cursor, idLen).Trim();
                cursor += idLen;

                _logger.Info("H9 ID: " + sampleId);

                var result = new JihazResult(sampleId);

                // 5) Skip demographics:
                // Rack(4) + Pos(2) + RunNo(4) + Type(1) + DateTime(12) = 23 chars
                if (cursor + 23 > cleanMsg.Length)
                    return;
                cursor += 23;

                // 6) Skip times for 6 variants: 6 * 2 chars
                if (cursor + 12 > cleanMsg.Length)
                    return;
                cursor += 12;

                // 7) Skip absorbance for 6 variants: 6 * 6 chars
                if (cursor + 36 > cleanMsg.Length)
                    return;
                cursor += 36;

                // 8) Skip peak areas for 6 variants: 6 * 6 chars
                // (firmware sends 6 chars here)
                if (cursor + 36 > cleanMsg.Length)
                    return;
                cursor += 36;

                // 9) Peak area ratios (the % values you actually care about)
                // Order: HbA1a, HbA1b, HbF, LA1c, HbA1c (4 chars each),
                // then HbA0 (5 chars)
                string[] variants = { "HbA1a", "HbA1b", "HbF", "LA1c", "HbA1c" };

                foreach (string v in variants)
                {
                    if (cursor + 4 > cleanMsg.Length)
                        break;

                    string val = cleanMsg.Substring(cursor, 4);
                    result.Results.Add(new LowResult(v, val.Trim(), "%", null, null));
                    cursor += 4;
                }

                // HbA0 (5 chars, e.g. "89.60")
                if (cursor + 5 <= cleanMsg.Length)
                {
                    string valA0 = cleanMsg.Substring(cursor, 5);
                    result.Results.Add(new LowResult("HbA0", valA0.Trim(), "%", null, null));
                    cursor += 5;
                }

                // 10) Main results after the ratios:
                // HbA1c IFCC (5 chars)
                if (cursor + 5 <= cleanMsg.Length)
                {
                    string ifcc = cleanMsg.Substring(cursor, 5);
                    result.Results.Add(
                        new LowResult("HbA1c_IFCC", ifcc.Trim(), "mmol/mol", null, null));
                    cursor += 5;
                }

                // eAG mmol/L (4 chars)
                if (cursor + 4 <= cleanMsg.Length)
                {
                    string gluMmol = cleanMsg.Substring(cursor, 4);
                    result.Results.Add(
                        new LowResult("eAG_mmol/l", gluMmol.Trim(), "mmol/L", null, null));
                    cursor += 4;
                }

                // eAG mg/dL (4 chars)
                if (cursor + 4 <= cleanMsg.Length)
                {
                    string gluMg = cleanMsg.Substring(cursor, 4);
                    result.Results.Add(
                        new LowResult("eAG_Mg/dl", gluMg.Trim(), "mg/dL", null, null));
                    cursor += 4;
                }

                // 11) Chromatogram curve (graph) ------------------------------
                // Next 3 chars = number of points
                if (cursor + 3 <= cleanMsg.Length)
                {
                    string pointsCountStr = cleanMsg.Substring(cursor, 3);
                    cursor += 3;

                    int pointsCount;
                    if (int.TryParse(pointsCountStr, out pointsCount) && pointsCount > 0)
                    {
                        const int pointWidth = 7;   // each point: "0.00300"
                        var graphData = new StringBuilder();

                        // We store a simple format:
                        // "Points:\t{count}\tY0\tY1\tY2\t..."
                        graphData.Append("Points:\t");
                        graphData.Append(pointsCount);

                        for (int i = 0; i < pointsCount; i++)
                        {
                            if (cursor + pointWidth > cleanMsg.Length)
                                break;

                            string rawVal = cleanMsg.Substring(cursor, pointWidth);
                            cursor += pointWidth;

                            decimal val;
                            if (decimal.TryParse(
                                    rawVal,
                                    NumberStyles.Float,
                                    CultureInfo.InvariantCulture,
                                    out val))
                            {
                                // Scale so 0.00300 -> 30 (easier for plotting)
                                int intVal = (int)(val * 10000m);
                                graphData.Append('\t');
                                graphData.Append(intVal);
                            }
                            else
                            {
                                graphData.Append('\t');
                                graphData.Append('0');
                            }
                        }

                        result.Results.Add(
                            new LowResult("Graph", graphData.ToString(), null, null, null));
                    }
                }

                // 12) Error code (1 char) ------------------------------------
                string errorCode = null;

                if (cursor + 1 <= cleanMsg.Length)
                {
                    errorCode = cleanMsg.Substring(cursor, 1);
                }
                else if (cleanMsg.Length > 0)
                {
                    // Fallback: last char of the message
                    errorCode = cleanMsg.Substring(cleanMsg.Length - 1);
                }

                if (!string.IsNullOrEmpty(errorCode))
                {
                    result.Results.Add(
                        new LowResult("Error_Code", errorCode, null, null, null));
                }

                // 13) Send everything to the common ASTM handler
                AstmHigh.LoadResults(result, instrument, null);
            }
            catch (Exception ex)
            {
                _logger.Error("H9 Parse Error: " + ex.Message);
            }
        }
    }
}
