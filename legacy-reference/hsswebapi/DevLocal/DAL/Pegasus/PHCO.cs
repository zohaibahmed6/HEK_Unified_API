using DAL.HelperClasses;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Data.SqlClient;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DAL.Pegasus
{
    public class PHCO
    {
        public static DataTable GetCurrentPatientData(string patientID, string practiceid, out string error)
        {

            DataTable dtResult = new DataTable();
            error = string.Empty;

            string connectionString = Convert.ToString(ConfigurationManager.ConnectionStrings["ConnIndiciDB"+practiceid].ConnectionString);

            List<SqlParameter> sqlParams = new List<SqlParameter>();
            sqlParams.Add(new SqlParameter("@pPatientId", patientID));
            try
            {
                dtResult = DALHelper.ExecuteDataTable(connectionString, CommandType.StoredProcedure, "[OnlineClaim].[uspGetPatientData]", sqlParams.ToArray());
            }
            catch (Exception ex)
            {
                error = ex.Message;
            }

            return dtResult;
        }
        public static DataTable GetProviderData(string patientID, string practiceid, out string error)
        {

            DataTable dtResult = new DataTable();
            error = string.Empty;

            string connectionString = Convert.ToString(ConfigurationManager.ConnectionStrings["ConnIndiciDB"+practiceid].ConnectionString);

            List<SqlParameter> sqlParams = new List<SqlParameter>();
            sqlParams.Add(new SqlParameter("@pPatientId", patientID));
            try
            {
                dtResult = DALHelper.ExecuteDataTable(connectionString, CommandType.StoredProcedure, "[OnlineClaim].[uspGetProvider]", sqlParams.ToArray());
            }
            catch (Exception ex)
            {
                error = ex.Message;
            }

            return dtResult;
        }
        public static DataTable GetSessionData(string patientID, string appostringmentid, string practiceid, out string error)
        {

            DataTable dtResult = new DataTable();
            error = string.Empty;

            string connectionString = Convert.ToString(ConfigurationManager.ConnectionStrings["ConnIndiciDB"+practiceid].ConnectionString);

            List<SqlParameter> sqlParams = new List<SqlParameter>();
            sqlParams.Add(new SqlParameter("@pPatientId", patientID));
            try
            {
                dtResult = DALHelper.ExecuteDataTable(connectionString, CommandType.StoredProcedure, "", sqlParams.ToArray());
            }
            catch (Exception ex)
            {
                error = ex.Message;
            }

            return dtResult;
        }
        public static DataTable GetDiagnosisData(string patientID, string sortOrder, DateTime dtMinDate, DateTime dtMaxDate, string practiceid, out string error)
        {

            DataTable dtResult = new DataTable();
            error = string.Empty;

            string connectionString = Convert.ToString(ConfigurationManager.ConnectionStrings["ConnIndiciDB"+practiceid].ConnectionString);

            List<SqlParameter> sqlParams = new List<SqlParameter>();
            sqlParams.Add(new SqlParameter("@pPatientId", patientID));
            if (!string.IsNullOrWhiteSpace(sortOrder))
                sqlParams.Add(new SqlParameter("@pSortOrder", sortOrder));
            if (!dtMinDate.Equals(DateTime.MinValue))
                sqlParams.Add(new SqlParameter("@pMinDate", dtMinDate));
            if (!dtMaxDate.Equals(DateTime.MinValue))
                sqlParams.Add(new SqlParameter("@pMaxDate", dtMaxDate));
            try
            {
                dtResult = DALHelper.ExecuteDataTable(connectionString, CommandType.StoredProcedure, "[OnlineClaim].[uspGetConditions]", sqlParams.ToArray());
            }
            catch (Exception ex)
            {
                error = ex.Message;
            }

            return dtResult;
        }
        public static DataTable GetSurgeryData(string patientID, string LocationId, string practiceid, out string error)
        {

            DataTable dtResult = new DataTable();
            error = string.Empty;

            string connectionString = Convert.ToString(ConfigurationManager.ConnectionStrings["ConnIndiciDB"+practiceid].ConnectionString);

            List<SqlParameter> sqlParams = new List<SqlParameter>();
            sqlParams.Add(new SqlParameter("@pPatientId", patientID));
            if (!string.IsNullOrWhiteSpace(LocationId))
                sqlParams.Add(new SqlParameter("@pLocationId", LocationId));
            try
            {
                dtResult = DALHelper.ExecuteDataTable(connectionString, CommandType.StoredProcedure, "[OnlineClaim].[uspGetSurgeryData]", sqlParams.ToArray());
            }
            catch (Exception ex)
            {
                error = ex.Message;
            }

            return dtResult;
        }
    }
}
