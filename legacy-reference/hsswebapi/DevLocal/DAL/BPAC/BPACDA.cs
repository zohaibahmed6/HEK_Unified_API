using DAL.HelperClasses;
using Logger;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Data.SqlClient;
using System.Linq;

namespace DAL
{
    public class BPACDA
    {
        #region Authenticate and Validation

        public static string Authenticate(string userName, string password, out string error)
        {
            string nhiNumber = string.Empty;
            string patientID = string.Empty;
            error = string.Empty;

            return Authenticate(userName, password, nhiNumber, patientID, false, out error);
        }

        public static string Authenticate(string userName, string password, string nhiNumber, string patientID, bool isReferral, out string error)
        {
            DataTable dtResult = new DataTable();
            string result = string.Empty;
            error = string.Empty;

            string connectionString = Convert.ToString(ConfigurationManager.ConnectionStrings["ConnMHNBPAC"].ConnectionString);

            List<SqlParameter> sqlParams = new List<SqlParameter>();
            sqlParams.Add(new SqlParameter("@pUserName", userName));
            sqlParams.Add(new SqlParameter("@pPassword", password));

            if (!string.IsNullOrEmpty(nhiNumber))
                sqlParams.Add(new SqlParameter("@pNHINumber", nhiNumber));

            if (!string.IsNullOrEmpty(patientID))
            {
                if (isReferral)
                    sqlParams.Add(new SqlParameter("@pAuthLogId", patientID));
                else
                    sqlParams.Add(new SqlParameter("@pPatientID", patientID));
            }

            if (!string.IsNullOrEmpty(userName) && !string.IsNullOrEmpty(password))
            {
                try
                {
                    dtResult = DALHelper.ExecuteDataTable(connectionString, CommandType.StoredProcedure, "uspHLBPACKAuthenticationInsertUpdate", sqlParams.ToArray());
                }
                catch (Exception ex)
                {
                    error = ex.Message;
                }
            }
            else
            {
                error = "Invalid credentials!";
            }

            if (dtResult.Rows.Count > 0)
            {
                //if (!dtResult.Columns.Contains("ERRORMESSAGE"))
                result = Convert.ToString(dtResult.Rows[0]["Token"]);
                //else if (dtResult.Columns.Contains("ERRORMESSAGE"))
                //throw new Exception(Convert.ToString(dtResult.Rows[0]["ERRORMESSAGE"]));
            }

            return result;
        }
        
        public static bool Validate(string sessionToken, out string error)
        {
            return Validate(sessionToken, string.Empty, out error);
        }

        public static bool Validate(string sessionToken, string userId, out string error)
        {
            DataTable dtResult = new DataTable();
            string result = string.Empty;
            error = string.Empty;

            string connectionString = Convert.ToString(ConfigurationManager.ConnectionStrings["ConnMHNBPAC"].ConnectionString);

            if (sessionToken.Length > 36)
                sessionToken = sessionToken.Substring(0, 36).TrimEnd(new char[] { '-' });

            List<SqlParameter> sqlParams = new List<SqlParameter>();
            sqlParams.Add(new SqlParameter("@pToken", sessionToken));

            if (!string.IsNullOrEmpty(userId))
                sqlParams.Add(new SqlParameter("@pUserID", userId));

            try
            {
                dtResult = DALHelper.ExecuteDataTable(connectionString, CommandType.StoredProcedure, "uspHLBPACKAuthenticationInsertUpdate", sqlParams.ToArray());

            }
            catch (Exception ex)
            {
                error = ex.Message;
            }

            return (dtResult.Rows.Count > 0);
        }

        #endregion

        #region ACC18 and ACC45

        public static DataSet ACC18GetAll(string acc45Number, out string error, string nhiNumber = "")
        {
            List<SqlParameter> sqlParams = new List<SqlParameter>();
            sqlParams.Add(new SqlParameter("@pACC45Number", acc45Number));

            return GetFinalResults("[Appointment].[uspACC18GetAll]", sqlParams, out error);
        }

        public static DataSet ACC45GetDemographic(string nhiNumber, string patientID, bool isTDHB, out string error)
        {
            List<SqlParameter> sqlParams = new List<SqlParameter>();

            //if (!string.IsNullOrEmpty(nhiNumber))
            //    sqlParams.Add(new SqlParameter("@pNHINumber", nhiNumber));

            if (!string.IsNullOrEmpty(patientID))
                sqlParams.Add(new SqlParameter("@pAuthLogId", patientID));

            if (!isTDHB)
                return GetFinalResults("[Appointment].[uspGetDemographicPMS]", sqlParams, out error);
            else
                return GetFinalResults("[TDHB].[uspGetDemographicPMS]", sqlParams, out error);
        }

        public static DataSet ACC45GetDemographic(string nhiNumber, string patientID, out string error)
        {
            error = string.Empty;
            return ACC45GetDemographic(nhiNumber, patientID, false, out error);
        }

        public static DataSet ACC45GetEthnicity(string patientID, bool isTDHB, out string error, string nhiNumber = "")
        {
            List<SqlParameter> sqlParams = new List<SqlParameter>();
            sqlParams.Add(new SqlParameter("@pPatientID", patientID));

            if (!string.IsNullOrEmpty(nhiNumber))
                sqlParams.Add(new SqlParameter("@pNHINumber", nhiNumber));

            if (!isTDHB)
                return GetFinalResults("[Appointment].[uspGetEthnicityPMS]", sqlParams, out error);
            else
                return GetFinalResults("[TDHB].[uspGetEthnicityPMS]", sqlParams, out error);
        }

        public static DataSet ACC45GetEthnicity(string patientID, out string error, string nhiNumber = "")
        {
            error = string.Empty;
            return ACC45GetEthnicity(patientID, false, out error, nhiNumber);
        }

        public static DataSet ACC45GetACCEvents(string nhiNumber, string patientID, bool isTDHB, out string error)
        {
            List<SqlParameter> sqlParams = new List<SqlParameter>();

            //if (!string.IsNullOrEmpty(nhiNumber))
            //    sqlParams.Add(new SqlParameter("@pNHINumber", nhiNumber));

            if (!string.IsNullOrEmpty(patientID))
                sqlParams.Add(new SqlParameter("@pAuthLogId", patientID));

            if (!isTDHB)
                return GetFinalResults("[Appointment].[uspGetACCEventsPMS]", sqlParams, out error);
            else
                return GetFinalResults("[TDHB].[uspGetACCEventsPMS]", sqlParams, out error);
        }

        public static DataSet ACC45GetACCEvents(string nhiNumber, string patientID, out string error)
        {
            error = string.Empty;
            return ACC45GetACCEvents(nhiNumber, patientID, false, out error);
        }

        public static DataSet ACC45GetRelevantAccident(string acc45Number, out string error, string nhiNumber = "")
        {
            List<SqlParameter> sqlParams = new List<SqlParameter>();
            sqlParams.Add(new SqlParameter("@pACC45Number", acc45Number));

            return GetFinalResults("[Appointment].[uspGetRelevantAccidentPMS]", sqlParams, out error);
        }

        public static DataSet ACC45GetConsultNote(string nhiNumber, string patientID, bool isTDHB, out string error)
        {
            List<SqlParameter> sqlParams = new List<SqlParameter>();

            //if (!string.IsNullOrEmpty(nhiNumber))
            //    sqlParams.Add(new SqlParameter("@pNHINumber", nhiNumber));

            if (!string.IsNullOrEmpty(patientID))
                sqlParams.Add(new SqlParameter("@pAuthLogId", patientID));

            if (!isTDHB)
                return GetFinalResults("[Appointment].[uspGetConsultNotePMS]", sqlParams, out error);
            else
                return GetFinalResults("[TDHB].[uspGetConsultNotePMS]", sqlParams, out error);
        }

        public static DataSet ACC45GetConsultNote(string nhiNumber, string patientID, out string error)
        {
            error = string.Empty;
            return ACC45GetConsultNote(nhiNumber, patientID, false, out error);
        }

        public static DataSet ACC45GetConsultDetail(string appointmentID, out string error)
        {
            List<SqlParameter> sqlParams = new List<SqlParameter>();
            sqlParams.Add(new SqlParameter("@pAppointmentID", appointmentID));

            return GetFinalResults("[Appointment].[uspACC45GetConsultDetail]", sqlParams, out error);
        }

        public static DataSet ACC45GetScreening(string nhiNumber, string patientID, bool isTDHB, out string error)
        {
            List<SqlParameter> sqlParams = new List<SqlParameter>();

            //if (!string.IsNullOrEmpty(nhiNumber))
            //    sqlParams.Add(new SqlParameter("@pNHINumber", nhiNumber));

            if (!string.IsNullOrEmpty(patientID))
                sqlParams.Add(new SqlParameter("@pAuthLogId", patientID));

            if (!isTDHB)
                return GetFinalResults("[Appointment].[uspGetScreeningPMS]", sqlParams, out error);
            else
                return GetFinalResults("[TDHB].[uspGetScreeningPMS]", sqlParams, out error);
        }
        public static DataSet ACC45GetScreening(string nhiNumber, string patientID, out string error)
        {
            error = string.Empty;
            return ACC45GetScreening(nhiNumber, patientID, false, out error);
        }

