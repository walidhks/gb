using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Data.Entity.Infrastructure;
using System.Data.Entity.Validation;
using System.Data.SqlClient;
using System.Text;
using GbService.Model.Domain;
using NLog;

namespace GbService.Communication.Common
{
	public class Ct
	{
		public static Sample Sample(int instrumentCode)
		{
			Patient patient = new Patient
			{
				PatientNom = "zemmar",
				PatientPrenom = "sami",
				PatientSexe = new SexeEnum?(SexeEnum.Masculin),
				PatientID = 1L
			};
			AnalysisRequest analysisRequest = new AnalysisRequest
			{
				AnalysisRequestId = 7L,
				AnalysisRequestId2 = new long?(78L),
				Patient = patient
			};
			List<AnalysisTypeInstrumentMapping> analysisTypeInstrumentMappings = new List<AnalysisTypeInstrumentMapping>
			{
				new AnalysisTypeInstrumentMapping(instrumentCode, "groupage-AboRh")
			};
			List<AnalysisTypeInstrumentMapping> analysisTypeInstrumentMappings2 = new List<AnalysisTypeInstrumentMapping>
			{
				new AnalysisTypeInstrumentMapping(instrumentCode, "Pheno-GPH")
			};
			List<AnalysisTypeInstrumentMapping> analysisTypeInstrumentMappings3 = new List<AnalysisTypeInstrumentMapping>
			{
				new AnalysisTypeInstrumentMapping(instrumentCode, "-c")
			};
			AnalysisType analysisType = new AnalysisType
			{
				AnalysisTypeId = 1L,
				AnalysisTypeInstrumentMappings = analysisTypeInstrumentMappings
			};
			AnalysisType analysisType2 = new AnalysisType
			{
				AnalysisTypeId = 2L,
				AnalysisTypeInstrumentMappings = analysisTypeInstrumentMappings2
			};
			AnalysisType analysisType3 = new AnalysisType
			{
				AnalysisTypeId = 3L,
				AnalysisTypeInstrumentMappings = analysisTypeInstrumentMappings3
			};
			Sample sample = new Sample
			{
				SampleId = 11L,
				SampleCode = new long?(9000078L),
				DateCreated = DateTime.Today,
				AnalysisRequest = analysisRequest
			};
			Analysis item = new Analysis
			{
				AnalysisId = 1L,
				Sample = sample,
				AnalysisRequest = analysisRequest,
				AnalysisType = analysisType,
				InstrumentId = new int?(instrumentCode)
			};
			Analysis item2 = new Analysis
			{
				AnalysisId = 2L,
				Sample = sample,
				AnalysisRequest = analysisRequest,
				AnalysisType = analysisType2,
				InstrumentId = new int?(instrumentCode)
			};
			Analysis item3 = new Analysis
			{
				AnalysisId = 3L,
				Sample = sample,
				AnalysisRequest = analysisRequest,
				AnalysisType = analysisType3,
				InstrumentId = new int?(instrumentCode)
			};
			sample.Analysis.Add(item);
			sample.Analysis.Add(item2);
			sample.Analysis.Add(item3);
			return sample;
		}

		public static string Prepare(string m, bool replaceNl = true)
		{
			if (replaceNl)
			{
				m = m.Replace(Tu.NL, "\r");
			}
			m = m.Replace("<CR>", "\r");
			m = m.Replace("<LF>", '\n'.ToString());
			m = m.Replace("<STX>", '\u0002'.ToString());
			m = m.Replace("<ETX>", '\u0003'.ToString());
			m = m.Replace("<SB>", '\v'.ToString());
			m = m.Replace("<EB>", '\u001c'.ToString());
			m = m.Replace("<RS>", '\u001e'.ToString());
			m = m.Replace("<GS>", '\u001d'.ToString());
			return m;
		}

		public static void LogError(string message, Exception ex)
		{
			StringBuilder stringBuilder = new StringBuilder();
			DbUpdateException ex2 = ex as DbUpdateException;
			bool flag = ex2 != null;
			if (flag)
			{
				stringBuilder.AppendLine("Affected Entities:");
				foreach (DbEntityEntry dbEntityEntry in ex2.Entries)
				{
					stringBuilder.AppendLine(string.Format(" - Entity: {0}, State: {1}", dbEntityEntry.Entity.GetType().Name, dbEntityEntry.State));
				}
			}
			SqlException ex3 = ex.InnerException as SqlException;
			bool flag2 = ex3 != null;
			if (flag2)
			{
				stringBuilder.AppendLine(string.Format("SQL Error Number: {0}", ex3.Number));
				stringBuilder.AppendLine(string.Format("SQL Error State: {0}", ex3.State));
				stringBuilder.AppendLine(string.Format("SQL Error Class: {0}", ex3.Class));
				stringBuilder.AppendLine("SQL Server: " + ex3.Server);
				stringBuilder.AppendLine("SQL Procedure: " + ex3.Procedure);
				stringBuilder.AppendLine(string.Format("SQL Line Number: {0}", ex3.LineNumber));
			}
			Ct.Logger.Error(ex, message, new object[]
			{
				new
				{
					Context = stringBuilder.ToString()
				}
			});
		}

		public static void LogValidationError(string message, DbEntityValidationException ex)
		{
			StringBuilder stringBuilder = new StringBuilder();
			stringBuilder.AppendLine("Validation Errors:");
			foreach (DbEntityValidationResult dbEntityValidationResult in ex.EntityValidationErrors)
			{
				string name = dbEntityValidationResult.Entry.Entity.GetType().Name;
				EntityState state = dbEntityValidationResult.Entry.State;
				stringBuilder.AppendLine(string.Format(" - Entity: {0}, State: {1}", name, state));
				foreach (DbValidationError dbValidationError in dbEntityValidationResult.ValidationErrors)
				{
					stringBuilder.AppendLine("   Property: " + dbValidationError.PropertyName + ", Error: " + dbValidationError.ErrorMessage);
				}
			}
			Ct.Logger.Error(ex, message, new object[]
			{
				new
				{
					Context = stringBuilder.ToString()
				}
			});
		}

		private static Logger Logger = LogManager.GetCurrentClassLogger();
	}
}
