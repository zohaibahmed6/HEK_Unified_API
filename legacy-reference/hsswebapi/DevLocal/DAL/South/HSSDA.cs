using DAL.HelperClasses;
using Logger;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Data.SqlClient;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace DAL
{
    public class HSSDA
    {

        private static readonly Dictionary<string, string> _mimeTypes = new Dictionary<string, string>()
        {
            { "DOC", "application/msword" },
            { "JPEG", "image/jpeg" },
            { "HTML", "text/html" },
            { "JPG", "image/jpeg" },
            { "BMP", "image/bmp" },
            { "PDF", "application/pdf" },
            { "PNG", "image/png" },
            { "GIF", "image/gif" },
            { "TIF", "image/tiff" },
            { "RTF", "application/rtf" },
            { "TXT", "text/plain" },
            { "DOCX", "application/msword" },
            { "XML", "application/xml" }
        };
        public static int UpdateExistingDocument(string referralid, string practiceid, out string error)
        {
            int result = 0;
            error = string.Empty;

            try
            {
                string connectionString = Convert.ToString(ConfigurationManager.ConnectionStrings["ConnIndiciDB"+practiceid].ConnectionString);

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
                                   byte[] contentData,string practiceid,string practiceid2, out string error)
        {
            int result = 0;
            error = string.Empty;

            try
            {
                string connectionString = Convert.ToString(ConfigurationManager.ConnectionStrings["ConnDMSDB"+ practiceid].ConnectionString);

                List<SqlParameter> sqlParams = new List<SqlParameter>();
                sqlParams.Add(new SqlParameter("@pClientID", 3));   //PMSNZ(3)
                sqlParams.Add(new SqlParameter("@pCategoryID", categoryID));    //outbox(18)/inbox(17)
                sqlParams.Add(new SqlParameter("@pDocumentName", documentName));
                sqlParams.Add(new SqlParameter("@pDocumentTypeID", documentTypeID));
                sqlParams.Add(new SqlParameter("@pDescription", description));
                sqlParams.Add(new SqlParameter("@pDocumentKey", documentKey));
                sqlParams.Add(new SqlParameter("@pDocumentSize", contentData.Length));
                sqlParams.Add(new SqlParameter("@pProfileID", "1"));
                sqlParams.Add(new SqlParameter("@pPracticeID", practiceid2));

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
        public static DataTable GetACC45(string patientId, string sortOrder, DateTime dtMinDate, DateTime dtMaxDate,string practiceid,
                                         out string error)
        {
            DataTable dtResult = new DataTable();
            error = string.Empty;

            string connectionString = Convert.ToString(ConfigurationManager.ConnectionStrings["ConnIndiciDB"+ practiceid].ConnectionString);

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
        public static DataTable GetConditions(string patientId, string practiceid, out string error)
        {
            error = string.Empty;
            return GetConditions(patientId, string.Empty, DateTime.MinValue, DateTime.MinValue,practiceid, out error);
        }
        public static DataTable GetConditions(string patientId, string sortOrder, DateTime dtMinDate, DateTime dtMaxDate,string practiceid, out string error)
        {
            DataTable dtResult = new DataTable();
            error = string.Empty;

            string connectionString = Convert.ToString(ConfigurationManager.ConnectionStrings["ConnIndiciDB"+practiceid].ConnectionString);

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
                dtResult = DALHelper.ExecuteDataTable(connectionString, CommandType.StoredProcedure, "[HSS].[uspGetConditions]", sqlParams.ToArray());
            }
            catch (Exception ex)
            {
                error = ex.Message;
            }

            return dtResult;
        }
        public static DataTable GetConsultNotes(string patientId,string practiceid,  out string error)
        {
            error = string.Empty;
            return GetConsultNotes(patientId, string.Empty, DateTime.MinValue, DateTime.MinValue, practiceid, out error);
        }
        public static DataTable GetConsultNotes(string patientId, string sortOrder, DateTime dtMinDate, DateTime dtMaxDate, string practiceid,
                                                out string error)
        {
            DataTable dtResult = new DataTable();
            error = string.Empty;

            string connectionString = Convert.ToString(ConfigurationManager.ConnectionStrings["ConnIndiciDB"+practiceid].ConnectionString);

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
                dtResult = DALHelper.ExecuteDataTable(connectionString, CommandType.StoredProcedure, "[HSS].[uspGetConsultNotes]", sqlParams.ToArray());
            }
            catch (Exception ex)
            {
                error = ex.Message;
            }

            return dtResult;
        }
        public static DataTable GetDemographics(string patientId,string practiceid, out string error)
        {
            DataTable dtResult = new DataTable();
            error = string.Empty;

            string connectionString = Convert.ToString(ConfigurationManager.ConnectionStrings["ConnIndiciDB"+practiceid].ConnectionString);

            List<SqlParameter> sqlParams = new List<SqlParameter>();
            sqlParams.Add(new SqlParameter("@pPatientId", patientId));

            try
            {
                dtResult = DALHelper.ExecuteDataTable(connectionString, CommandType.StoredProcedure, "[HSS].[uspGetDemographics]", sqlParams.ToArray());
            }
            catch (Exception ex)
            {
                error = ex.Message;
            }

            return dtResult;
        }
        public static DataTable GetDocResults(string referenceId, string referralId, bool isDischarge,string practiceid, out string error)
        {
            DataTable dtResult = new DataTable();
            error = string.Empty;

            string connectionString = Convert.ToString(ConfigurationManager.ConnectionStrings["ConnIndiciDB"+practiceid].ConnectionString);

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
        public static DataTable GetDocuments(string patientId, string identifier, string practiceid, int actualPracticeID, out string error)
        {
            Logging.Instance.WriteEventLog($"Patient: {patientId}, Identifier : {identifier}, PracticeID : {practiceid}, Actual Practice Id : {actualPracticeID}", TypeEnums.LogType.Default);

            DataTable dtResult = new DataTable();
            error = string.Empty;

            try
            {
                
                string connectionString = Convert.ToString(ConfigurationManager.ConnectionStrings["ConnIndiciDB" + practiceid].ConnectionString);
                string dMSConnectionString = Convert.ToString(ConfigurationManager.ConnectionStrings["ConnDMSDB" + practiceid].ConnectionString);

                var sqlParams = new List<SqlParameter> { new SqlParameter("@pPatientId", patientId) };
                if (!string.IsNullOrWhiteSpace(identifier))
                    sqlParams.Add(new SqlParameter("@pIdentifier", identifier));


                bool isAwsEnabled = AWSDoc.IndiciDMS.CheckAWSIsEnabled(actualPracticeID, connectionString);
                Logging.Instance.WriteEventLog($"Check AWS Is Enabled: {isAwsEnabled}", TypeEnums.LogType.Default);
                if (!isAwsEnabled)
                {
                    dtResult = DALHelper.ExecuteDataTable(connectionString, CommandType.StoredProcedure, "[HSS].[uspGetDocuments]", sqlParams.ToArray());
                    return dtResult;
                }

                // AWS enabled path
                if (string.IsNullOrEmpty(identifier))
                {
                    Logging.Instance.WriteEventLog($"If idenditifer is null then get patient all documents.", TypeEnums.LogType.Default);

                    dtResult = DALHelper.ExecuteDataTable(connectionString, CommandType.StoredProcedure, "[HSS].[uspGetDocuments_AWS]", sqlParams.ToArray());
                    if (dtResult?.Rows.Count > 0)
                    {
                        foreach (DataRow row in dtResult.Rows)
                        {
                            var identifierRow = row["identifier"];
                            if (identifierRow == DBNull.Value) 
                                continue;

                            var docKey = identifierRow.ToString().ToUpperInvariant();
                            if (string.IsNullOrWhiteSpace(docKey)) 
                                continue;

                            var docResult = AWSDoc.IndiciDMS.GetDocumentStatusFromIndici(docKey, actualPracticeID, connectionString);
                            if (docResult == null) 
                                continue;

                            if (docResult.DocumentType == null) 
                                continue;

                            var docTypeKey = docResult.DocumentType.ToUpperInvariant();
                            row["ContentType"] = _mimeTypes.TryGetValue(docTypeKey, out var contentType) ? contentType : null;
                        }
                    }
                    return dtResult;
                }


                // Identifier is provided, get a single document
                Logging.Instance.WriteEventLog($"Identifier is provided, get a single document.", TypeEnums.LogType.Default);

                dtResult = DALHelper.ExecuteDataTable(connectionString, CommandType.StoredProcedure, "[HSS].[uspGetDocuments_AWS]", sqlParams.ToArray());

                if (dtResult == null)
                    return dtResult;

                var identifierUpper = identifier.ToUpperInvariant();
                ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls | SecurityProtocolType.Tls11 | SecurityProtocolType.Tls12 | SecurityProtocolType.Tls13;
                var singleDocResult = AWSDoc.IndiciDMS.DocumentGetByDocumentKeyJsonResult(identifierUpper, actualPracticeID, connectionString, dMSConnectionString);
                
                Logging.Instance.WriteEventLog($"Single document against identifier is : {singleDocResult}", TypeEnums.LogType.Default);

                if (string.IsNullOrEmpty(singleDocResult))
                    return dtResult;

                var singleDoc = JsonConvert.DeserializeObject<DocumentFile>(singleDocResult);
                if (singleDoc == null)
                    return dtResult;

              
                // Prepare columns only once
                dtResult.Columns.Add("messageData", typeof(byte[]));
                dtResult.Columns.Add("messageTitle", typeof(string));

                if(dtResult.Rows.Count > 0)
                {
                    Logging.Instance.WriteEventLog($"Single document against identifier inside the if rows condition.", TypeEnums.LogType.Default);

                    dtResult.Rows[0]["messageData"] = singleDoc.DocumentData;
                    dtResult.Rows[0]["messageTitle"] = singleDoc.DocumentName;

                    string singleContentType = string.Empty;
                    if (!string.IsNullOrEmpty(singleDoc.DocumentType))
                        singleContentType = _mimeTypes.TryGetValue(singleDoc.DocumentType.ToUpperInvariant(), out var ct) ? ct : string.Empty;

                    dtResult.Rows[0]["ContentType"] = singleContentType;
                }
                else
                {
                    Logging.Instance.WriteEventLog($"Single document against identifier inside the else condition.", TypeEnums.LogType.Default);
                }

            }
            catch (Exception ex)
            {
                error = ex.Message;
                Logging.Instance.WriteExceptionLog("GetDocuments in DAL : ", ex);
            }

            return dtResult;
        }
        public static DataTable GetLabResults(string patientId, string referenceId,string practiceid, out string error)
        {
            DataTable dtResult = new DataTable();
            error = string.Empty;

            string connectionString = Convert.ToString(ConfigurationManager.ConnectionStrings["ConnIndiciDB"+practiceid].ConnectionString);

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
        public static DataTable GetLabResults(string patientId, string practiceid, out string error)
        {
            error = string.Empty;
            return GetLabResults(patientId, string.Empty,practiceid, out error);
        }
        public static DataTable GetLabs(string patientId, string sortOrder, DateTime dtMinDate, DateTime dtMaxDate, string practiceid,
                                         out string error)
        {
            DataTable dtResult = new DataTable();
            error = string.Empty;

            string connectionString = Convert.ToString(ConfigurationManager.ConnectionStrings["ConnIndiciDB"+practiceid].ConnectionString);

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
        public static DataTable GetMeasurement(string patientId, string practiceid, string encounterId, out string error)
        {
            DataTable dtResult = new DataTable();
            error = string.Empty;

            string connectionString = Convert.ToString(ConfigurationManager.ConnectionStrings["ConnIndiciDB"+practiceid].ConnectionString);

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
        public static DataTable GetMedicalAllergies(string patientId, string sortOrder, DateTime dtMinDate, DateTime dtMaxDate,string practiceid,
                                                    out string error)
        {
            DataTable dtResult = new DataTable();
            error = string.Empty;

            string connectionString = Convert.ToString(ConfigurationManager.ConnectionStrings["ConnIndiciDB"+practiceid].ConnectionString);

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
        public static DataTable GetMedications(string patientId, string practiceid, out string error)
        {
            error = string.Empty;
            return GetMedications(patientId, string.Empty, DateTime.MinValue, DateTime.MinValue,practiceid, null, out error);
        }
        public static DataTable GetMedications(string patientId, string sortOrder, DateTime dtMinDate, DateTime dtMaxDate, string practiceid,
                                                bool? isLongTerm, out string error)
        {
            DataTable dtResult = new DataTable();
            error = string.Empty;

            string connectionString = Convert.ToString(ConfigurationManager.ConnectionStrings["ConnIndiciDB"+practiceid].ConnectionString);

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
                dtResult = DALHelper.ExecuteDataTable(connectionString, CommandType.StoredProcedure, "[HSS].[uspGetMedications]", sqlParams.ToArray());
            }
            catch (Exception ex)
            {
                error = ex.Message;
            }

            return dtResult;
        }
        public static DataTable GetNextOfKin(string patientId,string practiceid, out string error)
        {
            DataTable dtResult = new DataTable();
            error = string.Empty;

            string connectionString = Convert.ToString(ConfigurationManager.ConnectionStrings["ConnIndiciDB"+practiceid].ConnectionString);

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
        public static DataTable GetObservations(string patientId, string conceptId, string practiceid, out string error)
        {
            DataTable dtResult = new DataTable();
            error = string.Empty;

            string connectionString = Convert.ToString(ConfigurationManager.ConnectionStrings["ConnIndiciDB"+practiceid].ConnectionString);

            List<SqlParameter> sqlParams = new List<SqlParameter>();
            sqlParams.Add(new SqlParameter("@pPatientId", patientId));

            if (!string.IsNullOrWhiteSpace(conceptId))
                sqlParams.Add(new SqlParameter("@pScreeningCode", conceptId));

            try
            {
                dtResult = DALHelper.ExecuteDataTable(connectionString, CommandType.StoredProcedure, "[HSS].[uspGetObservations]", sqlParams.ToArray());
            }
            catch (Exception ex)
            {
                error = ex.Message;
            }

            return dtResult;
        }
        public static DataTable GetOtherDocs(string patientId, string sortOrder, DateTime dtMinDate, DateTime dtMaxDate,string practiceid, bool isReferral,
                                             out string error)
        {
            DataTable dtResult = new DataTable();
            //DataTable dtResultwithAttachment = new DataTable();
            error = string.Empty;

            string connectionString = Convert.ToString(ConfigurationManager.ConnectionStrings["ConnIndiciDB"+practiceid].ConnectionString);

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
        public static DataTable GetProvider(string patientId, string practiceid, out string error)
        {
            error = string.Empty;

            return GetProvider(patientId, string.Empty,string.Empty,practiceid, out error,string.Empty);
        }
        public static DataTable GetProvider(string patientId, string userId,string LocationId,string practiceid, out string error, string Encounterid = "")
        {
            DataTable dtResult = new DataTable();
            error = string.Empty;

            string connectionString = Convert.ToString(ConfigurationManager.ConnectionStrings["ConnIndiciDB"+practiceid].ConnectionString);

            List<SqlParameter> sqlParams = new List<SqlParameter>();
            sqlParams.Add(new SqlParameter("@pPatientId", patientId));
            if (!string.IsNullOrWhiteSpace(LocationId))
                sqlParams.Add(new SqlParameter("@pLocationId", LocationId));

            if (!string.IsNullOrWhiteSpace(userId))
                sqlParams.Add(new SqlParameter("@pUserId", userId));
            if (!string.IsNullOrWhiteSpace(Encounterid))
                sqlParams.Add(new SqlParameter("@pEncounterid", Encounterid));

            try
            {
                dtResult = DALHelper.ExecuteDataTable(connectionString, CommandType.StoredProcedure, "[HSS].[uspGetProvider]", sqlParams.ToArray());
            }
            catch (Exception ex)
            {
                error = ex.Message;
            }

            return dtResult;
        }
        public static DataTable GetRadResults(string patientId, string referenceId, string practiceid, out string error)
        {
            DataTable dtResult = new DataTable();
            error = string.Empty;

            string connectionString = Convert.ToString(ConfigurationManager.ConnectionStrings["ConnIndiciDB"+practiceid].ConnectionString);

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
        public static DataTable GetRads(string patientId, string sortOrder, DateTime dtMinDate, DateTime dtMaxDate, string practiceid,
                                         out string error)
        {
            DataTable dtResult = new DataTable();
            error = string.Empty;

            string connectionString = Convert.ToString(ConfigurationManager.ConnectionStrings["ConnIndiciDB"+practiceid].ConnectionString);

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
        public static DataTable GetRecallCategories(string group, string practiceid,string practiceid2, out string error)
        {
            DataTable dtResult = new DataTable();
            error = string.Empty;

            string connectionString = Convert.ToString(ConfigurationManager.ConnectionStrings["ConnIndiciDB"+practiceid].ConnectionString);

            List<SqlParameter> sqlParams = new List<SqlParameter>();
            sqlParams.Add(new SqlParameter("@pRecallGroup", group));
            sqlParams.Add(new SqlParameter("@pPracticeid", practiceid2));
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
        public static DataTable GetRecalls(string patientId, string practiceid, out string error)
        {
            DataTable dtResult = new DataTable();
            error = string.Empty;

            string connectionString = Convert.ToString(ConfigurationManager.ConnectionStrings["ConnIndiciDB"+practiceid].ConnectionString);

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
        public static DataTable GetRegisteredPractitioners(string patientId, string locationId, string practiceid, out string error)
        {
            DataTable dtResult = new DataTable();
            error = string.Empty;

            string connectionString = Convert.ToString(ConfigurationManager.ConnectionStrings["ConnIndiciDB"+practiceid].ConnectionString);

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
        public static DataTable GetScreeningCodes(string practiceId, string practiceid2, out string error)
        {
            DataTable dtResult = new DataTable();
            error = string.Empty;

            string connectionString = Convert.ToString(ConfigurationManager.ConnectionStrings["ConnIndiciDB"+practiceid2].ConnectionString);

            List<SqlParameter> sqlParams = new List<SqlParameter>();
            sqlParams.Add(new SqlParameter("@pPracticeId", practiceId));

            try
            {
                dtResult = DALHelper.ExecuteDataTable(connectionString, CommandType.StoredProcedure, "[HSS].[uspGetScreeningCodes]", sqlParams.ToArray());
            }
            catch (Exception ex)
            {
                error = ex.Message;
            }

            return dtResult;
        }
        public static DataTable GetSmokingStatus(string patientId, string practiceid, out string error)
        {
            DataTable dtResult = new DataTable();
            error = string.Empty;

            string connectionString = Convert.ToString(ConfigurationManager.ConnectionStrings["ConnIndiciDB"+practiceid].ConnectionString);

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
        public static DataTable InsertAndValidateToken(string userName, string password, string patientId, string appointmentId, string practiceid,
                                             string token, string pho , out string error)
        {
            error = string.Empty;
            return InsertAndValidateToken(userName, password, patientId, appointmentId,practiceid, token, 0,pho, out error);
        }
        public static DataTable InsertAndValidateToken(string userName, string password, string patientId, string appointmentId, string practiceid,
                                             string token, out string error)
        {
            error = string.Empty;
            return InsertAndValidateToken(userName, password, patientId, appointmentId, practiceid, token, 0, "", out error);
        }
        public static DataTable InsertAndValidateToken(string userName, string password, string patientId, string appointmentId, string practiceid,
                                             string token, double expiryInDays, string pho, out string error)
        {
            DataTable dtResult = new DataTable();
            error = string.Empty;

            string connectionString = Convert.ToString(ConfigurationManager.ConnectionStrings["ConnIndiciDB"+practiceid].ConnectionString);

            List<SqlParameter> sqlParams = new List<SqlParameter>();

            if (!string.IsNullOrWhiteSpace(userName))
                sqlParams.Add(new SqlParameter("@pUsername", userName));
            if (!string.IsNullOrWhiteSpace(password))
                sqlParams.Add(new SqlParameter("@pPassword", password));
            if (!string.IsNullOrWhiteSpace(patientId))
                sqlParams.Add(new SqlParameter("@pPatientID", patientId));
            if (!string.IsNullOrWhiteSpace(appointmentId))
                sqlParams.Add(new SqlParameter("@pAppointmentID", appointmentId));
            if (!string.IsNullOrWhiteSpace(token))
                sqlParams.Add(new SqlParameter("@pToken", token));
            if (!string.IsNullOrWhiteSpace(pho))
                sqlParams.Add(new SqlParameter("@pPHO", pho));
            if (expiryInDays != 0)
                sqlParams.Add(new SqlParameter("@pExpiryInDays", expiryInDays));

            try
            {
                dtResult = DALHelper.ExecuteDataTable(connectionString, CommandType.StoredProcedure, "[HSS].[uspInsertAndValidateToken]", sqlParams.ToArray());
            }
            catch (Exception ex)
            {
                error = ex.Message;
            }

            return dtResult;
        }
        public static bool InsertAndValidateToken(string patientId, string appointmentId, string token, string practiceid,string pho, out string error)
        {
            error = string.Empty;
            DataTable dtResults = InsertAndValidateToken(string.Empty, string.Empty, patientId, appointmentId, practiceid, token,pho, out error);

            return (dtResults.Rows.Count > 0
                    && string.IsNullOrEmpty(Utility.Instance.ToString(dtResults.Rows[0]["StatusMessage"])));
        }

        public static bool InsertAndValidateToken(string patientId, string appointmentId,  string practiceid,string token, out string error)
        {
            error = string.Empty;
            DataTable dtResults = InsertAndValidateToken(string.Empty, string.Empty, patientId, appointmentId, practiceid, token, "", out error);

            return (dtResults.Rows.Count > 0
                    && string.IsNullOrEmpty(Utility.Instance.ToString(dtResults.Rows[0]["StatusMessage"])));
        }

        public static Int64 InsertDocument(string patientId, string messageSubject, string dmsId, int itemTypeId, out string error)
        {
            error = string.Empty;
            return InsertDocument(patientId, messageSubject, dmsId, itemTypeId, DateTime.MinValue, "1", out error);
        }
        public static Int64 InsertDocument(string patientId, string encounterId, string messageSubject, string dmsId, int itemTypeId, string practiceid, out string error)
        {
            error = string.Empty;
            return InsertDocument(patientId, encounterId, messageSubject, dmsId, itemTypeId, DateTime.MinValue, "25", string.Empty, string.Empty,practiceid, out error);
        }
        public static Int64 InsertDocument(string patientId, string messageSubject, string dmsId, int itemTypeId, DateTime dtResult,
                                        string dataSourceId, out string error)
        {
            error = string.Empty;
            return InsertDocument(patientId, string.Empty, messageSubject, dmsId, itemTypeId, dtResult, dataSourceId, string.Empty, string.Empty,"", out error);
        }
        public static Int64 InsertDocument(string patientId, string messageSubject, string dmsId, int itemTypeId, DateTime dtResult,
                                        string dataSourceId, string referralId, string userId,string practiceid, out string error)
        {
            error = string.Empty;
            return InsertDocument(patientId, string.Empty, messageSubject, dmsId, itemTypeId, dtResult,  dataSourceId, referralId,  userId, practiceid, out error);
        }
        public static Int64 InsertDocument(string patientId, string encounterId, string messageSubject, string dmsId, int itemTypeId, DateTime dtResult,
                                         string dataSourceId, string referralId, string userId,string practiceid, out string error)
        {
            Int64 result = 0;
            error = string.Empty;

            try
            {
                string connectionString = Convert.ToString(ConfigurationManager.ConnectionStrings["ConnIndiciDB"+practiceid].ConnectionString);

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


                    SqlParameter sqlParamOut = new SqlParameter("@pOutputParam", SqlDbType.BigInt);
                    sqlParamOut.Direction = ParameterDirection.Output;
                    sqlParamOut.Value = -1;
                    sqlParams.Add(sqlParamOut);

                    DALHelper.ExecuteNonQuery(objConn, CommandType.StoredProcedure, "[HSS].[uspInsertDocument]", sqlParams.ToArray());
                    result = Convert.ToInt64(sqlParams[sqlParams.Count - 1].Value);
                }
            }
            catch (Exception ex)
            {
                error = ex.Message;
            }

            return result;
        }
        public static int InsertUpdateService(string patientId, string AccountHolderID, string encounterId, string masterServiceName, string subServiceName, string subServiceCode,
                                              string fee, string locationId,
                                              string Description,string Payee,string ServiceProvider,string ServiceProviderType,string ServiceDate,string PegasusReference,
                                              string practiceid, out string error)
        {
            int result = 0;
            error = string.Empty;

            try
            {
                string connectionString = Convert.ToString(ConfigurationManager.ConnectionStrings["ConnIndiciDB"+practiceid].ConnectionString);

                using (SqlConnection objConn = new SqlConnection(connectionString))
                {
                    List<SqlParameter> sqlParams = new List<SqlParameter>();

                    sqlParams.Add(new SqlParameter("@pPatientID", Convert.ToInt32(patientId)));
                    sqlParams.Add(new SqlParameter("@pAppointmentId", encounterId));
                    sqlParams.Add(new SqlParameter("@pMasterServiceName", masterServiceName));
                    sqlParams.Add(new SqlParameter("@pSubServiceName", subServiceName));
                    if (!string.IsNullOrWhiteSpace(AccountHolderID))
                        sqlParams.Add(new SqlParameter("@pAccountHolderID", AccountHolderID));
                    if (!string.IsNullOrWhiteSpace(subServiceCode))
                        sqlParams.Add(new SqlParameter("@pSubServiceCode", subServiceCode));
                    if (!string.IsNullOrWhiteSpace(fee))
                        sqlParams.Add(new SqlParameter("@pFee", fee));
                    if (!string.IsNullOrWhiteSpace(locationId))
                        sqlParams.Add(new SqlParameter("@pLocationId", locationId));
                    else
                        sqlParams.Add(new SqlParameter("@pLocationId",DBNull.Value));

                    if (!string.IsNullOrWhiteSpace(Description))
                        sqlParams.Add(new SqlParameter("@pDescription", Description));
                    else
                        sqlParams.Add(new SqlParameter("@pDescription", DBNull.Value));



                    if (!string.IsNullOrWhiteSpace(Payee))
                        sqlParams.Add(new SqlParameter("@pPayee", Payee));
                    else
                        sqlParams.Add(new SqlParameter("@pPayee", DBNull.Value));

                    if (!string.IsNullOrWhiteSpace(ServiceProvider))
                        sqlParams.Add(new SqlParameter("@pServiceProvider", ServiceProvider));
                    else
                        sqlParams.Add(new SqlParameter("@pServiceProvider", DBNull.Value));
                    if (!string.IsNullOrWhiteSpace(ServiceProviderType))
                        sqlParams.Add(new SqlParameter("@pServiceProviderType", ServiceProviderType));
                    else
                        sqlParams.Add(new SqlParameter("@pServiceProviderType", DBNull.Value));
                    


                    if (!string.IsNullOrWhiteSpace(ServiceDate))
                        sqlParams.Add(new SqlParameter("@pServiceDate", ServiceDate));
                    else
                        sqlParams.Add(new SqlParameter("@pServiceDate", DBNull.Value));

                    if (!string.IsNullOrWhiteSpace(PegasusReference))
                        sqlParams.Add(new SqlParameter("@pPegasusReference", PegasusReference));
                    else
                        sqlParams.Add(new SqlParameter("@pPegasusReference", DBNull.Value));


                    SqlParameter sqlParamOut = new SqlParameter("@pOutputParam", SqlDbType.Int);
                    sqlParamOut.Direction = ParameterDirection.Output;
                    sqlParamOut.Value = -1;
                    sqlParams.Add(sqlParamOut);

                    DALHelper.ExecuteNonQuery(objConn, CommandType.StoredProcedure, "[OnlineClaim].[uspInsertUpdateService]", sqlParams.ToArray());
                    result = Convert.ToInt32(sqlParams[sqlParams.Count - 1].Value);
                }
            }
            catch (Exception ex)
            {
                error = ex.Message;
            }

            return result;
        }
        public static Int64 InsertUpdateConsultNotes(string patientId, string appointmentId, string subjective, string objective, string assessment, string plans, string practiceid,string UserId,
                                                   out string error)
        {
            Int64 result = 0;
            error = string.Empty;

            try
            {
                string connectionString = Convert.ToString(ConfigurationManager.ConnectionStrings["ConnIndiciDB"+practiceid].ConnectionString);

                using (SqlConnection objConn = new SqlConnection(connectionString))
                {
                    List<SqlParameter> sqlParams = new List<SqlParameter>();

                    sqlParams.Add(new SqlParameter("@pPatientId", Convert.ToInt32(patientId)));
                    sqlParams.Add(new SqlParameter("@pAppointmentId", Convert.ToInt64(appointmentId)));
                    
                    if (!string.IsNullOrWhiteSpace(UserId))
                    {
                        sqlParams.Add(new SqlParameter("@pUserId", Convert.ToInt32(UserId)));
                    }

                    if (!string.IsNullOrWhiteSpace(subjective))
                        sqlParams.Add(new SqlParameter("@pSubjective", subjective));

                    if (!string.IsNullOrWhiteSpace(objective))
                        sqlParams.Add(new SqlParameter("@pObjective", objective));

                    if (!string.IsNullOrWhiteSpace(assessment))
                        sqlParams.Add(new SqlParameter("@pAssessment", assessment));

                    if (!string.IsNullOrWhiteSpace(plans))
                        sqlParams.Add(new SqlParameter("@pPlans", plans));

                    SqlParameter sqlParamOut = new SqlParameter("@pOutputParam", SqlDbType.BigInt);
                    sqlParamOut.Direction = ParameterDirection.Output;
                    sqlParamOut.Value = -1;
                    sqlParams.Add(sqlParamOut);

                    DALHelper.ExecuteNonQuery(objConn, CommandType.StoredProcedure, "[HSS].[uspInsertUpdateConsultNotes]", sqlParams.ToArray());
                    result = Convert.ToInt64(sqlParams[sqlParams.Count - 1].Value);
                }
            }
            catch (Exception ex)
            {
                error = ex.Message;
            }

            return result;
        }
        public static int InsertUpdateDiagnosis(string patientId, string appointmentId, string userId, string diagnosisType, DateTime dtOnsetDate, string summary,
                                                  bool isLongTerm, string conceptId, string diseaseName, string fsn, string practiceid, out string error)
        {
            int result = 0;
            error = string.Empty;

            try
            {
                string connectionString = Convert.ToString(ConfigurationManager.ConnectionStrings["ConnIndiciDB"+practiceid].ConnectionString);

                using (SqlConnection objConn = new SqlConnection(connectionString))
                {
                    List<SqlParameter> sqlParams = new List<SqlParameter>();

                    sqlParams.Add(new SqlParameter("@pPatientId", Convert.ToInt32(patientId)));
                    sqlParams.Add(new SqlParameter("@pAppointmentId", Convert.ToInt64(appointmentId)));

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

                    DALHelper.ExecuteNonQuery(objConn, CommandType.StoredProcedure, "[HSS].[uspInsertUpdateDiagnosis]", sqlParams.ToArray());
                    result = Convert.ToInt32(sqlParams[sqlParams.Count - 1].Value);
                }
            }
            catch (Exception ex)
            {
                error = ex.Message;
            }

            return result;
        }
        public static int InsertUpdateObservation(string patientId, string appointmentId, string userId, string temperature, string waist, string height,
                                                  string weight, string bpSys, string bpDia, string heartRate, string notes,
                                                  string risk, string framingham, string practiceid, out string error)
        {
            int result = 0;
            error = string.Empty;

            try
            {
                string connectionString = Convert.ToString(ConfigurationManager.ConnectionStrings["ConnIndiciDB"+practiceid].ConnectionString);

                using (SqlConnection objConn = new SqlConnection(connectionString))
                {
                    List<SqlParameter> sqlParams = new List<SqlParameter>();

                    sqlParams.Add(new SqlParameter("@pPatientId", Convert.ToInt32(patientId)));
                    sqlParams.Add(new SqlParameter("@pAppointmentId", Convert.ToInt64(appointmentId)));

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

                    DALHelper.ExecuteNonQuery(objConn, CommandType.StoredProcedure, "[HSS].[uspInsertUpdateObservation]", sqlParams.ToArray());
                    result = Convert.ToInt32(sqlParams[sqlParams.Count - 1].Value);
                }
            }
            catch (Exception ex)
            {
                error = ex.Message;
            }

            return result;
        }
        public static DataSet GetHSSDemographics(string patientId, string practiceid, out string error)
        {
            //DataTable dtResult = new DataTable();
            DataSet dtResult = new DataSet();
            error = string.Empty;

            string connectionString = Convert.ToString(ConfigurationManager.ConnectionStrings["ConnIndiciDB" + practiceid].ConnectionString);

            List<SqlParameter> sqlParams = new List<SqlParameter>();
            sqlParams.Add(new SqlParameter("@pPatientId", patientId));

            try
            {
                dtResult = DALHelper.ExecuteDataset(connectionString, CommandType.StoredProcedure, "[HSS].[uspGetDemographics]", sqlParams.ToArray());
                //v = DALHelper.ExecuteDataTable(connectionString, CommandType.StoredProcedure, "[HSS].[uspGetDemographics]", sqlParams.ToArray());
            }
            catch (Exception ex)
            {
                error = ex.Message;
            }

            return dtResult;
        }
        public static int HSSInsertUpdateService(string patientId, string encounterId, string masterServiceName, string subServiceName, string subServiceCode,
                                             string fee, string UserId, string locationId,
                                             string practiceid,string payee, out string error)
        {
            int result = 0;
            error = string.Empty;

            try
            {
                string connectionString = Convert.ToString(ConfigurationManager.ConnectionStrings["ConnIndiciDB" + practiceid].ConnectionString);

                using (SqlConnection objConn = new SqlConnection(connectionString))
                {
                    List<SqlParameter> sqlParams = new List<SqlParameter>();

                    sqlParams.Add(new SqlParameter("@pPatientID", Convert.ToInt32(patientId)));
                    sqlParams.Add(new SqlParameter("@pAppointmentId", encounterId));
                    sqlParams.Add(new SqlParameter("@pMasterServiceName", masterServiceName));
                    sqlParams.Add(new SqlParameter("@pSubServiceName", subServiceName));

                    if (!string.IsNullOrWhiteSpace(subServiceCode))
                        sqlParams.Add(new SqlParameter("@pSubServiceCode", subServiceCode));
                    if (!string.IsNullOrWhiteSpace(fee))
                        sqlParams.Add(new SqlParameter("@pFee", fee));
                    if (!string.IsNullOrWhiteSpace(locationId))
                        sqlParams.Add(new SqlParameter("@pLocationId", locationId));
                    else
                        sqlParams.Add(new SqlParameter("@pLocationId", DBNull.Value));

                    if (!string.IsNullOrWhiteSpace(UserId))
                        sqlParams.Add(new SqlParameter("@pUserId", UserId));
                    else
                        sqlParams.Add(new SqlParameter("@pUserId", DBNull.Value));

                    //if (!string.IsNullOrWhiteSpace(copayment))
                    //    sqlParams.Add(new SqlParameter("@pCoPayment", copayment));
                    //if (!string.IsNullOrWhiteSpace(payee))
                    //    sqlParams.Add(new SqlParameter("@pPayee", payee));




                    SqlParameter sqlParamOut = new SqlParameter("@pOutputParam", SqlDbType.Int);
                    sqlParamOut.Direction = ParameterDirection.Output;
                    sqlParamOut.Value = -1;
                    sqlParams.Add(sqlParamOut);

                    DALHelper.ExecuteNonQuery(objConn, CommandType.StoredProcedure, "[HSS].[uspInsertUpdateService]", sqlParams.ToArray());
                    result = Convert.ToInt32(sqlParams[sqlParams.Count - 1].Value);
                }
            }
            catch (Exception ex)
            {
                error = ex.Message;
            }

            return result;
        }
        public static int InsertUpdateRecall(string patientId, string appointmentId, string userId, string priority, string group, DateTime dtDueDate, string notes,
                                             string categoryId, string practiceid, out string error)
        {
            int result = 0;
            error = string.Empty;

            try
            {
                string connectionString = Convert.ToString(ConfigurationManager.ConnectionStrings["ConnIndiciDB"+practiceid].ConnectionString);

                using (SqlConnection objConn = new SqlConnection(connectionString))
                {
                    List<SqlParameter> sqlParams = new List<SqlParameter>();

                    sqlParams.Add(new SqlParameter("@pPatientId", Convert.ToInt32(patientId)));
                    sqlParams.Add(new SqlParameter("@pAppointmentId", Convert.ToInt64(appointmentId)));

                    if (!string.IsNullOrWhiteSpace(priority))
                        sqlParams.Add(new SqlParameter("@pPriority", priority));
                    if (!string.IsNullOrWhiteSpace(group))
                        sqlParams.Add(new SqlParameter("@pGroup", group));
                    if (!dtDueDate.Equals(DateTime.MinValue))
                        sqlParams.Add(new SqlParameter("@pDueDate", dtDueDate));
                    if (!string.IsNullOrWhiteSpace(notes))
                        sqlParams.Add(new SqlParameter("@pNotes", notes));
                    if (!string.IsNullOrWhiteSpace(userId))
                        sqlParams.Add(new SqlParameter("@pUserId", userId));
                    if (!string.IsNullOrWhiteSpace(categoryId))
                        sqlParams.Add(new SqlParameter("@pRecallCategoryId", categoryId));

                    SqlParameter sqlParamOut = new SqlParameter("@pOutputParam", SqlDbType.Int);
                    sqlParamOut.Direction = ParameterDirection.Output;
                    sqlParamOut.Value = -1;
                    sqlParams.Add(sqlParamOut);

                    DALHelper.ExecuteNonQuery(objConn, CommandType.StoredProcedure, "[HSS].[uspInsertUpdateRecall]", sqlParams.ToArray());
                    result = Convert.ToInt32(sqlParams[sqlParams.Count - 1].Value);
                }
            }
            catch (Exception ex)
            {
                error = ex.Message;
            }

            return result;
        }

        public static DataTable GetTemplateSchema(string patientId, string identifier, string practiceid)
        {
            DataTable result = new DataTable();

            try
            {
                string connectionString = Convert.ToString(ConfigurationManager.ConnectionStrings["ConnIndiciDB" + practiceid].ConnectionString);

                List<SqlParameter> sqlParams = new List<SqlParameter>();

                if (!string.IsNullOrEmpty(patientId))
                    sqlParams.Add(new SqlParameter("@pPatientID", patientId));

                if (!string.IsNullOrEmpty(identifier))
                    sqlParams.Add(new SqlParameter("@pIdentifier", identifier));

                result = DALHelper.ExecuteDataTable(connectionString, CommandType.StoredProcedure, "[HSS].[uspGetTemplateSchema]", sqlParams.ToArray());
            }
            catch { }

            return result;
        }

        public static DataTable GetPatientAttachment(string practiceId2, string practiceid,string patientID,string referenceID,DateTime dtMinDate,DateTime dtMaxDate,string sortOrder,string subject, out string error)
        {
            DataTable dtResult = new DataTable();
            error = string.Empty;

            string connectionString = Convert.ToString(ConfigurationManager.ConnectionStrings["ConnIndiciDB" + practiceid].ConnectionString);

            List<SqlParameter> sqlParams = new List<SqlParameter>();
            if (!string.IsNullOrEmpty(patientID))
                sqlParams.Add(new SqlParameter("@pPatientId", Convert.ToInt32(patientID)));
            sqlParams.Add(new SqlParameter("@pPracticeID", practiceId2));

            if (!dtMinDate.Equals(DateTime.MinValue))
                sqlParams.Add(new SqlParameter("@pFromDate", dtMinDate));
            if (!dtMaxDate.Equals(DateTime.MinValue))
                sqlParams.Add(new SqlParameter("@pToDate", dtMaxDate));


            if (!string.IsNullOrEmpty(subject))
                sqlParams.Add(new SqlParameter("@pSearch", Convert.ToInt32(subject)));

            if (!string.IsNullOrWhiteSpace(sortOrder))
                sqlParams.Add(new SqlParameter("@pSortBy", sortOrder));

            if (!string.IsNullOrEmpty(referenceID))
                sqlParams.Add(new SqlParameter("@pReferenceID", referenceID));

            try
            {
                dtResult = DALHelper.ExecuteDataTable(connectionString, CommandType.StoredProcedure, "[HSS].[uspGetPatientDMS]", sqlParams.ToArray());
            }
            catch (Exception ex)
            {
                error = ex.Message;
            }

            return dtResult;
        }


        public static int InsertSummary(string patientID, string encounterID, string providerID, string identifier, string dateTimeRecorded, string outcome, string ds, string onset, DataTable dtResult, string practiceid)
        {
            int result = 0;

            try
            {
                //Logging.Instance.WriteEventLog("PutSummary: DAL InsertSummary Called " + conString);

                string connectionString = Convert.ToString(ConfigurationManager.ConnectionStrings["ConnIndiciDB" + practiceid].ConnectionString);
                using (SqlConnection objConn = new SqlConnection(connectionString))
                {
                    List<SqlParameter> sqlParams = new List<SqlParameter>();

                    if (!string.IsNullOrEmpty(patientID))
                        sqlParams.Add(new SqlParameter("@pPatientID", Convert.ToInt32(patientID)));

                    if (!string.IsNullOrEmpty(encounterID))
                        sqlParams.Add(new SqlParameter("@pEncounterID", Convert.ToInt32(encounterID)));

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
                   
                    DALHelper.ExecuteNonQuery(objConn, CommandType.StoredProcedure, "[HSS].[uspInsertSummary]", sqlParams.ToArray());
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

    }
    public class DocumentFile
    {
        public int DocumentID { get; set; }
        public string DocumentName { get; set; }
        public string DocumentType { get; set; }
        public string Description { get; set; }
        public byte[] DocumentData { get; set; }
        public string Base64String { get; set; }
        public int Counter { get; set; }
        public string AppendGuid { get; set; }
        public string FilePath { get; set; }
        public bool IsAWS { get; set; }
        public string AWSTransactionID { get; set; }
        public string DocumentKey { get; set; }
        public string AWSUrl { get; set; }
        public string DMSAPIPublicKey { get; set; }
        public string DMSAPIPrivateKey { get; set; }
        public int PracticeID { get; set; }

    }

}