        public static DataSet ACC45GetScreeningDetail(string screeningID, out string error)
        {
            List<SqlParameter> sqlParams = new List<SqlParameter>();
            sqlParams.Add(new SqlParameter("@pScreeningID", screeningID));

            DataSet dsResult = GetFinalResults("[Appointment].[uspGetScreeningDetailPMS]", sqlParams, out error);

            if (dsResult.Tables.Count > 0)
            {
                try
                {
                    var ScreeningData = DownloadSerializedData<List<TermJson>>(Convert.ToString(dsResult.Tables[0].Rows[0]["measurement.value"]));

                    string tempData = string.Empty;
                    foreach (TermJson result in ScreeningData)
                    {
                        if (string.IsNullOrEmpty(tempData))
                            tempData = string.Format("{0} : {1}", result.label, result.value);
                        else
                            tempData = tempData + ", " + string.Format("{0} : {1}", result.label, result.value);
                    }

                    dsResult.Tables[0].Rows[0]["measurement.value"] = tempData;
                }
                catch (Exception ex)
                {
                    error = ex.Message;
                }
            }

            return dsResult;
        }
        public static DataSet ACC45GetScreeningWithDetail(string nhiNumber, string patientID, bool isTDHB, out string error)
        {
            DataSet dsResult = new DataSet();
            List<SqlParameter> sqlParams = new List<SqlParameter>();
            error = string.Empty;
            try
            {
                if (!string.IsNullOrEmpty(patientID))
                    sqlParams.Add(new SqlParameter("@pAuthLogId", patientID));

                if (!isTDHB)
                    dsResult = GetFinalResults("[Appointment].[uspGetScreeningPMS_OptimizedTest]", sqlParams, out error);
                else
                    dsResult = GetFinalResults("[TDHB].[uspGetScreeningPMS]", sqlParams, out error);

                if (dsResult.Tables.Count > 0)
                {
                    try
                    {
                        foreach (DataRow dr in dsResult.Tables[0].Rows)
                        {
                            string tempData = string.Empty;
                            var ScreeningData = DownloadSerializedData<List<TermJson>>(Convert.ToString(dr["measurement.value"]));
                            foreach (TermJson result in ScreeningData)
                            {
                                if (string.IsNullOrEmpty(tempData))
                                    tempData = string.Format("{0} : {1}", result.label, result.value);
                                else
                                    tempData = tempData + ", " + string.Format("{0} : {1}", result.label, result.value);
                            }
                            dr["measurement.value"] = tempData;
                        }
                    }
                    catch (Exception ex)
                    {
                        error = ex.Message;
                    }
                }
            }
            catch (Exception ex)
            {
                error = ex.Message;
                return null;
            }
            return dsResult;
        }

        public static DataSet ACC45GetMedicalWarning(string nhiNumber, string patientID, bool isTDHB, out string error)
        {
            List<SqlParameter> sqlParams = new List<SqlParameter>();

            //if (!string.IsNullOrEmpty(nhiNumber))
            //    sqlParams.Add(new SqlParameter("@pNHINumber", nhiNumber));

            if (!string.IsNullOrEmpty(patientID))
                sqlParams.Add(new SqlParameter("@pAuthLogId", patientID));

            if (!isTDHB)
                return GetFinalResults("[Appointment].[uspGetMedicalWarningPMS]", sqlParams, out error);
            else
                return GetFinalResults("[TDHB].[uspGetMedicalWarningPMS]", sqlParams, out error);

        }

        public static DataSet ACC45GetMedicalWarning(string nhiNumber, string patientID, out string error)
        {
            error = string.Empty;
            return ACC45GetMedicalWarning(nhiNumber, patientID, false, out error);
        }

        public static DataSet ACC45GetDiagnosis(string nhiNumber, string patientID, bool isTDHB, out string error)
        {
            List<SqlParameter> sqlParams = new List<SqlParameter>();

            //if (!string.IsNullOrEmpty(nhiNumber))
            //    sqlParams.Add(new SqlParameter("@pNHINumber", nhiNumber));

            if (!string.IsNullOrEmpty(patientID))
                sqlParams.Add(new SqlParameter("@pAuthLogId", patientID));

            if (!isTDHB)
                return GetFinalResults("[Appointment].[uspGetDiagnosisPMS]", sqlParams, out error);
            else
                return GetFinalResults("[TDHB].[uspGetDiagnosisPMS]", sqlParams, out error);
        }

        public static DataSet ACC45GetDiagnosis(string nhiNumber, string patientID, out string error)
        {
            error = string.Empty;
            return ACC45GetDiagnosis(nhiNumber, patientID, false, out error);
        }

        public static DataSet ACC45GetFamilyHistory(string nhiNumber, string patientID, bool isTDHB, out string error)
        {
            List<SqlParameter> sqlParams = new List<SqlParameter>();

            //if (!string.IsNullOrEmpty(nhiNumber))
            //    sqlParams.Add(new SqlParameter("@pNHINumber", nhiNumber));

            if (!string.IsNullOrEmpty(patientID))
                sqlParams.Add(new SqlParameter("@pAuthLogId", patientID));

            if (!isTDHB)
                return GetFinalResults("[Appointment].[uspGetFamilyHistoryPMS]", sqlParams, out error);
            else
                return GetFinalResults("[TDHB].[uspGetFamilyHistoryPMS]", sqlParams, out error);
        }

        public static DataSet ACC45GetFamilyHistory(string nhiNumber, string patientID, out string error)
        {
            error = string.Empty;
            return ACC45GetFamilyHistory(nhiNumber, patientID, false, out error);
        }

        public static DataSet ACC45GetLongTermMedication(string nhiNumber, string patientID, bool isTDHB, out string error)
        {
            List<SqlParameter> sqlParams = new List<SqlParameter>();

            //if (!string.IsNullOrEmpty(nhiNumber))
            //    sqlParams.Add(new SqlParameter("@pNHINumber", nhiNumber));

            if (!string.IsNullOrEmpty(patientID))
                sqlParams.Add(new SqlParameter("@pAuthLogId", patientID));

            if (!isTDHB)
                return GetFinalResults("[Appointment].[uspGetLongTermMedicationPMS]", sqlParams, out error);
            else
                return GetFinalResults("[TDHB].[uspGetLongTermMedicationPMS]", sqlParams, out error);
        }

        public static DataSet ACC45GetLongTermMedication(string nhiNumber, string patientID, out string error)
        {
            error = string.Empty;
            return ACC45GetLongTermMedication(nhiNumber, patientID, false, out error);
        }

        public static DataSet ACC45GetRecentMedication(string nhiNumber, string patientID, bool isTDHB, out string error)
        {
            List<SqlParameter> sqlParams = new List<SqlParameter>();

            //if (!string.IsNullOrEmpty(nhiNumber))
            //    sqlParams.Add(new SqlParameter("@pNHINumber", nhiNumber));

            if (!string.IsNullOrEmpty(patientID))
                sqlParams.Add(new SqlParameter("@pAuthLogId", patientID));

            if (!isTDHB)
                return GetFinalResults("[Appointment].[uspGetRecentMedicationPMS]", sqlParams, out error);
            else
                return GetFinalResults("[TDHB].[uspGetRecentMedicationPMS]", sqlParams, out error);
        }

        public static DataSet ACC45GetRecentMedication(string nhiNumber, string patientID, out string error)
        {
            error = string.Empty;
            return ACC45GetRecentMedication(nhiNumber, patientID, false, out error);
        }

        public static DataSet ACC45ReferralDetails(string nhiNumber, string patientID, bool isTDHB, out string error)
        {
            List<SqlParameter> sqlParams = new List<SqlParameter>();

            //if (!string.IsNullOrEmpty(nhiNumber))
            //    sqlParams.Add(new SqlParameter("@pNHINumber", nhiNumber));

            if (!string.IsNullOrEmpty(patientID))
                sqlParams.Add(new SqlParameter("@pAuthLogId", patientID));

            if (!isTDHB)
                return GetFinalResults("[Appointment].[uspGetReferralDetailPMS]", sqlParams, out error);
            else
                return GetFinalResults("[TDHB].[uspGetReferralDetailPMS]", sqlParams, out error);
        }

        public static DataSet ACC45ReferralDetails(string nhiNumber, string patientID, out string error)
        {
            error = string.Empty;
            return ACC45ReferralDetails(nhiNumber, patientID, false, out error);
        }

        public static DataSet ACC45Investigations(string nhiNumber, string patientID, bool isTDHB, out string error)
        {
            List<SqlParameter> sqlParams = new List<SqlParameter>();

            //if (!string.IsNullOrEmpty(nhiNumber))
            //    sqlParams.Add(new SqlParameter("@pNHINumber", nhiNumber));

            if (!string.IsNullOrEmpty(patientID))
                sqlParams.Add(new SqlParameter("@pAuthLogId", patientID));

            if (!isTDHB)
                return GetFinalResults("[Appointment].[uspGetInvestigationsPMS]", sqlParams, out error);
            else
                return GetFinalResults("[TDHB].[uspGetInvestigationsPMS]", sqlParams, out error);
        }

        public static DataSet ACC45Investigations(string nhiNumber, string patientID, out string error)
        {
            error = string.Empty;
            return ACC45Investigations(nhiNumber, patientID, false, out error);
        }

        #endregion

        #region Referral

        public static DataSet QueryReferal(string ReferalId, out string error)
        {
            List<SqlParameter> sqlParams = new List<SqlParameter>();
            sqlParams.Add(new SqlParameter("@ReferralID", ReferalId));

            return GetFinalResults("[Appointment].[uspGetReferalInfo]", sqlParams, out error);
        }

        public static bool ReferalStatus(string ReferalId, out string error)
        {
            List<SqlParameter> sqlParams = new List<SqlParameter>();
            sqlParams.Add(new SqlParameter("@ReferralID", ReferalId));

            if (GetFinalResults("[Appointment].[uspGetReferalInfo]", sqlParams, out error).Tables[0].Rows.Count > 0)
            {
                return true;
            }
            else
            {
                return false;
            }
        }

        #endregion

        #region Waikato DHB

