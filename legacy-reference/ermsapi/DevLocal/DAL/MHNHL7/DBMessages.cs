using DAL.HelperClasses;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Data.SqlClient;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DAL.MHNHL7
{
    public class DBMessages
    {
        static DBMessages instance = null;
        string strQuery = string.Empty;

        public DataTable dtMessageSegments;
        public DataTable dtSegmentFields;
        public DataTable dtFieldComponents;
        public DataTable dtClientMessage;

        static string strConnection;

        public DBMessages()
        {
            dtMessageSegments = new DataTable();
            dtSegmentFields = new DataTable();
            dtFieldComponents = new DataTable();
            dtClientMessage = new DataTable();

            strConnection = string.Empty;
        }

        public static DBMessages Instance
        {
            get
            {
                if (instance == null)
                    instance = new DBMessages();

                return instance;
            }
        }

        public DataTable AuthenticateHL7Client(string userName, string password, out string error)
        {
            error = string.Empty;
            DataTable dtResults = new DataTable();

            List<SqlParameter> sqlParams = new List<SqlParameter>();
            sqlParams.Add(new SqlParameter("@pUserName", userName.Trim()));
            sqlParams.Add(new SqlParameter("@pPassword", password.Trim()));

            try
            {
                dtResults = DALHelper.ExecuteDataTable(strConnection, CommandType.StoredProcedure, "uspAuthenticateHL7Client", sqlParams.ToArray());
            }
            catch (Exception ex)
            {
                error = ex.Message;
                throw ex;
            }

            return dtResults;
        }

        public void GetMessageSegments()
        {
            try
            {
                if (dtMessageSegments.Rows.Count > 0)
                    dtMessageSegments.Clear();

                dtMessageSegments = DALHelper.ExecuteDataTable(strConnection, CommandType.StoredProcedure, "uspGetMessageSegments");
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }

        public void GetSegmentFields()
        {
            try
            {
                if (dtSegmentFields.Rows.Count > 0)
                    dtSegmentFields.Clear();

                dtSegmentFields = DALHelper.ExecuteDataTable(strConnection, CommandType.StoredProcedure, "uspGetSegmentFields");
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }

        public void GetFieldComponents()
        {
            try
            {
                if (dtFieldComponents.Rows.Count > 0)
                    dtFieldComponents.Clear();

                dtFieldComponents = DALHelper.ExecuteDataTable(strConnection, CommandType.StoredProcedure, "uspGetFieldComponents");
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }

        public DataTable GetMessageEDI(string connectionKey)
        {
            DataTable dtMessageEDIList = new DataTable("EDI Table");

            try
            {
                if (dtMessageEDIList.Rows.Count > 0)
                    dtMessageEDIList.Clear();

                dtMessageEDIList = DALHelper.ExecuteDataTable(connectionKey, CommandType.StoredProcedure, "uspGetMessageEDIList");
            }
            catch (Exception ex)
            {
                dtMessageEDIList = new DataTable("EDI Table");
                throw ex;
            }

            return dtMessageEDIList;
        }

        public DataTable uspGetFieldSubComponents(int FieldID, int MessageTypeID, string FieldName)
        {
            DataTable dtFieldSubComponents = new DataTable();

            List<SqlParameter> sqlParams = new List<SqlParameter>();

            if (!string.IsNullOrWhiteSpace(Convert.ToString(FieldName)))
                sqlParams.Add(new SqlParameter("@pFieldName", Convert.ToString(FieldName)));

            if (FieldID > 0)
                sqlParams.Add(new SqlParameter("@pFieldID", FieldID));

            if (MessageTypeID > 0)
                sqlParams.Add(new SqlParameter("@pMessageTypeID", MessageTypeID));

            try
            {
                dtFieldSubComponents = DALHelper.ExecuteDataTable(strConnection, CommandType.StoredProcedure, "uspGetFieldSubComponents", sqlParams.ToArray());
            }
            catch (Exception ex)
            {
                throw ex;
            }
            return dtFieldSubComponents;
        }

        public DataTable GetMessageHeader(Int64 messageID)
        {
            DataTable dtResults = new DataTable();

            List<SqlParameter> sqlParams = new List<SqlParameter>();

            sqlParams.Add(new SqlParameter("@pMessageID", messageID));

            try
            {
                dtResults = DALHelper.ExecuteDataTable(strConnection, CommandType.StoredProcedure, "uspGetMessageHeader", sqlParams.ToArray());
            }
            catch (Exception ex)
            {
                throw ex;
            }

            return dtResults;
        }

        public DataTable GetMessageSchema(string MessageTypeName, string HL7Version)
        {
            try
            {
                DataTable dtMessageSchema = new DataTable();
                List<SqlParameter> sqlParams = new List<SqlParameter>();

                if (MessageTypeName.Length > 7)
                    MessageTypeName = MessageTypeName.Substring(0, 7);

                ////if (HL7Version.Length > 3 )  Now length increase 2.3.1^AUS&Auskra1ia & ISO3166 - 1, 2.3.1^AUS (0,5) Or 2.3,2.4 (0,3)
                ////    HL7Version = HL7Version.Substring(0, 3);

                if (HL7Version.Contains("^"))
                    HL7Version = HL7Version.Split('^')[0].ToString();

                sqlParams.Add(new SqlParameter("@pMessageTypeName", MessageTypeName));
                sqlParams.Add(new SqlParameter("@pHL7Version", HL7Version));

                dtMessageSchema = DALHelper.ExecuteDataTable(strConnection, CommandType.StoredProcedure, "uspGetMessageSchema", sqlParams.ToArray());
                return dtMessageSchema;
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }

        public DataTable GetFieldValues(Int64 messageID)
        {
            return GetFieldValues(messageID, null);
        }

        public DataTable GetFieldValues(Int64 messageID, string messageType)
        {
            DataTable dtResults = new DataTable();

            List<SqlParameter> sqlParams = new List<SqlParameter>();

            if (messageID > -1)
                sqlParams.Add(new SqlParameter("@pMessageID", messageID));
            if (!string.IsNullOrEmpty(messageType))
                sqlParams.Add(new SqlParameter("@pMessageType", messageType));

            try
            {
                dtResults = DALHelper.ExecuteDataTable(strConnection, CommandType.StoredProcedure, "uspGetFieldValues", sqlParams.ToArray());
            }
            catch (Exception ex)
            {
                throw ex;
            }

            return dtResults;
        }

        public Int64 InsertMessageHeader(DataTable dtParameters)
        {
            Int64 currentMessageID = -1;
            return InsertMessageHeader(dtParameters, out currentMessageID);
        }

        public Int64 InsertMessageHeader(DataTable dtParameters, out Int64 currentMessageID)
        {
            Int64 result = -1;
            currentMessageID = -1;

            if (dtParameters.Rows.Count > 0)
            {
                using (SqlConnection objConn = new SqlConnection(strConnection))
                {
                    List<SqlParameter> sqlParams = new List<SqlParameter>();
                    sqlParams.Add(new SqlParameter("@pMessageControlID", Convert.ToString(dtParameters.Rows[0]["MessageControlID"])));
                    sqlParams.Add(new SqlParameter("@pFieldSeparator", Convert.ToString(dtParameters.Rows[0]["FieldSeparator"])));
                    sqlParams.Add(new SqlParameter("@pEncodingCharacters", Convert.ToString(dtParameters.Rows[0]["EncodingCharacters"])));
                    sqlParams.Add(new SqlParameter("@pSendingApplication", Convert.ToString(dtParameters.Rows[0]["SendingApplication"])));
                    sqlParams.Add(new SqlParameter("@pSendingFacility", Convert.ToString(dtParameters.Rows[0]["SendingFacility"])));
                    sqlParams.Add(new SqlParameter("@pReceivingApplication", Convert.ToString(dtParameters.Rows[0]["ReceivingApplication"])));
                    sqlParams.Add(new SqlParameter("@pReceivingFacility", Convert.ToString(dtParameters.Rows[0]["ReceivingFacility"])));
                    sqlParams.Add(new SqlParameter("@pDateTimeofMessage", Convert.ToString(dtParameters.Rows[0]["DateTimeofMessage"])));
                    sqlParams.Add(new SqlParameter("@pMessageType", Convert.ToString(dtParameters.Rows[0]["MessageType"])));
                    sqlParams.Add(new SqlParameter("@pSecurity", Convert.ToString(dtParameters.Rows[0]["Security"])));
                    sqlParams.Add(new SqlParameter("@pProcessingID", Convert.ToString(dtParameters.Rows[0]["ProcessingID"])));
                    sqlParams.Add(new SqlParameter("@pVersionID", Convert.ToString(dtParameters.Rows[0]["VersionID"])));
                    sqlParams.Add(new SqlParameter("@pMessageStatusID", Convert.ToInt32(dtParameters.Rows[0]["MessageStatusID"])));
                    sqlParams.Add(new SqlParameter("@pMessageRequestType", Convert.ToString(dtParameters.Rows[0]["MessageRequestType"])));
                    sqlParams.Add(new SqlParameter("@pIsAck", Convert.ToBoolean(dtParameters.Rows[0]["IsAck"])));
                    sqlParams.Add(new SqlParameter("@pSegmentCount", Convert.ToString(dtParameters.Rows[0]["SegmentCount"])));
                    sqlParams.Add(new SqlParameter("@pFileName", Convert.ToString(dtParameters.Rows[0]["FileName"])));

                    //if (dtParameters.Columns.Contains("SequenceNumber") && !string.IsNullOrEmpty(dtParameters.Rows[0]["SequenceNumber"].ToString()))
                    //    sqlParams.Add(new SqlParameter("@pSequenceNumber", Convert.ToString(dtParameters.Rows[0]["SequenceNumber"])));
                    //else
                    //    sqlParams.Add(new SqlParameter("@pSequenceNumber", DBNull.Value));

                    //if (dtParameters.Columns.Contains("ContinuationPointer") && !string.IsNullOrEmpty(dtParameters.Rows[0]["ContinuationPointer"].ToString()))
                    //    sqlParams.Add(new SqlParameter("@pContinuationPointer", Convert.ToString(dtParameters.Rows[0]["ContinuationPointer"])));
                    //else
                    //    sqlParams.Add(new SqlParameter("@pContinuationPointer", DBNull.Value));

                    //if (dtParameters.Columns.Contains("AcceptAcknowledgmentType") && !string.IsNullOrEmpty(dtParameters.Rows[0]["AcceptAcknowledgmentType"].ToString()))
                    //    sqlParams.Add(new SqlParameter("@pAcceptAcknowledgmentType", Convert.ToString(dtParameters.Rows[0]["AcceptAcknowledgmentType"])));
                    //else
                    //    sqlParams.Add(new SqlParameter("@pAcceptAcknowledgmentType", DBNull.Value));

                    //if (dtParameters.Columns.Contains("CountryCode") && !string.IsNullOrEmpty(dtParameters.Rows[0]["CountryCode"].ToString()))
                    //    sqlParams.Add(new SqlParameter("@pCountryCode", Convert.ToString(dtParameters.Rows[0]["CountryCode"])));
                    //else
                    //    sqlParams.Add(new SqlParameter("@pCountryCode", DBNull.Value));

                    //if (dtParameters.Columns.Contains("CharacterSet") && !string.IsNullOrEmpty(dtParameters.Rows[0]["CharacterSet"].ToString()))
                    //    sqlParams.Add(new SqlParameter("@pCharacterSet", Convert.ToString(dtParameters.Rows[0]["CharacterSet"])));
                    //else
                    //    sqlParams.Add(new SqlParameter("@pCharacterSet", DBNull.Value));

                    //if (dtParameters.Columns.Contains("PrincipalLanguageOfMessage") && !string.IsNullOrEmpty(dtParameters.Rows[0]["PrincipalLanguageOfMessage"].ToString()))
                    //    sqlParams.Add(new SqlParameter("@pPrincipalLanguageOfMessage", Convert.ToString(dtParameters.Rows[0]["PrincipalLanguageOfMessage"])));
                    //else
                    //    sqlParams.Add(new SqlParameter("@pPrincipalLanguageOfMessage", DBNull.Value));
                    ////if (dtParameters.Columns.Contains("AlternateCharacterSetHandlingScheme"))
                    //    sqlParams.Add(new SqlParameter("@pAlternateCharacterSetHandlingScheme", Convert.ToString(dtParameters.Rows[0]["AlternateCharacterSetHandlingScheme"])));

                    //
                    //if (dtParameters.Columns.Contains("ClientID") && !string.IsNullOrEmpty(dtParameters.Rows[0]["ClientID"].ToString()))
                    //    sqlParams.Add(new SqlParameter("@pClientID", Convert.ToInt64(dtParameters.Rows[0]["ClientID"])));
                    //else
                    //    sqlParams.Add(new SqlParameter("@pClientID", DBNull.Value));

                    if (dtParameters.Columns.Contains("IsTCP"))
                        sqlParams.Add(new SqlParameter("@pIsTCP", Convert.ToBoolean(dtParameters.Rows[0]["IsTCP"])));

                    DateTime dtEvent = new DateTime();
                    if (!string.IsNullOrEmpty(Convert.ToString(dtParameters.Rows[0]["EventDateTime"]))
                       && DateTime.TryParse(Convert.ToString(dtParameters.Rows[0]["EventDateTime"]), out dtEvent))
                        sqlParams.Add(new SqlParameter("@pEventDateTime", dtEvent));

                    if (dtParameters.Columns.Contains("AcknowledgementCode")
                        && !string.IsNullOrEmpty(Convert.ToString(dtParameters.Rows[0]["AcknowledgementCode"])))
                        sqlParams.Add(new SqlParameter("@pAcknowledgementCode", Convert.ToString(dtParameters.Rows[0]["AcknowledgementCode"])));

                    if (dtParameters.Columns.Contains("TextMessage")
                        && !string.IsNullOrEmpty(Convert.ToString(dtParameters.Rows[0]["TextMessage"])))
                        sqlParams.Add(new SqlParameter("@pTextMessage", Convert.ToString(dtParameters.Rows[0]["TextMessage"])));

                    SqlParameter sqlParamOut = new SqlParameter("@pMessageID", SqlDbType.BigInt);
                    sqlParamOut.Direction = ParameterDirection.Output;
                    sqlParamOut.Value = -1;
                    sqlParams.Add(sqlParamOut);

                    sqlParamOut = new SqlParameter("@pCurrentMessageID", SqlDbType.BigInt);
                    sqlParamOut.Direction = ParameterDirection.Output;
                    sqlParamOut.Value = -1;
                    sqlParams.Add(sqlParamOut);

                    //DALHelper.ExecuteNonQuery(objConn, CommandType.StoredProcedure, "[dbo].[uspInsertMessageHeader]", sqlParams.ToArray());
                    DALHelper.ExecuteNonQuery(objConn, CommandType.StoredProcedure, "[dbo].[uspInsertMessageHeader]", sqlParams.ToArray());


                    if (sqlParams[sqlParams.Count - 1].Value != DBNull.Value)
                        currentMessageID = Convert.ToInt64(sqlParams[sqlParams.Count - 1].Value);

                    if (sqlParams[sqlParams.Count - 2].Value != DBNull.Value)
                        result = Convert.ToInt64(sqlParams[sqlParams.Count - 2].Value);
                }
            }

            return result;
        }

        public Int64 UpdateMessageHeader(DataTable dtParameters)
        {
            Int64 result = -1;

            if (dtParameters.Rows.Count > 0)
            {
                using (SqlConnection objConn = new SqlConnection(strConnection))
                {
                    List<SqlParameter> sqlParams = new List<SqlParameter>();
                    sqlParams.Add(new SqlParameter("@pMessageID", Convert.ToString(dtParameters.Rows[0]["MessageID"])));
                    sqlParams.Add(new SqlParameter("@pResponseMessageID", Convert.ToString(dtParameters.Rows[0]["ResponseMessageID"])));

                    result = DALHelper.ExecuteNonQuery(objConn, CommandType.StoredProcedure, "[dbo].[uspUpdateMessageHeader]", sqlParams.ToArray());
                }
            }

            return result;
        }

        /// <summary>
        /// Insert ePS Message Detail
        /// </summary>
        /// <param name="dtParameters"></param>
        /// <returns></returns>
        public int InsertPrescriptionMessage(DataTable dtParameters)
        {
            int result = -1;

            if (dtParameters.Rows.Count > 0)
            {
                using (SqlConnection objConn = new SqlConnection(strConnection))
                {

                    List<SqlParameter> sqlParams = new List<SqlParameter>();
                    sqlParams.Add(new SqlParameter("@MessageSCID", Convert.ToString(dtParameters.Rows[0]["MessageSCID"])));
                    sqlParams.Add(new SqlParameter("@MessageGuid", Convert.ToString(dtParameters.Rows[0]["MessageGuid"])));
                    sqlParams.Add(new SqlParameter("@OriginalSCID", Convert.ToString(dtParameters.Rows[0]["OriginalSCID"])));
                    sqlParams.Add(new SqlParameter("@ConsolidatedSCID", Convert.ToString(dtParameters.Rows[0]["ConsolidatedSCID"])));
                    sqlParams.Add(new SqlParameter("@BatchNumber", Convert.ToString(dtParameters.Rows[0]["BatchNumber"])));
                    sqlParams.Add(new SqlParameter("@MessageType", Convert.ToString(dtParameters.Rows[0]["MessageType"])));
                    //sqlParams.Add(new SqlParameter("@StatusType", Convert.ToString(dtParameters.Rows[0]["StatusType"])));
                    //sqlParams.Add(new SqlParameter("@SourceType", Convert.ToString(dtParameters.Rows[0]["SourceType"])));
                    //sqlParams.Add(new SqlParameter("@FunctionType", Convert.ToString(dtParameters.Rows[0]["FunctionType"])));
                    //sqlParams.Add(new SqlParameter("@NotificationConsent", Convert.ToString(dtParameters.Rows[0]["NotificationConsent"])));
                    sqlParams.Add(new SqlParameter("@OrderDate", Convert.ToDateTime(dtParameters.Rows[0]["OrderDate"])));
                    //sqlParams.Add(new SqlParameter("@Comments", Convert.ToString(dtParameters.Rows[0]["Comments"])));
                    sqlParams.Add(new SqlParameter("@NzePSRequest", Convert.ToString(dtParameters.Rows[0]["NzePSRequest"])));
                    sqlParams.Add(new SqlParameter("@EntityID", Convert.ToString(dtParameters.Rows[0]["EntityID"])));
                    sqlParams.Add(new SqlParameter("@NzePSResponse", Convert.ToString(dtParameters.Rows[0]["NzePSResponse"])));


                    SqlParameter sqlParamOut = new SqlParameter("@MessageID", SqlDbType.BigInt);
                    sqlParamOut.Direction = ParameterDirection.Output;
                    sqlParamOut.Value = -1;
                    sqlParams.Add(sqlParamOut);

                    DALHelper.ExecuteNonQuery(objConn, CommandType.StoredProcedure, "[dbo].[uspInsertPrescriptionMessage]", sqlParams.ToArray());

                    result = Convert.ToInt32(sqlParams[sqlParams.Count - 1].Value);
                }
            }

            return result;
        }

        public int UpdatePrescriptionMessage(DataRow dr)
        {
            int result = -1;

            if (dr != null)
            {
                using (SqlConnection objConn = new SqlConnection(strConnection))
                {
                    List<SqlParameter> sqlParams = new List<SqlParameter>();
                    sqlParams.Add(new SqlParameter("@MessageID", Convert.ToString(dr["MessageID"])));
                    sqlParams.Add(new SqlParameter("@NzePSResponse", dr["NzePSResponse"]));

                    result = DALHelper.ExecuteNonQuery(objConn, CommandType.StoredProcedure, "[dbo].[uspUpdatePrescriptionMessage]", sqlParams.ToArray());
                }
            }

            return result;
        }

        public Int64 InsertMessageValues(DataTable dtParameters)
        {
            Int64 result = -1;

            if (dtParameters.Rows.Count > 0)
            {
                using (SqlConnection objConn = new SqlConnection(strConnection))
                {
                    List<SqlParameter> sqlParams = new List<SqlParameter>();

                    string[] selectedColumns = new[] { "MessageID", "SegmentID", "FieldID", "SegmentOrderNo", "FieldValue", "FieldOrderNo", "ParentID", "HasComponents", "ParentFieldID" };
                    sqlParams.Add(new SqlParameter("@pUDTMessageValues", new DataView(dtParameters).ToTable(false, selectedColumns)));

                    SqlParameter sqlParamOut = new SqlParameter("@pMessageValueID", SqlDbType.BigInt);
                    sqlParamOut.Direction = ParameterDirection.Output;
                    sqlParamOut.Value = -1;
                    sqlParams.Add(sqlParamOut);

                    DALHelper.ExecuteNonQuery(objConn, CommandType.StoredProcedure, "[uspInsertMessageValues]", sqlParams.ToArray());

                    result = Convert.ToInt64(sqlParams[sqlParams.Count - 1].Value);
                }
            }

            return result;
        }

        public DataSet GetMessageValues(int messageStatusID)
        {
            return GetMessageValues(messageStatusID, null, null);
        }

        public DataSet GetMessageValues(string nhiID, string messageControlID)
        {
            return GetMessageValues(-1, nhiID, messageControlID);
        }

        public DataSet GetMessageValues(string messageType, int messageStatusID)
        {
            return GetMessageValues(messageStatusID, string.Empty, string.Empty, messageType);
        }

        public DataSet GetMessageValues(string messageType, int messageStatusID, string clientAccount)
        {
            return GetMessageValues(messageStatusID, string.Empty, string.Empty, messageType, clientAccount);
        }

        public DataSet GetMessageValues(int messageStatusID, string nhiID, string messageControlID)
        {
            return GetMessageValues(messageStatusID, nhiID, messageControlID, string.Empty);
        }

        public DataSet GetMessageValues(int messageStatusID, string nhiID, string messageControlID, string messageType)
        {
            return GetMessageValues(messageStatusID, nhiID, messageControlID, messageType, string.Empty);
        }

        public DataSet GetMessageValues(int messageStatusID, string nhiID, string messageControlID, string messageType, string clientAccount)
        {
            try
            {
                DataSet dsResults = new DataSet();

                List<SqlParameter> sqlParams = new List<SqlParameter>();

                if (!string.IsNullOrEmpty(nhiID))
                    sqlParams.Add(new SqlParameter("@pNHIID", nhiID));
                if (messageStatusID > 0)
                    sqlParams.Add(new SqlParameter("@pMessageStatusID", messageStatusID));
                if (!string.IsNullOrEmpty(messageControlID))
                    sqlParams.Add(new SqlParameter("@pMessageControlID", messageControlID));
                if (!string.IsNullOrEmpty(messageType))
                    sqlParams.Add(new SqlParameter("@pMessageType", messageType));
                if (!string.IsNullOrEmpty(clientAccount))
                    sqlParams.Add(new SqlParameter("@pClientAccount", clientAccount));

                SqlParameter sqlParamOut = new SqlParameter("@pErrorCode", SqlDbType.TinyInt);
                sqlParamOut.Direction = ParameterDirection.Output;
                sqlParamOut.Value = -1;
                sqlParams.Add(sqlParamOut);

                dsResults = DALHelper.ExecuteDataset(strConnection, CommandType.StoredProcedure, "uspGetMessageValues", sqlParams.ToArray());
                //dsResults = DALHelper.ExecuteDataset(strConnection, CommandType.StoredProcedure, "uspGetMessageValues_Asim", 300, sqlParams.ToArray());

                int result = 0;
                if (sqlParams[sqlParams.Count - 1].Value != DBNull.Value)
                    result = Convert.ToInt32(sqlParams[sqlParams.Count - 1].Value);

                if (result == -1)
                    throw new Exception("Clent Doesnot Exsist");
                return dsResults;
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }

        public DataSet GetMessageStatus(DataTable dtValues)
        {
            try
            {
                DataSet dsResult = new DataSet();

                if ((dtValues != null && dtValues.Rows.Count > 0)
                    || dtValues == null)
                {
                    using (SqlConnection objConn = new SqlConnection(strConnection))
                    {
                        SqlParameter[] sqlParams = new SqlParameter[1];

                        if (dtValues != null)
                        {
                            sqlParams[0] = new SqlParameter("@pMessageListID", SqlDbType.Structured);
                            sqlParams[0].Value = dtValues;
                        }

                        dsResult = DALHelper.ExecuteDataset(objConn, CommandType.StoredProcedure, "uspGetMessageStatus", sqlParams.ToArray());
                    }
                }

                return dsResult;
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }

        public DataTable GetPendingMessages(DataTable dtValues)
        {
            try
            {
                DataTable dtResult = new DataTable();

                if (dtValues != null && dtValues.Rows.Count > 0)
                {
                    using (SqlConnection objConn = new SqlConnection(strConnection))
                    {
                        SqlParameter[] sqlParams = new SqlParameter[1];

                        if (dtValues != null)
                        {
                            sqlParams[0] = new SqlParameter("@pMessageNameList", SqlDbType.Structured);
                            sqlParams[0].Value = dtValues;
                        }

                        dtResult = DALHelper.ExecuteDataTable(objConn, CommandType.StoredProcedure, "uspGetPendingMessages", sqlParams.ToArray());
                    }
                }

                return dtResult;
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }
        public DataTable UpdateMessageStatus(DataTable dtMessageStatus)
        {
            Int64? clientID = 0;
            return UpdateMessageStatus(dtMessageStatus, clientID);
        }

        public DataTable UpdateMessageStatus(DataTable dtMessageStatus, Int64? clientID)
        {
            try
            {
                DataTable dtResult = new DataTable();

                if (!dtMessageStatus.Columns.Contains("FileName"))
                {
                    dtMessageStatus.Columns.Add("FileName");

                    for (int i = 0; i < dtMessageStatus.Rows.Count; i++)
                        dtMessageStatus.Rows[i]["FileName"] = string.Empty;
                }

                if (dtMessageStatus.Rows.Count > 0)
                {
                    using (SqlConnection objConn = new SqlConnection(strConnection))
                    {
                        //SqlParameter[] sqlParams = new SqlParameter[1];
                        //sqlParams[0] = new SqlParameter("@pMessageStatus", SqlDbType.Structured);
                        //sqlParams[0].Value = dtMessageStatus;

                        //dtResult = DALHelper.ExecuteDataTable(objConn, CommandType.StoredProcedure, "[dbo].[uspUpdateMessageStatus]", sqlParams.ToArray());

                        SqlParameter[] sqlParams = new SqlParameter[2];

                        sqlParams[0] = new SqlParameter("@pClientID", SqlDbType.BigInt);

                        if (clientID != null && clientID > 0)
                            sqlParams[0].Value = clientID;

                        sqlParams[1] = new SqlParameter("@pMessageStatus", SqlDbType.Structured);
                        sqlParams[1].Value = dtMessageStatus;

                        dtResult = DALHelper.ExecuteDataTable(objConn, CommandType.StoredProcedure, "[dbo].[uspUpdateMessageStatus]", sqlParams.ToArray());


                    }
                }

                return dtResult;
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }

        /// <summary>
        /// Get All NZeps Message From DB Including XML Request and Response.
        /// </summary>
        /// <param name="dt"></param>
        public DataTable GetPrescriptionMessages(DataTable dt)
        {
            try
            {
                return DALHelper.ExecuteDataTable(strConnection, CommandType.StoredProcedure, "uspGetPrescriptionMessages");
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }

        /// <summary>
        /// Get Client EDI with their Message Reponse Type
        /// </summary>
        public void GetClientSubscriptions()
        {
            try
            {
                if (dtClientMessage.Rows.Count > 0)
                    dtClientMessage.Clear();

                dtClientMessage = DALHelper.ExecuteDataTable(strConnection, CommandType.StoredProcedure, "uspGetClientSubscriptions");
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }

        public DataTable GetMessageByTypeAndStatus(string messageType, int messageStatusID)
        {
            return GetMessageByTypeAndStatus(messageType, string.Empty, false, messageStatusID);
        }

        public DataTable GetMessageByTypeAndStatus(int messageStatusID, bool returnAll)
        {
            return GetMessageByTypeAndStatus(string.Empty, string.Empty, returnAll, messageStatusID);
        }

        public DataTable GetMessageByTypeAndStatus(string messageType, string messageStatus, bool returnAll, int messageStatusID)
        {
            try
            {
                DataTable dtResult = new DataTable();

                if (!string.IsNullOrEmpty(messageType)
                    || returnAll)
                {
                    using (SqlConnection objConn = new SqlConnection(strConnection))
                    {
                        List<SqlParameter> sqlParams = new List<SqlParameter>();

                        if (!string.IsNullOrEmpty(messageType))
                            sqlParams.Add(new SqlParameter("@pMessageType", messageType));

                        if (messageStatusID > 0)
                            sqlParams.Add(new SqlParameter("@pMessageStatusID", messageStatusID));

                        //dtResult = DALHelper.ExecuteDataTable(objConn, CommandType.StoredProcedure, "[dbo].[uspGetMessageHeader]", sqlParams.ToArray());
                        dtResult = DALHelper.ExecuteDataTable(objConn, CommandType.StoredProcedure, "[dbo].[uspGetMessageHeader]", sqlParams.ToArray());
                    }
                }

                return dtResult;
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }

        public void CreateConnection()
        {
            try
            {
                strConnection = ConfigurationManager.ConnectionStrings["ConnMHNHL7"].ConnectionString;
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }

        public static int InsertScreeningData(DataTable dtValues, string strConn)
        {
            string strConnection = new SqlConnection(Convert.ToString(ConfigurationManager.ConnectionStrings["ConnMHNPMS"])).ConnectionString;
            int result = 0;
            try
            {
                using (SqlConnection objConn = new SqlConnection(strConnection))
                {
                    SqlParameter[] sqlParams = new SqlParameter[1];

                    if (dtValues != null)
                    {
                        sqlParams[0] = new SqlParameter("@ptblScreeningCombo", SqlDbType.Structured);
                        sqlParams[0].Value = dtValues;
                    }

                    result = DALHelper.ExecuteNonQuery(objConn, CommandType.StoredProcedure, "Config.uspInserttblScreeningCombo", sqlParams.ToArray());
                }
            }
            catch (Exception)
            {
                result = 0;
            }
            return result;
        }

        public static int InsertTemplateScreeningMappingData(DataTable dtValues)
        {
            string strConnection = new SqlConnection(Convert.ToString(ConfigurationManager.ConnectionStrings["ConnMHNPMS"])).ConnectionString;
            int result = 0;
            try
            {
                using (SqlConnection objConn = new SqlConnection(strConnection))
                {
                    SqlParameter[] sqlParams = new SqlParameter[1];

                    if (dtValues != null)
                    {
                        sqlParams[0] = new SqlParameter("@ptblScreeningMapping", SqlDbType.Structured);
                        sqlParams[0].Value = dtValues;
                    }

                    result = DALHelper.ExecuteNonQuery(objConn, CommandType.StoredProcedure, "Config.uspServiceTemplateScreeningMapping", sqlParams.ToArray());
                }
            }
            catch (Exception)
            {
                result = 0;
            }
            return result;
        }

        public static DataTable FillScreeningTemplate(string templateIDs)
        {
            DataTable dt = new DataTable();
            string strConnection = new SqlConnection(Convert.ToString(ConfigurationManager.ConnectionStrings["ConnMHNPMS"])).ConnectionString;
            try
            {
                using (SqlConnection con = new SqlConnection(strConnection))
                {
                    if (con.State == 0)
                    {
                        con.Open();
                    }
                    SqlCommand cmd = new SqlCommand();
                    cmd.CommandText = "SELECT * FROM Config.tblScreeningTemplate where ScreeningTypeID in (" + templateIDs + ")";

                    cmd.CommandType = CommandType.Text;
                    cmd.Connection = con;

                    SqlDataAdapter adapter = new SqlDataAdapter(cmd);
                    adapter.Fill(dt);
                }
            }
            catch (Exception)
            {
                //throw;
            }
            return dt;
        }

        public static DataTable FillScreeningDetail(DataRow row)
        {
            DataTable dt = new DataTable();
            string strConnection = new SqlConnection(Convert.ToString(ConfigurationManager.ConnectionStrings["ConnMHNPMS"])).ConnectionString;
            try
            {
                using (SqlConnection con = new SqlConnection(strConnection))
                {
                    if (con.State == 0)
                    {
                        con.Open();
                    }
                    SqlCommand cmd = new SqlCommand();

                    cmd.CommandText = " SELECT * FROM Appointment.tblScreening where ScreeningTypeID=" + Convert.ToString(row["ScreeningTypeID"]) + " and isnull(IsDeleted,0)=0 and   IsActive=1 " +
                                      " and ScreaningID not in (SELECT ScreaningID from  Appointment.tblScreeningDetail) ";

                    cmd.CommandType = CommandType.Text;
                    cmd.Connection = con;

                    SqlDataAdapter adapter = new SqlDataAdapter(cmd);
                    adapter.Fill(dt);
                    con.Close();
                }
            }
            catch (Exception)
            {
                //throw;
            }
            return dt;
        }

        public static void InsertScreeningDetailData(DataRow row, string jsonString)
        {
            string strConnection = new SqlConnection(Convert.ToString(ConfigurationManager.ConnectionStrings["ConnMHNPMS"])).ConnectionString;
            try
            {
                using (SqlConnection con = new SqlConnection(strConnection))
                {
                    if (con.State == 0)
                    {
                        con.Open();
                    }

                    SqlCommand cmd = new SqlCommand();
                    cmd.Parameters.Clear();
                    cmd.Connection = con;
                    cmd.CommandText = "INSERT INTO [Appointment].[tblScreeningDetail]([ScreaningID] " +
                                       " ,[PracticeID]    " +
                                       " ,[TemplateData]  " +
                                       " ,[TimeLineData]) " +
                                       " VALUES ('" + row["ScreaningID"] + "','" + row["PracticeID"] + "','" + jsonString + "','" + null + "')";
                    cmd.CommandType = CommandType.Text;
                    cmd.ExecuteNonQuery();
                }
            }
            catch (Exception)
            {
                //throw;
            }
        }

        public static int InsertAFData(DataTable dtAF)
        {
            string strConnection = new SqlConnection(Convert.ToString(ConfigurationManager.ConnectionStrings["ConnMHN"])).ConnectionString;
            int result = 0;
            try
            {
                using (SqlConnection objConn = new SqlConnection(strConnection))
                {
                    SqlParameter[] sqlParams = new SqlParameter[1];

                    if (dtAF != null)
                    {
                        sqlParams[0] = new SqlParameter("@ptblAF", SqlDbType.Structured);
                        sqlParams[0].Value = dtAF;
                    }

                    result = DALHelper.ExecuteNonQuery(objConn, CommandType.StoredProcedure, "Config.uspInsertAFData", sqlParams.ToArray());
                }
            }
            catch (Exception)
            {
                result = 0;
            }
            return result;
        }

        public static int InsertAppServiceValues(DataTable dtAppService)
        {
            string strConnection = new SqlConnection(Convert.ToString(ConfigurationManager.ConnectionStrings["ConnMHN"])).ConnectionString;
            int result = 0;
            try
            {
                using (SqlConnection objConn = new SqlConnection(strConnection))
                {
                    SqlParameter[] sqlParams = new SqlParameter[1];

                    if (dtAppService != null)
                    {
                        sqlParams[0] = new SqlParameter("@pAppService", SqlDbType.Structured);
                        sqlParams[0].Value = dtAppService;
                    }

                    result = DALHelper.ExecuteNonQuery(objConn, CommandType.StoredProcedure, "Config.uspInsertAppServiceValues", sqlParams.ToArray());
                }
            }
            catch (Exception)
            {
                result = 0;
            }
            return result;
        }

        public static int GetColumnMaxValue(string query)
        {
            string strConnection = new SqlConnection(Convert.ToString(ConfigurationManager.ConnectionStrings["ConnMHN"])).ConnectionString;

            try
            {
                return Convert.ToInt32(DALHelper.ExecuteScalar(strConnection, CommandType.Text, query));
            }
            catch
            {
                throw;
            }
        }

        /// <summary>
        /// Save Referral Data for PPS
        /// </summary>        
        public static int ReferralInsert(string sendingFacility, string receivingFacility, DateTime? dateTimeOfMessage, string messageControlID, string messageType, string homePhone, string versionID, string gender, DateTime? dateOfBirth,
                                            Int64 inboxFolderItemID, string externalPatientID, string patientFamilyName, string patientGivenName, string patientStreetAddress, string patientSuburb, string patientCity, string patientStateorProvince,
                                            Int64 patientPostCode, string referralStatus, string originatingReferralIdentifier, string referralReason, string role, string providerIdentifiers, string providerFamilyName, string providerGivenName,
                                            string providerMiddelName, string comments)
        {
            string strConnection = new SqlConnection(Convert.ToString(ConfigurationManager.ConnectionStrings["ConnMHNPMS"])).ConnectionString;
            int result = 0;
            try
            {
                using (SqlConnection objConn = new SqlConnection(strConnection))
                {
                    if (objConn.State == 0)
                        objConn.Open();

                    List<SqlParameter> sqlParams = new List<SqlParameter>();

                    if (!string.IsNullOrEmpty(sendingFacility))
                        sqlParams.Add(new SqlParameter("@SendingFacility", Convert.ToString(sendingFacility)));

                    if (!string.IsNullOrEmpty(receivingFacility))
                        sqlParams.Add(new SqlParameter("@ReceivingFacility", Convert.ToString(receivingFacility)));

                    if (dateTimeOfMessage != null)
                        sqlParams.Add(new SqlParameter("@DateTimeOfMessage", Convert.ToDateTime(dateTimeOfMessage)));

                    if (!string.IsNullOrEmpty(messageControlID))
                        sqlParams.Add(new SqlParameter("@MessageControlID", Convert.ToString(messageControlID)));

                    if (!string.IsNullOrEmpty(messageType))
                        sqlParams.Add(new SqlParameter("@MessageType", Convert.ToString(messageType)));

                    if (!string.IsNullOrEmpty(versionID))
                        sqlParams.Add(new SqlParameter("@VersionID", Convert.ToString(versionID)));

                    if (!string.IsNullOrEmpty(homePhone))
                        sqlParams.Add(new SqlParameter("@HomePhone", Convert.ToString(homePhone)));

                    if (!string.IsNullOrEmpty(gender))
                        sqlParams.Add(new SqlParameter("@Gender", Convert.ToString(gender)));

                    if (dateOfBirth != null)
                        sqlParams.Add(new SqlParameter("@DateOfBirth", Convert.ToDateTime(dateOfBirth)));

                    if (inboxFolderItemID > 0)
                        sqlParams.Add(new SqlParameter("@InboxFolderItemID", Convert.ToInt64(inboxFolderItemID)));

                    if (!string.IsNullOrEmpty(externalPatientID))
                        sqlParams.Add(new SqlParameter("@ExternalPatientID", Convert.ToString(externalPatientID)));

                    if (!string.IsNullOrEmpty(patientFamilyName))
                        sqlParams.Add(new SqlParameter("@PatientFamilyName", Convert.ToString(patientFamilyName)));

                    if (!string.IsNullOrEmpty(patientGivenName))
                        sqlParams.Add(new SqlParameter("@PatientGivenName", Convert.ToString(patientGivenName)));

                    if (!string.IsNullOrEmpty(patientStreetAddress))
                        sqlParams.Add(new SqlParameter("@PatientStreetAddress", Convert.ToString(patientStreetAddress)));

                    if (!string.IsNullOrEmpty(patientSuburb))
                        sqlParams.Add(new SqlParameter("@PatientSuburb", Convert.ToString(patientSuburb)));

                    if (!string.IsNullOrEmpty(patientCity))
                        sqlParams.Add(new SqlParameter("@PatientCity", Convert.ToString(patientCity)));

                    if (!string.IsNullOrEmpty(patientStateorProvince))
                        sqlParams.Add(new SqlParameter("@PatientStateorProvince", Convert.ToString(patientStateorProvince)));

                    if (patientPostCode > 0)
                        sqlParams.Add(new SqlParameter("@PatientPostCode", Convert.ToInt64(patientPostCode)));

                    if (!string.IsNullOrEmpty(referralStatus))
                        sqlParams.Add(new SqlParameter("@ReferralStatus", Convert.ToString(referralStatus)));

                    if (!string.IsNullOrEmpty(originatingReferralIdentifier))
                        sqlParams.Add(new SqlParameter("@OriginatingReferralIdentifier", Convert.ToString(originatingReferralIdentifier)));

                    if (!string.IsNullOrEmpty(referralReason))
                        sqlParams.Add(new SqlParameter("@ReferralReason", Convert.ToString(referralReason)));

                    if (!string.IsNullOrEmpty(role))
                        sqlParams.Add(new SqlParameter("@Role", Convert.ToString(role)));

                    if (!string.IsNullOrEmpty(providerIdentifiers))
                        sqlParams.Add(new SqlParameter("@ProviderIdentifiers", Convert.ToString(providerIdentifiers)));

                    if (!string.IsNullOrEmpty(providerFamilyName))
                        sqlParams.Add(new SqlParameter("@ProviderFamilyName", Convert.ToString(providerFamilyName)));

                    if (!string.IsNullOrEmpty(providerGivenName))
                        sqlParams.Add(new SqlParameter("@ProviderGivenName", Convert.ToString(providerGivenName)));

                    if (!string.IsNullOrEmpty(providerMiddelName))
                        sqlParams.Add(new SqlParameter("@ProviderMiddelName", Convert.ToString(providerMiddelName)));

                    if (!string.IsNullOrEmpty(comments))
                        sqlParams.Add(new SqlParameter("@Comments", Convert.ToString(comments)));

                    result = DALHelper.ExecuteNonQuery(objConn, CommandType.StoredProcedure, "[HL7Referral].[uspReferralInsert]", sqlParams.ToArray());
                }
            }
            catch (Exception)
            {
                result = 0;
            }
            return result;
        }

    }
}
