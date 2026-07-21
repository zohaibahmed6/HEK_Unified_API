using DAL.HelperClasses;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Data.SqlClient;
using System.Linq;
using System.Text;
using System.Threading.Tasks;


namespace DAL.GP2GP
{
    public class PIDAO
    {
        #region Operational Entities

        #region Get Data

        #region Get Patient Information

        public static DataSet GetPatientDemographicInfo(int PatientId, out string error)
        {
            error = string.Empty;
            DataSet dsResult = new DataSet();
            List<SqlParameter> sqlParams = new List<SqlParameter>();
            sqlParams.Add(new SqlParameter("@pPatientId", PatientId));
            try 
            {
                dsResult = GetFinalResultsDS("[GP2GP].[uspGetPatientDemography]", sqlParams, out error);
            }
            catch(Exception ex)
            {
                error = ex.Message;
                throw;
            }
            return dsResult;
        }

        #endregion

        #region Get Clinical Document Information
        public static DataTable GetPatientAuthorAndCustodian(string patientId, out string error)
        {
            error = string.Empty;
            DataTable dtResult = new DataTable();
            List<SqlParameter> sqlParams = new List<SqlParameter>();
            sqlParams.Add(new SqlParameter("@pPatientID", patientId));
            try 
            {
                dtResult = GetFinalResults("[GP2GP].[uspGetPatientAuthorAndCustodian]", sqlParams, out error);
            }
            catch(Exception ex)
            {
                error = ex.Message;
                throw;
            }
            return dtResult;
        }

        public static DataSet GetPatientDocCreator(string loggedInUserId, out string error)
        {
            error = string.Empty;
            DataSet dsResult = new DataSet();
            List<SqlParameter> sqlParams = new List<SqlParameter>();
            sqlParams.Add(new SqlParameter("@pLoggedInUserId", loggedInUserId));
            try 
            {
                dsResult = GetFinalResultsDS("[GP2GP].[uspGetPatientDocCreator]", sqlParams, out error);
            }
            catch(Exception ex)
            {
                error = ex.Message;
                throw;
            }
            return dsResult;
        }

        public static DataSet GetPatientUsualGP(string patientId, out string error)
        {
            error = string.Empty;
            DataSet dsResult = new DataSet();
            List<SqlParameter> sqlParams = new List<SqlParameter>();
            sqlParams.Add(new SqlParameter("@pPatientId", patientId));
            try 
            {
                dsResult = GetFinalResultsDS("[GP2GP].[uspGetPatientCurrentGP]", sqlParams, out error);
            }
            catch(Exception ex)
            {
                error = ex.Message;
                throw;
            }
            return dsResult;
        }

        public static DataSet GetNextOfKin(string patientId, out string error)
        {
            error = string.Empty;
            DataSet dsResult = new DataSet();
            List<SqlParameter> sqlParams = new List<SqlParameter>();
            sqlParams.Add(new SqlParameter("@pPatientId", patientId));
            try 
            {
                dsResult = GetFinalResultsDS("[GP2GP].[uspGetPatientNextOfKin]", sqlParams, out error);
            }
            catch(Exception ex)
            {
                error = ex.Message;
                throw;
            }
            return dsResult;
        }

        public static DataSet GetInformationRecepient(string patientId, out string error) 
        {
            error = string.Empty;
            DataSet dsResult = new DataSet();
            List<SqlParameter> sqlParams = new List<SqlParameter>();
            sqlParams.Add(new SqlParameter("@pPatientId", patientId));
            try 
            {
                dsResult = GetFinalResultsDS("[GP2GP].[uspGetPatientInformationRecepient]", sqlParams, out error);
            }
            catch(Exception ex)
            {
                error = ex.Message;
                throw;
            }
            return dsResult;
        }

        #endregion

        #region Get Patient Clinical Information