        public static DataSet GetPatientDetailWKT(string nhiNumber, out string error)
        {
            DataSet dsResult = new DataSet();
            error = string.Empty;

            string connectionString = Convert.ToString(ConfigurationManager.ConnectionStrings["ConnMHNBPAC"].ConnectionString);

            try
            {
                List<SqlParameter> sqlParams = new List<SqlParameter>();
                sqlParams.Add(new SqlParameter("@pNHINO", nhiNumber));

                dsResult = DALHelper.ExecuteDataset(connectionString, CommandType.StoredProcedure, "Profile.uspGetPatientDetailWKT", sqlParams.ToArray());
            }
            catch (Exception ex)
            {
                error = ex.Message;
            }

            return dsResult;
        }

        #endregion

        #region Common Forms

        #region Get
        //PAKISTAN

        public static DataSet CFGetObservation(string patientId, string encounterId, out string error, string code = "", string date = "", string system = "", string providerId = "")
        {
            DataSet dsResult = new DataSet();
            error = string.Empty;

            string connectionString = Convert.ToString(ConfigurationManager.ConnectionStrings["ConnMHNBPAC"].ConnectionString);

            try
            {
                List<SqlParameter> sqlParams = new List<SqlParameter>();

                if (date.Trim().ToLower().Contains("recent") || date.Trim().ToLower().Contains("latest"))
                {
                    sqlParams.Add(new SqlParameter("@pDate", DBNull.Value));
                    sqlParams.Add(new SqlParameter("@pIsRecent", 1));
                }
                else if (!string.IsNullOrEmpty(date) && date.Trim().Contains("-"))
                    sqlParams.Add(new SqlParameter("@pDate", date.Trim()));
                else
                    sqlParams.Add(new SqlParameter("@pDate", DBNull.Value));

                if (!string.IsNullOrEmpty(code))
                    sqlParams.Add(new SqlParameter("@pScreeningCode", code.Trim()));

                if (!string.IsNullOrEmpty(patientId))
                    sqlParams.Add(new SqlParameter("@pPatientId", patientId.Trim()));

                if (!string.IsNullOrEmpty(encounterId))
                    sqlParams.Add(new SqlParameter("@pEncounterId", encounterId.Trim()));

                if (!string.IsNullOrEmpty(providerId))
                    sqlParams.Add(new SqlParameter("@pProviderID", providerId.Trim()));

                dsResult = DALHelper.ExecuteDataset(connectionString, CommandType.StoredProcedure, "[CommonForm].[uspGetObservation]", sqlParams.ToArray());
            }
            catch (Exception ex)
            {
                error = ex.Message;
            }

            return dsResult;
        }

        public static DataSet CFGetDemographic(string patientId, string encounterId, out string error, string providerId = "")
        {
            DataSet dsResult = new DataSet();
            error = string.Empty;

            string connectionString = Convert.ToString(ConfigurationManager.ConnectionStrings["ConnMHNBPAC"].ConnectionString);

            try
            {
                List<SqlParameter> sqlParams = new List<SqlParameter>();

                if (!string.IsNullOrEmpty(patientId))
                    sqlParams.Add(new SqlParameter("@pPatientId", patientId.Trim()));

                if (!string.IsNullOrEmpty(encounterId))
                    sqlParams.Add(new SqlParameter("@pEncounterId", encounterId.Trim()));

                if (!string.IsNullOrEmpty(providerId))
                    sqlParams.Add(new SqlParameter("@pProviderID", providerId.Trim()));

                dsResult = DALHelper.ExecuteDataset(connectionString, CommandType.StoredProcedure, "[CommonForm].[uspGetDemographic]", sqlParams.ToArray());
            }
            catch (Exception ex)
            {
                error = ex.Message;
            }

            return dsResult;
        }

        public static DataSet CFGetConditions(string patientId, string encounterId, out string error, string system = "", string providerId = "")
        {
            DataSet dsResult = new DataSet();
            error = string.Empty;

            string connectionString = Convert.ToString(ConfigurationManager.ConnectionStrings["ConnMHNBPAC"].ConnectionString);

            try
            {
                List<SqlParameter> sqlParams = new List<SqlParameter>();

                if (!string.IsNullOrEmpty(patientId))
                    sqlParams.Add(new SqlParameter("@pPatientId", patientId.Trim()));

                if (!string.IsNullOrEmpty(encounterId))
                    sqlParams.Add(new SqlParameter("@pEncounterId", encounterId.Trim()));

                if (!string.IsNullOrEmpty(providerId))
                    sqlParams.Add(new SqlParameter("@pProviderID", providerId.Trim()));

                dsResult = DALHelper.ExecuteDataset(connectionString, CommandType.StoredProcedure, "[CommonForm].[uspGetCondition]", sqlParams.ToArray());
            }
            catch (Exception ex)
            {
                error = ex.Message;
            }

            return dsResult;
        }

        public static DataSet CFGetFamilyHistory(string patientId, string encounterId, out string error, string system = "", string providerId = "")
        {
            DataSet dsResult = new DataSet();
            error = string.Empty;

            string connectionString = Convert.ToString(ConfigurationManager.ConnectionStrings["ConnMHNBPAC"].ConnectionString);

            try
            {
                List<SqlParameter> sqlParams = new List<SqlParameter>();

                if (!string.IsNullOrEmpty(patientId))
                    sqlParams.Add(new SqlParameter("@pPatientId", patientId.Trim()));

                if (!string.IsNullOrEmpty(encounterId))
                    sqlParams.Add(new SqlParameter("@pEncounterId", encounterId.Trim()));

                if (!string.IsNullOrEmpty(providerId))
                    sqlParams.Add(new SqlParameter("@pProviderID", providerId.Trim()));

                dsResult = DALHelper.ExecuteDataset(connectionString, CommandType.StoredProcedure, "[CommonForm].[uspGetFamilyHistory]", sqlParams.ToArray());
            }
            catch (Exception ex)
            {
                error = ex.Message;
            }

            return dsResult;
        }

        public static DataSet CFGetLabs(string patientId, string encounterId, DataTable dtFilterValues, out string error, string date = "", string system = "", string providerId = "")
        {
            DataSet dsResult = new DataSet();
            error = string.Empty;

            string connectionString = Convert.ToString(ConfigurationManager.ConnectionStrings["ConnMHNBPAC"].ConnectionString);

            try
            {
                List<SqlParameter> sqlParams = new List<SqlParameter>();

                if (date.Trim().ToLower().Contains("recent") || date.Trim().ToLower().Contains("latest"))
                {
                    sqlParams.Add(new SqlParameter("@pDate", DBNull.Value));
                    sqlParams.Add(new SqlParameter("@pIsRecent", 1));
                }
                else if (!string.IsNullOrEmpty(date) && date.Trim().Contains("-"))
                    sqlParams.Add(new SqlParameter("@pDate", date.Trim()));
                else
                    sqlParams.Add(new SqlParameter("@pDate", DBNull.Value));

                if (!string.IsNullOrEmpty(patientId))
                    sqlParams.Add(new SqlParameter("@pPatientId", patientId.Trim()));

                if (!string.IsNullOrEmpty(encounterId))
                    sqlParams.Add(new SqlParameter("@pEncounterId", encounterId.Trim()));

                if (!string.IsNullOrEmpty(providerId))
                    sqlParams.Add(new SqlParameter("@pProviderID", providerId.Trim()));

                sqlParams.Add(new SqlParameter("@pFilterValuesUDT", dtFilterValues));

                dsResult = DALHelper.ExecuteDataset(connectionString, CommandType.StoredProcedure, "[CommonForm].[uspGetDiagnosticLabs]", sqlParams.ToArray());
            }
            catch (Exception ex)
            {
                error = ex.Message;
            }

            return dsResult;
        }

        public static DataSet CFGetMedicines(string patientId, string encounterId, out string error, string date = "", string providerId = "")
        {
            DataSet dsResult = new DataSet();
            error = string.Empty;
            string connectionString = Convert.ToString(ConfigurationManager.ConnectionStrings["ConnMHNBPAC"].ConnectionString);

            try
            {
                List<SqlParameter> sqlParams = new List<SqlParameter>();

                if (date.Trim().ToLower().Contains("recent") || date.Trim().ToLower().Contains("latest"))
                {
                    sqlParams.Add(new SqlParameter("@pDate", DBNull.Value));
                    sqlParams.Add(new SqlParameter("@pIsRecent", 1));
                }
                else if (!string.IsNullOrEmpty(date) && date.Trim().Contains("-"))
                    sqlParams.Add(new SqlParameter("@pDate", date.Trim()));
                else
                    sqlParams.Add(new SqlParameter("@pDate", DBNull.Value));

                if (!string.IsNullOrEmpty(patientId))
                    sqlParams.Add(new SqlParameter("@pPatientId", patientId.Trim()));

                if (!string.IsNullOrEmpty(encounterId))
                    sqlParams.Add(new SqlParameter("@pEncounterId", encounterId.Trim()));

                if (!string.IsNullOrEmpty(providerId))
                    sqlParams.Add(new SqlParameter("@pProviderID", providerId.Trim()));

                dsResult = DALHelper.ExecuteDataset(connectionString, CommandType.StoredProcedure, "[CommonForm].[uspGetMedicines]", sqlParams.ToArray());
            }
            catch (Exception ex)
            {
                error = ex.Message;
            }

            return dsResult;
        }

        public static DataSet CFGetMedicalWarning(string patientId, string encounterId, out string error, string providerId = "")
        {
            DataSet dsResult = new DataSet();
            error = string.Empty;
            string connectionString = Convert.ToString(ConfigurationManager.ConnectionStrings["ConnMHNBPAC"].ConnectionString);

            try
            {
                List<SqlParameter> sqlParams = new List<SqlParameter>();

                if (!string.IsNullOrEmpty(patientId))
                    sqlParams.Add(new SqlParameter("@pPatientId", patientId.Trim()));

                if (!string.IsNullOrEmpty(encounterId))
                    sqlParams.Add(new SqlParameter("@pEncounterId", encounterId.Trim()));

                if (!string.IsNullOrEmpty(providerId))
                    sqlParams.Add(new SqlParameter("@pProviderID", providerId.Trim()));

                dsResult = DALHelper.ExecuteDataset(connectionString, CommandType.StoredProcedure, "[CommonForm].[uspGetMedicalWarning]", sqlParams.ToArray());
            }
            catch (Exception ex)
            {
                error = ex.Message;
            }

            return dsResult;
        }

