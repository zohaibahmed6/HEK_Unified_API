using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Linq;
using System.Web;
using System.Xml;

namespace Hiso
{
    public class RegisteredPractitionerBuilder
    {
        public DataTable GenerateTable(XmlDocument xDoc, HealthLinkSession objSession)
        {
            DataTable dtPatient = new DataTable();
            string[] strColumns = Utitlity.GetColumnNameByTableName("UDT_tblRegisteredPractitioner");
            DataRow dr = dtPatient.NewRow();
            for (int i = 0; i < strColumns.Length; i++)
            {
                string colName = strColumns[i];
                dtPatient.Columns.Add(colName);
                dr[colName] = DBNull.Value;
            }
            dr["ReferenceId"] = 0;
            if (objSession.ReferenceId > 0)
            {
                dr["ReferenceId"] = objSession.ReferenceId;
            }
            else
            {
                dr["ReferenceId"] = objSession.ProviderId;
            }
            XmlNodeList sectionList = xDoc.DocumentElement.GetElementsByTagName("section");
            Dictionary<string, string> dictPat = new Dictionary<string, string>();
            DataTable dtMapping = Mapper.GetConceptMappingTable();
            foreach (XmlNode section in sectionList)
            {
                if (1 == 1)
                {
                    XmlNodeList xnFields = section.ChildNodes;
                    foreach (XmlNode xn in xnFields)
                    {
                        if (xn.Name == "field" && Mapper.IsGroupConcept(xn) == false)
                        {
                            if (xn.Attributes["conceptName"] != null && xn.Attributes["conceptName"].Value != "")
                            {
                                dictPat.Add(xn.Attributes["conceptName"].Value, xn.InnerText);
                            }
                            else if (xn.Attributes["name"] != null && xn.Attributes["name"].Value != "")
                            {
                                dictPat.Add(xn.Attributes["name"].Value, xn.InnerText);
                            }
                        }
                    }



                    foreach (KeyValuePair<string, string> item in dictPat)
                    {
                        DataRow[] rows = dtMapping.Select("ConceptName = '" + item.Key + "' OR Description = '" + item.Key + "'");
                        if (rows.Length > 0)
                        {
                            string columnName = rows[0]["ConceptName"].ToString();
                            if (dtPatient.Columns[columnName] != null)
                            {
                                dr[columnName] = item.Value;
                            }
                        }
                    }
                }

            }
            dtPatient.Rows.Add(dr);
            return dtPatient;
        }
        public bool Save(DataTable dtPatient, HealthLinkSession objSession)
        {
            bool retVal = false;
            SqlConnection con = new SqlConnection(System.Configuration.ConfigurationManager.ConnectionStrings["ConectionStringPMS_NZ"].ToString());
            try
            {
                SqlCommand cmd = new SqlCommand();
                cmd.CommandText = "[Hiso].[uspRegisteredPractitioner_Update]";
                cmd.CommandType = CommandType.StoredProcedure;
                cmd.Parameters.Add(new SqlParameter("@tblRegisteredPractitioner", dtPatient));

                cmd.Parameters.Add(new SqlParameter("@pPatientId", objSession.PatientId));
                cmd.Parameters.Add(new SqlParameter("@pAppointmentId", objSession.AppointmentId));
                cmd.Parameters.Add(new SqlParameter("@pPracticeId", objSession.PracticeId));
                cmd.Parameters.Add(new SqlParameter("@pUpdatedBy", objSession.ProviderId));
                cmd.Parameters.Add(new SqlParameter("@pUserLoggingID", -1));
                SqlParameter outparam = new SqlParameter();
                outparam.DbType = DbType.Int32;
                outparam.Direction = ParameterDirection.Output;
                outparam.ParameterName = "@pOutPutParam";
                outparam.Value = 0;
                cmd.Parameters.Add(outparam);
                cmd.Connection = con;
                con.Open();
                cmd.ExecuteNonQuery();
                if (outparam.Value != DBNull.Value && int.Parse(outparam.Value.ToString()) < 0)
                {
                    throw new Exception("Failed to update patient consult info");
                }
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
    }
}