        public static DataTable GetPatientAdvanceDirectives(int PatientId, out string error)
        {
            error = string.Empty;
            DataTable dtResult = new DataTable();
            List<SqlParameter> sqlParams = new List<SqlParameter>();
            sqlParams.Add(new SqlParameter("@pPatientId", PatientId));
            try 
            {
                dtResult = GetFinalResults("[GP2GP].[uspGetAdvanceDirectives]", sqlParams, out error);
            }
            catch(Exception ex)
            {
                error = ex.Message;
                throw;
            }
            return dtResult;
        }

        public static DataTable GetPatientAllergies(int PatientId, out string error)
        {
            error = string.Empty;
            DataTable dtResult = new DataTable();
            List<SqlParameter> sqlParams = new List<SqlParameter>();
            sqlParams.Add(new SqlParameter("@pPatientId", PatientId));
            try 
            {
                dtResult = GetFinalResults("[GP2GP].[uspGetPatientAllergies]", sqlParams, out error); 
            }
            catch(Exception ex)
            {
                error = ex.Message;
                throw;
            }
            return dtResult;
        }

        public static DataTable GetPatientCarePlan(int PatientId, out string error)
        {
            error = string.Empty;
            DataTable dtResult = new DataTable();
            List<SqlParameter> sqlParams = new List<SqlParameter>();
            sqlParams.Add(new SqlParameter("@pPatientId", PatientId));
            try
            {
                dtResult = GetFinalResults("[GP2GP].[uspGetPatientCarePlans]", sqlParams, out error);
            }
            catch (Exception ex) 
            {
                error = ex.Message;
                throw;
            }
            return dtResult;
        }

        public static DataSet GetPatientDiagnosticReports(int PatientId, out string error)
        {
            error = string.Empty;
            DataSet dsResult = new DataSet();
            List<SqlParameter> sqlParams = new List<SqlParameter>();
            sqlParams.Add(new SqlParameter("@pPatientId", PatientId));

            try 
            {
                dsResult = GetFinalResultsDS("[GP2GP].[uspGetPatientDiagnosticReports]", sqlParams, out error);
            }
            catch(Exception ex)
            {
                 error = ex.Message;
                 throw;
            }
            return dsResult;
        }

        public static DataTable GetPatientEncounters(int PatientId, out string error)
        {
            error = string.Empty;
            DataTable dtResult = new DataTable();
            List<SqlParameter> sqlParams = new List<SqlParameter>();
            sqlParams.Add(new SqlParameter("@pPatientId", PatientId));
            try 
            {
               // dtResult = GetFinalResults("[GP2GP].[uspGetPatientEncounters]", sqlParams, out error);
            }
            catch(Exception ex)
            {
                error = ex.Message;
                throw;
            }
            return dtResult;
        }

        public static DataTable GetPatientFamilyHistory(int PatientId, out string error)
        {
            error = string.Empty;
            DataTable dtResult = new DataTable();
            List<SqlParameter> sqlParams = new List<SqlParameter>();
            sqlParams.Add(new SqlParameter("@pPatientId", PatientId));
            try
            {
                dtResult = GetFinalResults("[GP2GP].[uspGetPatientFamilyHistory]", sqlParams, out error);
            }
            catch(Exception ex)
            {
             error = ex.Message;
             throw;
            }
            return dtResult;
        }

        public static DataTable GetPatientFunctionalStatus(int PatientId, out string error)
        { 
            error = string.Empty;
            DataTable dtResult = new DataTable();
            List<SqlParameter> sqlParams = new List<SqlParameter>();
            sqlParams.Add(new SqlParameter("@pPatientId", PatientId));
            try 
            {
                dtResult = GetFinalResults("[GP2GP].[uspGetPatientFunctionalStatus]", sqlParams, out error);
            }
            catch(Exception ex)
            {
                error = ex.Message;
                throw;
            }
            return dtResult; 
        }