        public static DataSet CFGetRecalls(string patientId, string encounterId, out string error, string purposeCode = "", string providerId = "")
        {
            DataSet dsResult = new DataSet();
            error = string.Empty;
            string connectionString = Convert.ToString(ConfigurationManager.ConnectionStrings["ConnMHNBPAC"].ConnectionString);

            try
            {
                List<SqlParameter> sqlParams = new List<SqlParameter>();

                if (!string.IsNullOrEmpty(purposeCode))
                    sqlParams.Add(new SqlParameter("@pCode", purposeCode.Trim()));

                if (!string.IsNullOrEmpty(patientId))
                    sqlParams.Add(new SqlParameter("@pPatientId", patientId.Trim()));

                if (!string.IsNullOrEmpty(encounterId))
                    sqlParams.Add(new SqlParameter("@pEncounterId", encounterId.Trim()));

                if (!string.IsNullOrEmpty(providerId))
                    sqlParams.Add(new SqlParameter("@pProviderID", providerId.Trim()));

                dsResult = DALHelper.ExecuteDataset(connectionString, CommandType.StoredProcedure, "[CommonForm].[uspGetRecalls]", sqlParams.ToArray());
            }
            catch (Exception ex)
            {
                error = ex.Message;
            }

            return dsResult;
        }

        public static DataSet CFGetSummary(string patientId, string encounterId, string identifierCode, out string error, string providerId = "")
        {
            DataSet dsResult = new DataSet();
            error = string.Empty;
            string connectionString = Convert.ToString(ConfigurationManager.ConnectionStrings["ConnMHNBPAC"].ConnectionString);

            try
            {
                List<SqlParameter> sqlParams = new List<SqlParameter>();

                if (!string.IsNullOrEmpty(patientId))
                    sqlParams.Add(new SqlParameter("@pPatientId", patientId.Trim()));

                if (!string.IsNullOrEmpty(encounterId))
                    sqlParams.Add(new SqlParameter("@pEncounterId", encounterId.Trim()));

                if (!string.IsNullOrEmpty(identifierCode))
                    sqlParams.Add(new SqlParameter("@pIdentifier", identifierCode.Trim()));

                if (!string.IsNullOrEmpty(providerId))
                    sqlParams.Add(new SqlParameter("@pProviderID", providerId.Trim()));

                //dsResult = DALHelper.ExecuteDataset(connectionString, CommandType.StoredProcedure, "[CommonForm].[uspGetSummary]", sqlParams.ToArray());
                dsResult = DALHelper.ExecuteDataset(connectionString, CommandType.StoredProcedure, "[CommonForm].[uspGetScreeningSummary]", sqlParams.ToArray());
            }
            catch (Exception ex)
            {
                error = ex.Message;
            }

            return dsResult;
        }

        public static DataSet CFGetConformance(out string error)
        {
            DataSet dsResult = new DataSet();
            error = string.Empty;
            string connectionString = Convert.ToString(ConfigurationManager.ConnectionStrings["ConnMHNBPAC"].ConnectionString);

            try
            {
                dsResult = DALHelper.ExecuteDataset(connectionString, CommandType.StoredProcedure, "CommonForm.uspGetConformance");
            }
            catch (Exception ex)
            {
                error = ex.Message;
            }

            return dsResult;
        }

        public static string GetTemplateByIdentifier(string patientId, string encounterID, string identifier)
        {
            string result = string.Empty;

            try
            {
                string connectionString = Convert.ToString(ConfigurationManager.ConnectionStrings["ConnMHNBPAC"].ConnectionString);

                List<SqlParameter> sqlParams = new List<SqlParameter>();

                if (!string.IsNullOrEmpty(patientId))
                    sqlParams.Add(new SqlParameter("@pPatientID", Convert.ToInt32(patientId)));

                if (!string.IsNullOrEmpty(encounterID))
                    sqlParams.Add(new SqlParameter("@pEncounterID", encounterID));

                if (!string.IsNullOrEmpty(identifier))
                    sqlParams.Add(new SqlParameter("@pIdentifier", Convert.ToString(identifier)));

                DataTable dtResult = DALHelper.ExecuteDataTable(connectionString, CommandType.StoredProcedure, "[CommonForm].[uspGetTemplateByIdentifier]", sqlParams.ToArray());

                if (dtResult != null && dtResult.Rows.Count > 0)
                    result = Convert.ToString(dtResult.Rows[0][0]);
            }
            catch { }

            return result;
        }
        public static DataTable GetTemplateSchema(string patientId, string identifier)
        {
            DataTable result = new DataTable();

            try
            {
                string connectionString = Convert.ToString(ConfigurationManager.ConnectionStrings["ConnMHNBPAC"].ConnectionString);

                List<SqlParameter> sqlParams = new List<SqlParameter>();

                if (!string.IsNullOrEmpty(patientId))
                    sqlParams.Add(new SqlParameter("@pPatientID", patientId));

                if (!string.IsNullOrEmpty(identifier))
                    sqlParams.Add(new SqlParameter("@pIdentifier", identifier));

                result = DALHelper.ExecuteDataTable(connectionString, CommandType.StoredProcedure, "[CommonForm].[uspGetTemplateSchema]", sqlParams.ToArray());
            }
            catch { }

            return result;
        }

        public static DataTable GetPatientProviderPractice(string encounterID, string providerId)
        {
            DataTable dtResult = new DataTable();

            try
            {
                string connectionString = Convert.ToString(ConfigurationManager.ConnectionStrings["ConnMHNBPAC"].ConnectionString);

                List<SqlParameter> sqlParams = new List<SqlParameter>();

                if (!string.IsNullOrEmpty(encounterID))
                    sqlParams.Add(new SqlParameter("@pEncounterID", encounterID));

                if (!string.IsNullOrEmpty(providerId))
                    sqlParams.Add(new SqlParameter("@pProviderId", providerId));

                dtResult = DALHelper.ExecuteDataTable(connectionString, CommandType.StoredProcedure, "[CommonForm].[uspGetPatientProviderPractice]", sqlParams.ToArray());
            }
            catch (Exception ex) { }

            return dtResult;
        }

        public static DataSet CFGetDocumentDetail(string patientId, string encounterId, string identifier, string date, out string error)
        {
            DataSet dsResult = new DataSet();
            error = string.Empty;
            string connectionString = Convert.ToString(ConfigurationManager.ConnectionStrings["ConnMHNBPAC"].ConnectionString);

            try
            {
                List<SqlParameter> sqlParams = new List<SqlParameter>();

                if (!string.IsNullOrEmpty(patientId))
                    sqlParams.Add(new SqlParameter("@pPatientId", patientId.Trim()));

                if (!string.IsNullOrEmpty(encounterId))
                    sqlParams.Add(new SqlParameter("@pEncounterId", encounterId.Trim()));

                if (!string.IsNullOrEmpty(identifier))
                    sqlParams.Add(new SqlParameter("@pIdentifier", identifier.Trim()));

                if (!string.IsNullOrEmpty(date))
                    sqlParams.Add(new SqlParameter("@pDate", date.Trim()));


                dsResult = DALHelper.ExecuteDataset(connectionString, CommandType.StoredProcedure, "[CommonForm].[uspGetDocumentDetail]", sqlParams.ToArray());
            }
            catch (Exception ex)
            {
                error = ex.Message;
            }

            return dsResult;
        }

        public static DataSet CFGetDMSIds(string patientId, string encounterId, out string error)
        {
            DataSet dsResult = new DataSet();
            error = string.Empty;
            string connectionString = Convert.ToString(ConfigurationManager.ConnectionStrings["ConnMHNBPAC"].ConnectionString);

            try
            {
                List<SqlParameter> sqlParams = new List<SqlParameter>();

                if (!string.IsNullOrEmpty(patientId))
                    sqlParams.Add(new SqlParameter("@pPatientId", patientId.Trim()));

                if (!string.IsNullOrEmpty(encounterId))
                    sqlParams.Add(new SqlParameter("@pEncounterId", encounterId.Trim()));

                dsResult = DALHelper.ExecuteDataset(connectionString, CommandType.StoredProcedure, "[CommonForm].[uspCFGetDMSIds]", sqlParams.ToArray());
            }
            catch (Exception ex)
            {
                error = ex.Message;
            }

            return dsResult;
        }

        public static DataSet CFRetinalLookUp()
        {
            DataSet dsResult = new DataSet();
            string connectionString = Convert.ToString(ConfigurationManager.ConnectionStrings["ConnMHNBPAC"].ConnectionString);

            try
            {
                dsResult = DALHelper.ExecuteDataset(connectionString, CommandType.StoredProcedure, "[CommonForm].[uspRetinalMappings]");
            }
            catch (Exception ex)
            {
            }

            return dsResult;
        }

        #endregion Get

        #region Save

