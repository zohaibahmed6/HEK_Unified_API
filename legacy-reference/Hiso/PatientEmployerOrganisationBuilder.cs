using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Linq;
using System.Web;
using System.Xml;

namespace Hiso
{
    public class PatientEmployerOrganisationBuilder
    {
        public DataTable GenerateTable(XmlDocument xDoc, HealthLinkSession objSession)
        {
            DataTable dtPatient = new DataTable();
            try
            {
                string[] strColumns = Utitlity.GetColumnNameByTableName("UDT_tblPatientEmployerOrganisation");
                DataRow dr = dtPatient.NewRow();
                for (int i = 0; i < strColumns.Length; i++)
                {
                    string colName = strColumns[i];
                    dtPatient.Columns.Add(colName);
                    dr[colName] = DBNull.Value;
                }
                bool IsFields = false;
                XmlNodeList sectionList = xDoc.DocumentElement.GetElementsByTagName("section");
                if (sectionList.Count <= 0)
                {
                    sectionList = xDoc.DocumentElement.GetElementsByTagName("field");
                    IsFields = true;
                }
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

                dtPatient.Rows.Add(dr);
            }
            catch (Exception ex)
            {
                Logger.Logging.Instance.WriteExceptionLog(ex);
            }
            return dtPatient;
        }
        public bool Save(DataTable dtPatient, HealthLinkSession objSession)
        {
            bool retVal = false;
            int countryId = 0, EmployerCityId = 0, SuburbTownID = 0, DesignationID = 0;
            try {
                countryId = SaveCountry(dtPatient, objSession);
                if (countryId > 0)
                    EmployerCityId = SaveCity(dtPatient, objSession, countryId);
                if (EmployerCityId > 0)
                    SuburbTownID = SaveSuburb(dtPatient, objSession, EmployerCityId);
                DesignationID = SaveDesignation(dtPatient, objSession);
                if (countryId == 0 || EmployerCityId == 0 || SuburbTownID == 0 || DesignationID == 0)
                    Logger.Logging.Instance.WriteExceptionLog(new Exception(string.Format("Incomplete data in request form Patient Employer Information: CountryId: {0}, CityId: {1}, Suburb: {2}, Designation: {3}, SessionID: {4}", countryId, EmployerCityId, SuburbTownID, DesignationID, objSession.GUID)));
            }
            catch (Exception ex) {
                Logger.Logging.Instance.WriteExceptionLog(ex);
            } 
            
            using (SqlConnection con = new SqlConnection(System.Configuration.ConfigurationManager.ConnectionStrings["ConectionStringPMS_NZ"].ToString()))
            {
                try
                {
                    List<SqlParameter> sqlParam = new List<SqlParameter>();
                    sqlParam.Add(new SqlParameter("@tblOrganization", dtPatient));
                    sqlParam.Add(new SqlParameter("@pPatientId", objSession.PatientId));
                    sqlParam.Add(new SqlParameter("@pAppointmentId", objSession.AppointmentId));
                    sqlParam.Add(new SqlParameter("@pPracticeId", objSession.PracticeId));
                    sqlParam.Add(new SqlParameter("@pUpdatedBy", objSession.ProviderId));
                    sqlParam.Add(new SqlParameter("@pUserLoggingID", -1));
                    sqlParam.Add(new SqlParameter("@pEmployerCityId", EmployerCityId));
                    sqlParam.Add(new SqlParameter("@pSuburbTownID", SuburbTownID));
                    sqlParam.Add(new SqlParameter("@pCountryId", countryId));
                    sqlParam.Add(new SqlParameter("@pDesignationID", DesignationID));
                    SqlParameter outparam = new SqlParameter();
                    outparam.DbType = DbType.Int32;
                    outparam.Direction = ParameterDirection.Output;
                    outparam.ParameterName = "@pOutPutParam";
                    outparam.Value = 0;
                    sqlParam.Add(outparam);
                    //DALHelper.ExecuteNonQuery(con, commandType: CommandType.StoredProcedure, "[Hiso].[uspPatientEmployerOrganisation_AddUpdate]", sqlParam.ToArray());
                    DALHelper.ExecuteNonQuery(con, commandType: CommandType.StoredProcedure, commandText: "[Hiso].[uspPatientEmployerOrganisation_AddUpdate]", commandParameters: sqlParam.ToArray());
                    if (outparam.Value != DBNull.Value && int.Parse(outparam.Value.ToString()) < 0)
                    {
                        Logger.Logging.Instance.WriteExceptionLog(new Exception(string.Format("Failed to update patient consult info: SessionID {0}", objSession.GUID)));
                    }
                    retVal = true;
                }
                catch (Exception ex)
                {
                    retVal = false;
                    Logger.Logging.Instance.WriteExceptionLog(ex);
                }
                finally
                {
                    if (con.State == ConnectionState.Open)
                        con.Close();
                }
            }
            return retVal;
        }
        public int SaveCountry(DataTable dtPatient, HealthLinkSession objSession)
        {
            int countryId = 0;
            string countryName = string.Empty;
            string columnName = "PatientEmployerOrganisation_PhysicalCountry";
            if (!IsValidColumn(dtPatient, columnName, "country", out countryName))
                return countryId;

            using (SqlConnection con = new SqlConnection(System.Configuration.ConfigurationManager.ConnectionStrings["ConectionStringIndici_Master"].ToString()))
            {
                try
                {
                    List<SqlParameter> sqlParam = new List<SqlParameter>();
                    sqlParam.Add(new SqlParameter("@pCountryID", 0));
                    sqlParam.Add(new SqlParameter("@pCountryName", countryName));
                    sqlParam.Add(new SqlParameter("@pHL7Code", DBNull.Value));
                    sqlParam.Add(new SqlParameter("@pUpdatedBy", objSession.ProviderId));
                    sqlParam.Add(new SqlParameter("@pIsActive", true));
                    sqlParam.Add(new SqlParameter("@pIsDeleted", false));
                    sqlParam.Add(new SqlParameter("@pCountryCode", DBNull.Value));
                    sqlParam.Add(new SqlParameter("@pDialingCode", DBNull.Value));
                    sqlParam.Add(new SqlParameter("@pUserLoggingID", -1));
                    sqlParam.Add(new SqlParameter("@pSourceName", "Hiso Web Service"));
                    SqlParameter outparam = new SqlParameter();
                    outparam.DbType = DbType.Int32;
                    outparam.Direction = ParameterDirection.Output;
                    outparam.ParameterName = "@pOutPutParam";
                    outparam.Value = 0;
                    sqlParam.Add(outparam);
                    DALHelper.ExecuteNonQuery(con, commandType: CommandType.StoredProcedure, commandText: "Lookup.uspCountryInsertUpdate", commandParameters: sqlParam.ToArray());
                    if (outparam.Value != DBNull.Value && int.TryParse(outparam.Value.ToString(), out countryId) == false)
                    {
                        Logger.Logging.Instance.WriteExceptionLog(new Exception(string.Format("Failed to update Country: SessionID {0}", objSession.GUID)));
                    }
                }
                catch(Exception ex)
                {
                    countryId = 0;
                    Logger.Logging.Instance.WriteExceptionLog(ex);
                }
                finally
                {
                    if(con.State == ConnectionState.Open)
                    con.Close();
                }

            }
            return countryId;
        }
        public int SaveSuburb(DataTable dtPatient, HealthLinkSession objSession, int cityAreaId)
        {
            int suburbId = 0;
            string suburbName = string.Empty;
            string columnName = "PatientEmployerOrganisation_PhysicalAddress_Suburb";
            if (!IsValidColumn(dtPatient, columnName, "suburb", out suburbName))
                return suburbId;

            using (SqlConnection con = new SqlConnection(System.Configuration.ConfigurationManager.ConnectionStrings["ConectionStringIndici_Master"].ToString()))
            {
                try
                {
                    List<SqlParameter> sqlParam = new List<SqlParameter>();
                    sqlParam.Add(new SqlParameter("@pSuburbTownID", 0));
                    sqlParam.Add(new SqlParameter("@pSuburbTownTitle", suburbName));
                    if (cityAreaId > 0)
                        sqlParam.Add(new SqlParameter("@pCityAreaID", cityAreaId));
                    sqlParam.Add(new SqlParameter("@pHMLTownSuburbID", DBNull.Value));
                    sqlParam.Add(new SqlParameter("@pUpdatedBy", objSession.ProviderId));
                    sqlParam.Add(new SqlParameter("@pIsActive", true));
                    sqlParam.Add(new SqlParameter("@pIsDeleted", false));
                    sqlParam.Add(new SqlParameter("@pUserLoggingID", -1));
                    sqlParam.Add(new SqlParameter("@pSourceName", "Hiso Web Service"));

                    SqlParameter outparam = new SqlParameter();
                    outparam.DbType = DbType.Int32;
                    outparam.Direction = ParameterDirection.Output;
                    outparam.ParameterName = "@pOutPutParam";
                    outparam.Value = 0;
                    sqlParam.Add(outparam);
                    DALHelper.ExecuteNonQuery(con, commandType: CommandType.StoredProcedure,commandText:"Lookup.uspSuburbTownInsertUpdate", commandParameters: sqlParam.ToArray());
                    if (outparam.Value != DBNull.Value && int.TryParse(outparam.Value.ToString(), out suburbId) == false)
                    {
                        Logger.Logging.Instance.WriteExceptionLog(new Exception(string.Format("Failed to update suburb: SessionID {0}", objSession.GUID)));
                    }
                }
                catch(Exception ex)
                {
                    suburbId = 0;
                    Logger.Logging.Instance.WriteExceptionLog(ex);
                }
                finally
                {
                    if (con.State == ConnectionState.Open)
                        con.Close();
                }

            }
            return suburbId;
        }
        public int SaveDesignation(DataTable dtPatient, HealthLinkSession objSession)
        {
            int designationId = 0;
            string designationName = string.Empty;
            string columnName = "Patient_Occupation_Description";
            if (!IsValidColumn(dtPatient, columnName, "designation", out designationName))
                return designationId;

            using (SqlConnection con = new SqlConnection(System.Configuration.ConfigurationManager.ConnectionStrings["ConectionStringIndici_Master"].ToString()))
            {
                try
                {
                    List<SqlParameter> sqlParam = new List<SqlParameter>();
                    sqlParam.Add(new SqlParameter("@pDesignationID", 0));
                    sqlParam.Add(new SqlParameter("@pDesignationName", designationName));
                    sqlParam.Add(new SqlParameter("@pIsActive", true));
                    sqlParam.Add(new SqlParameter("@pIsDeleted", false));
                    sqlParam.Add(new SqlParameter("@pUpdatedBy", objSession.ProviderId));
                    sqlParam.Add(new SqlParameter("@pUserLoggingID", -1));

                    SqlParameter outparam = new SqlParameter();
                    outparam.DbType = DbType.Int32;
                    outparam.Direction = ParameterDirection.Output;
                    outparam.ParameterName = "@pOutPutParam";
                    outparam.Value = 0;
                    sqlParam.Add(outparam);
                    DALHelper.ExecuteNonQuery(con, commandType: CommandType.StoredProcedure,commandText:"[Lookup].[uspDesignationInsertUpdate]", commandParameters: sqlParam.ToArray());
                    if (outparam.Value != DBNull.Value && int.TryParse(outparam.Value.ToString(), out designationId) == false)
                    {
                        Logger.Logging.Instance.WriteExceptionLog(new Exception(string.Format("Failed to update designation: SessionID {0}", objSession.GUID)));
                    }
                }
                catch (Exception ex)
                {
                    designationId = 0;
                    Logger.Logging.Instance.WriteExceptionLog(ex);
                }
                finally
                {
                    if (con.State == ConnectionState.Open)
                        con.Close();
                }
            }
            return designationId;
        }
        public int SaveCity(DataTable dtPatient, HealthLinkSession objSession, int countryId)
        {
            // Validate City
            int cityId = 0;
            string cityName = string.Empty;
            string columnName = "PatientEmployerOrganisation_PhysicalAddress_City";
            if (!IsValidColumn(dtPatient, columnName, "city", out cityName))
                return cityId;
            // Validate postcode
            string postCodeId = string.Empty;
            columnName = "PatientEmployerOrganisation_PhysicalAddress_Postcode";
            if (!IsValidColumn(dtPatient, columnName, "postcode", out postCodeId))
            {
                postCodeId = string.Empty;
            }
            using (SqlConnection con = new SqlConnection(System.Configuration.ConfigurationManager.ConnectionStrings["ConectionStringIndici_Master"].ToString()))
            {
                try
                {
                    List<SqlParameter> sqlParam = new List<SqlParameter>();
                    sqlParam.Add(new SqlParameter("@pCityAreaID", 0));
                    sqlParam.Add(new SqlParameter("@pCityAreaTitle", cityName));
                    if (!string.IsNullOrEmpty(postCodeId))
                        sqlParam.Add(new SqlParameter("@pPostCodeID", DBNull.Value));
                    if (countryId > 0)
                        sqlParam.Add(new SqlParameter("@pCountryID", countryId));
                    sqlParam.Add(new SqlParameter("@pUpdatedBy", objSession.ProviderId));
                    sqlParam.Add(new SqlParameter("@pIsActive", true));
                    sqlParam.Add(new SqlParameter("@pHMLAreaID", DBNull.Value));
                    sqlParam.Add(new SqlParameter("@pIsDeleted", false));
                    sqlParam.Add(new SqlParameter("@pUserLoggingID", -1));
                    sqlParam.Add(new SqlParameter("@pSourceName", "Hiso Web Service"));
                    sqlParam.Add(new SqlParameter("@pCode", DBNull.Value));

                    SqlParameter outparam = new SqlParameter();
                    outparam.DbType = DbType.Int32;
                    outparam.Direction = ParameterDirection.Output;
                    outparam.ParameterName = "@pOutPutParam";
                    outparam.Value = 0;
                    sqlParam.Add(outparam);
                    DALHelper.ExecuteNonQuery(con, commandType: CommandType.StoredProcedure, commandText: "[Lookup].[uspCityAreaInsertUpdate]", commandParameters: sqlParam.ToArray());
                    if (outparam.Value != DBNull.Value && int.TryParse(outparam.Value.ToString(), out cityId) == false)
                    {
                        Logger.Logging.Instance.WriteExceptionLog(new Exception(string.Format("Failed to update city: SessionID {0}", objSession.GUID)));
                    }
                }
                catch (Exception ex)
                {
                    cityId = 0;
                    Logger.Logging.Instance.WriteExceptionLog(ex);
                }
                finally
                {
                    if (con.State == ConnectionState.Open)
                        con.Close();
                }
            }
            return cityId;
        }
        private bool IsValidColumn(DataTable dtPatient, string columnName, string fieldName, out string columnValue, bool isValidateEmpty = true)
        {
            bool isValid = false;
            columnValue = string.Empty;
            Exception ex = null;
            if (dtPatient == null || dtPatient.Rows.Count == 0)
            {
                ex = new Exception("Patient Employer Information not available in request xml.", new Exception("Patient Employer Information not available when try to extract " + fieldName + " from request xml."));
            }
            if (!dtPatient.Columns.Contains(columnName) || dtPatient.Rows[0][columnName] == DBNull.Value || dtPatient.Rows[0][columnName] == null)
            {
                ex = new Exception(columnName + " does not belong to the table.", new Exception("Either " + fieldName + " field is not present or Unable to parse " + fieldName + " field in request xml."));
            }
            columnValue = dtPatient.Rows[0][columnName].ToString();
            if (isValidateEmpty && string.IsNullOrEmpty(columnValue))
            {
                ex = new Exception(columnName + " is empty in request xml or in processed datatable.");
            }
            if (ex != null)
            {
                Logger.Logging.Instance.WriteExceptionLog(ex);
                return isValid;
            }
            else
            {
                isValid = true;
                return isValid;
            }
        }
    }
}