        public static DataTable GetPatientImmunisations(int PatientId, out string error)
        {
            error = string.Empty;
            DataTable dtResult = new DataTable();
            List<SqlParameter> sqlParams = new List<SqlParameter>();
            sqlParams.Add(new SqlParameter("@pPatientId", PatientId));
            try 
            {
                dtResult = GetFinalResults("[GP2GP].[uspGetPatientImmunisation]", sqlParams, out error);

            }
            catch(Exception ex)
            {
                error = ex.Message;
                throw;
            }
            return dtResult;
        }

        public static DataTable GetPatientMaternityInfo(int PatientId, out string error)
        {
            error = string.Empty;
            DataTable dtResult = new DataTable();
            List<SqlParameter> sqlParams = new List<SqlParameter>();
            sqlParams.Add(new SqlParameter("@pPatientId", PatientId));
            try 
            {
                 dtResult = GetFinalResults("[GP2GP].[uspGetPatientMaternityCases]", sqlParams, out error);
            }
            catch(Exception ex)
            {
                error = ex.Message;
                throw;
            }
            return dtResult;
        }

        public static DataTable GetPatientMedicationInfo(int PatientId, out string error)
        {
            error = string.Empty;
            DataTable dtResult = new DataTable();
            List<SqlParameter> sqlParams = new List<SqlParameter>();
            sqlParams.Add(new SqlParameter("@pPatientId", PatientId));
            try 
            {
                dtResult = GetFinalResults("[GP2GP].[uspGetPatientMedicationHistory]", sqlParams, out error);
            }
            catch(Exception ex)
            {
                error = ex.Message;
                throw;
            }
            return dtResult;
        }

        public static DataTable GetPatientProblems(int PatientId, out string error)
        {
            error = string.Empty;
            DataTable dtResult = new DataTable();
            List<SqlParameter> sqlParams = new List<SqlParameter>();
            sqlParams.Add(new SqlParameter("@pPatientId", PatientId));
            try 
            {
                dtResult = GetFinalResults("[GP2GP].[uspGetPatientProblems]", sqlParams, out error);
            }
            catch(Exception ex)
            {
                error = ex.Message;
                throw;
            }
            return dtResult;
        }

        public static DataTable GetPatientProcedures(int PatientId, out string error)
        {
            error = string.Empty;
            DataTable dtResult = new DataTable();
            List<SqlParameter> sqlParams = new List<SqlParameter>();
            sqlParams.Add(new SqlParameter("@pPatientId", PatientId));
            try 
            {
                dtResult =GetFinalResults("[GP2GP].[uspGetPatientProcedure]", sqlParams, out error);
            }
            catch (Exception ex)
            {
                error = ex.Message;
                throw;
            }
            return dtResult;
        }

        public static DataTable GetPatientSocialHistory(int PatientId, out string error)
        {
            error = string.Empty;
            DataTable dtResult = new DataTable();
            List<SqlParameter> sqlParams = new List<SqlParameter>();
            sqlParams.Add(new SqlParameter("@pPatientId", PatientId));
            try 
            {
                dtResult = GetFinalResults("[GP2GP].[uspGetPatientSocialHistory]", sqlParams, out error);
            }
            catch(Exception ex)
            {
                error = ex.Message;
                throw;
            }
            return dtResult;
        }

        public static DataTable GetPatientVitals(int PatientId, out string error)
        {
            error = string.Empty;
            DataTable dtResult = new DataTable();
            List<SqlParameter> sqlParams = new List<SqlParameter>();
            sqlParams.Add(new SqlParameter("@pPatientId", PatientId));
            try
            {
                dtResult =  GetFinalResults("[GP2GP].[uspGetPatientVitals]", sqlParams, out error);
            }
            catch(Exception ex)
            {
                error = ex.Message;
                throw;
            }
            return dtResult;
        }

        #endregion

        #region Get Admin and Financial Sect Information.
        public static DataTable GetExternalDocumentInfo(int ExternalDocumentId, out string error)
        {
            error = string.Empty;
            DataTable dtResult = new DataTable();
            List<SqlParameter> sqlParams = new List<SqlParameter>();
            sqlParams.Add(new SqlParameter("@pExternalDocumentId", ExternalDocumentId));
            try
            {
               // dtResult = GetFinalResults("[GP2GP].[uspGetExternalDocument]", sqlParams, out error); 
            }
            catch(Exception ex)
            {
                error = ex.Message;
                throw;
            }
            return dtResult;
        }

