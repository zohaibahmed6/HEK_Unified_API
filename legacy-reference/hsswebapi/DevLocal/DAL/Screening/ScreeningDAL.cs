using DAL.HelperClasses;
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
    public class ScreeningDAL
    {
        #region Screening Data

        public DataTable GetScreeningTemplate(string templeteIDs, out string error)
        {
            error = string.Empty;
            Exception exception = new Exception();

            return GetScreeningTemplate(templeteIDs, out error, out exception);
        }

        private DataTable GetScreeningTemplate(string templeteIDs, out string error, out Exception exception)
        {
            DataTable dtResult = new DataTable();
            error = string.Empty;
            exception = new Exception();

            SqlConnection connectionString = new SqlConnection(Convert.ToString(ConfigurationManager.ConnectionStrings["ConnMHN"].ConnectionString));

            try
            {
                List<SqlParameter> sqlParams = new List<SqlParameter>();
                sqlParams.Add(new SqlParameter("@pTempleteIDs", templeteIDs));

                dtResult = DALHelper.ExecuteDataTable(connectionString, CommandType.StoredProcedure, "[dbo].[uspGetScreeningTemplate]", 300, sqlParams.ToArray());
                //dtResult = DALHelper.ExecuteDataTable(connectionString, CommandType.StoredProcedure, "[dbo].[uspGetScreeningTemplate]", 300, sqlParams.ToArray());
            }
            catch (Exception ex)
            {
                error = "GetScreeningTemplate: " + ex.Message;
                exception = ex;
            }

            return dtResult;
        }

        public DataTable GetScreeningDetail(string ScreeningTypeID, out string error)
        {
            error = string.Empty;
            Exception exception = new Exception();

            return GetScreeningDetail(ScreeningTypeID, out error, out exception);
        }

        private DataTable GetScreeningDetail(string ScreeningTypeID, out string error, out Exception exception)
        {
            DataTable dtResult = new DataTable();
            error = string.Empty;
            exception = new Exception();

            SqlConnection connectionString = new SqlConnection(Convert.ToString(ConfigurationManager.ConnectionStrings["ConnMHN"].ConnectionString));

            try
            {
                List<SqlParameter> sqlParams = new List<SqlParameter>();
                sqlParams.Add(new SqlParameter("@pScreeningTypeID", ScreeningTypeID));

                dtResult = DALHelper.ExecuteDataTable(connectionString, CommandType.StoredProcedure, "[dbo].[uspGetScreeningDetail]", 300, sqlParams.ToArray());
            }
            catch (Exception ex)
            {
                error = "GetScreeningDetail: " + ex.Message;
                exception = ex;
            }

            return dtResult;
        }

        public bool SaveJsonData(string ScreaningID, string PracticeID, string JsonString, string TempLineData, out string error)
        {
            error = string.Empty;
            Exception exception = new Exception();

            return SaveJsonData(ScreaningID, PracticeID, JsonString, TempLineData, out error, out exception);
        }

        private bool SaveJsonData(string ScreeningID, string PracticeID, string JsonString, string TempLineData, out string error, out Exception exception)
        {
            bool result = false;
            error = string.Empty;
            exception = new Exception();

            try
            {
                string connectionString = Convert.ToString(ConfigurationManager.ConnectionStrings["ConnMHN"].ConnectionString);

                List<SqlParameter> sqlParams = new List<SqlParameter>();
                sqlParams.Add(new SqlParameter("@pScreeningID", ScreeningID));
                sqlParams.Add(new SqlParameter("@pPracticeID", PracticeID));
                sqlParams.Add(new SqlParameter("@pJsonString", JsonString));
                sqlParams.Add(new SqlParameter("@pTempLineData", TempLineData));

                DALHelper.ExecuteNonQuery(connectionString, CommandType.StoredProcedure, "[dbo].[uspSaveScreeningJSonData]", 300, sqlParams.ToArray());
                result = true;

            }
            catch (Exception ex)
            {
                error = ex.Message;
                exception = ex;
                result = false;
            }

            return result;
        }

        public DataTable GetEthnicGroup(string MedTechID, out string error)
        {
            error = string.Empty;
            Exception exception = new Exception();

            return GetEthnicGroup(MedTechID, out error, out exception);
        }

        private DataTable GetEthnicGroup(string MedTechID, out string error, out Exception exception)
        {
            DataTable dtResult = new DataTable();
            error = string.Empty;
            exception = new Exception();

            SqlConnection connectionString = new SqlConnection(Convert.ToString(ConfigurationManager.ConnectionStrings["ConnMHN"].ConnectionString));

            try
            {
                List<SqlParameter> sqlParams = new List<SqlParameter>();
                sqlParams.Add(new SqlParameter("@pMedTechID", MedTechID));

                dtResult = DALHelper.ExecuteDataTable(connectionString, CommandType.StoredProcedure, "[dbo].[uspGetEthnicGroup]", 300, sqlParams.ToArray());
            }
            catch (Exception ex)
            {
                error = ex.Message;
                exception = ex;
            }

            return dtResult;
        }

        #endregion

        #region AF Screening Data

        public DataSet GetAFScreeningTemplate(string AFIDs, out string error)
        {
            error = string.Empty;
            Exception exception = new Exception();

            return GetAFScreeningTemplate(AFIDs, out error, out exception);
        }

        private DataSet GetAFScreeningTemplate(string AFIDs, out string error, out Exception exception)
        {
            DataSet dsResult = new DataSet();
            error = string.Empty;
            exception = new Exception();

            string connectionString = Convert.ToString(ConfigurationManager.ConnectionStrings["ConnMHN"].ConnectionString);

            try
            {
                List<SqlParameter> sqlParams = new List<SqlParameter>();
                sqlParams.Add(new SqlParameter("@pAFIDs", AFIDs));

                dsResult = DALHelper.ExecuteDataset(connectionString, CommandType.StoredProcedure, "[dbo].[uspGetAFScreeningTemplate]", 300, sqlParams.ToArray());
            }
            catch (Exception ex)
            {
                error = ex.Message;
                exception = ex;
            }

            return dsResult;
        }

        public DataSet GetAFScreeningDetail(string AFIDs, string PatientIDs, out string error)
        {
            error = string.Empty;
            Exception exception = new Exception();

            return GetAFScreeningDetail(AFIDs, PatientIDs, out error, out exception);
        }

        private DataSet GetAFScreeningDetail(string AFIDs, string PatientIDs, out string error, out Exception exception)
        {
            DataSet dsResult = new DataSet();
            error = string.Empty;
            exception = new Exception();

            string connectionString = Convert.ToString(ConfigurationManager.ConnectionStrings["ConnMHN"].ConnectionString);

            try
            {
                List<SqlParameter> sqlParams = new List<SqlParameter>();
                sqlParams.Add(new SqlParameter("@pAFIDs", AFIDs));
                sqlParams.Add(new SqlParameter("@pPatientIDs", PatientIDs));

                dsResult = DALHelper.ExecuteDataset(connectionString, CommandType.StoredProcedure, "[dbo].[uspGetAFScreeningDetail]", 300, sqlParams.ToArray());
            }
            catch (Exception ex)
            {
                error = ex.Message;
                exception = ex;
            }

            return dsResult;
        }

        public bool SaveAFJsonData(string AppointmentServicesAFID, string JsonString, out string error)
        {
            error = string.Empty;
            Exception exception = new Exception();

            return SaveAFJsonData(AppointmentServicesAFID, JsonString, out error, out exception);

        }

        private bool SaveAFJsonData(string AppointmentServicesAFID, string JsonString, out string error, out Exception exception)
        {
            bool result = false;
            error = string.Empty;
            exception = new Exception();

            try
            {
                string connectionString = Convert.ToString(ConfigurationManager.ConnectionStrings["ConnMHN"].ConnectionString);

                List<SqlParameter> sqlParams = new List<SqlParameter>();
                sqlParams.Add(new SqlParameter("@pAppointmentServicesAFID", AppointmentServicesAFID));
                sqlParams.Add(new SqlParameter("@pJsonString", JsonString));

                DALHelper.ExecuteNonQuery(connectionString, CommandType.StoredProcedure, "[dbo].[uspSaveAFScreeningJSonData]", sqlParams.ToArray());
                result = true;

            }
            catch (Exception ex)
            {
                error = ex.Message;
                exception = ex;
                result = false;
            }
            return result;
        }

        #endregion
    }
}