        public static int InsertSummary(string patientID, string encounterID, string providerID, string identifier, string dateTimeRecorded, string outcome, string ds, string onset, DataTable dtResult)
        {
            int result = 0;

            try
            {
                string connectionString = Convert.ToString(ConfigurationManager.ConnectionStrings["ConnMHNBPAC"].ConnectionString);
                using (SqlConnection objConn = new SqlConnection(connectionString))
                {
                    List<SqlParameter> sqlParams = new List<SqlParameter>();

                    if (!string.IsNullOrEmpty(patientID))
                        sqlParams.Add(new SqlParameter("@pPatientID", Convert.ToInt32(patientID)));

                    if (!string.IsNullOrEmpty(encounterID))
                        sqlParams.Add(new SqlParameter("@pEncounterID", encounterID));

                    if (!string.IsNullOrEmpty(providerID))
                        sqlParams.Add(new SqlParameter("@pProviderID", Convert.ToInt32(providerID)));

                    if (!string.IsNullOrEmpty(identifier))
                        sqlParams.Add(new SqlParameter("@pIdentifier", Convert.ToString(identifier)));

                    if (!string.IsNullOrEmpty(outcome))
                        sqlParams.Add(new SqlParameter("@pOutCome", Convert.ToString(outcome)));

                    if (!string.IsNullOrEmpty(ds))
                        sqlParams.Add(new SqlParameter("@pDS", Convert.ToDateTime(ds)));

                    if (!string.IsNullOrEmpty(onset))
                        sqlParams.Add(new SqlParameter("@pOnSet", Convert.ToDateTime(onset)));

                    sqlParams.Add(new SqlParameter("@pScreeningUDT", dtResult));

                    SqlParameter sqlParamOut = new SqlParameter("@outputparam", SqlDbType.Int);
                    sqlParamOut.Direction = ParameterDirection.Output;
                    sqlParamOut.Value = -1;
                    sqlParams.Add(sqlParamOut);

                    DALHelper.ExecuteNonQuery(objConn, CommandType.StoredProcedure, "[CommonForm].[uspInsertSummary]", sqlParams.ToArray());
                    result = Convert.ToInt32(sqlParams[sqlParams.Count - 1].Value);
                }
            }
            catch { }

            return result;
        }

        public static int InsertCondition(string patientId, string encounterID, string providerID, DataTable dtResult)
        {
            int result = 0;

            try
            {
                string connectionString = Convert.ToString(ConfigurationManager.ConnectionStrings["ConnMHNBPAC"].ConnectionString);

                using (SqlConnection objConn = new SqlConnection(connectionString))
                {
                    List<SqlParameter> sqlParams = new List<SqlParameter>();

                    if (!string.IsNullOrEmpty(patientId))
                        sqlParams.Add(new SqlParameter("@pPatientID", Convert.ToInt32(patientId)));

                    if (!string.IsNullOrEmpty(encounterID))
                        sqlParams.Add(new SqlParameter("@pEncounterId", encounterID));

                    if (!string.IsNullOrEmpty(providerID))
                        sqlParams.Add(new SqlParameter("@pProviderID", Convert.ToInt32(providerID)));

                    sqlParams.Add(new SqlParameter("@pConditionUDT", dtResult));

                    SqlParameter sqlParamOut = new SqlParameter("@outputparam", SqlDbType.Int);
                    sqlParamOut.Direction = ParameterDirection.Output;
                    sqlParamOut.Value = -1;
                    sqlParams.Add(sqlParamOut);

                    DALHelper.ExecuteNonQuery(objConn, CommandType.StoredProcedure, "[CommonForm].[uspInsertCondition]", sqlParams.ToArray());
                    result = Convert.ToInt32(sqlParams[sqlParams.Count - 1].Value);
                }
            }
            catch { }

            return result;
        }

        public static int InsertFamilyHistory(string patientId, string encounterID, string providerID, string display, string code, DateTime onsetDateTime, DateTime dateTimeRecorded)
        {
            int result = 0;

            try
            {
                string connectionString = Convert.ToString(ConfigurationManager.ConnectionStrings["ConnMHNBPAC"].ConnectionString);
                using (SqlConnection objConn = new SqlConnection(connectionString))
                {
                    List<SqlParameter> sqlParams = new List<SqlParameter>();

                    sqlParams.Add(new SqlParameter("@pPatientID", Convert.ToInt32(patientId)));
                    sqlParams.Add(new SqlParameter("@pEncounterId", encounterID));
                    sqlParams.Add(new SqlParameter("@pProviderID", Convert.ToInt32(providerID)));
                    sqlParams.Add(new SqlParameter("@pDisplay", display));
                    sqlParams.Add(new SqlParameter("@pCode", code));
                    sqlParams.Add(new SqlParameter("@pOnsetDateTime", onsetDateTime));
                    sqlParams.Add(new SqlParameter("@pDateTimeRecorded", dateTimeRecorded));

                    //uspInsertFamilyHistory_New
                    result = DALHelper.ExecuteNonQuery(objConn, CommandType.StoredProcedure, "[CommonForm].[uspInsertFamilyHistory]", sqlParams.ToArray());

                }
            }
            catch { }

            return result;
        }

        public static int InsertRecall(string patientId, string encounterID, string providerID, int reCallID, DateTime reCallDate, string description, int cycle, string code, bool replace)
        {
            int result = 0;
            try
            {
                string connectionString = Convert.ToString(ConfigurationManager.ConnectionStrings["ConnMHNBPAC"].ConnectionString);
                using (SqlConnection objConn = new SqlConnection(connectionString))
                {
                    List<SqlParameter> sqlParams = new List<SqlParameter>();

                    sqlParams.Add(new SqlParameter("@pPatientID", Convert.ToInt32(patientId)));
                    sqlParams.Add(new SqlParameter("@pProviderID", Convert.ToInt32(providerID)));

                    sqlParams.Add(new SqlParameter("@pReCallID", reCallID));
                    sqlParams.Add(new SqlParameter("@pReCallDate", reCallDate));
                    sqlParams.Add(new SqlParameter("@pDescription", Convert.ToString(description)));

                    sqlParams.Add(new SqlParameter("@pCycle", Convert.ToString(cycle)));
                    sqlParams.Add(new SqlParameter("@pCode", Convert.ToString(code)));
                    sqlParams.Add(new SqlParameter("@pReplace", replace));

                    result = DALHelper.ExecuteNonQuery(objConn, CommandType.StoredProcedure, "[CommonForm].[uspInsertRecalls]", sqlParams.ToArray());
                }
            }
            catch (Exception ex)
            {
                result = -1;
            }

            return result;
        }

        public static int InsertObservation(Screening objScreening)
        {
            int result = 0;

            try
            {
                string connectionString = Convert.ToString(ConfigurationManager.ConnectionStrings["ConnMHNBPAC"].ConnectionString);

                using (SqlConnection objConn = new SqlConnection(connectionString))
                {
                    var param = new List<SqlParameter>();
                    SqlParameter outParam = new SqlParameter();

                    if (objScreening.ScreeningDate == default(DateTime))
                        objScreening.ScreeningDate = DateTime.Now;

                    param.Add(new SqlParameter("@pEncounterID", objScreening.AppointmentID));
                    param.Add(new SqlParameter("@pScreeningDate", objScreening.ScreeningDate));

                    if (!string.IsNullOrWhiteSpace(objScreening.Waist))
                        param.Add(new SqlParameter("@pWaist", objScreening.Waist));

                    if (objScreening.Height > 0)
                        param.Add(new SqlParameter("@pHeight", objScreening.Height));

                    if (objScreening.Weight > 0)
                        param.Add(new SqlParameter("@pWeight", objScreening.Weight));

                    if (objScreening.BMIValue > 0)
                        param.Add(new SqlParameter("@pBMIValue", objScreening.BMIValue));

                    if (objScreening.BPSYS > 0)
                        param.Add(new SqlParameter("@pBPSys", objScreening.BPSYS));

                    if (objScreening.BPDia > 0)
                        param.Add(new SqlParameter("@pBPDia", objScreening.BPDia));

                    #region Commented HR
                    //if (objScreening.HeartRate > 0)
                    //    param.Add(new SqlParameter("@pHeartRate", objScreening.HeartRate));

                    //if (!string.IsNullOrEmpty(objScreening.HeartRateComments))
                    //    param.Add(new SqlParameter("@pHeartRateComments", objScreening.HeartRateComments)); 
                    #endregion

                    if (!string.IsNullOrEmpty(objScreening.CoronaryRisk))
                        param.Add(new SqlParameter("@pCoronaryRisk", objScreening.CoronaryRisk));

                    if (objScreening.Framingham > 0)
                        param.Add(new SqlParameter("@pFramingham", objScreening.Framingham));

                    if (objScreening.ProviderID > 0)
                        param.Add(new SqlParameter("@pProviderId", objScreening.ProviderID));

                    param.Add(new SqlParameter("@pPatientID", objScreening.PatientID));


                    outParam.Direction = ParameterDirection.Output;
                    outParam.ParameterName = "@outPutpram";
                    outParam.DbType = DbType.Int32;
                    param.Add(outParam);

                    DALHelper.ExecuteNonQuery(objConn, CommandType.StoredProcedure, "[CommonForm].[uspInsertObservation]", param.ToArray());
                    result = (outParam.Value != DBNull.Value) ? Convert.ToInt32(outParam.Value) : 0;
                }
            }
            catch { }

            return result;
        }

