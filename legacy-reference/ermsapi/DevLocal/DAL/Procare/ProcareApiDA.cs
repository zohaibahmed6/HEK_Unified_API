using DAL.HelperClasses;
using Logger;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Data.SqlClient;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DAL
{
    public class ProcareApiDA
    {
        public static int UpdateExistingDocument(string referralid, out string error)
        {
            int result = 0;
            error = string.Empty;

            try
            {
                string connectionString = Convert.ToString(ConfigurationManager.ConnectionStrings["ConnIndiciDB"].ConnectionString);

                List<SqlParameter> sqlParams = new List<SqlParameter>();
                sqlParams.Add(new SqlParameter("@pReferralId", referralid));

                DALHelper.ExecuteNonQuery(connectionString, CommandType.StoredProcedure, "[HSS].[uspUpdateExistingDoc]", sqlParams.ToArray());
            }
            catch (Exception ex)
            {
                error = ex.Message;
            }

            return result;
        }
        public static int DocumentDelete(string dmsId, out string error)
        {
            int result = 0;
            error = string.Empty;

            try
            {
                string connectionString = Convert.ToString(ConfigurationManager.ConnectionStrings["ConnDMSDB"].ConnectionString);

                List<SqlParameter> sqlParams = new List<SqlParameter>();
                sqlParams.Add(new SqlParameter("@pDocumentKey", dmsId));

                DALHelper.ExecuteNonQuery(connectionString, CommandType.StoredProcedure, "[dbo].[uspDocumentDelete]", sqlParams.ToArray());
            }
            catch (Exception ex)
            {
                error = ex.Message;
            }

            return result;
        }
        public static int DocumentSave(int categoryID, string documentName, int documentTypeID, string description, string documentKey,
                                   byte[] contentData, string conString, out string error)
        {
            int result = 0;
            error = string.Empty;

            try
            {
                //string connectionString = Convert.ToString(ConfigurationManager.ConnectionStrings["ConnDMSDB"].ConnectionString);
                string connectionString = conString;

                List<SqlParameter> sqlParams = new List<SqlParameter>();
                sqlParams.Add(new SqlParameter("@pClientID", 3));   //PMSNZ(3)
                sqlParams.Add(new SqlParameter("@pCategoryID", categoryID));    //outbox(18)/inbox(17)
                sqlParams.Add(new SqlParameter("@pDocumentName", documentName));
                sqlParams.Add(new SqlParameter("@pDocumentTypeID", documentTypeID));
                sqlParams.Add(new SqlParameter("@pDescription", description));
                sqlParams.Add(new SqlParameter("@pDocumentKey", documentKey));
                sqlParams.Add(new SqlParameter("@pDocumentSize", contentData.Length));
                sqlParams.Add(new SqlParameter("@pProfileID", "1"));

                SqlParameter sqlParamContents = new SqlParameter("@pDocumentData", SqlDbType.VarBinary);
                sqlParamContents.Value = contentData;
                sqlParams.Add(sqlParamContents);

                SqlParameter sqlParamOut = new SqlParameter("@pDocumentIDOut", SqlDbType.Int);
                sqlParamOut.Direction = ParameterDirection.Output;
                sqlParamOut.Value = -1;
                sqlParams.Add(sqlParamOut);

                DALHelper.ExecuteNonQuery(connectionString, CommandType.StoredProcedure, "[dbo].[uspDocumentSave]", sqlParams.ToArray());
                result = (!(sqlParams[sqlParams.Count - 1].Value is DBNull)) ? Convert.ToInt32(sqlParams[sqlParams.Count - 1].Value) : result;
            }
            catch (Exception ex)
            {
                error = ex.Message;
            }

            return result;
        }
        public static DataTable GetACC45(string patientId, string sortOrder, DateTime dtMinDate, DateTime dtMaxDate,
                                         out string error)
        {
            DataTable dtResult = new DataTable();
            error = string.Empty;

            string connectionString = Convert.ToString(ConfigurationManager.ConnectionStrings["ConnIndiciDB"].ConnectionString);

            List<SqlParameter> sqlParams = new List<SqlParameter>();
            sqlParams.Add(new SqlParameter("@pPatientId", patientId));

            if (!string.IsNullOrWhiteSpace(sortOrder))
                sqlParams.Add(new SqlParameter("@pSortOrder", sortOrder));
            if (!dtMinDate.Equals(DateTime.MinValue))
                sqlParams.Add(new SqlParameter("@pMinDate", dtMinDate));
            if (!dtMaxDate.Equals(DateTime.MinValue))
                sqlParams.Add(new SqlParameter("@pMaxDate", dtMaxDate));

            try
            {
                dtResult = DALHelper.ExecuteDataTable(connectionString, CommandType.StoredProcedure, "[HSS].[uspGetACC45]", sqlParams.ToArray());
            }
            catch (Exception ex)
            {
                error = ex.Message;
            }

            return dtResult;
        }
        //public static DataTable GetConditions(string patientId, string conString, string conceptId, string sortOrder, DateTime dtMinDate, DateTime dtMaxDate, out string error)
        //{
        //    error = string.Empty;
        //    return GetConditions(patientId,conString,conceptId, sortOrder, dtMinDate, dtMinDate, out error);
        //}
        public static DataTable GetConditions(string patientId, string conString, string conceptId, out string error, string sortOrder, DateTime? dtMinDate, DateTime? dtMaxDate, int PageNo = 1, int PageSize = 100, int level = 0)
        {
            DataTable dtResult = new DataTable();
            error = string.Empty;

            //string connectionString = Convert.ToString(ConfigurationManager.ConnectionStrings["ConnIndiciDB"].ConnectionString);
            string connectionString = conString;

            List<SqlParameter> sqlParams = new List<SqlParameter>();
            sqlParams.Add(new SqlParameter("@pPatientId", patientId));

            if (!string.IsNullOrWhiteSpace(sortOrder))
                sqlParams.Add(new SqlParameter("@pSortOrder", sortOrder.ToUpper()));
            if (!dtMinDate.Equals(DateTime.MinValue))
                sqlParams.Add(new SqlParameter("@pMinDate", dtMinDate));
            if (!dtMaxDate.Equals(DateTime.MinValue))
                sqlParams.Add(new SqlParameter("@pMaxDate", dtMaxDate));

            if (!string.IsNullOrWhiteSpace(conceptId))
                sqlParams.Add(new SqlParameter("@pConceptId", conceptId));

            if (!PageNo.Equals(0))
                sqlParams.Add(new SqlParameter("@PageNo", PageNo));
            if (!PageSize.Equals(0))
                sqlParams.Add(new SqlParameter("@PageSize", PageSize));

            if (!level.Equals(0))
                sqlParams.Add(new SqlParameter("@pLevel", level));

            try
            {
                dtResult = DALHelper.ExecuteDataTable(connectionString, CommandType.StoredProcedure, "[HSS].[uspProcareGetConditions]", sqlParams.ToArray());
            }
            catch (Exception ex)
            {
                error = ex.Message;
            }

            return dtResult;
        }
        public static DataTable GetConsultNotes(string patientId, string conString, out string error)
        {
            error = string.Empty;
            return GetConsultNotes(patientId,conString, string.Empty, DateTime.MinValue, DateTime.MinValue, out error);
        }
        public static DataTable GetConsultNotes(string patientId, string conString, string sortOrder, DateTime dtMinDate, DateTime dtMaxDate,
                                                out string error)
        {
            DataTable dtResult = new DataTable();
            error = string.Empty;

            //  string connectionString = Convert.ToString(ConfigurationManager.ConnectionStrings["ConnIndiciDB"].ConnectionString);
            string connectionString = conString;
            List<SqlParameter> sqlParams = new List<SqlParameter>();
            sqlParams.Add(new SqlParameter("@pPatientId", patientId));

            if (!string.IsNullOrWhiteSpace(sortOrder))
                sqlParams.Add(new SqlParameter("@pSortOrder", sortOrder));
            if (!dtMinDate.Equals(DateTime.MinValue))
                sqlParams.Add(new SqlParameter("@pMinDate", dtMinDate));
            if (!dtMaxDate.Equals(DateTime.MinValue))
                sqlParams.Add(new SqlParameter("@pMaxDate", dtMaxDate));

            try
            {
                dtResult = DALHelper.ExecuteDataTable(connectionString, CommandType.StoredProcedure, "[HSS].[uspProcareGetConsultNotes]", sqlParams.ToArray());
            }
            catch (Exception ex)
            {
                error = ex.Message;
            }

            return dtResult;
        }
        public static DataTable GetDemographics(string patientId, string conString, out string error)
        {
            DataTable dtResult = new DataTable();
            error = string.Empty;

            //string connectionString = Convert.ToString(ConfigurationManager.ConnectionStrings["ConnIndiciDB"].ConnectionString);
            string connectionString = conString;

            List<SqlParameter> sqlParams = new List<SqlParameter>();
            sqlParams.Add(new SqlParameter("@pPatientId", patientId));

            try
            {
                dtResult = DALHelper.ExecuteDataTable(connectionString, CommandType.StoredProcedure, "[HSS].[uspProcareGetDemographics]", sqlParams.ToArray());
            }
            catch (Exception ex)
            {
                
                error = ex.Message;
            }

            return dtResult;
        }
        public static DataTable GetDocResults(string referenceId, string referralId, bool isDischarge, out string error)
        {
            DataTable dtResult = new DataTable();
            error = string.Empty;

            string connectionString = Convert.ToString(ConfigurationManager.ConnectionStrings["ConnIndiciDB"].ConnectionString);

            List<SqlParameter> sqlParams = new List<SqlParameter>();

            if (!string.IsNullOrWhiteSpace(referenceId))
                sqlParams.Add(new SqlParameter("@pReferenceId", referenceId));
            if (!string.IsNullOrWhiteSpace(referralId))
                sqlParams.Add(new SqlParameter("@pReferralId", referralId));

            sqlParams.Add(new SqlParameter("@pIsDischarge", isDischarge));

            try
            {
                dtResult = DALHelper.ExecuteDataTable(connectionString, CommandType.StoredProcedure, "[HSS].[uspGetDocResults]", sqlParams.ToArray());
            }
            catch (Exception ex)
            {
                error = ex.Message;
            }

            return dtResult;
        }
        public static DataTable GetDocuments(string patientId, string conString, string identifier, out string error)
        {
            DataTable dtResult = new DataTable();
            error = string.Empty;

            //string connectionString = Convert.ToString(ConfigurationManager.ConnectionStrings["ConnIndiciDB"].ConnectionString);
            string connectionString = conString;

            List<SqlParameter> sqlParams = new List<SqlParameter>();
            sqlParams.Add(new SqlParameter("@pPatientId", patientId));

            if (!string.IsNullOrWhiteSpace(identifier))
                sqlParams.Add(new SqlParameter("@pIdentifier", identifier));

            try
            {
                dtResult = DALHelper.ExecuteDataTable(connectionString, CommandType.StoredProcedure, "[HSS].[uspProcareGetDocuments]", sqlParams.ToArray());
            }
            catch (Exception ex)
            {
                error = ex.Message;
            }

            return dtResult;
        }

        public static DataTable GetSctidByULMCode(string Condition, out string error)
        {
            
            error = string.Empty;
            DataTable dtResult = new DataTable();
            try
            {
                string connectionString = Convert.ToString(ConfigurationManager.ConnectionStrings["ConnulmDB"].ConnectionString);
                //string connectionString = conString;

                List<SqlParameter> sqlParams = new List<SqlParameter>();

                sqlParams.Add(new SqlParameter("@pULMCondition", Condition));

                dtResult = DALHelper.ExecuteDataTable(connectionString, CommandType.StoredProcedure, "[nzulm].[uspProcareGetSctIdByULMCode]", sqlParams.ToArray());

            }
            catch (Exception ex)
            {
                error = ex.Message;
            }

            return dtResult;
        }

        public static DataTable GetLabResults(string patientId, string conString, string referenceId, out string error)
        {
            DataTable dtResult = new DataTable();
            error = string.Empty;

            // string connectionString = Convert.ToString(ConfigurationManager.ConnectionStrings["ConnIndiciDB"].ConnectionString);
            string connectionString = conString;

            List<SqlParameter> sqlParams = new List<SqlParameter>();

            if (!string.IsNullOrWhiteSpace(patientId))
                sqlParams.Add(new SqlParameter("@pPatientId", patientId));
            if (!string.IsNullOrWhiteSpace(referenceId))
                sqlParams.Add(new SqlParameter("@pReferenceId", referenceId));

            try
            {
                dtResult = DALHelper.ExecuteDataTable(connectionString, CommandType.StoredProcedure, "[HSS].[uspGetLabResults]", sqlParams.ToArray());
            }
            catch (Exception ex)
            {
                error = ex.Message;
            }

            return dtResult;
        }
        public static DataTable GetLabResults(string patientId, string conString, out string error)
        {
            error = string.Empty;
            return GetLabResults(patientId,conString, string.Empty, out error);
        }
        public static DataTable GetLabs(string patientId, string sortOrder, DateTime dtMinDate, DateTime dtMaxDate,
                                         out string error)
        {
            DataTable dtResult = new DataTable();
            error = string.Empty;

            string connectionString = Convert.ToString(ConfigurationManager.ConnectionStrings["ConnIndiciDB"].ConnectionString);

            List<SqlParameter> sqlParams = new List<SqlParameter>();
            sqlParams.Add(new SqlParameter("@pPatientId", patientId));

            if (!string.IsNullOrWhiteSpace(sortOrder))
                sqlParams.Add(new SqlParameter("@pSortOrder", sortOrder));
            if (!dtMinDate.Equals(DateTime.MinValue))
                sqlParams.Add(new SqlParameter("@pMinDate", dtMinDate));
            if (!dtMaxDate.Equals(DateTime.MinValue))
                sqlParams.Add(new SqlParameter("@pMaxDate", dtMaxDate));

            try
            {
                dtResult = DALHelper.ExecuteDataTable(connectionString, CommandType.StoredProcedure, "[HSS].[uspGetLabs]", sqlParams.ToArray());
            }
            catch (Exception ex)
            {
                error = ex.Message;
            }

            return dtResult;
        }
        public static DataTable GetMeasurement(string patientId, string encounterId, out string error)
        {
            DataTable dtResult = new DataTable();
            error = string.Empty;

            string connectionString = Convert.ToString(ConfigurationManager.ConnectionStrings["ConnIndiciDB"].ConnectionString);

            List<SqlParameter> sqlParams = new List<SqlParameter>();
            sqlParams.Add(new SqlParameter("@pPatientId", patientId));

            try
            {
                dtResult = DALHelper.ExecuteDataTable(connectionString, CommandType.StoredProcedure, "[HSS].[uspGetMeasurement]", sqlParams.ToArray());
            }
            catch (Exception ex)
            {
                error = ex.Message;
            }

            return dtResult;
        }
        public static DataTable GetMedicalAllergies(string patientId, string sortOrder, DateTime dtMinDate, DateTime dtMaxDate,
                                                    out string error)
        {
            DataTable dtResult = new DataTable();
            error = string.Empty;

            string connectionString = Convert.ToString(ConfigurationManager.ConnectionStrings["ConnIndiciDB"].ConnectionString);

            List<SqlParameter> sqlParams = new List<SqlParameter>();
            sqlParams.Add(new SqlParameter("@pPatientId", patientId));

            if (!string.IsNullOrWhiteSpace(sortOrder))
                sqlParams.Add(new SqlParameter("@pSortOrder", sortOrder));
            if (!dtMinDate.Equals(DateTime.MinValue))
                sqlParams.Add(new SqlParameter("@pMinDate", dtMinDate));
            if (!dtMaxDate.Equals(DateTime.MinValue))
                sqlParams.Add(new SqlParameter("@pMaxDate", dtMaxDate));

            try
            {
                dtResult = DALHelper.ExecuteDataTable(connectionString, CommandType.StoredProcedure, "[HSS].[uspGetAllergies]", sqlParams.ToArray());
            }
            catch (Exception ex)
            {
                error = ex.Message;
            }

            return dtResult;
        }
        public static DataTable GetMedications(string patientId, string conString, out string error)
        {
            error = string.Empty;
            return GetMedications(patientId,conString, string.Empty, DateTime.MinValue, DateTime.MinValue, null, out error);
        }
        public static DataTable GetMedications(string patientId, string conString, string sortOrder, DateTime dtMinDate, DateTime dtMaxDate,
                                                bool? isLongTerm, out string error)
        {
            DataTable dtResult = new DataTable();
            error = string.Empty;

            //string connectionString = Convert.ToString(ConfigurationManager.ConnectionStrings["ConnIndiciDB"].ConnectionString);
            string connectionString = conString;

            List<SqlParameter> sqlParams = new List<SqlParameter>();
            sqlParams.Add(new SqlParameter("@pPatientId", patientId));

            if (!string.IsNullOrWhiteSpace(sortOrder))
                sqlParams.Add(new SqlParameter("@pSortOrder", sortOrder));
            if (!dtMinDate.Equals(DateTime.MinValue))
                sqlParams.Add(new SqlParameter("@pMinDate", dtMinDate));
            if (!dtMaxDate.Equals(DateTime.MinValue))
                sqlParams.Add(new SqlParameter("@pMaxDate", dtMaxDate));
            if (isLongTerm != null)
                sqlParams.Add(new SqlParameter("@pIsLongTerm", isLongTerm));

            sqlParams.Add(new SqlParameter("@pShowStop", false));

            try
            {
                dtResult = DALHelper.ExecuteDataTable(connectionString, CommandType.StoredProcedure, "[HSS].[uspProcareGetMedications]", sqlParams.ToArray());
            }
            catch (Exception ex)
            {
                error = ex.Message;
            }

            return dtResult;
        }


        public static DataTable GetULMMedications(string patientId, string conString, string SctId, out string error)
        {
            error = string.Empty;
            return GetULMMedications(patientId, conString,SctId, string.Empty, DateTime.MinValue, DateTime.MinValue, null, out error);
        }

        public static DataTable GetULMMedications(string patientId, string conString,string SctId, string sortOrder, DateTime dtMinDate, DateTime dtMaxDate,
                                                bool? isLongTerm, out string error)
        {
            DataTable dtResult = new DataTable();
            error = string.Empty;

            //string connectionString = Convert.ToString(ConfigurationManager.ConnectionStrings["ConnIndiciDB"].ConnectionString);
            string connectionString = conString;

            List<SqlParameter> sqlParams = new List<SqlParameter>();
            sqlParams.Add(new SqlParameter("@pPatientId", patientId));

            sqlParams.Add(new SqlParameter("@pSctId", SctId));

            if (!string.IsNullOrWhiteSpace(sortOrder))
                sqlParams.Add(new SqlParameter("@pSortOrder", sortOrder));
            if (!dtMinDate.Equals(DateTime.MinValue))
                sqlParams.Add(new SqlParameter("@pMinDate", dtMinDate));
            if (!dtMaxDate.Equals(DateTime.MinValue))
                sqlParams.Add(new SqlParameter("@pMaxDate", dtMaxDate));
            if (isLongTerm != null)
                sqlParams.Add(new SqlParameter("@pIsLongTerm", isLongTerm));

            sqlParams.Add(new SqlParameter("@pShowStop", false));

            try
            {
                dtResult = DALHelper.ExecuteDataTable(connectionString, CommandType.StoredProcedure, "[HSS].[uspProcareGetMedications_ulm]", sqlParams.ToArray());
            }
            catch (Exception ex)
            {
                error = ex.Message;
            }

            return dtResult;
        }
        public static DataTable GetNextOfKin(string patientId, out string error)
        {
            DataTable dtResult = new DataTable();
            error = string.Empty;

            string connectionString = Convert.ToString(ConfigurationManager.ConnectionStrings["ConnIndiciDB"].ConnectionString);

            List<SqlParameter> sqlParams = new List<SqlParameter>();
            sqlParams.Add(new SqlParameter("@pPatientId", patientId));

            try
            {
                dtResult = DALHelper.ExecuteDataTable(connectionString, CommandType.StoredProcedure, "[HSS].[uspGetNextOfKin]", sqlParams.ToArray());
            }
            catch (Exception ex)
            {
                error = ex.Message;
            }

            return dtResult;
        }
        public static DataTable GetObservations(string patientId, string conceptId, string conString, out string error, string sortColumn = "", string sortOrder = "", DateTime? MinDate = null, DateTime? MaxDate = null, int PageNo = 1, int PageSize = 100, int level = 0)
        {
            DataTable dtResult = new DataTable();
            error = string.Empty;

            // string connectionString = Convert.ToString(ConfigurationManager.ConnectionStrings["ConnIndiciDB"].ConnectionString);
            string connectionString = conString;
            List<SqlParameter> sqlParams = new List<SqlParameter>();
            sqlParams.Add(new SqlParameter("@pPatientId", patientId));

            if (!string.IsNullOrWhiteSpace(conceptId))
                sqlParams.Add(new SqlParameter("@pScreeningCode", conceptId));

            if (!string.IsNullOrWhiteSpace(sortColumn))
                sqlParams.Add(new SqlParameter("@pSortColumn", sortColumn));
            if (!MinDate.Equals(DateTime.MinValue))
                sqlParams.Add(new SqlParameter("@pDateFrom", MinDate));
            if (!MaxDate.Equals(DateTime.MinValue))
                sqlParams.Add(new SqlParameter("@pDateTo", MaxDate));
            if (!PageNo.Equals(0))
                sqlParams.Add(new SqlParameter("@PageNo", PageNo));
            if (!PageSize.Equals(0))
                sqlParams.Add(new SqlParameter("@PageSize", PageSize));
            //if (!sortOrder.Equals(0))
            //    sqlParams.Add(new SqlParameter("@pSortColumnIndex", sortOrder));

            if (!string.IsNullOrWhiteSpace(sortOrder))
                sqlParams.Add(new SqlParameter("@pSortColumnIndex", sortOrder));

            if (!level.Equals(0))
                sqlParams.Add(new SqlParameter("@pLevel", level));

            try
            {
                dtResult = DALHelper.ExecuteDataTable(connectionString, CommandType.StoredProcedure, "[HSS].[uspProcareGetObservations2]", sqlParams.ToArray());
            }
            catch (Exception ex)
            {

                error = ex.Message;
            }

            return dtResult;
        }

        public static DataTable GetEncounterSummary(string patientId, string conString, string identifier, out string error)
        {
            DataTable dtResult = new DataTable();
            error = string.Empty;

            //string connectionString = Convert.ToString(ConfigurationManager.ConnectionStrings["ConnIndiciDB"].ConnectionString);
            string connectionString = conString;

            List<SqlParameter> sqlParams = new List<SqlParameter>();
            sqlParams.Add(new SqlParameter("@pPatientID", patientId));

            if (!string.IsNullOrWhiteSpace(identifier))
                sqlParams.Add(new SqlParameter("@pIdentifier", identifier));
           

            try
            {
                dtResult = DALHelper.ExecuteDataTable(connectionString, CommandType.StoredProcedure, "[HSS].[uspProcareGetScreeningSummary]", sqlParams.ToArray());
            }
            catch (Exception ex)
            {
                error = ex.Message;
            }

            return dtResult;
        }


        /*    public static DataTable GetNewObservations(string patientId, string conceptId, string conString, out string error, string sortColumn = "", string sortOrder = "", DateTime? MinDate = null, DateTime? MaxDate = null, int PageNo = 1, int PageSize = 100, int level = 0)
            {
                DataTable dtResult = new DataTable();
                error = string.Empty;

                // string connectionString = Convert.ToString(ConfigurationManager.ConnectionStrings["ConnIndiciDB"].ConnectionString);
                string connectionString = conString;
                List<SqlParameter> sqlParams = new List<SqlParameter>();
                sqlParams.Add(new SqlParameter("@pPatientId", patientId));

                if (!string.IsNullOrWhiteSpace(conceptId))
                    sqlParams.Add(new SqlParameter("@pScreeningCode", conceptId));

                if (!string.IsNullOrWhiteSpace(sortColumn))
                    sqlParams.Add(new SqlParameter("@pSortColumn", sortColumn));
                if (!MinDate.Equals(DateTime.MinValue))
                    sqlParams.Add(new SqlParameter("@pDateFrom", MinDate));
                if (!MaxDate.Equals(DateTime.MinValue))
                    sqlParams.Add(new SqlParameter("@pDateTo", MaxDate));
                if (!PageNo.Equals(0))
                    sqlParams.Add(new SqlParameter("@PageNo", PageNo));
                if (!PageSize.Equals(0))
                    sqlParams.Add(new SqlParameter("@PageSize", PageSize));
                //if (!sortOrder.Equals(0))
                //    sqlParams.Add(new SqlParameter("@pSortColumnIndex", sortOrder));

                if (!string.IsNullOrWhiteSpace(sortOrder))
                    sqlParams.Add(new SqlParameter("@pSortColumnIndex", sortOrder));

                if (!level.Equals(0))
                    sqlParams.Add(new SqlParameter("@pLevel", level));

                try
                {
                    dtResult = DALHelper.ExecuteDataTable(connectionString, CommandType.StoredProcedure, "[HSS].[uspProcareGetObservations2]", sqlParams.ToArray());
                }
                catch (Exception ex)
                {

                    error = ex.Message;
                }

                return dtResult;
            }*/

        public static DataTable GetOtherDocs(string patientId, string sortOrder, DateTime dtMinDate, DateTime dtMaxDate, bool isReferral,
                                             out string error)
        {
            DataTable dtResult = new DataTable();
            //DataTable dtResultwithAttachment = new DataTable();
            error = string.Empty;

            string connectionString = Convert.ToString(ConfigurationManager.ConnectionStrings["ConnIndiciDB"].ConnectionString);

            List<SqlParameter> sqlParams = new List<SqlParameter>();
            sqlParams.Add(new SqlParameter("@pPatientId", patientId));

            if (!string.IsNullOrWhiteSpace(sortOrder))
                sqlParams.Add(new SqlParameter("@pSortOrder", sortOrder));
            if (!dtMinDate.Equals(DateTime.MinValue))
                sqlParams.Add(new SqlParameter("@pMinDate", dtMinDate));
            if (!dtMaxDate.Equals(DateTime.MinValue))
                sqlParams.Add(new SqlParameter("@pMaxDate", dtMaxDate));
            if (isReferral)
                sqlParams.Add(new SqlParameter("@pType", "Discharge Summary"));

            try
            {
                dtResult = DALHelper.ExecuteDataTable(connectionString, CommandType.StoredProcedure, "[HSS].[uspGetOtherDocs]", sqlParams.ToArray());
                //dtResultwithAttachment = DALHelper.ExecuteDataTable(connectionString, CommandType.StoredProcedure, "[HSS].[uspGetOtherDocswithAttachements]", sqlParams.ToArray());
            }
            catch (Exception ex)
            {
                error = ex.Message;
            }
            //dtResult.Merge(dtResultwithAttachment);
            return dtResult;
        }
        public static DataTable GetProvider(string patientId, out string error)
        {
            error = string.Empty;

            return GetProvider(patientId,string.Empty, string.Empty, string.Empty, out error);
        }
        public static DataTable GetProvider(string patientId, string conString, string userId, string LocationId, out string error)
        {
            DataTable dtResult = new DataTable();
            error = string.Empty;

            //string connectionString = Convert.ToString(ConfigurationManager.ConnectionStrings["ConnIndiciDB"].ConnectionString);
            string connectionString = conString;

            List<SqlParameter> sqlParams = new List<SqlParameter>();
            sqlParams.Add(new SqlParameter("@pPatientId", patientId));
            if (!string.IsNullOrWhiteSpace(LocationId))
                sqlParams.Add(new SqlParameter("@pLocationId", LocationId));

            if (!string.IsNullOrWhiteSpace(userId))
                sqlParams.Add(new SqlParameter("@pUserId", userId));

            try
            {
                dtResult = DALHelper.ExecuteDataTable(connectionString, CommandType.StoredProcedure, "[HSS].[uspProcareGetProvider]", sqlParams.ToArray());
            }
            catch (Exception ex)
            {
                error = ex.Message;
            }

            return dtResult;
        }
        public static DataTable GetRadResults(string patientId, string referenceId, out string error)
        {
            DataTable dtResult = new DataTable();
            error = string.Empty;

            string connectionString = Convert.ToString(ConfigurationManager.ConnectionStrings["ConnIndiciDB"].ConnectionString);

            List<SqlParameter> sqlParams = new List<SqlParameter>();

            if (!string.IsNullOrWhiteSpace(patientId))
                sqlParams.Add(new SqlParameter("@pPatientId", patientId));
            if (!string.IsNullOrWhiteSpace(referenceId))
                sqlParams.Add(new SqlParameter("@pReferenceId", referenceId));

            try
            {
                dtResult = DALHelper.ExecuteDataTable(connectionString, CommandType.StoredProcedure, "[HSS].[uspGetRadResults]", sqlParams.ToArray());
            }
            catch (Exception ex)
            {
                error = ex.Message;
            }

            return dtResult;
        }
        public static DataTable GetRads(string patientId, string sortOrder, DateTime dtMinDate, DateTime dtMaxDate,
                                         out string error)
        {
            DataTable dtResult = new DataTable();
            error = string.Empty;

            string connectionString = Convert.ToString(ConfigurationManager.ConnectionStrings["ConnIndiciDB"].ConnectionString);

            List<SqlParameter> sqlParams = new List<SqlParameter>();
            sqlParams.Add(new SqlParameter("@pPatientId", patientId));

            if (!string.IsNullOrWhiteSpace(sortOrder))
                sqlParams.Add(new SqlParameter("@pSortOrder", sortOrder));
            if (!dtMinDate.Equals(DateTime.MinValue))
                sqlParams.Add(new SqlParameter("@pMinDate", dtMinDate));
            if (!dtMaxDate.Equals(DateTime.MinValue))
                sqlParams.Add(new SqlParameter("@pMaxDate", dtMaxDate));

            try
            {
                dtResult = DALHelper.ExecuteDataTable(connectionString, CommandType.StoredProcedure, "[HSS].[uspGetRads]", sqlParams.ToArray());
            }
            catch (Exception ex)
            {
                error = ex.Message;
            }

            return dtResult;
        }
        public static DataTable GetRecallCategories(string group, out string error)
        {
            DataTable dtResult = new DataTable();
            error = string.Empty;

            string connectionString = Convert.ToString(ConfigurationManager.ConnectionStrings["ConnIndiciDB"].ConnectionString);

            List<SqlParameter> sqlParams = new List<SqlParameter>();
            sqlParams.Add(new SqlParameter("@pRecallGroup", group));

            try
            {
                dtResult = DALHelper.ExecuteDataTable(connectionString, CommandType.StoredProcedure, "[HSS].[uspGetRecallCategories]", sqlParams.ToArray());
            }
            catch (Exception ex)
            {
                error = ex.Message;
            }

            return dtResult;
        }
        public static DataTable GetRecalls(string patientId, out string error)
        {
            DataTable dtResult = new DataTable();
            error = string.Empty;

            string connectionString = Convert.ToString(ConfigurationManager.ConnectionStrings["ConnIndiciDB"].ConnectionString);

            List<SqlParameter> sqlParams = new List<SqlParameter>();
            sqlParams.Add(new SqlParameter("@pPatientId", patientId));

            try
            {
                dtResult = DALHelper.ExecuteDataTable(connectionString, CommandType.StoredProcedure, "[HSS].[uspGetRecalls]", sqlParams.ToArray());
            }
            catch (Exception ex)
            {
                error = ex.Message;
            }

            return dtResult;
        }
        public static DataTable GetRegisteredPractitioners(string patientId, string locationId, out string error)
        {
            DataTable dtResult = new DataTable();
            error = string.Empty;

            string connectionString = Convert.ToString(ConfigurationManager.ConnectionStrings["ConnIndiciDB"].ConnectionString);

            List<SqlParameter> sqlParams = new List<SqlParameter>();
            sqlParams.Add(new SqlParameter("@pPatientId", patientId));

            if (!string.IsNullOrWhiteSpace(locationId))
                sqlParams.Add(new SqlParameter("@pLocationId", locationId));

            try
            {
                dtResult = DALHelper.ExecuteDataTable(connectionString, CommandType.StoredProcedure, "[HSS].[uspGetRegisteredPractitioners]", sqlParams.ToArray());
            }
            catch (Exception ex)
            {
                error = ex.Message;
            }

            return dtResult;
        }
        public static DataTable GetScreeningCodes(string practiceId, string conString, out string error)
        {
            DataTable dtResult = new DataTable();
            error = string.Empty;

            //  string connectionString = Convert.ToString(ConfigurationManager.ConnectionStrings["ConnIndiciDB"].ConnectionString);
            string connectionString = conString;

            List<SqlParameter> sqlParams = new List<SqlParameter>();
            sqlParams.Add(new SqlParameter("@pPracticeId", practiceId));

            try
            {
                dtResult = DALHelper.ExecuteDataTable(connectionString, CommandType.StoredProcedure, "[HSS].[uspProcareGetScreeningCodes]", sqlParams.ToArray());
            }
            catch (Exception ex)
            {
                error = ex.Message;
            }

            return dtResult;
        }
        public static DataTable GetSmokingStatus(string patientId, out string error)
        {
            DataTable dtResult = new DataTable();
            error = string.Empty;

            string connectionString = Convert.ToString(ConfigurationManager.ConnectionStrings["ConnIndiciDB"].ConnectionString);

            List<SqlParameter> sqlParams = new List<SqlParameter>();
            sqlParams.Add(new SqlParameter("@pPatientId", patientId));

            try
            {
                dtResult = DALHelper.ExecuteDataTable(connectionString, CommandType.StoredProcedure, "[HSS].[uspGetSmokingStatus]", sqlParams.ToArray());
            }
            catch (Exception ex)
            {
                error = ex.Message;
            }

            return dtResult;
        }
        public static DataTable InsertAndValidateToken(string userName, string password, string patientId, string appointmentId,
                                             string token, out string error)
        {
            error = string.Empty;
            return InsertAndValidateToken(userName, password, patientId, appointmentId, token, 0, out error);
        }
        public static DataTable InsertAndValidateToken(string userName, string password, string patientId, string appointmentId,
                                             string token, double expiryInDays, out string error)
        {
            DataTable dtResult = new DataTable();
            error = string.Empty;

            string connectionString = Convert.ToString(ConfigurationManager.ConnectionStrings["ConnIndiciDB"].ConnectionString);

            List<SqlParameter> sqlParams = new List<SqlParameter>();

            if (!string.IsNullOrWhiteSpace(userName))
                sqlParams.Add(new SqlParameter("@pUsername", userName));
            if (!string.IsNullOrWhiteSpace(password))
                sqlParams.Add(new SqlParameter("@pPassword", password));
            if (!string.IsNullOrWhiteSpace(patientId))
                sqlParams.Add(new SqlParameter("@pPatientID", patientId));
            if (!string.IsNullOrWhiteSpace(appointmentId))
                sqlParams.Add(new SqlParameter("@pEnounterID", appointmentId));
            if (!string.IsNullOrWhiteSpace(token))
                sqlParams.Add(new SqlParameter("@pToken", token));
            

            try
            {
                dtResult = DALHelper.ExecuteDataTable(connectionString, CommandType.StoredProcedure, "[Auth].[uspProcareValidateToken]", sqlParams.ToArray());
            }
            catch (Exception ex)
            {
                error = ex.Message;
            }

            return dtResult;
        }
        public static bool InsertAndValidateToken(string patientId, string appointmentId, string token, out string error)
        {
            error = string.Empty;
            DataTable dtResults = InsertAndValidateToken(string.Empty, string.Empty, patientId, appointmentId, token, out error);

            return (dtResults.Rows.Count > 0
                    && string.IsNullOrEmpty(Utility.Instance.ToString(dtResults.Rows[0]["StatusMessage"])));
        }

     /*   public static int InsertDocument(string patientId, string messageSubject, string dmsId, int itemTypeId, out string error)
        {
            error = string.Empty;
            return InsertDocument(patientId, messageSubject, dmsId, itemTypeId, DateTime.MinValue, "1", out error);
        }*/
        public static int InsertDocument(string patientId, string conString, string encounterId, string messageSubject, string dmsId, int itemTypeId, out string error)
        {
            error = string.Empty;
            return InsertDocument(patientId,conString, encounterId, messageSubject, dmsId, itemTypeId, DateTime.MinValue, "30", string.Empty, string.Empty,out error);
        }
        /*public static int InsertDocument(string patientId, string messageSubject, string dmsId, int itemTypeId, DateTime dtResult,
                                        string dataSourceId, out string error)
        {
            error = string.Empty;
            return InsertDocument(patientId,string.Empty, string.Empty, messageSubject, dmsId, itemTypeId, dtResult, dataSourceId, string.Empty, string.Empty, out error);
        }*/
        //public static int InsertDocument(string patientId, string messageSubject, string dmsId, int itemTypeId, DateTime dtResult,
        //                                string dataSourceId, string referralId, string userId, out string error)
        //{
        //    error = string.Empty;
        //    return InsertDocument(patientId,string.Empty, string.Empty, messageSubject, dmsId, itemTypeId, dtResult, dataSourceId, referralId, userId, out error);
        //}
        private static int InsertDocument(string patientId, string conString,  string encounterId, string messageSubject, string dmsId, int itemTypeId, DateTime dtResult,
                                         string dataSourceId, string referralId, string userId, out string error)
        {
            int result = 0;
            error = string.Empty;

            try
            {
                //string connectionString = Convert.ToString(ConfigurationManager.ConnectionStrings["ConnIndiciDB"].ConnectionString);
                string connectionString = conString;

                using (SqlConnection objConn = new SqlConnection(connectionString))
                {
                    List<SqlParameter> sqlParams = new List<SqlParameter>();

                    sqlParams.Add(new SqlParameter("@pPatientID", Convert.ToInt32(patientId)));
                    sqlParams.Add(new SqlParameter("@pMessageSubject", messageSubject));
                    sqlParams.Add(new SqlParameter("@pDMSID", dmsId));
                    sqlParams.Add(new SqlParameter("@pInboxItemTypeID", itemTypeId));

                    if (!dtResult.Equals(DateTime.MinValue))
                        sqlParams.Add(new SqlParameter("@pResultDate", dtResult));
                    if (!string.IsNullOrWhiteSpace(dataSourceId))
                        sqlParams.Add(new SqlParameter("@pDataSourceId", dataSourceId));
                    if (!string.IsNullOrWhiteSpace(referralId))
                        sqlParams.Add(new SqlParameter("@pReferralId", referralId));
                    if (!string.IsNullOrWhiteSpace(userId))
                        sqlParams.Add(new SqlParameter("@pUserId", userId));
                    if (!string.IsNullOrWhiteSpace(encounterId))
                        sqlParams.Add(new SqlParameter("@pEnounterId", encounterId));


                    SqlParameter sqlParamOut = new SqlParameter("@pOutputParam", SqlDbType.Int);
                    sqlParamOut.Direction = ParameterDirection.Output;
                    sqlParamOut.Value = -1;
                    sqlParams.Add(sqlParamOut);

                    DALHelper.ExecuteNonQuery(objConn, CommandType.StoredProcedure, "[HSS].[uspProcareInsertDocument]", sqlParams.ToArray());
                    result = Convert.ToInt32(sqlParams[sqlParams.Count - 1].Value);
                }
            }
            catch (Exception ex)
            {
                error = ex.Message;
            }

            return result;
        }


        public static int InsertDocument(string patientId, string conString, string encounterId, string messageSubject, string dmsId, int itemTypeId, DateTime dtResult,
                                         string dataSourceId, string referralId, string userId, string ContentType, string EDI, string AutoSend, out string error)
        {
            int result = 0;
            error = string.Empty;

            try
            {
                //string connectionString = Convert.ToString(ConfigurationManager.ConnectionStrings["ConnIndiciDB"].ConnectionString);
                string connectionString = conString;

                using (SqlConnection objConn = new SqlConnection(connectionString))
                {
                    List<SqlParameter> sqlParams = new List<SqlParameter>();

                    sqlParams.Add(new SqlParameter("@pPatientID", Convert.ToInt32(patientId)));
                    sqlParams.Add(new SqlParameter("@pMessageSubject", messageSubject));
                    sqlParams.Add(new SqlParameter("@pDMSID", dmsId));
                    sqlParams.Add(new SqlParameter("@pInboxItemTypeID", itemTypeId));

                    if (!dtResult.Equals(DateTime.MinValue))
                        sqlParams.Add(new SqlParameter("@pResultDate", dtResult));
                    if (!string.IsNullOrWhiteSpace(dataSourceId))
                        sqlParams.Add(new SqlParameter("@pDataSourceId", dataSourceId));
                    if (!string.IsNullOrWhiteSpace(referralId))
                        sqlParams.Add(new SqlParameter("@pReferralId", referralId));
                    if (!string.IsNullOrWhiteSpace(userId))
                        sqlParams.Add(new SqlParameter("@pUserId", userId));
                    if (!string.IsNullOrWhiteSpace(encounterId))
                        sqlParams.Add(new SqlParameter("@pEnounterId", encounterId));

                    if (!string.IsNullOrWhiteSpace(ContentType))
                        sqlParams.Add(new SqlParameter("@pContentType", ContentType));

                    if (!string.IsNullOrWhiteSpace(EDI))
                        sqlParams.Add(new SqlParameter("@pEDI", EDI));

                    if (!string.IsNullOrWhiteSpace(AutoSend))
                        sqlParams.Add(new SqlParameter("@pAutoSend", AutoSend));


                    SqlParameter sqlParamOut = new SqlParameter("@pOutputParam", SqlDbType.Int);
                    sqlParamOut.Direction = ParameterDirection.Output;
                    sqlParamOut.Value = -1;
                    sqlParams.Add(sqlParamOut);
                    // Create New SP its temp
                    DALHelper.ExecuteNonQuery(objConn, CommandType.StoredProcedure, "[HSS].[uspProcareInsertLetterDocument]", sqlParams.ToArray());
                    result = Convert.ToInt32(sqlParams[sqlParams.Count - 1].Value);
                }
            }
            catch (Exception ex)
            {
                error = ex.Message;
            }

            return result;
        }


        public static int InsertUpdateInvoice(string patientId, string conString, string encounterId, string subServiceCode,
                                              string fee, string userId,string contentType, byte[] messageData,string dmsGuidKey,string EncounterID,string notes, out string error)
        {
            int result = 0;
            error = string.Empty;

            try
            {
                //string connectionString = Convert.ToString(ConfigurationManager.ConnectionStrings["ConnIndiciDB"].ConnectionString);
                string connectionString = conString;
                using (SqlConnection objConn = new SqlConnection(connectionString))
                {
                    List<SqlParameter> sqlParams = new List<SqlParameter>();

                    sqlParams.Add(new SqlParameter("@pPatientID", Convert.ToInt32(patientId)));
                    sqlParams.Add(new SqlParameter("@pAppointmentId", string.Empty));
                    sqlParams.Add(new SqlParameter("@pEncounterId", EncounterID));
                    //  sqlParams.Add(new SqlParameter("@pMasterServiceName", masterServiceName));
                    //   sqlParams.Add(new SqlParameter("@pSubServiceName", subServiceName));

                    if (!string.IsNullOrWhiteSpace(subServiceCode))
                        sqlParams.Add(new SqlParameter("@pSubServiceCode", subServiceCode));
                    if (!string.IsNullOrWhiteSpace(fee))
                        sqlParams.Add(new SqlParameter("@pFee", fee));
                    //if (!string.IsNullOrWhiteSpace(locationId))
                    //    sqlParams.Add(new SqlParameter("@pLocationId", locationId));
                    if (!string.IsNullOrWhiteSpace(userId))
                        sqlParams.Add(new SqlParameter("@pUserId", userId));

                    if (!string.IsNullOrWhiteSpace(dmsGuidKey))
                        sqlParams.Add(new SqlParameter("@pDMSGuidKey", dmsGuidKey));

                    if (!string.IsNullOrWhiteSpace(notes))
                        sqlParams.Add(new SqlParameter("@pNotes", notes));

                    //        if (!string.IsNullOrWhiteSpace(companyName))
                    //            sqlParams.Add(new SqlParameter("@pCompanyName", companyName));

                    /*        if (!string.IsNullOrWhiteSpace(contentType))
                                sqlParams.Add(new SqlParameter("@pContentType", contentType));

                            SqlParameter sqlParamContents = new SqlParameter("@pMessageData", SqlDbType.VarBinary);
                            sqlParamContents.Value = messageData;
                            sqlParams.Add(sqlParamContents);*/

                    SqlParameter sqlParamOut = new SqlParameter("@pOutputParam", SqlDbType.Int);
                    sqlParamOut.Direction = ParameterDirection.Output;
                    sqlParamOut.Value = -1;
                    sqlParams.Add(sqlParamOut);

                    DALHelper.ExecuteNonQuery(objConn, CommandType.StoredProcedure, "[HSS].[uspProcareInsertInvoiceData]", sqlParams.ToArray());
                    result = Convert.ToInt32(sqlParams[sqlParams.Count - 1].Value);
                }
            }
            catch (Exception ex)
            {
                error = ex.Message;
            }

            return result;
        }
        public static int InsertUpdateService(string patientId, string conString, string encounterId, string masterServiceName, string subServiceName, string subServiceCode,
                                              string fee, string userId, string locationId, out string error)
        {
            int result = 0;
            error = string.Empty;

            try
            {
                //string connectionString = Convert.ToString(ConfigurationManager.ConnectionStrings["ConnIndiciDB"].ConnectionString);
                string connectionString = conString;
                using (SqlConnection objConn = new SqlConnection(connectionString))
                {
                    List<SqlParameter> sqlParams = new List<SqlParameter>();

                    sqlParams.Add(new SqlParameter("@pPatientID", Convert.ToInt32(patientId)));
                    sqlParams.Add(new SqlParameter("@pAppointmentId", string.Empty));
                    sqlParams.Add(new SqlParameter("@pMasterServiceName", masterServiceName));
                    sqlParams.Add(new SqlParameter("@pSubServiceName", subServiceName));

                    if (!string.IsNullOrWhiteSpace(subServiceCode))
                        sqlParams.Add(new SqlParameter("@pSubServiceCode", subServiceCode));
                    if (!string.IsNullOrWhiteSpace(fee))
                        sqlParams.Add(new SqlParameter("@pFee", fee));
                    if (!string.IsNullOrWhiteSpace(locationId))
                        sqlParams.Add(new SqlParameter("@pLocationId", locationId));
                    if (!string.IsNullOrWhiteSpace(userId))
                        sqlParams.Add(new SqlParameter("@pUserId", userId));

                    SqlParameter sqlParamOut = new SqlParameter("@pOutputParam", SqlDbType.Int);
                    sqlParamOut.Direction = ParameterDirection.Output;
                    sqlParamOut.Value = -1;
                    sqlParams.Add(sqlParamOut);

                    DALHelper.ExecuteNonQuery(objConn, CommandType.StoredProcedure, "[HSS].[uspProcareInsertUpdateService]", sqlParams.ToArray());
                    result = Convert.ToInt32(sqlParams[sqlParams.Count - 1].Value);
                }
            }
            catch (Exception ex)
            {
                error = ex.Message;
            }

            return result;
        }
        public static int InsertUpdateConsultNotes(string patientId, string appointmentId, string subjective, string objective, string assessment, string plans,
                                                   out string error)
        {
            int result = 0;
            error = string.Empty;

            try
            {
                string connectionString = Convert.ToString(ConfigurationManager.ConnectionStrings["ConnIndiciDB"].ConnectionString);

                using (SqlConnection objConn = new SqlConnection(connectionString))
                {
                    List<SqlParameter> sqlParams = new List<SqlParameter>();

                    sqlParams.Add(new SqlParameter("@pPatientId", Convert.ToInt32(patientId)));
                    sqlParams.Add(new SqlParameter("@pAppointmentId", Convert.ToInt64(appointmentId)));

                    if (!string.IsNullOrWhiteSpace(subjective))
                        sqlParams.Add(new SqlParameter("@pSubjective", subjective));

                    if (!string.IsNullOrWhiteSpace(objective))
                        sqlParams.Add(new SqlParameter("@pObjective", objective));

                    if (!string.IsNullOrWhiteSpace(assessment))
                        sqlParams.Add(new SqlParameter("@pAssessment", assessment));

                    if (!string.IsNullOrWhiteSpace(plans))
                        sqlParams.Add(new SqlParameter("@pPlans", plans));

                    SqlParameter sqlParamOut = new SqlParameter("@pOutputParam", SqlDbType.Int);
                    sqlParamOut.Direction = ParameterDirection.Output;
                    sqlParamOut.Value = -1;
                    sqlParams.Add(sqlParamOut);

                    DALHelper.ExecuteNonQuery(objConn, CommandType.StoredProcedure, "[HSS].[uspInsertUpdateConsultNotes]", sqlParams.ToArray());
                    result = Convert.ToInt32(sqlParams[sqlParams.Count - 1].Value);
                }
            }
            catch (Exception ex)
            {
                error = ex.Message;
            }

            return result;
        }

        public static DataTable GetPractice(string patientId, string connectionString, out string error)
        {
            DataTable dtResult = new DataTable();
            error = string.Empty;

            //string connectionString = Convert.ToString(ConfigurationManager.ConnectionStrings["ConnIndiciDB"].ConnectionString);
          //  string connectionString = connectionString;

            List<SqlParameter> sqlParams = new List<SqlParameter>();
            sqlParams.Add(new SqlParameter("@pPatientId", patientId));
           

            try
            {
                dtResult = DALHelper.ExecuteDataTable(connectionString, CommandType.StoredProcedure, "[Admin].[uspProcareGetPracticeByPatientD]", sqlParams.ToArray());
            }
            catch (Exception ex)
            {
                error = ex.Message;
            }

            return dtResult;
        }

        public static int InsertUpdateDiagnosis(string patientId, string conString, string appointmentId, string userId, string diagnosisType, DateTime dtOnsetDate, string summary,
                                                  bool isLongTerm, string conceptId, string diseaseName, string fsn, out string error)
        {
            int result = 0;
            error = string.Empty;

            try
            {
                //string connectionString = Convert.ToString(ConfigurationManager.ConnectionStrings["ConnIndiciDB"].ConnectionString);
                string connectionString = conString;

                using (SqlConnection objConn = new SqlConnection(connectionString))
                {
                    List<SqlParameter> sqlParams = new List<SqlParameter>();

                    sqlParams.Add(new SqlParameter("@pPatientId", Convert.ToInt32(patientId)));
                    sqlParams.Add(new SqlParameter("@pAppointmentId", 0));

                    if (!string.IsNullOrWhiteSpace(diagnosisType))
                        sqlParams.Add(new SqlParameter("@pDiagnosisType", diagnosisType));
                    if (!dtOnsetDate.Equals(DateTime.MinValue))
                        sqlParams.Add(new SqlParameter("@pOnsetDate", dtOnsetDate));
                    if (!string.IsNullOrWhiteSpace(summary))
                        sqlParams.Add(new SqlParameter("@pSummary", summary));
                    if (!string.IsNullOrWhiteSpace(conceptId))
                        sqlParams.Add(new SqlParameter("@pConceptId", conceptId));
                    if (!string.IsNullOrWhiteSpace(diseaseName))
                        sqlParams.Add(new SqlParameter("@pDiseaseName", diseaseName));
                    if (!string.IsNullOrWhiteSpace(fsn))
                        sqlParams.Add(new SqlParameter("@pFSN", fsn));

                    sqlParams.Add(new SqlParameter("@pIsLongTerm", isLongTerm));

                    SqlParameter sqlParamOut = new SqlParameter("@pOutputParam", SqlDbType.Int);
                    sqlParamOut.Direction = ParameterDirection.Output;
                    sqlParamOut.Value = -1;
                    sqlParams.Add(sqlParamOut);

                    DALHelper.ExecuteNonQuery(objConn, CommandType.StoredProcedure, "[HSS].[uspProcareInsertUpdateDiagnosis]", sqlParams.ToArray());
                    result = Convert.ToInt32(sqlParams[sqlParams.Count - 1].Value);
                }
            }
            catch (Exception ex)
            {
                error = ex.Message;
            }

            return result;
        }
        public static int InsertUpdateObservation(string patientId, string conString, string appointmentId, string userId, string temperature, string waist, string height,
                                                  string weight, string bpSys, string bpDia, string heartRate, string notes,
                                                  string risk, string framingham, out string error)
        {
            int result = 0;
            error = string.Empty;

            try
            {
                // string connectionString = Convert.ToString(ConfigurationManager.ConnectionStrings["ConnIndiciDB"].ConnectionString);
                string connectionString = conString;
                using (SqlConnection objConn = new SqlConnection(connectionString))
                {
                    List<SqlParameter> sqlParams = new List<SqlParameter>();

                    sqlParams.Add(new SqlParameter("@pPatientId", Convert.ToInt32(patientId)));
              //      sqlParams.Add(new SqlParameter("@pAppointmentId", Convert.ToInt64(appointmentId)));

                    if (!string.IsNullOrWhiteSpace(temperature))
                        sqlParams.Add(new SqlParameter("@pTemperature", temperature));
                    if (!string.IsNullOrWhiteSpace(waist))
                        sqlParams.Add(new SqlParameter("@pWaist", waist));
                    if (!string.IsNullOrWhiteSpace(height))
                        sqlParams.Add(new SqlParameter("@pHeight", height));
                    if (!string.IsNullOrWhiteSpace(weight))
                        sqlParams.Add(new SqlParameter("@pWeight", weight));
                    if (!string.IsNullOrWhiteSpace(bpSys))
                        sqlParams.Add(new SqlParameter("@pBPSys", bpSys));
                    if (!string.IsNullOrWhiteSpace(bpDia))
                        sqlParams.Add(new SqlParameter("@pBPDia", bpDia));
                    if (!string.IsNullOrWhiteSpace(heartRate))
                        sqlParams.Add(new SqlParameter("@pHeartRate", heartRate));
                    if (!string.IsNullOrWhiteSpace(risk))
                        sqlParams.Add(new SqlParameter("@pRisk", risk));
                    if (!string.IsNullOrWhiteSpace(framingham))
                        sqlParams.Add(new SqlParameter("@pFramingham", framingham));
                    if (!string.IsNullOrWhiteSpace(notes))
                        sqlParams.Add(new SqlParameter("@pNotes", notes));

                    SqlParameter sqlParamOut = new SqlParameter("@pOutputParam", SqlDbType.Int);
                    sqlParamOut.Direction = ParameterDirection.Output;
                    sqlParamOut.Value = -1;
                    sqlParams.Add(sqlParamOut);

                    DALHelper.ExecuteNonQuery(objConn, CommandType.StoredProcedure, "[HSS].[uspProcareInsertUpdateObservation]", sqlParams.ToArray());
                    result = Convert.ToInt32(sqlParams[sqlParams.Count - 1].Value);
                }
            }
            catch (Exception ex)
            {
                
                error = ex.Message;
            }

            return result;
        }


        public static int InsertUpdateObservation(string patientId, string conString, string userId, string strConceptID,string strValue, string strConceptID2, string strValue2, string notes,
                                                   out string error)
        {
            int result = 0;
            error = string.Empty;

            try
            {
                // string connectionString = Convert.ToString(ConfigurationManager.ConnectionStrings["ConnIndiciDB"].ConnectionString);
                string connectionString = conString;
                using (SqlConnection objConn = new SqlConnection(connectionString))
                {
                    List<SqlParameter> sqlParams = new List<SqlParameter>();

                    sqlParams.Add(new SqlParameter("@pPatientId", Convert.ToInt32(patientId)));

                    if (!string.IsNullOrWhiteSpace(strConceptID))
                        sqlParams.Add(new SqlParameter("@pConceptid", strConceptID));
                    if (!string.IsNullOrWhiteSpace(strValue))
                        sqlParams.Add(new SqlParameter("@pValue", strValue));

                    if (!string.IsNullOrWhiteSpace(strConceptID2))
                        sqlParams.Add(new SqlParameter("@pConceptid2", strConceptID2));
                    if (!string.IsNullOrWhiteSpace(strValue2))
                        sqlParams.Add(new SqlParameter("@pValue2", strValue2));

                    if (!string.IsNullOrWhiteSpace(notes))
                        sqlParams.Add(new SqlParameter("@pNotes", notes));

                    SqlParameter sqlParamOut = new SqlParameter("@pOutputParam", SqlDbType.Int);
                    sqlParamOut.Direction = ParameterDirection.Output;
                    sqlParamOut.Value = -1;
                    sqlParams.Add(sqlParamOut);

                    DALHelper.ExecuteNonQuery(objConn, CommandType.StoredProcedure, "[HSS].[uspProcareInsertObservationWithConceptId]", sqlParams.ToArray());
                    result = Convert.ToInt32(sqlParams[sqlParams.Count - 1].Value);
                }
            }
            catch (Exception ex)
            {

                error = ex.Message;
            }

            return result;
        }
        public static int InsertUpdateRecall(string patientId, string connectionString, string EncounterID, string userId, string priority, DateTime dtDueDate, string notes,
                                             string categoryCode, out string error)
        {
            int result = 0;
            error = string.Empty;

            try
            {
                //string connectionString = Convert.ToString(ConfigurationManager.ConnectionStrings["ConnIndiciDB"].ConnectionString);
                //string connectionString = conString;
                using (SqlConnection objConn = new SqlConnection(connectionString))
                {
                    List<SqlParameter> sqlParams = new List<SqlParameter>();

                    sqlParams.Add(new SqlParameter("@pPatientId", Convert.ToInt32(patientId)));
                    sqlParams.Add(new SqlParameter("@pEncounterId", EncounterID));

                    if (!string.IsNullOrWhiteSpace(priority))
                        sqlParams.Add(new SqlParameter("@pPriority", priority));
                    //if (!string.IsNullOrWhiteSpace(group))
                    //    sqlParams.Add(new SqlParameter("@pGroup", group));
                    if (!dtDueDate.Equals(DateTime.MinValue))
                        sqlParams.Add(new SqlParameter("@pDueDate", dtDueDate));
                    if (!string.IsNullOrWhiteSpace(notes))
                        sqlParams.Add(new SqlParameter("@pNotes", notes));
                    if (!string.IsNullOrWhiteSpace(userId))
                        sqlParams.Add(new SqlParameter("@pUserId", userId));
                    if (!string.IsNullOrWhiteSpace(categoryCode))
                        sqlParams.Add(new SqlParameter("@pRecallCategoryId", categoryCode));

                    SqlParameter sqlParamOut = new SqlParameter("@pOutputParam", SqlDbType.Int);
                    sqlParamOut.Direction = ParameterDirection.Output;
                    sqlParamOut.Value = -1;
                    sqlParams.Add(sqlParamOut);

                    DALHelper.ExecuteNonQuery(objConn, CommandType.StoredProcedure, "[HSS].[uspProcareInsertUpdateRecall]", sqlParams.ToArray());
                    result = Convert.ToInt32(sqlParams[sqlParams.Count - 1].Value);
                }
            }
            catch (Exception ex)
            {
                error = ex.Message;
            }

            return result;
        }


        public static DataTable RegisterProcare(string username, string password, string nhiNumber, string patientID, string calledBy, string providerID, string practiceId, string encounterId, string DBID, out string error)
        {
            error = string.Empty;
            DataTable dtResult = new DataTable();
            string patientId = string.Empty;
            string token = string.Empty;
            try
            {
                error = string.Empty;

                string connectionString = Convert.ToString(ConfigurationManager.ConnectionStrings["ConnIndiciDB"].ConnectionString);

                List<SqlParameter> sqlParams = new List<SqlParameter>();
                sqlParams.Add(new SqlParameter("@pUserName", username));
                sqlParams.Add(new SqlParameter("@pPassword", password));
                sqlParams.Add(new SqlParameter("@pNHINumber", nhiNumber));
                sqlParams.Add(new SqlParameter("@pPatientID", patientID));
                sqlParams.Add(new SqlParameter("@pCalledBy", calledBy));
                sqlParams.Add(new SqlParameter("@pProviderId", providerID));
                sqlParams.Add(new SqlParameter("@pPracticeId", practiceId));
                sqlParams.Add(new SqlParameter("@pEncounterId", encounterId));
                sqlParams.Add(new SqlParameter("@pMDConfigID", DBID));

                dtResult = DALHelper.ExecuteDataTable(connectionString, CommandType.StoredProcedure, "[MDConfig].[uspProcareTokenInsertUpdate]", sqlParams.ToArray());

                if (dtResult != null && dtResult.Rows.Count > 0)
                {
                   // patientId = Convert.ToString(dtResult.Rows[0]["PatientId"]);
                  //  token = Convert.ToString(dtResult.Rows[0]["Token"]);
                }
            }
            catch (Exception ex)
            {
                error = ex.Message;
            }
            return dtResult;
        }

        public static DataTable GetAuthenticationConnection(string Token, out string error)
        {
            error = string.Empty;
            string token = string.Empty;
            DataTable dtResult = new DataTable();
            try
            {
                error = string.Empty;

                string connectionString = Convert.ToString(ConfigurationManager.ConnectionStrings["ConnIndiciDB"].ConnectionString);

                List<SqlParameter> sqlParams = new List<SqlParameter>();
                sqlParams.Add(new SqlParameter("@pToken", Token));
                
                dtResult = DALHelper.ExecuteDataTable(connectionString, CommandType.StoredProcedure, "[MDConfig].[uspGetConnectionStringByToken]", sqlParams.ToArray());

              /*  if (dtResult != null && dtResult.Rows.Count > 0)
                {
                    token = Convert.ToString(dtResult.Rows[0]["Token"]);
                }*/
            }
            catch (Exception ex)
            {
                error = ex.Message;
            }
            return dtResult;

        }

        public static DataTable GetSessionProvider(string patientId, string Token, string encounterId , out string error)
        {

            error = string.Empty;
            string token = string.Empty;
            DataTable dtResult = new DataTable();
            try
            {
                error = string.Empty;

                string connectionString = Convert.ToString(ConfigurationManager.ConnectionStrings["ConnIndiciDB"].ConnectionString);

                List<SqlParameter> sqlParams = new List<SqlParameter>();
                sqlParams.Add(new SqlParameter("@pToken", Token));
                sqlParams.Add(new SqlParameter("@pPatienid", patientId));
                sqlParams.Add(new SqlParameter("@pEncounterId", encounterId));

                dtResult = DALHelper.ExecuteDataTable(connectionString, CommandType.StoredProcedure, "[MDconfig].[uspProcareGetSessionProvider]", sqlParams.ToArray());

                /*  if (dtResult != null && dtResult.Rows.Count > 0)
                  {
                      token = Convert.ToString(dtResult.Rows[0]["Token"]);
                  }*/
            }
            catch (Exception ex)
            {
                error = ex.Message;
            }
            return dtResult;

        }

        public static DataTable GetSessionProviderInformation(int ProviderID, string conString, out string error)
        {
            DataTable dtResult = new DataTable();
            error = string.Empty;

            //string connectionString = Convert.ToString(ConfigurationManager.ConnectionStrings["ConnIndiciDB"].ConnectionString);
            string connectionString = conString;

            List<SqlParameter> sqlParams = new List<SqlParameter>();
            sqlParams.Add(new SqlParameter("@pProviderID", ProviderID));
           
            try
            {
                dtResult = DALHelper.ExecuteDataTable(connectionString, CommandType.StoredProcedure, "[HSS].[uspProcareGetSessionProvider]", sqlParams.ToArray());
            }
            catch (Exception ex)
            {
                
                error = ex.Message;
            }

            return dtResult;
        }

        public static DataTable GetTemplateSchema(string patientId, string identifier, string conString)
        {
            DataTable result = new DataTable();

            try
            {
                //string connectionString = Convert.ToString(ConfigurationManager.ConnectionStrings["ConnMHNBPAC"].ConnectionString);
                string connectionString = conString;

                List<SqlParameter> sqlParams = new List<SqlParameter>();

                if (!string.IsNullOrEmpty(patientId))
                    sqlParams.Add(new SqlParameter("@pPatientID", patientId));

                if (!string.IsNullOrEmpty(identifier))
                    sqlParams.Add(new SqlParameter("@pIdentifier", identifier));

                result = DALHelper.ExecuteDataTable(connectionString, CommandType.StoredProcedure, "[HSS].[uspProcareGetTemplateSchema]", sqlParams.ToArray());
            }
            catch { }

            return result;
        }



        public static int InsertSummary(string patientID, string encounterID, string providerID, string identifier, string dateTimeRecorded, string outcome, string ds, string onset, DataTable dtResult, string conString)
        {
            int result = 0;

            try
            {
                Logging.Instance.WriteEventLog("PutSummary: DAL InsertSummary Called " + conString);
                //string connectionString = Convert.ToString(ConfigurationManager.ConnectionStrings["ConnMHNBPAC"].ConnectionString);
                string connectionString = conString;
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
                    Logging.Instance.WriteEventLog("PutSummary: DAL Before SP: InsertSummary Called " + conString);
                    DALHelper.ExecuteNonQuery(objConn, CommandType.StoredProcedure, "[HSS].[uspProcareInsertSummary]", sqlParams.ToArray());
                    result = Convert.ToInt32(sqlParams[sqlParams.Count - 1].Value);

                    Logging.Instance.WriteEventLog("PutSummary: DAL After SP: InsertSummary Called result>>> " + result);
                }
            }
            catch (Exception ex)
            {
                
                Logging.Instance.WriteExceptionLog("SaveSummary DAL SP Exception: ", ex);
                
                Logging.Instance.WriteExceptionLog(" SaveSummary Exception : ", ex);
            }

            return result;
        }

        public static DataSet GetSummary(string patientId, string encounterId, string identifier, string conString, out string error )
        {
            DataSet dtResult = new DataSet();
            error = string.Empty;

            //  string connectionString = Convert.ToString(ConfigurationManager.ConnectionStrings["ConnIndiciDB"].ConnectionString);
            string connectionString = conString;
            List<SqlParameter> sqlParams = new List<SqlParameter>();
            sqlParams.Add(new SqlParameter("@pPatientId", patientId));

            if (!string.IsNullOrEmpty(encounterId))
                sqlParams.Add(new SqlParameter("@pEncounterId", encounterId.Trim()));

            if (!string.IsNullOrEmpty(identifier))
                sqlParams.Add(new SqlParameter("@pIdentifier", identifier.Trim()));

            try
            {
                dtResult = DALHelper.ExecuteDataset(connectionString, CommandType.StoredProcedure, "[HSS].[uspProcareGetPatientScreeningSummary]", sqlParams.ToArray());
            }
            catch (Exception ex)
            {
                error = ex.Message;
            }

            return dtResult;
        }


        //        private void SaveLetterAndDocument()
        //        {
        //            int result = 0;
        //            error = string.Empty;

        //            try
        //            {
        //                string connectionString = Convert.ToString(ConfigurationManager.ConnectionStrings["ConnIndiciDB"].ConnectionString);

        //                using (SqlConnection objConn = new SqlConnection(connectionString))
        //                {
        //                    List<SqlParameter> sqlParams = new List<SqlParameter>();


        //                    sqlParams.Add(new SqlParameter("@pLetterDocumentID", Convert.ToInt32(LetterDocumentID)));
        //                    sqlParams.Add(new SqlParameter("@pAppointmentID", Convert.ToInt64(AppointmentID)));

        //                    sqlParams.Add(new SqlParameter("@pPracticeID", Convert.ToInt32(PracticeID)));

        //                    sqlParams.Add(new SqlParameter("@pPatientID", Convert.ToInt32(patientId)));
        //                    sqlParams.Add(new SqlParameter("@pDocumentHTML", DocumentHTML));
        //                    sqlParams.Add(new SqlParameter("@pTitle", Title));

        //                    sqlParams.Add(new SqlParameter("@pIsActive", Convert.ToBoolean(IsActive)));

        //                    sqlParams.Add(new SqlParameter("@pIsDeleted", Convert.ToInt32(IsDeleted)));

        //                    sqlParams.Add(new SqlParameter("@pInsertedBy", Convert.ToInt32(InsertedBy)));

        //                    sqlParams.Add(new SqlParameter("@pIsConfidential", Convert.ToBoolean(IsConfidential)));

        //                    sqlParams.Add(new SqlParameter("@pInsertedBy", Convert.ToInt32(UserLoggingID)));

        //                    sqlParams.Add(new SqlParameter("@pHLStatus", pHLStatus));

        //                    sqlParams.Add(new SqlParameter("@pMessageControlID", MessageControlID));

        //                    sqlParams.Add(new SqlParameter("@pAddressBookID", Convert.ToInt32(AddressBookID)));

        //                    sqlParams.Add(new SqlParameter("@pGeneratedFromID", Convert.ToInt32(GeneratedFromID)));

        //                    sqlParams.Add(new SqlParameter("@pComments", Comments));


        //                    sqlParams.Add(new SqlParameter("@pComments", Convert.ToInt32(pTemplateID)));

        //                    sqlParams.Add(new SqlParameter("@pEDIAccount", EDIAccount));

        //                    sqlParams.Add(new SqlParameter("@pNZMCNO", NZMCNO));
        //                    sqlParams.Add(new SqlParameter("@pDescription", Description));

        //                    sqlParams.Add(new SqlParameter("pDHBLetterandDocumentID", DHBLetterandDocumentID));

        //                    sqlParams.Add(new SqlParameter("@pDMSID", DMSID));

        //                    sqlParams.Add(new SqlParameter("@pIsHealthPoint", Convert.ToBoolean(IsHealthPoint)));

        //                    sqlParams.Add(new SqlParameter("@pIsAutoSave", Convert.ToBoolean(IsAutoSave)));

        //                    sqlParams.Add(new SqlParameter("@pDatasourceId", Convert.ToInt32(DatasourceId)));


        //                    sqlParams.Add(new SqlParameter("@pNewLetterDocumentID", Convert.ToInt32(DatasourceId)));
        //                    pIsAutoSave
        //                pDatasourceId
        //                pNewLetterDocumentID
        //                plinkeMessageControlID



        //        }
    }
    
    
}
