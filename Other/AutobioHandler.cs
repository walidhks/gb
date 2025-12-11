using System;
using System.Collections.Generic;
using System.Linq;
using GbService.Communication;
using GbService.Communication.Common;
using GbService.HL7;
using GbService.Model.Contexts;
using GbService.Model.Domain;
using NHapi.Base.Model;
using NHapi.Model.V231.Message;
using NHapi.Model.V231.Segment;
using NLog;

namespace GbService.Other
{
    public class AutobioMessageHandler
    {
        private static readonly Logger _log = LogManager.GetCurrentClassLogger();
        private readonly Instrument _instrument;
        private readonly ILowManager _lowManager;

        public AutobioMessageHandler(ILowManager lowManager, Instrument instrument)
        {
            _lowManager = lowManager;
            _instrument = instrument;
        }

        /// <summary>
        /// Handle incoming MLLP-wrapped HL7 message from AUTOBIO
        /// </summary>
        public void OnMessageReceived(string message)
        {
            try
            {
                // Remove MLLP framing: <VT>....<FS><CR>
                string hl7Message = Ct.Prepare(message, true);

                _log.Info($"[AUTOBIO] Received: {hl7Message}");

                // Determine message type
                if (hl7Message.Contains("ORU^R01"))
                {
                    HandleResultMessage(hl7Message);
                }
                else if (hl7Message.Contains("QRY^Q01"))
                {
                    HandleQueryMessage(hl7Message);
                }
                else
                {
                    _log.Warn($"[AUTOBIO] Unknown message type: {hl7Message}");
                }
            }
            catch (Exception ex)
            {
                _log.Error(ex, "[AUTOBIO] Error processing message");
            }
        }

        /// <summary>
        /// Handle ORU^R01 result messages (Command 5 or 7)
        /// </summary>
        private void HandleResultMessage(string hl7Message)
        {
            try
            {
                // Parse using NHapi
                var parser = new NHapi.Base.Parser.PipeParser();
                var message = parser.Parse(hl7Message) as ORU_R01;

                if (message == null)
                {
                    _log.Error("[AUTOBIO] Failed to parse ORU^R01");
                    SendAck("5", "AE", "Parse error");
                    return;
                }

                var msh = message.MSH;
                string commandWord = msh.MessageControlID.Value; // "5" or "7"
                string sampleBarcode = "";
                string patientNo = "";

                // Extract Patient ID if PID segment exists
                if (message.PATIENT_RESULTRepetitionsUsed > 0)
                {
                    var pid = message.GetPATIENT_RESULT(0).PATIENT.PID;
                    if (pid != null && pid.PatientIdentifierList.Length > 0)
                    {
                        patientNo = pid.PatientIdentifierList[0].IDNumber.Value;
                    }
                }

                // Extract OBR
                var obr = message.GetPATIENT_RESULT(0).ORDER_OBSERVATION.OBR;
                sampleBarcode = obr.PlacerOrderNumber.EntityIdentifier.Value;
                string sampleId = obr.FillerOrderNumber.EntityIdentifier.Value;

                _log.Info($"[AUTOBIO] Processing results for Sample: {sampleBarcode}, Patient: {patientNo}");

                // Extract OBX results
                var observationGroup = message.GetPATIENT_RESULT(0).ORDER_OBSERVATION;
                int obxCount = observationGroup.OBSERVATIONRepetitionsUsed;

                using (var db = new LaboContext())
                {
                    // Find sample
                    var sample = db.Samples.FirstOrDefault(s => s.SampleBarCode == sampleBarcode);
                    if (sample == null)
                    {
                        _log.Warn($"[AUTOBIO] Sample not found: {sampleBarcode}");
                        SendAck(commandWord, "AE", $"Sample {sampleBarcode} not found");
                        return;
                    }

                    for (int i = 0; i < obxCount; i++)
                    {
                        var obx = observationGroup.GetOBSERVATION(i).OBX;

                        string testCode = obx.ObservationIdentifier.Identifier.Value;
                        string testRequestId = obx.SetIDOBX.Value; // Only for command 5
                        string resultValue = obx.ObservationValue[0].Data.ToString();
                        string resultStatus = obx.ObservationResultStatus.Value; // "F"
                        DateTime resultTime = DateTime.ParseExact(
                            obx.DateTimeOfTheObservation.TimeOfAnEvent.Value,
                            "yyyyMMddHHmmss",
                            null);

                        // Find analysis request
                        var analysisRequest = db.AnalysisRequests
                            .Where(ar => ar.SampleId == sample.SampleId)
                            .Join(db.AnalysisTypeInstrumentMappings,
                                ar => ar.AnalysisTypeId,
                                map => map.AnalysisTypeId,
                                (ar, map) => new { ar, map })
                            .Where(x => x.map.InstrumentCode == _instrument.InstrumentCode
                                     && x.map.InstrumentTestCode == testCode)
                            .Select(x => x.ar)
                            .FirstOrDefault();

                        if (analysisRequest == null)
                        {
                            _log.Warn($"[AUTOBIO] No request found for test {testCode}");
                            continue;
                        }

                        // Create or update analysis
                        var analysis = db.Analysis
                            .FirstOrDefault(a => a.AnalysisRequestId == analysisRequest.AnalysisRequestId);

                        if (analysis == null)
                        {
                            analysis = new Analysis
                            {
                                AnalysisRequestId = analysisRequest.AnalysisRequestId,
                                InstrumentId = _instrument.InstrumentId
                            };
                            db.Analysis.Add(analysis);
                        }

                        analysis.Result = resultValue;
                        analysis.ResultDate = resultTime;
                        analysis.AnalysisState = AnalysisState.Validated;

                        _log.Info($"[AUTOBIO] Saved result: {testCode} = {resultValue}");
                    }

                    db.SaveChanges();
                }

                // Send ACK
                string ackText = commandWord == "5"
                    ? $"{sampleBarcode},{patientNo},testCode" // Adjust as needed
                    : $"{sampleBarcode},{patientNo}";

                SendAck(commandWord, "AA", ackText);
            }
            catch (Exception ex)
            {
                _log.Error(ex, "[AUTOBIO] Error handling result message");
                SendAck("5", "AE", ex.Message);
            }
        }