        public static int ScanDocmentsStatusUpdate(string DMSID, int UpdateBy, int FolderID, int PatientID_hidden, int PracticeID, int OrganizationID,
                                                   int AttentionID, DateTime ResultDate, int MessageSubjectID, string MessageSubjectName, string Comments,
                                                   bool IsConfidential, bool PostOnTimeLine, int InsertedBy, bool IsCallFromSMS, int SMSID,
                                                   bool IsDocumentFiled, int UserLoggingID)
        {
            int Result = 0;
            var sqlParams = new List<SqlParameter>();

            SqlConnection conn = new SqlConnection(Convert.ToString(ConfigurationManager.ConnectionStrings["ConnMHNBPAC"]));
            {
                sqlParams.Add(new SqlParameter("@DMSID", DMSID));
                sqlParams.Add(new SqlParameter("@UpdatedBy", UpdateBy));
                sqlParams.Add(new SqlParameter("@FolderID", FolderID));
                sqlParams.Add(new SqlParameter("@PatientID", PatientID_hidden));
                sqlParams.Add(new SqlParameter("@PracticeID", PracticeID));
                sqlParams.Add(new SqlParameter("@OrganizationId", OrganizationID));
                sqlParams.Add(new SqlParameter("@AttentionID", AttentionID));
                sqlParams.Add(new SqlParameter("@ResultDate", ResultDate));
                sqlParams.Add(new SqlParameter("@MessageSubjectID", MessageSubjectID));
                sqlParams.Add(new SqlParameter("@MessageSubjectTitle", MessageSubjectName));
                sqlParams.Add(new SqlParameter("@Comments", Comments));
                sqlParams.Add(new SqlParameter("@IsConfidential", IsConfidential));
                sqlParams.Add(new SqlParameter("@ShowOnTimeLine", PostOnTimeLine));
                sqlParams.Add(new SqlParameter("@InsertedBy", InsertedBy));
                sqlParams.Add(new SqlParameter("@pIsCallFromSMS", IsCallFromSMS));
                sqlParams.Add(new SqlParameter("@SMSID", SMSID));
                sqlParams.Add(new SqlParameter("@IsDocumentFiled", IsDocumentFiled));
                sqlParams.Add(new SqlParameter("@pCalledby", "CommonForm"));

                SqlParameter outParam = new SqlParameter();
                outParam.Direction = ParameterDirection.Output;
                outParam.ParameterName = "@OutputParam";
                sqlParams.Add(new SqlParameter("@pUserLoggingID", UserLoggingID));
                outParam.DbType = DbType.Int32;
                sqlParams.Add(outParam);

                DALHelper.ExecuteNonQuery(conn, CommandType.StoredProcedure, "[Config].[uspScanDocmentsStatusUpdate]", sqlParams.ToArray());

                if (outParam.Value != DBNull.Value)
                    Result = Convert.ToInt32(outParam.Value);
            }

            return Result;
        }

        #endregion Save

        #endregion Common Forms

        #region Others

        private static T DownloadSerializedData<T>(string JsonData) where T : new()
        {
            return !string.IsNullOrEmpty(JsonData) ? JsonConvert.DeserializeObject<T>(JsonData) : new T();
        }

        private static DataSet GetFinalResults(string procedureName, List<SqlParameter> sqlParams, out string error)
        {
            DataSet dsResult = new DataSet();
            error = string.Empty;

            string connectionString = Convert.ToString(ConfigurationManager.ConnectionStrings["ConnMHNBPAC"].ConnectionString);

            try
            {
                dsResult = DALHelper.ExecuteDataset(connectionString, CommandType.StoredProcedure, procedureName, sqlParams.ToArray());
            }
            catch (Exception ex)
            {
                error = ex.Message;
            }

            return dsResult;
        }

        public static string GetPatientID(string NHI)
        {
            Int64 appointmentID = -1;

            return GetPatientID(NHI, appointmentID);
        }

        /// <summary>
        /// Get Patient ID by NHI
        /// </summary>
        /// <param name="NHI">Patient National Health Index</param>
        /// <returns></returns>
        public static string GetPatientID(string NHI, Int64 appointmentID)
        {
            DataTable dtResult = new DataTable();
            string result = "0";
            string connectionString = Convert.ToString(ConfigurationManager.ConnectionStrings["ConnMHNBPAC"].ConnectionString);

            List<SqlParameter> sqlParams = new List<SqlParameter>();
            sqlParams.Add(new SqlParameter("@pNHINumber", NHI));

            if (appointmentID > 0)
                sqlParams.Add(new SqlParameter("@pAppointmentID", appointmentID));

            try
            {
                dtResult = DALHelper.ExecuteDataTable(connectionString, CommandType.StoredProcedure, "[Profile].[uspGetPatientbyNHI]", sqlParams.ToArray());

                if (dtResult.Rows.Count > 0)
                    result = Convert.ToString(dtResult.Rows[0]["PatientID"]);
            }
            catch
            {

            }
            return result;
        }

        ///Azam Khan
        /// <summary>
        /// Save Bpac Referral detail into DB
        /// </summary>
        /// <param name="pBPACReferralID"></param>
        /// <param name="pDocumentSource"></param>
        /// <param name="pInternalID"></param>
        /// <param name="pSessionID"></param>
        /// <param name="pReferralID"></param>
        /// <param name="pReferralOrganisation"></param>
        /// <param name="pReferralAttention"></param>
        /// <param name="pReferralDepartment"></param>
        /// <param name="pReferralSpecialty"></param>
        /// <param name="pReferralUrgency"></param>
        /// <param name="pReferralcCompany"></param>
        /// <param name="pReferralAddress1"></param>
        /// <param name="pReferralAddress2"></param>
        /// <param name="pReferralAddress3"></param>
        /// <param name="pReferralPhone"></param>
        /// <param name="pReferralFax"></param>
        /// <param name="pStatus"></param>
        /// <param name="pCreationDateTime"></param>
        /// <param name="pPatientID"></param>
        /// <param name="pAppointmentID"></param>
        /// <param name="pPracticeID"></param>
        /// <param name="pPatientConsent"></param>
        /// <param name="pPatientConsentComment"></param>
        /// <param name="pPatientInterpreter"></param>
        /// <param name="pPatientInterpreterComent"></param>
        /// <param name="pInterpreterLanguage"></param>
        /// <param name="pPatientResidencyStatus"></param>
        /// <param name="pComments"></param>
        /// <param name="pXML"></param>
        /// <returns></returns>
        public static int SaveBPACRef(out long pBPACReferralID, string pDocumentSource, string pInternalID, string pSessionID,
                                     string pReferralID, string pReferralOrganisation, string pReferralAttention, string pReferralDepartment,
                                     string pReferralSpecialty, string pReferralUrgency, string pReferralcCompany, string pReferralAddress1,
                                     string pReferralAddress2, string pReferralAddress3, string pReferralPhone, string pReferralFax, string pStatus,
                                     string pCreationDateTime, string pPatientID, Int64 pAppointmentID, string pPracticeID, bool pPatientConsent,
                                     string pPatientConsentComment, bool pPatientInterpreter, string pPatientInterpreterComent, string pInterpreterLanguage,
                                     string pPatientResidencyStatus, string pComments, string pXML, string SendingApplication = "", string SendingFacility = "",
                                     string ReceivingApplication = "", string ReceivingFacility = "", string MessageID = "", string MessageType = "",
                                     string NHI = "", string MessageSubject = "", string MessageBody = "", string FileName = "")
        {
            int result = 0;
            pBPACReferralID = 0;

            try
            {
                string connectionString = Convert.ToString(ConfigurationManager.ConnectionStrings["ConnMHNBPAC"].ConnectionString);
                List<SqlParameter> sqlParams = new List<SqlParameter>();
                sqlParams.Add(new SqlParameter("@pBPACReferralID", pBPACReferralID));
                sqlParams.Add(new SqlParameter("@pDocumentSource", pDocumentSource));
                sqlParams.Add(new SqlParameter("@pInternalID", pInternalID));
                sqlParams.Add(new SqlParameter("@pSessionID", pSessionID));
                sqlParams.Add(new SqlParameter("@pReferralID", pReferralID));
                sqlParams.Add(new SqlParameter("@pReferralOrganisation", pReferralOrganisation));
                sqlParams.Add(new SqlParameter("@pReferralAttention", pReferralAttention));
                sqlParams.Add(new SqlParameter("@pReferralDepartment", pReferralDepartment));
                sqlParams.Add(new SqlParameter("@pReferralSpecialty", pReferralSpecialty));
                sqlParams.Add(new SqlParameter("@pReferralUrgency", pReferralUrgency));
                sqlParams.Add(new SqlParameter("@pReferralcCompany", pReferralcCompany));
                sqlParams.Add(new SqlParameter("@pReferralAddress1", pReferralAddress1));
                sqlParams.Add(new SqlParameter("@pReferralAddress2", pReferralAddress2));
                sqlParams.Add(new SqlParameter("@pReferralAddress3", pReferralAddress3));
                sqlParams.Add(new SqlParameter("@pReferralPhone", pReferralPhone));
                sqlParams.Add(new SqlParameter("@pReferralFax", pReferralFax));
                sqlParams.Add(new SqlParameter("@pStatus", pStatus));
                sqlParams.Add(new SqlParameter("@pCreationDateTime", pCreationDateTime));
                sqlParams.Add(new SqlParameter("@pAuthLogId", pPatientID));
                sqlParams.Add(new SqlParameter("@pAppointmentID", pAppointmentID));
                sqlParams.Add(new SqlParameter("@pPracticeID", pPracticeID));
                sqlParams.Add(new SqlParameter("@pPatientConsent", pPatientConsent));
                sqlParams.Add(new SqlParameter("@pPatientConsentComment", pPatientConsentComment));
                sqlParams.Add(new SqlParameter("@pPatientInterpreter", pPatientInterpreter));
                sqlParams.Add(new SqlParameter("@pPatientInterpreterComent", pPatientInterpreterComent));
                sqlParams.Add(new SqlParameter("@pInterpreterLanguage", pInterpreterLanguage));
                sqlParams.Add(new SqlParameter("@pPatientResidencyStatus", pPatientResidencyStatus));
                sqlParams.Add(new SqlParameter("@pComments", pComments));
                sqlParams.Add(new SqlParameter("@pXML", pXML));

                sqlParams.Add(new SqlParameter("@pSendingApplication", SendingApplication));
                sqlParams.Add(new SqlParameter("@pSendingFacility", SendingFacility));
                sqlParams.Add(new SqlParameter("@pReceivingApplication", ReceivingApplication));
                sqlParams.Add(new SqlParameter("@pReceivingFacility", ReceivingFacility));
                sqlParams.Add(new SqlParameter("@pMessageID", MessageID));
                sqlParams.Add(new SqlParameter("@pMessageType", MessageType));
                sqlParams.Add(new SqlParameter("@pNHI", NHI));
                sqlParams.Add(new SqlParameter("@pMessageSubject", MessageSubject));
                sqlParams.Add(new SqlParameter("@pMessageBody", MessageBody));

                SqlParameter sqlParamOut = new SqlParameter("@pOutPutParam", SqlDbType.Int);
                sqlParamOut.Direction = ParameterDirection.Output;
                sqlParamOut.Value = -1;
                sqlParams.Add(sqlParamOut);

                result = DALHelper.ExecuteNonQuery(connectionString, CommandType.StoredProcedure, "[Appointment].[uspInsertBPACReferral]", sqlParams.ToArray());
                pBPACReferralID = Convert.ToInt32(sqlParams[sqlParams.Count - 1].Value);
            }
            catch (Exception)
            {
                throw;
            }
            return result;
        }

