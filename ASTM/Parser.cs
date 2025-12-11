using System;
using System.Linq;
using System.Text.RegularExpressions;
using GbService.Communication.Common;
using NLog;

namespace GbService.ASTM
{
	public class Parser
	{
        public static ASTM_Message Parse(string data)
        {
            // Normalize line breaks
            data = data.Replace(Tu.NL, "\r");
            data = Regex.Replace(data, "\r([^HPQLCMOR])",
                (Match m) => m.Value.Substring(1));

            string[] lines = data.Split(new[] { "\r" }, StringSplitOptions.RemoveEmptyEntries);

            ASTM_Message astm_Message = null;
            ASTM_Record astm_Record = null;
            ASTM_Patient astm_Patient = null;

            foreach (string line in lines)
            {
                string[] fields = Regex.Split(line, "[|]");
                string recordType = fields.ElementAt(0);

                // --- Header --------------------------------------------------------
                if (recordType == "H")
                {
                    astm_Message = new ASTM_Message();
                    astm_Record = astm_Message;

                    astm_Message.parseData(line);
                    continue;
                }

                // Any record after H but before H present is invalid
                if (astm_Message == null)
                {
                    Parser._logger.Error("m == null");
                    return null;
                }

                // --- Patient -------------------------------------------------------
                if (recordType == "P")
                {
                    astm_Patient = new ASTM_Patient();
                    astm_Record = astm_Patient;

                    astm_Message.patientRecords.Add(astm_Patient);
                    astm_Patient.parseData(line);
                    continue;
                }

                // --- Request (Q) ---------------------------------------------------
                if (recordType == "Q")
                {
                    var req = new ASTM_Request();
                    astm_Message.requestRecords.Add(req);

                    astm_Record = req;
                    astm_Record.parseData(line);
                    continue;
                }

                // --- Terminator ----------------------------------------------------
                if (recordType == "L")
                {
                    return astm_Message;
                }

                // --- Comment -------------------------------------------------------
                if (recordType == "C")
                {
                    astm_Record.addComment(line);
                    continue;
                }

                // --- Manufacturer Info --------------------------------------------
                if (recordType == "M")
                {
                    astm_Record.addManufacturerInfo(line);
                    continue;
                }

                // --- Order (O) or Result (R) ---------------------------------------
                if (recordType == "O" || recordType == "R")
                {
                    if (astm_Patient == null)
                    {
                        Parser._logger.Error("patient == null");
                        return null;
                    }

                    if (recordType == "O")
                    {
                        var order = new AstmOrder { Patient = astm_Patient };
                        astm_Patient.OrderRecords.Add(order);

                        astm_Record = order;
                        astm_Record.parseData(line);
                        continue;
                    }

                    if (recordType == "R")
                    {
                        AstmOrder lastOrder = astm_Patient.OrderRecords.LastOrDefault();

                        if (lastOrder != null)
                        {
                            var result = new AstmResult();
                            lastOrder.ResultRecords.Add(result);

                            astm_Record = result;
                            astm_Record.parseData(line);
                        }
                        continue;
                    }
                }

                // --- Unknown Record ------------------------------------------------
                Parser._logger.Error("unknown record : " + recordType);
            }

            return null;
        }


        private static Logger _logger = LogManager.GetCurrentClassLogger();
	}
}
