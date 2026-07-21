using System;
using System.Collections;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DAL.HelperClasses
{
    public sealed class DbAccess
    {
        private static SqlConnection DbConn = new SqlConnection();
        private static SqlDataAdapter DbAdapter = new SqlDataAdapter();
        private static SqlCommand DbCommand = new SqlCommand();
        public static SqlTransaction DbTran;
        public static DataSet ds = new DataSet();
        private static string strConnString;

        public static void setConnString(string strConn)
        {
            try
            {
                strConnString = strConn;
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }

        public static string getConnString()
        {
            try
            {
                return strConnString;
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }

        public static int createConn()
        {
            try
            {
                if (DbConn.State == ConnectionState.Open)
                {
                    return Convert.ToInt32(DbConn.State);
                }
                else
                {
                    DbConn.ConnectionString = strConnString;
                    DbConn.Open();

                    return Convert.ToInt32(DbConn.State);
                }

            }
            catch (Exception ex)
            {
                throw ex;
            }
        }

        public static void closeConnection()
        {
            try
            {
                if (DbConn.State != 0)
                    DbConn.Close();
            }
            catch (Exception ex)
            {
                throw ex;
            }

        }

        public static void beginTrans()
        {
            try
            {
                if (DbTran == null)
                {
                    if (DbConn.State == 0)
                    {
                        createConn();
                        DbTran = DbConn.BeginTransaction();
                        DbAdapter.SelectCommand.Transaction = DbTran;
                    }
                    else
                    {
                        DbTran = DbConn.BeginTransaction();
                        DbAdapter.SelectCommand.Transaction = DbTran;
                    }
                }

            }
            catch (Exception ex)
            {
                throw ex;
            }

        }

        public static void commitTrans()
        {
            try
            {
                if (DbTran != null)
                {
                    DbTran.Commit();
                    DbTran = null;
                }

            }
            catch (Exception ex)
            {
                throw ex;
            }
        }

        public static void rollbackTrans()
        {
            try
            {
                if (DbTran != null)
                {
                    DbTran.Rollback();
                    DbTran = null;
                }

            }
            catch (Exception ex)
            {
                throw ex;
            }
        }

        /// <summary>
        /// Fills the Dataset dset and its Table tblname via stored procedure provided as spName arguement.Takes Parameters as param
        /// </summary>
        /// <param name="dSet"></param>
        /// <param name="spName"></param>
        /// <param name="param"></param>
        /// <param name="tblName"></param>
        public static void selectStoredProcedure(DataSet dSet, string spName, Hashtable param, string tblName)
        {
            try
            {
                if (DbConn.State == 0)
                {
                    createConn();
                }
                DbCommand.Parameters.Clear();
                ////
                DbCommand.Connection = DbConn;
                DbCommand.CommandText = spName;
                DbCommand.CommandType = CommandType.StoredProcedure;
                foreach (string para in param.Keys)
                {
                    DbCommand.Parameters.AddWithValue(para, param[para]);
                }

                DbAdapter.SelectCommand = (DbCommand);
                DbAdapter.Fill(dSet, tblName);
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }

        /// <summary>
        /// Fills the Dataset dset and its Table tblname via stored procedure provided as spName arguement.
        /// </summary>
        /// <param name="dSet"></param>
        /// <param name="spName"></param>
        /// <param name="tblName"></param>

        public static void selectStoredProcedure(DataSet dSet, string spName, string tblName)
        {
            try
            {
                if (DbConn.State == 0)
                {
                    createConn();
                }
                DbCommand.Parameters.Clear();
                DbCommand.Connection = DbConn;
                DbCommand.CommandText = spName;
                DbCommand.CommandType = CommandType.StoredProcedure;
                DbAdapter.SelectCommand = (DbCommand);
                DbAdapter.Fill(dSet, tblName);
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }

        /// <summary>
        /// Fills the Dataset dset and its Table tblName with the query arguement provided
        /// </summary>
        /// <param name="dSet"></param>
        /// <param name="query"></param>
        /// <param name="tblName"></param>
        public static void selectQuery(DataSet dSet, string query, string tblName)
        {
            try
            {
                if (DbConn.State == 0)
                {
                    createConn();
                }
                DbCommand.Parameters.Clear();
                DbCommand.Connection = DbConn;
                DbCommand.CommandText = query;
                DbCommand.CommandType = CommandType.Text;
                DbCommand.CommandTimeout = 0;
                DbAdapter = new SqlDataAdapter(DbCommand);

                DbAdapter.Fill(dSet, tblName);
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }

        /// <summary>
        /// Runs DML's provided as the query arguement.
        /// </summary>
        /// <param name="query"></param>
        /// <returns></returns>
        public static int executeQuery(string query)
        {
            try
            {
                if (DbConn.State == 0)
                {
                    createConn();
                }
                DbCommand.Parameters.Clear();
                DbCommand.Connection = DbConn;
                DbCommand.CommandText = query;
                DbCommand.CommandType = CommandType.Text;
                return DbCommand.ExecuteNonQuery();
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }

        /// <summary>
        /// Runs the DML stored procedure provided as spName with the paramters provided as param
        /// </summary>
        /// <param name="spName"></param>
        /// <param name="param"></param>
        /// <returns></returns>
        public static int executeStoredProcedure(string spName, Hashtable param)
        {
            try
            {
                if (DbConn.State == 0)
                {
                    createConn();
                }
                DbCommand.Parameters.Clear();
                DbCommand.Connection = DbConn;
                DbCommand.CommandText = spName;
                DbCommand.CommandType = CommandType.StoredProcedure;
                foreach (string para in param.Keys)
                {
                    DbCommand.Parameters.AddWithValue(para, param[para]);
                }
                DbCommand.ExecuteNonQuery();
                return 0;
            }
            catch (Exception ex)
            {
                throw ex;
            }

        }

        /// <summary>
        /// Updates the Database With the changes provided in the Dataset dSet
        /// </summary>
        /// <param name="dSet"></param>
        /// <param name="tblName"></param>
        /// <returns></returns>
        public static int executeDataSet(DataSet dSet, string tblName, string strSelectSql)
        {
            try
            {
                if (DbConn.State == 0)
                {
                    createConn();
                }
                DbAdapter.SelectCommand.CommandText = strSelectSql;
                DbAdapter.SelectCommand.CommandType = CommandType.Text;
                SqlCommandBuilder DbCommandBuilder = new SqlCommandBuilder(DbAdapter);
                DbCommandBuilder.ConflictOption = ConflictOption.OverwriteChanges;

                return DbAdapter.Update(dSet, tblName);
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }
        public static string GetColumnValue(string Query)
        {
            try
            {
                if (DbConn.State == 0)
                {
                    createConn();
                }
                DbCommand.Parameters.Clear();

                DbCommand.Connection = DbConn;
                DbCommand.CommandType = CommandType.Text;
                DbCommand.CommandText = Query;
                object objResult = DbCommand.ExecuteScalar();
                if (objResult == null)
                {
                    return "";
                }
                if (objResult == System.DBNull.Value)
                {
                    return "";
                }
                else
                {
                    return Convert.ToString(objResult);
                }
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }

        public static SqlDataReader eligibility_Reader(string Query)
        {
            try
            {
                if (DbConn.State == 0)
                {
                    createConn();
                }

                DbCommand.Parameters.Clear();
                DbCommand.Connection = DbConn;
                DbCommand.CommandType = CommandType.Text;
                DbCommand.CommandText = Query;

                return DbCommand.ExecuteReader();

            }
            catch (Exception ex)
            {
                throw ex;
            }
        }

        public static SqlDataReader selectEligibilityProcedure(SqlCommand DbCommand, string spName)
        {
            SqlDataReader dtReader;
            try
            {
                if (DbConn.State == 0)
                {
                    createConn();
                }

                DbCommand.Connection = DbConn;
                DbCommand.CommandText = spName;
                DbCommand.CommandType = CommandType.StoredProcedure;
                dtReader = DbCommand.ExecuteReader();
                return dtReader;
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }


        /// <summary>       
        ///Get the database server name
        /// </summary>
        /// <returns></returns>
        public static string getServerName()
        {
            try
            {
                return DbConn.DataSource.ToString();
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }

        /// <summary>       
        ///Get the database Name
        /// </summary>
        /// <returns></returns>
        public static string getDatabaseName()
        {
            try
            {
                return DbConn.Database.ToString();
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }

        public static void FillLocalTable(DataTable tblName, string query)
        {
            SqlDataAdapter DbAdapter = new SqlDataAdapter();
            SqlCommand DbCommand = new SqlCommand();
            try
            {
                lock (DbConn)
                {
                    if (DbConn.State == 0)
                    {
                        createConn();
                    }
                    DbCommand.Connection = DbConn;
                    DbCommand.CommandText = query;
                    DbCommand.CommandType = CommandType.Text;
                    DbAdapter.SelectCommand = DbCommand;
                    DbAdapter.Fill(tblName);

                    DbAdapter.Dispose();
                    DbCommand.Dispose();
                }
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }


        /// <summary>
        /// Used to exectue stored procedure, sqlparameters functions can be utilized with it easily
        /// </summary>
        /// <param name="spName"></param>
        /// <param name="param"></param>
        public static void executeStoredProcedure(string spName, SqlParameter[] param)
        {
            try
            {
                if (DbConn.State == 0)
                {
                    createConn();
                }
                DbCommand.Parameters.Clear();
                DbCommand.Connection = DbConn;
                DbCommand.CommandText = spName;
                DbCommand.CommandType = CommandType.StoredProcedure;
                foreach (SqlParameter item in param)
                {
                    DbCommand.Parameters.Add(item);
                }
                DbCommand.ExecuteReader().Dispose();
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }


        /// Runs DML's provided as the query arguement.
        /// </summary>
        /// <param name="query"></param>
        /// <returns></returns>
        public static string executeScalarQuery(string query)
        {
            SqlCommand DbCommand = new SqlCommand();
            try
            {
                if (DbConn.State == 0)
                {
                    createConn();
                }
                DbCommand.Connection = DbConn;
                DbCommand.CommandText = query;
                DbCommand.CommandType = CommandType.Text;
                object obj = DbCommand.ExecuteScalar();

                if (obj != null)
                    return obj.ToString();
                else
                    return null;

            }
            catch (Exception)
            {
                throw;
            }
        }
        public static int executeDataTable(DataTable tblName, string strSQL)
        {

            SqlDataAdapter DbAdapter = new SqlDataAdapter();
            SqlCommand DbCommand = new SqlCommand();
            try
            {
                if (DbConn.State == 0)
                {
                    createConn();
                }

                DbCommand.Connection = DbConn;
                DbCommand.CommandText = strSQL;
                DbCommand.CommandType = CommandType.Text;
                DbAdapter.SelectCommand = DbCommand;

                SqlCommandBuilder DbCommandBuilder = new SqlCommandBuilder(DbAdapter);
                DbCommandBuilder.ConflictOption = ConflictOption.OverwriteChanges;
                DbCommandBuilder.SetAllValues = true;
                return DbAdapter.Update(tblName);

            }

            catch (Exception exp)
            {
                throw exp;
            }
        }
        public static SqlDataReader executeDataReader(string strSql)
        {
            SqlCommand DbCommand = new SqlCommand();

            try
            {
                if (DbConn.State == 0)
                {
                    createConn();
                }
                DbCommand.Connection = DbConn;
                DbCommand.CommandText = strSql;
                DbCommand.CommandType = CommandType.Text;
                SqlDataReader dr = DbCommand.ExecuteReader();
                return dr;
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }
    }
}