        /// <summary>
        /// Insert attachment to Bpac Referral Docs
        /// </summary>
        /// <param name="refID">Referral ID</param>
        /// <param name="guid">DMS ID</param>
        /// <param name="DocType">Document type like PDF,HTML,XML</param>
        /// <returns></returns>
        public static int SaveBPACAttachments(string refID, string guid, int DocType)
        {
            int result = 0;

            try
            {
                string connectionString = Convert.ToString(ConfigurationManager.ConnectionStrings["ConnMHNBPAC"].ConnectionString);
                List<SqlParameter> sqlParams = new List<SqlParameter>();
                sqlParams.Add(new SqlParameter("@pBPACReferralID", refID));
                sqlParams.Add(new SqlParameter("@pDocDMSID", guid));
                sqlParams.Add(new SqlParameter("@pDocType", DocType));
                result = DALHelper.ExecuteNonQuery(connectionString, CommandType.StoredProcedure, "[Appointment].[uspInsertBPACReferralDocument]", sqlParams.ToArray());
            }
            catch (Exception)
            {
                throw;
            }
            return result;
        }

        /// <summary>
        /// Save ACC18 Form Data
        /// </summary>
        /// <param name="pAcc18">Primary ID of ACC18 Table</param>
        /// <param name="Acc18Number">Acc18 Number</param>
        /// <param name="appointmentID">Patient Appointment ID</param>
        /// <param name="Acc45Number">Acc45 Number</param>
        /// <param name="Comments">Acc18 Comments if any</param>
        /// <returns></returns>
        public static long SaveACC18(long pAcc18, string Acc18Number, string Acc45Number, string Comments, Int64 appointmentID)
        {
            long result = 0;

            try
            {
                string connectionString = Convert.ToString(ConfigurationManager.ConnectionStrings["ConnMHNBPAC"].ConnectionString);
                List<SqlParameter> sqlParams = new List<SqlParameter>();
                sqlParams.Add(new SqlParameter("@pAcc18ID", pAcc18));
                //sqlParams.Add(new SqlParameter("@pAppointmentID", appointmentID));
                sqlParams.Add(new SqlParameter("@pAuthLogId", appointmentID));
                sqlParams.Add(new SqlParameter("@pACC45Number", Acc45Number));
                sqlParams.Add(new SqlParameter("@pDateofInjury", DBNull.Value));
                sqlParams.Add(new SqlParameter("@pACC18StatusID", 3));
                sqlParams.Add(new SqlParameter("@pBPACACC18ID", Acc18Number));
                sqlParams.Add(new SqlParameter("@pComments", Comments));

                SqlParameter sqlParamOut = new SqlParameter("@pOutput", SqlDbType.Int);
                sqlParamOut.Direction = ParameterDirection.Output;
                sqlParamOut.Value = -1;
                sqlParams.Add(sqlParamOut);

                DALHelper.ExecuteNonQuery(connectionString, CommandType.StoredProcedure, "[Appointment].[usptblACC18InsertUpdate]", sqlParams.ToArray());
                result = Convert.ToInt32(sqlParams[sqlParams.Count - 1].Value);
            }
            catch (Exception)
            {
                throw;
            }
            return result;
        }

        /// <summary>
        /// Save Acc18 Form as Attachment
        /// </summary>
        /// <param name="pACC18ID">Acc18 Reference ID</param>
        /// <param name="pDMSID">DMS ID</param>
        /// <param name="pType">File Type like HTML, PDF, XML</param>
        /// <returns></returns>
        public static int SaveAcc18Documents(string pACC18ID, string pDMSID, string pType, string Comments)
        {
            int result = 0;

            try
            {
                string connectionString = Convert.ToString(ConfigurationManager.ConnectionStrings["ConnMHNBPAC"].ConnectionString);

                List<SqlParameter> sqlParams = new List<SqlParameter>();
                sqlParams.Add(new SqlParameter("@pACC18ID", pACC18ID));
                sqlParams.Add(new SqlParameter("@pDMSID", pDMSID));
                sqlParams.Add(new SqlParameter("@pType", pType));
                sqlParams.Add(new SqlParameter("@pComments", Comments));

                result = DALHelper.ExecuteNonQuery(connectionString, CommandType.StoredProcedure, "[Appointment].[usptblACC18DocsInsertUpdate]", sqlParams.ToArray());
            }
            catch (Exception)
            {
                throw;
            }

            return result;
        }

        public static DataSet GetNextofKin(string patientID, out string error)
        {
            try
            {
                error = string.Empty;
                //List<SqlParameter> sqlParams = new List<SqlParameter>();
                //sqlParams.Add(new SqlParameter("@patientid", patientID));

                return FillNOKDS("NextofKin");//GetFinalResults("[Appointment].[uspGetNextofKinByPatientId]", sqlParams, out error);
            }
            catch (Exception)
            {
                throw;
            }
        }

        public static DataSet GetPatientInbox()
        {
            try
            {
                return FillNOKDS("PatInBox");
            }
            catch (Exception)
            {
                throw;
            }
        }

        public static DataSet GetPatientInboxDetail()
        {
            try
            {
                return FillNOKDS("PatInBoxDetail");
            }
            catch (Exception)
            {
                throw;
            }
        }

        public static DataSet GetPatientInboxDoc()
        {
            try
            {
                return FillNOKDS("PatInBoxDoc");
            }
            catch (Exception)
            {
                throw;
            }
        }

        private static DataSet FillNOKDS(string type)
        {
            DataSet ds = new DataSet();
            DataTable dt = new DataTable();

            #region NextofKin
            if (type.Trim().Equals("NextofKin"))
            {

                dt.Columns.Add("firstName");
                dt.Columns.Add("middleName");
                dt.Columns.Add("familyName");
                dt.Columns.Add("gender");
                dt.Columns.Add("dateofBirth");
                dt.Columns.Add("contactNumber");
                dt.Columns.Add("email");
                dt.Columns.Add("relationship");

                DataRow dr = dt.NewRow();

                dr["firstName"] = "Jhon";
                dr["middleName"] = "Doe";
                dr["familyName"] = "Smith";
                dr["gender"] = "M";
                dr["dateofBirth"] = "1992-09-11";
                dr["contactNumber"] = "123-45678";
                dr["email"] = "abc@def.com";
                dr["relationship"] = "Husband";

                dt.Rows.Add(dr);
            }

            #endregion NextofKin

            #region PatInBox
            else if (type.Trim().Equals("PatInBox"))
            {

                dt.Columns.Add("InboxId");
                dt.Columns.Add("Date");
                dt.Columns.Add("subject");
                dt.Columns.Add("Type");
                dt.Columns.Add("Comments");
                dt.Columns.Add("Provider");
                dt.Columns.Add("DisplayType");

                DataRow dr = dt.NewRow();

                dr["InboxId"] = "12345";
                dr["Date"] = "2017-11-02";
                dr["subject"] = "CBC";
                dr["Type"] = "Lab";
                dr["Comments"] = "Normal";
                dr["Provider"] = "Dr Sam Evas";
                dr["DisplayType"] = "JSON";

                dt.Rows.Add(dr);

                DataRow dr1 = dt.NewRow();

                dr1["InboxId"] = "67890";
                dr1["Date"] = "2017-11-02";
                dr1["subject"] = "LFT";
                dr1["Type"] = "Lab";
                dr1["Comments"] = "-ve";
                dr1["Provider"] = "Dr Jhon";
                dr1["DisplayType"] = "JSON";

                dt.Rows.Add(dr1);

                DataRow dr2 = dt.NewRow();

                dr2["InboxId"] = "67895";
                dr2["Date"] = "2017-11-02";
                dr2["subject"] = "Liver Ultrasound";
                dr2["Type"] = "Rad";
                dr2["Comments"] = "Stable appearances";
                dr2["Provider"] = "Dr Jhon";
                dr2["DisplayType"] = "JSON";

                dt.Rows.Add(dr2);

                DataRow dr3 = dt.NewRow();

                dr3["InboxId"] = "67895";
                dr3["Date"] = "2017-11-02";
                dr3["subject"] = "Discharge Summary";
                dr3["Type"] = "Ref";
                dr3["Comments"] = "Rt elbow pain";
                dr3["Provider"] = "Dr Jhon";
                dr3["DisplayType"] = "PDF";

                dt.Rows.Add(dr3);
            }
            #endregion PatInBox

            #region PatInBoxDetail
            else if (type.Trim().Equals("PatInBoxDetail"))
            {

                dt.Columns.Add("InboxDetailId ");
                dt.Columns.Add("Term");
                dt.Columns.Add("TermCode");
                dt.Columns.Add("value");
                dt.Columns.Add("unit");

                DataRow dr = dt.NewRow();

                dr["InboxDetailId "] = "36250";
                dr["Term"] = "HEMOGLOBIN";
                dr["TermCode"] = "4120";
                dr["value"] = "101";
                dr["unit"] = "g/L";

                dt.Rows.Add(dr);

                DataRow dr1 = dt.NewRow();

                dr1["InboxDetailId "] = "36251";
                dr1["Term"] = "MCH";
                dr1["TermCode"] = "4170";
                dr1["value"] = "22";
                dr1["unit"] = "pg";

                dt.Rows.Add(dr1);
            }
            #endregion PatInBoxDetail

            #region PatInBoxDoc
            else if (type.Trim().Equals("PatInBoxDoc"))
            {
                dt.Columns.Add("InboxDetailId");
                dt.Columns.Add("documentType");
                dt.Columns.Add("documentData");

                DataRow dr = dt.NewRow();

                dr["InboxDetailId"] = "67890";
                dr["documentType"] = "pdf";
                dr["documentData"] = "0xC3AFC2BBC2BF3C21444F43545950452048544D4C3E0D0A3C68746D6C3E0D0A3C686561643E0D0A202020203C6D6574612068…. [SNIP] …...3B0D0A0D0A20202020202020";

                dt.Rows.Add(dr);
            }
            #endregion PatInBoxDoc

            ds.Tables.Add(dt);

            return ds;
        }

