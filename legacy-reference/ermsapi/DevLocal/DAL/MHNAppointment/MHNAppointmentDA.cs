using DAL.HelperClasses;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Data.SqlClient;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DAL.MHNAppointment
{
    public class MHNAppointmentDA
    {
        public static string connectionString = ConfigurationManager.ConnectionStrings["ConnMHNDataMigration"].ConnectionString;

        public static string Authenticate(string username, string password)
        {
            string token = string.Empty;

            List<SqlParameter> sqlParams = new List<SqlParameter>();

            if (!string.IsNullOrEmpty(username))
                sqlParams.Add(new SqlParameter("@pUserName ", username));

            if (!string.IsNullOrEmpty(password))
                sqlParams.Add(new SqlParameter("@pPassword ", password));

            DataSet dsUsers = new DataSet();
            DALHelper.FillDataset(connectionString, CommandType.StoredProcedure, "mhn.uspAuthenticateUser", dsUsers, new string[] { "AuthenticateUserList" }, sqlParams.ToArray());

            if (dsUsers.Tables.Count > 0 && dsUsers.Tables[0].Rows.Count > 0)
                token = Convert.ToString(dsUsers.Tables[0].Rows[0]["SessionToken"]);

            return token;
        }

        public static bool UploadAppointmentData(string sessionToken, string xmlData, out string error)
        {
            error = string.Empty;

            if (ValidateToken(sessionToken))
            {
                List<SqlParameter> sqlParams = new List<SqlParameter>();

                if (!string.IsNullOrEmpty(xmlData))
                    sqlParams.Add(new SqlParameter("@pXMLString", xmlData));

                try
                {
                    int result = DALHelper.ExecuteNonQuery(connectionString, CommandType.StoredProcedure, "[mhn].[uspInsertAppointmentData]", sqlParams.ToArray());

                    if (result > 0)
                        return true;
                    else
                        return false;
                }
                catch (Exception ex)
                {
                    error = ex.Message;
                    return false;
                }
            }
            else
            {
                error = "Error: Your token is invalid or has expired!";
                return false;
            }
        }

        public static bool UploadHL7Data(string sessionToken, string hl7Data, string type, string comments, out string error)
        {
            error = string.Empty;

            if (ValidateToken(sessionToken))
            {
                List<SqlParameter> sqlParams = new List<SqlParameter>();

                if (!string.IsNullOrEmpty(hl7Data))
                    sqlParams.Add(new SqlParameter("@pHL7Data", hl7Data));

                if (!string.IsNullOrEmpty(type))
                    sqlParams.Add(new SqlParameter("@pType", type));

                if (!string.IsNullOrEmpty(comments))
                    sqlParams.Add(new SqlParameter("@pComments", comments));

                try
                {
                    int result = DALHelper.ExecuteNonQuery(connectionString, CommandType.StoredProcedure, "[mhn].[uspInsertHL7Data]", sqlParams.ToArray());

                    if (result > 0)
                        return true;
                    else
                        return false;
                }
                catch (Exception ex)
                {
                    error = ex.Message;
                    return false;
                }
            }
            else
            {
                error = "Error: Your token is invalid or has expired!";
                return false;
            }
        }

        private static bool ValidateToken(string sessionToken)
        {
            List<SqlParameter> sqlParams = new List<SqlParameter>();

            if (!string.IsNullOrEmpty(sessionToken))
                sqlParams.Add(new SqlParameter("@pSession", sessionToken));

            DataSet dsTokens = new DataSet();
            DALHelper.FillDataset(connectionString, CommandType.StoredProcedure, "mhn.uspAuthenticateUserSession", dsTokens, new string[] { "AuthenticateUserList" }, sqlParams.ToArray());

            if (dsTokens.Tables.Count > 0
                && dsTokens.Tables[0].Rows.Count > 0
                && !Convert.ToString(dsTokens.Tables[0].Rows[0][0]).Equals("0"))
                return true;

            return false;
        }
    }
}
