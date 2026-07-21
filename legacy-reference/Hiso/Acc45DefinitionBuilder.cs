using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Data;
using System.Data.SqlClient;
using DMSProxy.DMSService;


namespace Hiso
{
    public class Acc45DefinitionBuilder
    {
        public DataTable GenerateTable(FormMetaData objMetaData, FormSettings objSettings, HealthLinkSession objSession)
        {
            DataTable dtACC45Definition = new DataTable();
            string[] strColumns = Utitlity.GetColumnNameByTableName("UDT_tblACC45Definition");
            
            DataRow dr = dtACC45Definition.NewRow();
            for (int i = 0; i < strColumns.Length; i++)
            {
                string colName = strColumns[i];
                dtACC45Definition.Columns.Add(colName);
                dr[colName] = DBNull.Value;
            }
            dr["ACC45ID"] = 0;
            if (objSession.ReferenceId>0)
                dr["ACC45ID"] = objSession.ReferenceId;
            dr["AppointmentID"] = objSession.AppointmentId;
            dr["PatientId"] = objSession.PatientId;
            dr["ProviderID"] = objSession.ProviderId;
            dr["FormInstanceID"] = objMetaData.formInstanceId.Value;
            dr["FormInstanceVersion"] = objMetaData.formInstanceVersion.Value;
            dr["FormEngineID"] = objMetaData.formEngineId.Value;
            dr["FormInstanceCreationDate"] = objMetaData.formInstanceCreationDate.Value;
            dr["FormInstanceOperationMode"] = objMetaData.formInstanceOperationMode.Value;
            dr["FormInstanceDescription"] = objMetaData.formInstanceDescription.Value;
            dr["FormDefinitionId"] = objMetaData.formDefinitionId.Value;
            dr["FormDefinitionVersion"] = objMetaData.formDefinitionVersion.Value;
            dr["FormDefinitionDescription"] = objMetaData.formDefinitionDescription.Value;
            dr["FormDefinitionTitle"] = objMetaData.formDefinitionTitle.Value;
            dr["RecipientAccount"] = objMetaData.recipientAccount.Value;
            dr["ViewType"] = objSettings.ViewType;
            dr["ViewSignature"] = objSettings.ViewSignature;
            dr["ResumePath"] = objSettings.ResumePath;
            dr["DMSDocumentID"] = objSettings.DMSDocumentID;
            dr["IsActive"] = true;
            dr["IsDeleted"] = false;
            dr["InsertedBy"] = objSession.ProviderId;
            dr["UpdatedBy"] = objSession.ProviderId;
            dr["InsertedAt"] = DateTime.Now.ToString("s");
            dr["UpdatedAt"] = DateTime.Now.ToString("s");
            //dr["SessionKey"] = objSession.GUID.ToString();
            dtACC45Definition.Rows.Add(dr);
            return dtACC45Definition;
        }
        public bool SaveAcc45Definition(DataTable dt, string FormXml,string formComments,string sessionKey="")
        {
            bool retVal = false;
            SqlConnection con = new SqlConnection(System.Configuration.ConfigurationManager.ConnectionStrings["ConectionStringPMS_NZ"].ToString());
            try
            {
                SqlCommand cmd = new SqlCommand();
                cmd.CommandText = "Appointment.usptblACC45DefinitionInsert";
                cmd.CommandType = CommandType.StoredProcedure;
                cmd.Parameters.Add(new SqlParameter("@tblAcc45Def", dt));
                if (string.IsNullOrEmpty(FormXml) == false)
                    cmd.Parameters.Add(new SqlParameter("@FormXml", FormXml));
                if(string.IsNullOrEmpty(formComments)==false)
                    cmd.Parameters.Add(new SqlParameter("@FormComments", formComments));

                if (string.IsNullOrEmpty(sessionKey) == false)
                    cmd.Parameters.Add(new SqlParameter("@pSessionKey", sessionKey));

                cmd.Connection = con;
                con.Open();
                cmd.ExecuteNonQuery();
                retVal = true;
            }
            catch (Exception ex)
            {
                retVal = false;
                throw ex;
            }
            finally
            {
                con.Close();
            }
            return retVal;
        }
        public Acc45XMl GetFormXML(HealthLinkSession objSession)
        {
            DataSet dsResult = new DataSet();
            Acc45XMl accXml = new Acc45XMl();
            string xml = string.Empty;
            SqlConnection con = new SqlConnection(System.Configuration.ConfigurationManager.ConnectionStrings["ConectionStringPMS_NZ"].ToString());
            try
            {
                SqlCommand cmd = new SqlCommand();
                cmd.CommandText = "Appointment.usptblACC45XML_Get";
                cmd.CommandType = CommandType.StoredProcedure;
                cmd.Parameters.Add(new SqlParameter("@ACC45ID", objSession.ReferenceId));
                cmd.Parameters.Add(new SqlParameter("@pSessionGUID", objSession.GUID));
                cmd.Connection = con;
                con.Open();
                SqlDataAdapter adapter = new SqlDataAdapter(cmd);
                adapter.Fill(dsResult);
            }
            catch (Exception ex)
            {
                throw ex;
            }
            finally
            {
                con.Close();
            }
            if (dsResult.Tables.Count > 0)
            {
                if (dsResult.Tables[0].Rows.Count > 0)
                {
                    accXml.ACC45ID = int.Parse(dsResult.Tables[0].Rows[0]["ACC45ID"].ToString());
                    accXml.ACC45XML = dsResult.Tables[0].Rows[0]["ACC45XML"].ToString();
                    accXml.OldProviderId = dsResult.Tables[0].Rows[0]["OldProviderId"]==DBNull.Value? 0: int.Parse(dsResult.Tables[0].Rows[0]["OldProviderId"].ToString());
                    accXml.OldProfileTypeId = dsResult.Tables[0].Rows[0]["OldProfileTypeId"] == DBNull.Value ? 0 : int.Parse(dsResult.Tables[0].Rows[0]["OldProfileTypeId"].ToString());
                    accXml.NewProviderId = dsResult.Tables[0].Rows[0]["NewProviderId"] == DBNull.Value ? 0 : int.Parse(dsResult.Tables[0].Rows[0]["NewProviderId"].ToString());
                    accXml.NewProfileTypeId = dsResult.Tables[0].Rows[0]["NewProfileTypeId"] == DBNull.Value ? 0 : int.Parse(dsResult.Tables[0].Rows[0]["NewProfileTypeId"].ToString());
                }
            }
            return accXml;
        }
        public FormSettings GetACC45Definition(HealthLinkSession objSession)
        {
            FormSettings objFormSettings = null;// new FormSettings();
            DataSet dsResult = new DataSet();
            string xml = string.Empty;
            SqlConnection con = new SqlConnection(System.Configuration.ConfigurationManager.ConnectionStrings["ConectionStringPMS_NZ"].ToString());
            try
            {
                SqlCommand cmd = new SqlCommand();
                cmd.CommandText = "Appointment.usptblACC45Definition_Get";
                cmd.CommandType = CommandType.StoredProcedure;
                cmd.Parameters.Add(new SqlParameter("@ACC45ID", objSession.ReferenceId));
               
               
                    cmd.Parameters.Add(new SqlParameter("@pSessionKey", objSession.GUID.ToString()));
              //  cmd.Parameters.Add(new SqlParameter("@pPracticeID", objSession.PracticeId));

                cmd.Connection = con;
                con.Open();
                SqlDataAdapter adapter = new SqlDataAdapter(cmd);
                adapter.Fill(dsResult);
            }
            catch (Exception ex)
            {
                throw ex;
            }
            finally
            {
                con.Close();
            }
            if (dsResult.Tables.Count > 0)
            {
                if (dsResult.Tables[0].Rows.Count > 0)
                {
                    //dsResult.Tables[0].Rows[0]["DMSDocumentID"].ToString()
                    string error = string.Empty;
                    Guid guid = Guid.Parse(dsResult.Tables[0].Rows[0]["DMSDocumentID"].ToString());
                    //MHN.Entity.DMS.Document docCol =  DMSProxy.DMSProxy.GetInstance().GetDocuments(new List<string>() { guid.ToString() }).FirstOrDefault();
                    DocumentCollection docCol = DMSProxy.DMSProxy.InstanceDMSProxy.GetDocumentData(guid, false, out error);
                    objFormSettings = new FormSettings();
                    objFormSettings.ResumePath = dsResult.Tables[0].Rows[0]["ResumePath"].ToString();
                    objFormSettings.ViewType = dsResult.Tables[0].Rows[0]["ViewType"].ToString();
                    try
                    {
                        if (objFormSettings.ViewType.ToString().ToLower().EndsWith("html"))
                        {
                            objFormSettings.View = System.Text.Encoding.UTF8.GetString(docCol.DocumentData);

                        }
                        else
                        {
                            objFormSettings.View = Convert.ToBase64String(docCol.DocumentData);
                        }
                    }
                    catch (Exception ex)
                    {
                        Logger.Logging.Instance.WriteExceptionLog(ex);
                    }
                    objFormSettings.FormXML = dsResult.Tables[0].Rows[0]["ACC45XML"].ToString();
                }
            }
            return objFormSettings;
        }
    }
}