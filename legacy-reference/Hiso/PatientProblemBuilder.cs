using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Linq;
using System.Web;
using System.Xml;

namespace Hiso
{
    public class PatientProblemBuilder
    {
        public DataTable GenerateTable(XmlDocument xDoc, HealthLinkSession objSession)
        {
            DataTable dtProblem = new DataTable();
            string[] strColumns = Utitlity.GetColumnNameByTableName("UDT_tblPatientProblem");
            DataRow dr = dtProblem.NewRow();
            for (int i = 0; i < strColumns.Length; i++)
            {
                string colName = strColumns[i];
                dtProblem.Columns.Add(colName);
                dr[colName] = DBNull.Value;
            }
            dr["DiagnosisID"] = 0;
            if (objSession.ReferenceId > 0)
            {
                dr["DiagnosisID"] = objSession.ReferenceId;
            }


            DataTable dtMapping = Mapper.GetConceptMappingTable();
            DataRow drProblem;
            XmlNodeList groups = xDoc.DocumentElement.GetElementsByTagName("group");
            foreach (XmlNode xn in groups)
            {
                XmlNodeList groupChilds = null;
                if (xn.Attributes["conceptName"] != null && xn.Attributes["conceptName"].Value.ToLower() == "patient_problem")
                {
                    drProblem = dtProblem.NewRow();
                    groupChilds = xn.ChildNodes;
                }
                else
                {
                    drProblem = null;
                }
                if (drProblem != null && groupChilds.Count > 0)
                {
                    foreach (XmlNode gc in groupChilds)
                    {
                        string attrName = string.Empty;
                        string value = string.Empty;
                        if (gc.Attributes["conceptName"] != null && gc.Attributes["conceptName"].Value != "")
                        {
                            attrName = gc.Attributes["conceptName"].Value;

                        }
                        else {
                            continue;
                        }
                        value = gc.InnerText;
                        DataRow[] rows = dtMapping.Select("ConceptName='" + attrName + "' OR Description = '" + attrName + "'");
                        if (rows.Length > 0)
                        {
                            string columnName = rows[0]["conceptName"].ToString();
                            if (dtProblem.Columns[columnName] != null)
                            {
                                drProblem[columnName] = value;
                            }
                        }
                        else {
                            drProblem[attrName] = DBNull.Value;
                        }
                    }
                    dtProblem.Rows.Add(drProblem);
                }
            }
            return dtProblem;

        }
        public bool Save(DataTable dtProblem, HealthLinkSession objSession)
        {
            bool retVal = false;
            SqlConnection con = new SqlConnection(System.Configuration.ConfigurationManager.ConnectionStrings["ConectionStringPMS_NZ"].ToString());
            try
            {
                SqlCommand cmd = new SqlCommand();
                cmd.CommandText = "[Hiso].[uspPatientProblem_Add]";
                cmd.CommandType = CommandType.StoredProcedure;
                cmd.Parameters.Add(new SqlParameter("@tblProblems", dtProblem));

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
                //cmd.CommandTimeout = 3600;
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