        public static DataSet GetReferalInformation(string patientID, string providerId, out string error)
        {
            try
            {
                List<SqlParameter> sqlParams = new List<SqlParameter>();
                sqlParams.Add(new SqlParameter("@patientid", patientID));
                sqlParams.Add(new SqlParameter("@providerId", providerId));

                return GetFinalResults("[Appointment].[uspGetNextofKinByPatientId]", sqlParams, out error);
            }
            catch (Exception)
            {
                throw;
            }
        }

        public static DataSet GetLongTermMedications(string patientID, string providerId, string date, out string error)
        {
            try
            {
                List<SqlParameter> sqlParams = new List<SqlParameter>();
                sqlParams.Add(new SqlParameter("@pPatientid", patientID));
                sqlParams.Add(new SqlParameter("@pProviderId", providerId));
                sqlParams.Add(new SqlParameter("@pDate", date));

                return GetFinalResults("[Appointment].[uspGetLongTermMedications]", sqlParams, out error);
            }
            catch (Exception ex)
            {
                error = ex.Message;
                return null;
            }
        }

        public static DataSet GetLabMessages(string patientID, string providerId, out string error)
        {
            try
            {
                List<SqlParameter> sqlParams = new List<SqlParameter>();
                sqlParams.Add(new SqlParameter("@pPatientid", patientID));
                //sqlParams.Add(new SqlParameter("@pProviderId", providerId));

                return GetFinalResults("[Appointment].[uspGetDiagnosticLabs]", sqlParams, out error);
            }
            catch (Exception ex)
            {
                error = ex.Message;
                return null;
            }
        }

        public static DataSet GetBodyMapData(string encounterId, out string error)
        {
            try
            {
                List<SqlParameter> sqlParams = new List<SqlParameter>();
                sqlParams.Add(new SqlParameter("@pAppointmentID", encounterId));
                //sqlParams.Add(new SqlParameter("@pProviderId", providerId));

                return GetFinalResults("[Appointment].[uspGetGenogramDataBPAC]", sqlParams, out error);
            }
            catch (Exception ex)
            {
                error = ex.Message;
                return null;
            }
        }


        #endregion
        //----
        #region Bilal
        public static int RegisterPracticeConnectionString(int PracticeID, string ConnectionString, string GUID)
        {
            int result = 0;

            try
            {
                string connectionString = Convert.ToString(ConfigurationManager.ConnectionStrings["ConnMasterDatabase"].ConnectionString);
                List<SqlParameter> sqlParams = new List<SqlParameter>();
                sqlParams.Add(new SqlParameter("@pPracticeID", PracticeID));
                sqlParams.Add(new SqlParameter("@pConnectionString", ConnectionString));
                SqlParameter OutputParam = new SqlParameter("@pOutputParam", SqlDbType.Int);
                OutputParam.Direction = ParameterDirection.Output;
                OutputParam.Value = 0;

                sqlParams.Add(OutputParam);
                //sqlParams.Add(new SqlParameter("@pGUID", GUID));
                DALHelper.ExecuteNonQuery(connectionString, CommandType.StoredProcedure, "[MDConfig].[uspRegisterPracticeConnectionString]", sqlParams.ToArray());
                result = Convert.ToInt32(sqlParams[sqlParams.Count - 1].Value);
            }
            catch (Exception)
            {
                throw;
            }
            return result;
        }


        public static int RegisterPractice(int PracticeID, int CallFromID)
        {
            int result = 0;

            try
            {
                string connectionString = Convert.ToString(ConfigurationManager.ConnectionStrings["ConnMHNBPAC"].ConnectionString);
                List<SqlParameter> sqlParams = new List<SqlParameter>();
                sqlParams.Add(new SqlParameter("@pPracticeID", PracticeID));
                sqlParams.Add(new SqlParameter("@pCallFromID", CallFromID));
                SqlParameter OutputParam = new SqlParameter("@pOutputParam", SqlDbType.Int);
                OutputParam.Direction = ParameterDirection.Output;
                OutputParam.Value = 0;

                sqlParams.Add(OutputParam);
                //sqlParams.Add(new SqlParameter("@pGUID", GUID));
                DALHelper.ExecuteNonQuery(connectionString, CommandType.StoredProcedure, "[LOG].[uspRegisterPractice]", sqlParams.ToArray());
                result = Convert.ToInt32(sqlParams[sqlParams.Count - 1].Value);
            }
            catch (Exception)
            {
                throw;
            }
            return result;
        }

        public static string GetPracticeConnectionStringByPatientID(int PatientID, out string error)
        {
            error = string.Empty;
            int PracticeID = 0;
            string ConnectionString = string.Empty;

            try
            {
                string connectionString = Convert.ToString(ConfigurationManager.ConnectionStrings["ConnMHNBPAC"].ConnectionString);
                List<SqlParameter> sqlParams = new List<SqlParameter>();
                sqlParams.Add(new SqlParameter("@pPatientID", PatientID));

                SqlParameter OutputParam = new SqlParameter("@pOutputPracticeID", SqlDbType.Int);
                OutputParam.Direction = ParameterDirection.Output;
                OutputParam.Value = string.Empty;

                sqlParams.Add(OutputParam);

                DALHelper.ExecuteDataTable(connectionString, CommandType.StoredProcedure, "[LOG].[uspGetPracticeIDByPatientID]", sqlParams.ToArray());
                if (sqlParams[sqlParams.Count - 1].Value == DBNull.Value)
                {
                    error = "No Record found!";
                    return string.Empty;
                }
                PracticeID = Convert.ToInt32(sqlParams[sqlParams.Count - 1].Value);


                connectionString = Convert.ToString(ConfigurationManager.ConnectionStrings["ConnMasterDatabase"].ConnectionString);
                sqlParams = new List<SqlParameter>();
                sqlParams.Add(new SqlParameter("@pPracticeID", PracticeID));

                OutputParam = new SqlParameter("@pOutputConnectionString", SqlDbType.VarChar);
                OutputParam.Direction = ParameterDirection.Output;
                OutputParam.Value = string.Empty;
                OutputParam.Size = -1;

                sqlParams.Add(OutputParam);

                DALHelper.ExecuteDataTable(connectionString, CommandType.StoredProcedure, "[MDConfig].[uspGetConnectionStringByPracticeID]", sqlParams.ToArray());
                if (sqlParams[sqlParams.Count - 1].Value == DBNull.Value)
                {
                    error = "No Record found!";
                    return string.Empty;
                }
                ConnectionString = Convert.ToString(sqlParams[sqlParams.Count - 1].Value);
            }
            catch (Exception)
            {
                throw;
            }
            return ConnectionString;
        }



        public static string GETPracticeConnectionString(int PracticeID)
        {
            string result = string.Empty;

            try
            {
                string connectionString = Convert.ToString(ConfigurationManager.ConnectionStrings["ConnMasterDatabase"].ConnectionString);
                List<SqlParameter> sqlParams = new List<SqlParameter>();
                sqlParams.Add(new SqlParameter("@pPracticeID", PracticeID));

                SqlParameter OutputParam = new SqlParameter("@pOutputConnectionString", SqlDbType.Int);
                OutputParam.Direction = ParameterDirection.Output;
                OutputParam.Value = string.Empty;

                sqlParams.Add(OutputParam);

                DALHelper.ExecuteDataTable(connectionString, CommandType.StoredProcedure, "[CommonForm].[uspGETPractiveConnectionString]", sqlParams.ToArray());

                result = Convert.ToString(sqlParams[sqlParams.Count - 1].Value);
            }
            catch (Exception)
            {
                throw;
            }
            return result;
        }


        #endregion
    }

    public class Screening
    {
        public string AppointmentID { get; set; }
        public DateTime ScreeningDate { get; set; }
        public decimal Height { get; set; }
        public decimal Weight { get; set; }
        public decimal BMIValue { get; set; }
        public int BPSYS { get; set; }
        public int BPDia { get; set; }
        //public int HeartRate { get; set; }
        //public string HeartRateComments { get; set; }
        public int PatientID { get; set; }
        public string Waist { get; set; }
        public int ProviderID { get; set; }
        public decimal Framingham { get; set; }
        public string CoronaryRisk { get; set; }
    }

    public class TermJson
    {
        public string value { set; get; }
        public string label { set; get; }
    }

}