        public static DataTable GetMedicalEquipmentInfo(int MedicalEquipmentId, out string error)
        {
            error = string.Empty;
            DataTable dtResult = new DataTable();
            List<SqlParameter> sqlParams = new List<SqlParameter>();
            sqlParams.Add(new SqlParameter("@pMedicalEquipmentId", MedicalEquipmentId));
            try 
            {
                //dtResult = GetFinalResults("[GP2GP].[uspGetMedicalEquipment]", sqlParams, out error);
            }
            catch(Exception ex) 
            {
                error = ex.Message;
                throw;
            }
            return dtResult;
        }

        public static DataTable GetPayerInfo(int PayerId, out string error)
        {
            error = string.Empty;
            DataTable dtResult = new DataTable();
            List<SqlParameter> sqlParams = new List<SqlParameter>();
            sqlParams.Add(new SqlParameter("@pPayerId", PayerId));
            try 
            {
               // dtResult = GetFinalResults("[GP2GP].[uspGetPayer]", sqlParams, out error);
            }
            catch(Exception ex)
            {
                error = ex.Message;
                throw;
            }
            return dtResult;
        }

        public static DataTable GetWarningInfo(int WarningId, out string error)
        {
            error = string.Empty;
            DataTable dtResult = new DataTable();
            List<SqlParameter> sqlParams = new List<SqlParameter>();
            sqlParams.Add(new SqlParameter("@pWarningId", WarningId));
            try 
            {
                dtResult = GetFinalResults("[GP2GP].[uspGetWarning]", sqlParams, out error);
            }
            catch(Exception ex)
            {
                error = ex.Message;
                throw;
            }
            return dtResult;
            
        }
        #endregion

        #endregion

        #region Set Data

        #region Information Recipient
        public static DataTable GetRecipientInfo(string RecipientId, out string error)
        {
            error = string.Empty;
            DataTable dtResult = new DataTable();
            List<SqlParameter> sqlParams = new List<SqlParameter>();
            sqlParams.Add(new SqlParameter("@pRecipientId", RecipientId));
            try 
            {
                dtResult = GetFinalResults("[GP2GP].[uspGetRecipientInfo]", sqlParams, out error);
            }
            catch(Exception ex)
            {
                error = ex.Message;
                throw;
            }
            return dtResult;
            
        }
        #endregion

        #endregion

        #region Save/Update Data
        #endregion

        #endregion

        #region Generic Entities

        #endregion

        #region Lookups

        #endregion

        #region Others

        private static DataTable GetFinalResults(string procedureName, List<SqlParameter> sqlParams, out string error)
        {
            DataSet dsResult = new DataSet();
            error = string.Empty;

            string connectionString = Convert.ToString(ConfigurationManager.ConnectionStrings["ConnGP2GP"].ConnectionString);

            try
            {
                dsResult = DALHelper.ExecuteDataset(connectionString, CommandType.StoredProcedure, procedureName, sqlParams.ToArray());
            }
            catch (Exception ex)
            {
                error = ex.Message;
                throw;
            }

            return dsResult.Tables[0];
        }

        private static DataSet GetFinalResultsDS(string procedureName, List<SqlParameter> sqlParams, out string error)
        {
            DataSet dsResult = new DataSet();
            error = string.Empty;

            string connectionString = Convert.ToString(ConfigurationManager.ConnectionStrings["ConnGP2GP"].ConnectionString);

            try
            {
                dsResult = DALHelper.ExecuteDataset(connectionString, CommandType.StoredProcedure, procedureName, sqlParams.ToArray());
            }
            catch (Exception ex)
            {
                error = ex.Message;
                throw;
            }

            return dsResult;
        }
        #endregion
    }
}
