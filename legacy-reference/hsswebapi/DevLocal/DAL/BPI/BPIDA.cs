using DAL.HelperClasses;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Data.SqlClient;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DAL.BPI
{
    public class BPIDA
    {
        public DataSet PatientExtract(DateTime? dtValue, int practiceId, out string error)
        {
            Exception exception = new Exception();
            error = string.Empty;

            return PatientExtract(dtValue, practiceId, out error, out exception);
        }

        public DataSet PatientExtract(DateTime? dtValue, int practiceId, out string error, out Exception exception)
        {
            DataSet dsResult = new DataSet();
            error = string.Empty;
            exception = new Exception();

            string connectionString = Convert.ToString(ConfigurationManager.ConnectionStrings["ConnBPI"].ConnectionString);

            try
            {
                List<SqlParameter> sqlParams = new List<SqlParameter>();

                if (dtValue != null)
                    sqlParams.Add(new SqlParameter("@date", dtValue));
                else
                    sqlParams.Add(new SqlParameter("@date", DateTime.Today.AddDays(-1)));

                sqlParams.Add(new SqlParameter("@pPracticeID", practiceId));

                dsResult = DALHelper.ExecuteDataset(connectionString, CommandType.StoredProcedure, "[localBCP].[uspPatientExtract]", 300, sqlParams.ToArray());
            }
            catch (Exception ex)
            {
                error = ex.Message;
                exception = ex;
            }

            return dsResult;
        }

        public DataSet DiagnosisExtract(DateTime? dtValue, int practiceId, out string error)
        {
            error = string.Empty;
            Exception exception = new Exception();

            return DiagnosisExtract(dtValue, practiceId, out error, out exception);
        }

        public DataSet DiagnosisExtract(DateTime? dtValue, int practiceId, out string error, out Exception exception)
        {
            DataSet dsResult = new DataSet();
            error = string.Empty;
            exception = new Exception();

            string connectionString = Convert.ToString(ConfigurationManager.ConnectionStrings["ConnBPI"].ConnectionString);

            try
            {
                List<SqlParameter> sqlParams = new List<SqlParameter>();

                if (dtValue != null)
                    sqlParams.Add(new SqlParameter("@date", dtValue));
                else
                    sqlParams.Add(new SqlParameter("@date", DateTime.Today.AddDays(-1)));

                sqlParams.Add(new SqlParameter("@pPracticeID", practiceId));

                dsResult = DALHelper.ExecuteDataset(connectionString, CommandType.StoredProcedure, "[localBCP].[UspCLASSIFICATION]", 300, sqlParams.ToArray());
            }
            catch (Exception ex)
            {
                error = ex.Message;
                exception = ex;
            }

            return dsResult;
        }

        public DataSet EthnicityExtract(DateTime? dtValue, int practiceId, out string error)
        {
            error = string.Empty;
            Exception exception = new Exception();

            return EthnicityExtract(dtValue, practiceId, out error, out exception);
        }

        public DataSet EthnicityExtract(DateTime? dtValue, int practiceId, out string error, out Exception exception)
        {
            DataSet dsResult = new DataSet();
            error = string.Empty;
            exception = new Exception();

            string connectionString = Convert.ToString(ConfigurationManager.ConnectionStrings["ConnBPI"].ConnectionString);

            try
            {
                List<SqlParameter> sqlParams = new List<SqlParameter>();

                if (dtValue != null)
                    sqlParams.Add(new SqlParameter("@date", dtValue));
                else
                    sqlParams.Add(new SqlParameter("@date", DateTime.Today.AddDays(-1)));

                sqlParams.Add(new SqlParameter("@pPracticeID", practiceId));

                dsResult = DALHelper.ExecuteDataset(connectionString, CommandType.StoredProcedure, "[localBCP].[UspEthnicityExtract]", 300, sqlParams.ToArray());
            }
            catch (Exception ex)
            {
                error = ex.Message;
                exception = ex;
            }

            return dsResult;
        }

        public DataSet HistoryExtract(DateTime? dtValue, int practiceId, out string error)
        {
            error = string.Empty;
            Exception exception = new Exception();

            return HistoryExtract(dtValue, practiceId, out error, out exception);
        }

        public DataSet HistoryExtract(DateTime? dtValue, int practiceId, out string error, out Exception exception)
        {
            DataSet dsResult = new DataSet();
            error = string.Empty;
            exception = new Exception();

            string connectionString = Convert.ToString(ConfigurationManager.ConnectionStrings["ConnBPI"].ConnectionString);

            try
            {
                List<SqlParameter> sqlParams = new List<SqlParameter>();

                if (dtValue != null)
                    sqlParams.Add(new SqlParameter("@date", dtValue));
                else
                    sqlParams.Add(new SqlParameter("@date", DateTime.Today.AddDays(-1)));

                sqlParams.Add(new SqlParameter("@pPracticeID", practiceId));

                dsResult = DALHelper.ExecuteDataset(connectionString, CommandType.StoredProcedure, "[localBCP].[UspHistoryExtract]", 300, sqlParams.ToArray());
            }
            catch (Exception ex)
            {
                error = ex.Message;
                exception = ex;
            }

            return dsResult;
        }

        public DataSet ImmunizationExtract(DateTime? dtValue, int practiceId, out string error)
        {
            error = string.Empty;
            Exception exception = new Exception();

            return ImmunizationExtract(dtValue, practiceId, out error, out exception);
        }

        public DataSet ImmunizationExtract(DateTime? dtValue, int practiceId, out string error, out Exception exception)
        {
            DataSet dsResult = new DataSet();
            error = string.Empty;
            exception = new Exception();

            string connectionString = Convert.ToString(ConfigurationManager.ConnectionStrings["ConnBPI"].ConnectionString);

            try
            {
                List<SqlParameter> sqlParams = new List<SqlParameter>();

                if (dtValue != null)
                    sqlParams.Add(new SqlParameter("@date", dtValue));
                else
                    sqlParams.Add(new SqlParameter("@date", DateTime.Today.AddDays(-1)));

                sqlParams.Add(new SqlParameter("@pPracticeID", practiceId));

                dsResult = DALHelper.ExecuteDataset(connectionString, CommandType.StoredProcedure, "[localBCP].[UspImmunizationExtract]", 300, sqlParams.ToArray());
            }
            catch (Exception ex)
            {
                error = ex.Message;
                exception = ex;
            }

            return dsResult;
        }

        public DataSet InboxExtract(DateTime? dtValue, int practiceId, out string error)
        {
            error = string.Empty;
            Exception exception = new Exception();

            return InboxExtract(dtValue, practiceId, out error, out exception);
        }

        public DataSet InboxExtract(DateTime? dtValue, int practiceId, out string error, out Exception exception)
        {
            DataSet dsResult = new DataSet();
            error = string.Empty;
            exception = new Exception();

            string connectionString = Convert.ToString(ConfigurationManager.ConnectionStrings["ConnBPI"].ConnectionString);

            try
            {
                List<SqlParameter> sqlParams = new List<SqlParameter>();

                if (dtValue != null)
                    sqlParams.Add(new SqlParameter("@date", dtValue));
                else
                    sqlParams.Add(new SqlParameter("@date", DateTime.Today.AddDays(-1)));

                sqlParams.Add(new SqlParameter("@pPracticeID", practiceId));

                dsResult = DALHelper.ExecuteDataset(connectionString, CommandType.StoredProcedure, "[localBCP].[UspINBOX]", 300, sqlParams.ToArray());
            }
            catch (Exception ex)
            {
                error = ex.Message;
                exception = ex;
            }

            return dsResult;
        }

        public DataSet InvoiceExtract(DateTime? dtValue, int practiceId, out string error)
        {
            error = string.Empty;
            Exception exception = new Exception();

            return InvoiceExtract(dtValue, practiceId, out error, out exception);
        }

        public DataSet InvoiceExtract(DateTime? dtValue, int practiceId, out string error, out Exception exception)
        {
            DataSet dsResult = new DataSet();
            error = string.Empty;
            exception = new Exception();

            string connectionString = Convert.ToString(ConfigurationManager.ConnectionStrings["ConnBPI"].ConnectionString);

            try
            {
                List<SqlParameter> sqlParams = new List<SqlParameter>();

                if (dtValue != null)
                    sqlParams.Add(new SqlParameter("@date", dtValue));
                else
                    sqlParams.Add(new SqlParameter("@date", DateTime.Today.AddDays(-1)));

                sqlParams.Add(new SqlParameter("@pPracticeID", practiceId));

                dsResult = DALHelper.ExecuteDataset(connectionString, CommandType.StoredProcedure, "[localBCP].[UspINVLINE]", 300, sqlParams.ToArray());
            }
            catch (Exception ex)
            {
                error = ex.Message;
                exception = ex;
            }

            return dsResult;
        }

        public DataSet MedicalWarningAllergiesExtract(DateTime? dtValue, int practiceId, out string error)
        {
            error = string.Empty;
            Exception exception = new Exception();

            return MedicalWarningAllergiesExtract(dtValue, practiceId, out error, out exception);
        }

        public DataSet MedicalWarningAllergiesExtract(DateTime? dtValue, int practiceId, out string error, out Exception exception)
        {
            DataSet dsResult = new DataSet();
            error = string.Empty;
            exception = new Exception();

            string connectionString = Convert.ToString(ConfigurationManager.ConnectionStrings["ConnBPI"].ConnectionString);

            try
            {
                List<SqlParameter> sqlParams = new List<SqlParameter>();

                if (dtValue != null)
                    sqlParams.Add(new SqlParameter("@date", dtValue));
                else
                    sqlParams.Add(new SqlParameter("@date", DateTime.Today.AddDays(-1)));

                sqlParams.Add(new SqlParameter("@pPracticeID", practiceId));

                dsResult = DALHelper.ExecuteDataset(connectionString, CommandType.StoredProcedure, "[localBCP].[UspMedicalWarningAllergiesExtract]", 300, sqlParams.ToArray());
            }
            catch (Exception ex)
            {
                error = ex.Message;
                exception = ex;
            }

            return dsResult;
        }

        public DataSet ProviderExtract(DateTime? dtValue, int practiceId, out string error)
        {
            error = string.Empty;
            Exception exception = new Exception();

            return ProviderExtract(dtValue, practiceId, out error, out exception);
        }

        public DataSet ProviderExtract(DateTime? dtValue, int practiceId, out string error, out Exception exception)
        {
            DataSet dsResult = new DataSet();
            error = string.Empty;
            exception = new Exception();

            string connectionString = Convert.ToString(ConfigurationManager.ConnectionStrings["ConnBPI"].ConnectionString);

            try
            {
                List<SqlParameter> sqlParams = new List<SqlParameter>();

                if (dtValue != null)
                    sqlParams.Add(new SqlParameter("@date", dtValue));
                else
                    sqlParams.Add(new SqlParameter("@date", DateTime.Today.AddDays(-1)));

                sqlParams.Add(new SqlParameter("@pPracticeID", practiceId));

                dsResult = DALHelper.ExecuteDataset(connectionString, CommandType.StoredProcedure, "[localBCP].[UspProviderExtract]", 300, sqlParams.ToArray());
            }
            catch (Exception ex)
            {
                error = ex.Message;
                exception = ex;
            }

            return dsResult;
        }

        public DataSet ReadV2Extract(DateTime? dtValue, int practiceId, out string error)
        {
            error = string.Empty;
            Exception exception = new Exception();

            return ReadV2Extract(dtValue, practiceId, out error, out exception);
        }

        public DataSet ReadV2Extract(DateTime? dtValue, int practiceId, out string error, out Exception exception)
        {
            DataSet dsResult = new DataSet();
            error = string.Empty;
            exception = new Exception();

            string connectionString = Convert.ToString(ConfigurationManager.ConnectionStrings["ConnBPI"].ConnectionString);

            try
            {
                List<SqlParameter> sqlParams = new List<SqlParameter>();

                if (dtValue != null)
                    sqlParams.Add(new SqlParameter("@date", dtValue));
                else
                    sqlParams.Add(new SqlParameter("@date", DateTime.Today.AddDays(-1)));

                sqlParams.Add(new SqlParameter("@pPracticeID", practiceId));

                dsResult = DALHelper.ExecuteDataset(connectionString, CommandType.StoredProcedure, "[localBCP].[UspReadV2Extract]", 300, sqlParams.ToArray());
            }
            catch (Exception ex)
            {
                error = ex.Message;
                exception = ex;
            }

            return dsResult;
        }

        public DataSet RolesExtract(DateTime? dtValue, int practiceId, out string error)
        {
            error = string.Empty;
            Exception exception = new Exception();

            return RolesExtract(dtValue, practiceId, out error, out exception);
        }

        public DataSet RolesExtract(DateTime? dtValue, int practiceId, out string error, out Exception exception)
        {
            DataSet dsResult = new DataSet();
            error = string.Empty;
            exception = new Exception();

            string connectionString = Convert.ToString(ConfigurationManager.ConnectionStrings["ConnBPI"].ConnectionString);

            try
            {
                List<SqlParameter> sqlParams = new List<SqlParameter>();

                if (dtValue != null)
                    sqlParams.Add(new SqlParameter("@date", dtValue));
                else
                    sqlParams.Add(new SqlParameter("@date", DateTime.Today.AddDays(-1)));

                sqlParams.Add(new SqlParameter("@pPracticeID", practiceId));

                dsResult = DALHelper.ExecuteDataset(connectionString, CommandType.StoredProcedure, "[localBCP].[UspRolesExtract]", 300, sqlParams.ToArray());
            }
            catch (Exception ex)
            {
                error = ex.Message;
                exception = ex;
            }

            return dsResult;
        }

        public DataSet ScreeningMeasurementExtract(DateTime? dtValue, int practiceId, out string error)
        {
            error = string.Empty;
            Exception exception = new Exception();

            return ScreeningMeasurementExtract(dtValue, practiceId, out error, out exception);
        }

        public DataSet ScreeningMeasurementExtract(DateTime? dtValue, int practiceId, out string error, out Exception exception)
        {
            DataSet dsResult = new DataSet();
            error = string.Empty;
            exception = new Exception();

            string connectionString = Convert.ToString(ConfigurationManager.ConnectionStrings["ConnBPI"].ConnectionString);

            try
            {
                List<SqlParameter> sqlParams = new List<SqlParameter>();

                if (dtValue != null)
                    sqlParams.Add(new SqlParameter("@date", dtValue));
                else
                    sqlParams.Add(new SqlParameter("@date", DateTime.Today.AddDays(-1)));

                sqlParams.Add(new SqlParameter("@pPracticeID", practiceId));

                dsResult = DALHelper.ExecuteDataset(connectionString, CommandType.StoredProcedure, "[localBCP].[UspScreeningMeasurementExtract]", 300, sqlParams.ToArray());
            }
            catch (Exception ex)
            {
                error = ex.Message;
                exception = ex;
            }

            return dsResult;
        }

        public DataSet ScreeningComboExtract(int practiceId, out string error)
        {
            error = string.Empty;
            Exception exception = new Exception();

            return ScreeningComboExtract(practiceId, out error, out exception);
        }

        public DataSet ScreeningComboExtract(int practiceId, out string error, out Exception exception)
        {
            DataSet dsResult = new DataSet();
            error = string.Empty;
            exception = new Exception();

            string connectionString = Convert.ToString(ConfigurationManager.ConnectionStrings["ConnBPI"].ConnectionString);

            try
            {
                List<SqlParameter> sqlParams = new List<SqlParameter>();

                sqlParams.Add(new SqlParameter("@pPracticeID", practiceId));

                //sqlParams.Add(new SqlParameter("@date", DateTime.Today.AddDays(-1)));

                //dsResult = DALHelper.ExecuteDataset(connectionString, CommandType.StoredProcedure, "[localBCP].[uspSCNCOMBO]", sqlParams.ToArray());
                dsResult = DALHelper.ExecuteDataset(connectionString, CommandType.StoredProcedure, "[localBCP].[uspSCNCOMBO]", 300, sqlParams.ToArray());
            }
            catch (Exception ex)
            {
                error = ex.Message;
                exception = ex;
            }

            return dsResult;
        }

        public DataSet ScreeningFieldExtract(int practiceId, out string error)
        {
            error = string.Empty;
            Exception exception = new Exception();

            return ScreeningFieldExtract(practiceId, out error, out exception);
        }

        public DataSet ScreeningFieldExtract(int practiceId, out string error, out Exception exception)
        {
            DataSet dsResult = new DataSet();
            error = string.Empty;
            exception = new Exception();

            string connectionString = Convert.ToString(ConfigurationManager.ConnectionStrings["ConnBPI"].ConnectionString);

            try
            {
                List<SqlParameter> sqlParams = new List<SqlParameter>();

                sqlParams.Add(new SqlParameter("@pPracticeID", practiceId));
                //sqlParams.Add(new SqlParameter("@date", DateTime.Today.AddDays(-1)));

                //dsResult = DALHelper.ExecuteDataset(connectionString, CommandType.StoredProcedure, "[localBCP].[USPSCNFIELDS]", sqlParams.ToArray());
                dsResult = DALHelper.ExecuteDataset(connectionString, CommandType.StoredProcedure, "[localBCP].[USPSCNFIELDS]", 300, sqlParams.ToArray());
            }
            catch (Exception ex)
            {
                error = ex.Message;
                exception = ex;
            }

            return dsResult;
        }

        public DataSet ScreeningOutCOmeExtract(DateTime? dtValue, int practiceId, out string error)
        {
            error = string.Empty;
            Exception exception = new Exception();

            return ScreeningOutCOmeExtract(dtValue, practiceId, out error, out exception);
        }

        public DataSet ScreeningOutCOmeExtract(DateTime? dtValue, int practiceId, out string error, out Exception exception)
        {
            DataSet dsResult = new DataSet();
            error = string.Empty;
            exception = new Exception();

            string connectionString = Convert.ToString(ConfigurationManager.ConnectionStrings["ConnBPI"].ConnectionString);

            try
            {
                List<SqlParameter> sqlParams = new List<SqlParameter>();

                if (dtValue != null)
                    sqlParams.Add(new SqlParameter("@date", dtValue));
                else
                    sqlParams.Add(new SqlParameter("@date", DateTime.Today.AddDays(-1)));

                sqlParams.Add(new SqlParameter("@pPracticeID", practiceId));

                dsResult = DALHelper.ExecuteDataset(connectionString, CommandType.StoredProcedure, "[localBCP].[UspScreeningOutCOmeExtract]", 300, sqlParams.ToArray());
            }
            catch (Exception ex)
            {
                error = ex.Message;
                exception = ex;
            }

            return dsResult;
        }

        public DataSet HBLVaccinationMapExtract(DateTime? dtValue, int practiceId, out string error)
        {
            error = string.Empty;
            Exception exception = new Exception();

            return HBLVaccinationMapExtract(dtValue, practiceId, out error, out exception);
        }

        public DataSet HBLVaccinationMapExtract(DateTime? dtValue, int practiceId, out string error, out Exception exception)
        {
            DataSet dsResult = new DataSet();
            error = string.Empty;
            exception = new Exception();

            string connectionString = Convert.ToString(ConfigurationManager.ConnectionStrings["ConnBPI"].ConnectionString);

            try
            {
                List<SqlParameter> sqlParams = new List<SqlParameter>();

                if (dtValue != null)
                    sqlParams.Add(new SqlParameter("@date", dtValue));
                else
                    sqlParams.Add(new SqlParameter("@date", DateTime.Today.AddDays(-1)));

                sqlParams.Add(new SqlParameter("@pPracticeID", practiceId));

                dsResult = DALHelper.ExecuteDataset(connectionString, CommandType.StoredProcedure, "[localBCP].[UspHBLVACCMAP]", 300, sqlParams.ToArray());
            }
            catch (Exception ex)
            {
                error = ex.Message;
                exception = ex;
            }

            return dsResult;
        }

        public DataSet NIRINLINEExtract(DateTime? dtValue, int practiceId, out string error)
        {
            error = string.Empty;
            Exception exception = new Exception();

            return NIRINLINEExtract(dtValue, practiceId, out error, out exception);
        }

        public DataSet NIRINLINEExtract(DateTime? dtValue, int practiceId, out string error, out Exception exception)
        {
            DataSet dsResult = new DataSet();
            error = string.Empty;
            exception = new Exception();

            string connectionString = Convert.ToString(ConfigurationManager.ConnectionStrings["ConnBPI"].ConnectionString);

            try
            {
                List<SqlParameter> sqlParams = new List<SqlParameter>();

                if (dtValue != null)
                    sqlParams.Add(new SqlParameter("@date", dtValue));
                else
                    sqlParams.Add(new SqlParameter("@date", DateTime.Today.AddDays(-1)));

                sqlParams.Add(new SqlParameter("@pPracticeID", practiceId));

                dsResult = DALHelper.ExecuteDataset(connectionString, CommandType.StoredProcedure, "localBCP.uspNIRINLINE", 300, sqlParams.ToArray());
            }
            catch (Exception ex)
            {
                error = ex.Message;
                exception = ex;
            }

            return dsResult;
        }

        public DataSet AFITEMExtract(int practiceId, out string error)
        {
            error = string.Empty;
            Exception exception = new Exception();

            return AFITEMExtract(practiceId, out error, out exception);
        }

        public DataSet AFITEMExtract(int practiceId, out string error, out Exception exception)
        {
            DataSet dsResult = new DataSet();
            error = string.Empty;
            exception = new Exception();

            string connectionString = Convert.ToString(ConfigurationManager.ConnectionStrings["ConnBPI"].ConnectionString);

            try
            {
                List<SqlParameter> sqlParams = new List<SqlParameter>();
                sqlParams.Add(new SqlParameter("@pPracticeID", practiceId));

                dsResult = DALHelper.ExecuteDataset(connectionString, CommandType.StoredProcedure, "[localBCP].[UspAFITEMExtract]", 300, sqlParams.ToArray());
            }
            catch (Exception ex)
            {
                error = ex.Message;
                exception = ex;
            }

            return dsResult;
        }

        public DataSet PATADVFORMExtract(int practiceId, out string error)
        {
            error = string.Empty;
            Exception exception = new Exception();

            return PATADVFORMExtract(practiceId, out error, out exception);
        }

        public DataSet PATADVFORMExtract(int practiceId, out string error, out Exception exception)
        {
            DataSet dsResult = new DataSet();
            error = string.Empty;
            exception = new Exception();

            string connectionString = Convert.ToString(ConfigurationManager.ConnectionStrings["ConnBPI"].ConnectionString);

            try
            {
                List<SqlParameter> sqlParams = new List<SqlParameter>();
                sqlParams.Add(new SqlParameter("@pPracticeID", practiceId));

                dsResult = DALHelper.ExecuteDataset(connectionString, CommandType.StoredProcedure, "[localBCP].[UspPATADVFORMExtract]", 300, sqlParams.ToArray());
            }
            catch (Exception ex)
            {
                error = ex.Message;
                exception = ex;
            }

            return dsResult;
        }

        public DataSet PATAFTERMVALUEExtract(int practiceId, out string error)
        {
            error = string.Empty;
            Exception exception = new Exception();

            return PATAFTERMVALUEExtract(practiceId, out error, out exception);
        }

        public DataSet PATAFTERMVALUEExtract(int practiceId, out string error, out Exception exception)
        {
            DataSet dsResult = new DataSet();
            error = string.Empty;
            exception = new Exception();

            string connectionString = Convert.ToString(ConfigurationManager.ConnectionStrings["ConnBPI"].ConnectionString);

            try
            {
                List<SqlParameter> sqlParams = new List<SqlParameter>();

                sqlParams.Add(new SqlParameter("@pPracticeID", practiceId));

                dsResult = DALHelper.ExecuteDataset(connectionString, CommandType.StoredProcedure, "[localBCP].[UspPATAFTERMVALUEExtract]", 300, sqlParams.ToArray());
            }
            catch (Exception ex)
            {
                error = ex.Message;
                exception = ex;
            }

            return dsResult;
        }

        public DataSet MedicationExtract(DateTime? dtValue, int practiceId, out string error)
        {
            error = string.Empty;
            Exception exception = new Exception();

            return MedicationExtract(dtValue, practiceId, out error, out exception);
        }

        public DataSet MedicationExtract(DateTime? dtValue, int practiceId, out string error, out Exception exception)
        {
            DataSet dsResult = new DataSet();
            error = string.Empty;
            exception = new Exception();

            string connectionString = Convert.ToString(ConfigurationManager.ConnectionStrings["ConnBPI"].ConnectionString);

            try
            {
                List<SqlParameter> sqlParams = new List<SqlParameter>();

                if (dtValue != null)
                    sqlParams.Add(new SqlParameter("@date", dtValue));
                else
                    sqlParams.Add(new SqlParameter("@date", DateTime.Today.AddDays(-1)));

                sqlParams.Add(new SqlParameter("@pPracticeID", practiceId));

                dsResult = DALHelper.ExecuteDataset(connectionString, CommandType.StoredProcedure, "[localBCP].[UspScriptExtract]", 300, sqlParams.ToArray());
            }
            catch (Exception ex)
            {
                error = ex.Message;
                exception = ex;
            }

            return dsResult;
        }

        public DataSet ScreeningExtract(DateTime? dtValue, int practiceId, out string error)
        {
            error = string.Empty;
            Exception exception = new Exception();

            return ScreeningExtract(dtValue, practiceId, out error, out exception);
        }

        public DataSet ScreeningExtract(DateTime? dtValue, int practiceId, out string error, out Exception exception)
        {
            DataSet dsResult = new DataSet();
            error = string.Empty;
            exception = new Exception();

            string connectionString = Convert.ToString(ConfigurationManager.ConnectionStrings["ConnBPI"].ConnectionString);

            try
            {
                List<SqlParameter> sqlParams = new List<SqlParameter>();

                if (dtValue != null)
                    sqlParams.Add(new SqlParameter("@date", dtValue));
                else
                    sqlParams.Add(new SqlParameter("@date", DateTime.Today.AddDays(-1)));

                sqlParams.Add(new SqlParameter("@pPracticeID", practiceId));

                dsResult = DALHelper.ExecuteDataset(connectionString, CommandType.StoredProcedure, "[localBCP].[UspScreening]", 300, sqlParams.ToArray());
            }
            catch (Exception ex)
            {
                error = ex.Message;
                exception = ex;
            }

            return dsResult;
        }

        public DataSet TransactionsExtract(DateTime? dtValue, int practiceId, out string error)
        {
            error = string.Empty;
            Exception exception = new Exception();

            return TransactionsExtract(dtValue, practiceId, out error, out exception);
        }

        public DataSet TransactionsExtract(DateTime? dtValue, int practiceId, out string error, out Exception exception)
        {
            DataSet dsResult = new DataSet();
            error = string.Empty;
            exception = new Exception();

            string connectionString = Convert.ToString(ConfigurationManager.ConnectionStrings["ConnBPI"].ConnectionString);

            try
            {
                List<SqlParameter> sqlParams = new List<SqlParameter>();

                if (dtValue != null)
                    sqlParams.Add(new SqlParameter("@date", dtValue));
                else
                    sqlParams.Add(new SqlParameter("@date", DateTime.Today.AddDays(-1)));

                sqlParams.Add(new SqlParameter("@pPracticeID", practiceId));

                dsResult = DALHelper.ExecuteDataset(connectionString, CommandType.StoredProcedure, "[localBCP].[UsTransactionsExtract]", 300, sqlParams.ToArray());
            }
            catch (Exception ex)
            {
                error = ex.Message;
                exception = ex;
            }

            return dsResult;
        }

        public DataSet ServiceSubsidyExtract(DateTime? dtValue, int practiceId, out string error)
        {
            error = string.Empty;
            Exception exception = new Exception();

            return ServiceSubsidyExtract(dtValue, practiceId, out error, out exception);
        }

        public DataSet ServiceSubsidyExtract(DateTime? dtValue, int practiceId, out string error, out Exception exception)
        {
            DataSet dsResult = new DataSet();
            error = string.Empty;
            exception = new Exception();

            string connectionString = Convert.ToString(ConfigurationManager.ConnectionStrings["ConnBPI"].ConnectionString);

            try
            {
                List<SqlParameter> sqlParams = new List<SqlParameter>();

                if (dtValue != null)
                    sqlParams.Add(new SqlParameter("@date", dtValue));
                else
                    sqlParams.Add(new SqlParameter("@date", DateTime.Today.AddDays(-1)));

                sqlParams.Add(new SqlParameter("@pPracticeID", practiceId));

                dsResult = DALHelper.ExecuteDataset(connectionString, CommandType.StoredProcedure, "[localBCP].[UspSERVSUBS]", 300, sqlParams.ToArray());
            }
            catch (Exception ex)
            {
                error = ex.Message;
                exception = ex;
            }

            return dsResult;
        }

        public DataSet ServiceExtract(DateTime? dtValue, int practiceId, out string error)
        {
            error = string.Empty;
            Exception exception = new Exception();

            return ServiceExtract(dtValue, practiceId, out error, out exception);
        }

        public DataSet ServiceExtract(DateTime? dtValue, int practiceId, out string error, out Exception exception)
        {
            DataSet dsResult = new DataSet();
            error = string.Empty;
            exception = new Exception();

            string connectionString = Convert.ToString(ConfigurationManager.ConnectionStrings["ConnBPI"].ConnectionString);

            try
            {
                List<SqlParameter> sqlParams = new List<SqlParameter>();

                if (dtValue != null)
                    sqlParams.Add(new SqlParameter("@date", dtValue));
                else
                    sqlParams.Add(new SqlParameter("@date", DateTime.Today.AddDays(-1)));

                sqlParams.Add(new SqlParameter("@pPracticeID", practiceId));

                dsResult = DALHelper.ExecuteDataset(connectionString, CommandType.StoredProcedure, "[localBCP].[UspServiceExtract]", 300, sqlParams.ToArray());
            }
            catch (Exception ex)
            {
                error = ex.Message;
                exception = ex;
            }

            return dsResult;
        }

        public DataSet LabResultExtract(DateTime? dtValue, int practiceId, out string error)
        {
            error = string.Empty;
            Exception exception = new Exception();

            return LabResultExtract(dtValue, practiceId, out error, out exception);
        }

        public DataSet LabResultExtract(DateTime? dtValue, int practiceId, out string error, out Exception exception)
        {
            DataSet dsResult = new DataSet();
            error = string.Empty;
            exception = new Exception();

            string connectionString = Convert.ToString(ConfigurationManager.ConnectionStrings["ConnBPI"].ConnectionString);

            try
            {
                List<SqlParameter> sqlParams = new List<SqlParameter>();

                if (dtValue != null)
                    sqlParams.Add(new SqlParameter("@date", dtValue));
                else
                    sqlParams.Add(new SqlParameter("@date", DateTime.Today.AddDays(-1)));

                sqlParams.Add(new SqlParameter("@pPracticeID", practiceId));

                dsResult = DALHelper.ExecuteDataset(connectionString, CommandType.StoredProcedure, "[localBCP].[UspInline]", 300, sqlParams.ToArray());
            }
            catch (Exception ex)
            {
                error = ex.Message;
                exception = ex;
            }

            return dsResult;
        }

        public DataSet UsDrugExtract(DateTime? dtValue, int practiceId, out string error)
        {
            error = string.Empty;
            Exception exception = new Exception();

            return UsDrugExtract(dtValue, practiceId, out error, out exception);
        }

        public DataSet UsDrugExtract(DateTime? dtValue, int practiceId, out string error, out Exception exception)
        {
            DataSet dsResult = new DataSet();
            error = string.Empty;
            exception = new Exception();

            string connectionString = Convert.ToString(ConfigurationManager.ConnectionStrings["ConnBPI"].ConnectionString);

            try
            {
                List<SqlParameter> sqlParams = new List<SqlParameter>();

                if (dtValue != null)
                    sqlParams.Add(new SqlParameter("@date", dtValue));
                else
                    sqlParams.Add(new SqlParameter("@date", DateTime.Today.AddDays(-1)));

                sqlParams.Add(new SqlParameter("@pPracticeID", practiceId));

                dsResult = DALHelper.ExecuteDataset(connectionString, CommandType.StoredProcedure, "[localBCP].[UsDrugExtract]", 300, sqlParams.ToArray());
            }
            catch (Exception ex)
            {
                error = ex.Message;
                exception = ex;
            }

            return dsResult;
        }

    }
}