        /// <summary>
        /// Handle QRY^Q01 query messages (Command 1, 9, or 15)
        /// </summary>
        private void HandleQueryMessage(string hl7Message)
        {
            try
            {
                var parser = new NHapi.Base.Parser.PipeParser();
                var message = parser.Parse(hl7Message) as QRY_Q01;

                if (message == null)
                {
                    _log.Error("[AUTOBIO] Failed to parse QRY^Q01");
                    return;
                }

                var msh = message.MSH;
                var qrd = message.QRD;
                var qrf = message.QRF;

                string commandWord = msh.MessageControlID.Value; // "1", "9", or "15"
                string sampleId = qrd.WhoSubjectFilter[0].IDNumber.Value;

                _log.Info($"[AUTOBIO] Query command {commandWord} for sample: {sampleId}");

                using (var db = new LaboContext())
                {
                    // Find sample
                    var sample = db.Samples.FirstOrDefault(s => s.SampleBarCode == sampleId);

                    if (sample == null)
                    {
                        _log.Warn($"[AUTOBIO] Sample not found: {sampleId}");
                        SendQueryResponse(commandWord, sampleId, null, message);
                        return;
                    }

                    // Get analysis requests for this sample
                    var requests = db.AnalysisRequests
                        .Where(ar => ar.SampleId == sample.SampleId)
                        .ToList();

                    SendQueryResponse(commandWord, sampleId, sample, message);
                }
            }
            catch (Exception ex)
            {
                _log.Error(ex, "[AUTOBIO] Error handling query message");
            }
        }

        /// <summary>
        /// Send DSR^Q01 response with DSP segments
        /// </summary>
        private void SendQueryResponse(string commandWord, string sampleId, Sample sample, QRY_Q01 requestMessage)
        {
            var sb = new System.Text.StringBuilder();

            // MSH
            string timestamp = DateTime.Now.ToString("yyyyMMddHHmmss");
            string seqNum = DateTime.Now.ToString("yyMMddHHmmssfff");
            sb.Append($"MSH|^~\\&||Autobio|LIS||{timestamp}|DSR^Q01|{commandWord}|P|2.3.1||{seqNum}\r");

            // MSA
            string ackCode = sample != null ? "AA" : "AE";
            string errorCode = sample != null ? "0" : "1";
            sb.Append($"MSA|{ackCode}|{commandWord}|||{errorCode}\r");

            // ERR
            sb.Append($"ERR|{errorCode}\r");

            // QAK
            string qakStatus = sample != null ? "OK" : "NF";
            sb.Append($"QAK|SR|{qakStatus}\r");

            // Echo back QRD and QRF
            sb.Append(requestMessage.QRD.ToString().Replace("\n", "\r"));
            sb.Append(requestMessage.QRF.ToString().Replace("\n", "\r"));

            if (sample != null)
            {
                // Build DSP segments based on command word
                if (commandWord == "9")
                {
                    // Quick query format
                    sb.Append("DSP|1||1\r"); // Dilution factor
                    sb.Append("DSP|2||0\r"); // Priority (0=normal)
                    sb.Append("DSP|3||0\r"); // Sample type (0=serum)
                    sb.Append($"DSP|4||{sample.PatientId}\r"); // Patient No (if configured)

                    // Get tests
                    using (var db = new LaboContext())
                    {
                        var tests = db.AnalysisRequests
                            .Where(ar => ar.SampleId == sample.SampleId)
                            .Join(db.AnalysisTypeInstrumentMappings,
                                ar => ar.AnalysisTypeId,
                                map => map.AnalysisTypeId,
                                (ar, map) => new { ar, map })
                            .Where(x => x.map.InstrumentCode == _instrument.InstrumentCode)
                            .Select(x => new
                            {
                                TestCode = x.map.InstrumentTestCode,
                                Dilution = 1
                            })
                            .ToList();

                        sb.Append($"DSP|5||{tests.Count}\r"); // Quantity of tests

                        int dspIndex = 6;
                        foreach (var test in tests)
                        {
                            sb.Append($"DSP|{dspIndex++}||{test.TestCode}\r"); // Test code
                            sb.Append($"DSP|{dspIndex++}||{test.Dilution}\r"); // Dilution
                        }
                    }
                }
            }

            // Wrap in MLLP
            string response = $"\x0B{sb.ToString()}\x1C\r";

            _lowManager.Send(response);
            _log.Info($"[AUTOBIO] Sent query response for {sampleId}");
        }

        /// <summary>
        /// Send ACK^R01 acknowledgment
        /// </summary>
        private void SendAck(string commandWord, string ackCode, string textMessage)
        {
            string timestamp = DateTime.Now.ToString("yyyyMMddHHmmss");
            string seqNum = DateTime.Now.ToString("yyMMddHHmmssfff");

            var sb = new System.Text.StringBuilder();
            sb.Append($"MSH|^~\\&||Autobio|LIS||{timestamp}|ACK||{commandWord}|P|2.3.1||{seqNum}\r");
            sb.Append($"MSA|{ackCode}|{commandWord}|{textMessage}\r");

            string response = $"\x0B{sb.ToString()}\x1C\r";

            _lowManager.Send(response);
            _log.Info($"[AUTOBIO] Sent ACK: {ackCode}");
        }
    }
}
