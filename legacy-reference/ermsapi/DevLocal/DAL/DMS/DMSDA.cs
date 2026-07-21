using DAL.HelperClasses;
using PdfSharp.Drawing;
using PdfSharp.Pdf;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Data.SqlClient;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DAL.DMS
{
    public class DMSDA
    {
        private int UpdateInboxFolderDocuments(string Guid, string InboxFolderItemID)
        {
            SqlConnection con = new SqlConnection(Convert.ToString(ConfigurationManager.ConnectionStrings["ConnMHNPMS"]));
            int result = 0;

            try
            {
                SqlCommand cmd = new SqlCommand();
                cmd.CommandText = "Update Prompt.tblInboxFolderItem set DMSID='" + Guid + "' where InboxFolderItemID=" + InboxFolderItemID + "";
                cmd.CommandType = CommandType.Text;
                cmd.Connection = con;
                con.Open();
                result = cmd.ExecuteNonQuery();
            }
            catch (Exception)
            {
                //Logger.Logging.Instance.WriteExceptionLog(ex);
            }
            finally
            {
                con.Close();
            }
            return result;
        }

        public static int HL7SaveInbox(Guid guidDMS, string nhiNumber, string receivingFacility, string nzMC)
        {
            return HL7SaveInbox(guidDMS, nhiNumber, receivingFacility, nzMC, string.Empty, string.Empty, string.Empty, string.Empty, string.Empty,
                                string.Empty, string.Empty, string.Empty, string.Empty, string.Empty, string.Empty, string.Empty, null, -1,
                                string.Empty, string.Empty/*, DateTime.Now*/);
        }

        public static int HL7SaveInbox(Guid guidDMS, string nhiNumber, string receivingFacility, string nzMC, string comments, string providerFamilyName,
                                        string providerGivenName, string providerMiddleName, string sendingApplication, string sendingFacility,
                                        string versionID, string messageType, string messageControlID, string patientFamilyName, string patientGivenName,
                                        string patientMiddelName, DateTime? dOB, int inboxFolderID, string messageSubject, string msaMessageControlID/*,
                                        DateTime dtMessageDateTime*/)
        {
            return HL7SaveInbox(guidDMS, nhiNumber, receivingFacility, nzMC, comments, providerFamilyName,
                                providerGivenName, providerMiddleName, sendingApplication, sendingFacility,
                                versionID, messageType, messageControlID, patientFamilyName, patientGivenName,
                                patientMiddelName, dOB, inboxFolderID, messageSubject, msaMessageControlID, -1, string.Empty);
        }

        public static int HL7SaveInbox(Guid guidDMS, string nhiNumber, string receivingFacility, string nzMC, string comments, string providerFamilyName, string providerGivenName,
            string providerMiddleName, string sendingApplication, string sendingFacility, string versionID, string messageType, string messageControlID, string patientFamilyName,
            string patientGivenName, string patientMiddelName, DateTime? dOB, int inboxFolderID, string messageSubject, string msaMessageControlID, int inboxItemTypeId, string gender)
        {
            return HL7SaveInbox(guidDMS, nhiNumber, receivingFacility, nzMC, comments, providerFamilyName, providerGivenName, providerMiddleName, sendingApplication, sendingFacility,
                versionID, messageType, messageControlID, patientFamilyName, patientGivenName, patientMiddelName, dOB, inboxFolderID, messageSubject, msaMessageControlID, inboxItemTypeId,
                string.Empty, null, gender);
        }

        public static int HL7SaveInbox(Guid guidDMS, string nhiNumber, string receivingFacility, string nzMC, string comments, string providerFamilyName,
                                        string providerGivenName, string providerMiddleName, string sendingApplication, string sendingFacility,
                                        string versionID, string messageType, string messageControlID, string patientFamilyName, string patientGivenName,
                                        string patientMiddelName, DateTime? dOB, int inboxFolderID, string messageSubject, string msaMessageControlID, int inboxItemTypeId, string usDescription,
                                        DateTime? receivingDate, string gender)
        {
            SqlConnection connString = new SqlConnection(Convert.ToString(ConfigurationManager.ConnectionStrings["ConnMHNPMS"]));
            int result = -1;

            try
            {
                List<SqlParameter> sqlParams = new List<SqlParameter>();
                sqlParams.Add(new SqlParameter("@pNHINumber", nhiNumber));
                sqlParams.Add(new SqlParameter("@pReceivingFacility", receivingFacility));
                sqlParams.Add(new SqlParameter("@pNZMC", nzMC));
                //sqlParams.Add(new SqlParameter("@pReceivingDate", dtMessageDateTime));

                if (!guidDMS.Equals(Guid.Empty))
                    sqlParams.Add(new SqlParameter("@pDMSID", guidDMS.ToString()));

                if (!string.IsNullOrEmpty(comments))
                    sqlParams.Add(new SqlParameter("@pComments", comments));
                if (!string.IsNullOrEmpty(providerFamilyName))
                    sqlParams.Add(new SqlParameter("@pProviderFamilyName", providerFamilyName));
                if (!string.IsNullOrEmpty(providerGivenName))
                    sqlParams.Add(new SqlParameter("@pProviderGivenName", providerGivenName));
                if (!string.IsNullOrEmpty(providerMiddleName))
                    sqlParams.Add(new SqlParameter("@pProviderMiddleName", providerMiddleName));
                if (!string.IsNullOrEmpty(sendingApplication))
                    sqlParams.Add(new SqlParameter("@pSendingApplication", sendingApplication));
                if (!string.IsNullOrEmpty(sendingFacility))
                    sqlParams.Add(new SqlParameter("@pSendingFacility", sendingFacility));
                if (!string.IsNullOrEmpty(versionID))
                    sqlParams.Add(new SqlParameter("@pVersionID", versionID));
                if (!string.IsNullOrEmpty(messageType))
                    sqlParams.Add(new SqlParameter("@pMessageType", messageType));
                if (!string.IsNullOrEmpty(messageControlID))
                    sqlParams.Add(new SqlParameter("@pMessageControlID", messageControlID));
                if (!string.IsNullOrEmpty(patientFamilyName))
                    sqlParams.Add(new SqlParameter("@pPatientFamilyName", patientFamilyName));
                if (!string.IsNullOrEmpty(patientGivenName))
                    sqlParams.Add(new SqlParameter("@pPatientGivenName", patientGivenName));
                if (!string.IsNullOrEmpty(patientMiddelName))
                    sqlParams.Add(new SqlParameter("@pPatientMiddelName", patientMiddelName));
                //if (!string.IsNullOrEmpty(dOB))
                if (dOB != null)
                    sqlParams.Add(new SqlParameter("@pDOB", dOB));
                if (!string.IsNullOrEmpty(messageSubject))
                    sqlParams.Add(new SqlParameter("@pMessageSubject", messageSubject));
                if (inboxFolderID > -1)
                    sqlParams.Add(new SqlParameter("@pInBoxFolderID", inboxFolderID));
                if (!string.IsNullOrEmpty(msaMessageControlID))
                    sqlParams.Add(new SqlParameter("@pMSAMessageControlID", msaMessageControlID));
                if (inboxItemTypeId > -1)
                    sqlParams.Add(new SqlParameter("@pInboxItemTypeID", inboxItemTypeId));
                if (!string.IsNullOrEmpty(usDescription))
                    sqlParams.Add(new SqlParameter("@pUSDescription", usDescription));
                if (receivingDate != null)
                    sqlParams.Add(new SqlParameter("@pReceivingDate", receivingDate));
                if (!string.IsNullOrWhiteSpace(gender))
                    sqlParams.Add(new SqlParameter("@pGender", gender));
                else
                    sqlParams.Add(new SqlParameter("@pGender", DBNull.Value));

                SqlParameter sqlParamOut = new SqlParameter("@pOutputParam", SqlDbType.BigInt);
                sqlParamOut.Direction = ParameterDirection.Output;
                sqlParamOut.Value = -1;
                sqlParams.Add(sqlParamOut);

                DALHelper.ExecuteNonQuery(connString, CommandType.StoredProcedure, "[dbo].[uspHL7SaveInbox]", sqlParams.ToArray());
                Int32.TryParse((sqlParams[sqlParams.Count - 1].Value).ToString(), out result);
            }
            catch (Exception)
            {
                throw;
            }

            return result;
        }

        public static int TaskPathLabInsert(string nhiNumber, string nzMC, string receivingFacility, string taskSubject, int inboxFolderItemID)
        {
            SqlConnection connString = new SqlConnection(Convert.ToString(ConfigurationManager.ConnectionStrings["ConnMHNPMS"]));
            int result = -1;

            try
            {
                List<SqlParameter> sqlParams = new List<SqlParameter>();

                sqlParams.Add(new SqlParameter("@pNhiNumber", nhiNumber));
                sqlParams.Add(new SqlParameter("@pEDIAccount", receivingFacility));
                sqlParams.Add(new SqlParameter("@pNZMCNo", nzMC));

                if (!string.IsNullOrEmpty(taskSubject))
                    sqlParams.Add(new SqlParameter("@pTaskSubject", taskSubject));

                if (inboxFolderItemID > -1)
                    sqlParams.Add(new SqlParameter("@pInboxFolderItemID", inboxFolderItemID));

                SqlParameter sqlParamOut = new SqlParameter("@pOutputParam", SqlDbType.Int);
                sqlParamOut.Direction = ParameterDirection.Output;
                sqlParamOut.Value = -1;
                sqlParams.Add(sqlParamOut);

                DALHelper.ExecuteNonQuery(connString, CommandType.StoredProcedure, "[Task].[uspTaskPathLabInsertUpdate]", sqlParams.ToArray());

                result = Convert.ToInt32(sqlParams[sqlParams.Count - 1].Value);
            }
            catch (Exception)
            {

                throw;
            }
            return result;
        }

        public static string GetOrganizationByEDI(string ediAccount)
        {
            SqlConnection connString = new SqlConnection(Convert.ToString(ConfigurationManager.ConnectionStrings["ConnMHNPMS"]));
            DataTable dtResults = new DataTable();
            string result = string.Empty;

            try
            {
                List<SqlParameter> sqlParams = new List<SqlParameter>();
                sqlParams.Add(new SqlParameter("@pEDIAccount", ediAccount));

                dtResults = DALHelper.ExecuteDataTable(connString, CommandType.StoredProcedure, "[Profile].[uspGetOrganizationByEDI]", sqlParams.ToArray());

                if (dtResults.Rows.Count > 0)
                    result = Convert.ToString(dtResults.Rows[0]["OrgName"]);
            }
            catch { }

            return result;
        }

        public static int SaveDMS(int clientID, int categoryID, string documentName, int documentTypeID, string description, string documentKey,
                                   byte[] contentData)
        {
            int result = 0;

            try
            {
                string connectionString = Convert.ToString(ConfigurationManager.ConnectionStrings["ConnMHNDMS"].ConnectionString);

                List<SqlParameter> sqlParams = new List<SqlParameter>();
                sqlParams.Add(new SqlParameter("@pDocumentID", 0));
                sqlParams.Add(new SqlParameter("@pClientID", clientID));
                sqlParams.Add(new SqlParameter("@pCategoryID", categoryID));
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
                throw;
            }

            return result;
        }

        public static Guid AddDocument(string extension, string fileName, byte[] docbyte, string description = "", int categoryId = 12, int clientId = 3)
        {
            Guid GUID = Guid.Empty;
            GUID = Guid.NewGuid();
            int DMSDocumentTypeId = 0;
            DMSDocumentTypeId = (extension.ToLower().EndsWith("tiff") ? GetDocumentTypeID("pdf") : GetDocumentTypeID(extension));

            try
            {
                int result = DMSDA.SaveDMS(clientId, categoryId, fileName, DMSDocumentTypeId, description, GUID.ToString(), extension.ToLower().EndsWith("tiff") ? ConvertImageBytes(docbyte) : docbyte);

                if (result <= 0)
                    GUID = Guid.Empty;
            }
            catch (Exception)
            {
                GUID = Guid.Empty;
                throw;
            }

            return GUID;
        }

        public static Guid AddDocument(string base64String, string extension, string fileName, string description = "")
        {
            byte[] bytesResult;

            if (extension == ".html")
                bytesResult = System.Text.Encoding.UTF8.GetBytes(base64String);
            else
                bytesResult = Convert.FromBase64String(base64String);

            return AddDocument(extension, fileName, bytesResult, description);
        }

        private static int GetDocumentTypeID(string extension)
        {
            string[] documentTypes = Convert.ToString(ConfigurationManager.AppSettings["DMSDocTypes"]).Split('|');

            if (documentTypes.Length > 0)
            {
                for (int i = 0; i <= documentTypes.Length - 1; i++)
                {
                    if (documentTypes[i].Contains(",")
                        && documentTypes[i].Split(',').Length > 1
                        && documentTypes[i].Split(',')[1].ToLower().Contains(extension.Replace(".", string.Empty).ToLower()))
                        return Convert.ToInt16(documentTypes[i].Split(',')[0]);
                }
            }

            return 0;
        }

        private static byte[] ConvertImageBytes(byte[] receivedBytes)
        {
            byte[] convertedBytes = null;

            try
            {
                using (MemoryStream memoryStream = new MemoryStream(receivedBytes))
                {
                    memoryStream.Position = 0;
                    memoryStream.Write(receivedBytes, 0, receivedBytes.Length);
                    System.Drawing.Image image = System.Drawing.Image.FromStream(memoryStream, true, true);

                    int Framecount = image.GetFrameCount(FrameDimension.Page);

                    using (PdfDocument doc = new PdfDocument())
                    {
                        XGraphics xgr;

                        for (int pageNum = 0; pageNum < Framecount; pageNum++)
                        {
                            image.SelectActiveFrame(FrameDimension.Page, pageNum);
                            PdfPage page = new PdfPage();
                            doc.Pages.Add(page);
                            xgr = XGraphics.FromPdfPage(page);
                            XImage ximg = XImage.FromGdiPlusImage(image);
                            xgr.DrawImage(ximg, 25, 25, page.Width - 25, page.Height - 25);
                        }

                        xgr = null;

                        using (MemoryStream pdfStream = new MemoryStream())
                        {
                            doc.Save(pdfStream);
                            convertedBytes = pdfStream.ToArray();
                        }
                    }
                }
            }
            catch
            {
                convertedBytes = receivedBytes;
            }

            return convertedBytes;
        }

        #region DMS - PDF Converter

        public static int GetColumnMaxValue(string query)
        {
            string strConnection = new SqlConnection(Convert.ToString(ConfigurationManager.ConnectionStrings["ConnMHNDMS"])).ConnectionString;

            try
            {
                return Convert.ToInt32(DALHelper.ExecuteScalar(strConnection, CommandType.Text, query));
            }
            catch
            {
                throw;
            }
        }

        public static DataTable GetDMSData(int PageNo, int PageSize)
        {
            DataTable dtResult = new DataTable();
            string strConnection = new SqlConnection(Convert.ToString(ConfigurationManager.ConnectionStrings["ConnMHNDMS"])).ConnectionString;

            try
            {
                using (SqlConnection objConn = new SqlConnection(strConnection))
                {
                    List<SqlParameter> sqlParams = new List<SqlParameter>();

                    sqlParams.Add(new SqlParameter("@pPageNo", PageNo));
                    sqlParams.Add(new SqlParameter("@pPageSize", PageSize));

                    dtResult = DALHelper.ExecuteDataTable(objConn, CommandType.StoredProcedure, "[dbo].[uspGetDMSData]", sqlParams.ToArray());
                }
            }
            catch
            {
                throw;
            }

            return dtResult;
        }

        public static int UpdateData(DataRow drData, bool idOnly, bool IsCorrupt)
        {
            string strConnection = new SqlConnection(Convert.ToString(ConfigurationManager.ConnectionStrings["ConnMHNDMS"])).ConnectionString;
            int result = 0;

            try
            {
                using (SqlConnection conn = new SqlConnection(strConnection))
                {
                    using (SqlCommand command = new SqlCommand("uspUpdateDocumentDetailData", conn) { CommandType = CommandType.StoredProcedure })
                    {
                        command.Parameters.Add(new SqlParameter("@pDocumentID", drData["DocumentID"]));
                        command.Parameters.Add(new SqlParameter("@pIsUpdateID", idOnly == false ? 0 : 1));
                        command.Parameters.Add(new SqlParameter("@pIsCorrupt", IsCorrupt == false ? 0 : 1));

                        if (drData != null)
                        {
                            SqlParameter sqlParamContents = new SqlParameter("@pDocumentData", SqlDbType.VarBinary);
                            sqlParamContents.Value = drData["DocumentData"];
                            command.Parameters.Add(sqlParamContents);
                        }

                        conn.Open();
                        command.CommandTimeout = Convert.ToInt32(ConfigurationManager.AppSettings["SqlCommandTimeOutInSeconds"]);
                        result = command.ExecuteNonQuery();
                    }
                }
            }
            catch
            {
                throw;
            }

            return result;
        }

        public static int UpdateData(long documentID, bool IsCorrupt, out string error)
        {
            error = string.Empty;

            string strConnection = new SqlConnection(Convert.ToString(ConfigurationManager.ConnectionStrings["ConnMHNDMS"])).ConnectionString;
            int result = 0;

            try
            {
                using (SqlConnection conn = new SqlConnection(strConnection))
                {
                    using (SqlCommand command = new SqlCommand("uspUpdateDocumentDetailData", conn) { CommandType = CommandType.StoredProcedure })
                    {
                        command.Parameters.Add(new SqlParameter("@pDocumentID", documentID));
                        command.Parameters.Add(new SqlParameter("@pIsCorrupt", IsCorrupt == false ? 0 : 1));

                        conn.Open();
                        command.CommandTimeout = Convert.ToInt32(ConfigurationManager.AppSettings["SqlCommandTimeOutInSeconds"]);
                        result = command.ExecuteNonQuery();
                    }
                }
            }
            catch (Exception ex)
            {
                error = ex.Message;
            }

            return result;
        }

        public static void UpdateAllData(DataTable dtModifiedData)
        {
            try
            {
                string strConnection = new SqlConnection(Convert.ToString(ConfigurationManager.ConnectionStrings["ConnMHNDMS"])).ConnectionString;

                using (SqlConnection con = new SqlConnection(strConnection))
                {
                    using (SqlCommand cmd = new SqlCommand("uspUpdateDocumentDetailDataInBulk"))
                    {
                        cmd.CommandType = CommandType.StoredProcedure;
                        cmd.Connection = con;
                        cmd.CommandTimeout = Convert.ToInt32(ConfigurationManager.AppSettings["SqlCommandTimeOutInSeconds"]);
                        cmd.Parameters.AddWithValue("@ptblDocDetail", dtModifiedData);
                        con.Open();
                        cmd.ExecuteNonQuery();
                    }
                }
            }
            catch
            {
                throw;
            }
        }

        #endregion
    }
}
