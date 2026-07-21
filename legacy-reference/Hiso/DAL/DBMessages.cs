using AWSDoc;
using Hiso.ConceptMapper;


using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Data.SqlClient;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;

namespace Hiso
{
    //Azam Khan
    public class DBMessages
    {
        private static readonly Dictionary<string, string> _mimeTypes = new Dictionary<string, string>()
        {
            { "DOC", "application/msword" },
            { "JPEG", "image/jpeg" },
            { "HTML", "text/html" },
            { "JPG", "image/jpeg" },
            { "BMP", "image/bmp" },
            { "PDF", "application/pdf" },
            { "PNG", "image/png" },
            { "GIF", "image/gif" },
            { "TIF", "image/tiff" },
            { "RTF", "application/rtf" },
            { "TXT", "text/plain" },
            { "DOCX", "application/msword" },
            { "XML", "application/xml" }
        };
        static DBMessages instance = null;
        string strQuery = string.Empty;

        static string strConnection;
        static string strConnectionSecondNode;

        public DBMessages()
        {
            strConnection = string.Empty;
        }

        public static DBMessages Instance
        {
            get
            {
                if (instance == null)
                    instance = new DBMessages();

                return instance;
            }
        }


        public static DataTable GetHisoConceptDetail()
        {
            try
            {
                CreateConnection();

                DataTable dtResults = new DataTable();

                dtResults = DALHelper.ExecuteDataTable(strConnection, CommandType.StoredProcedure, "[Hiso].[UspGetHisoConcepts]");

                return dtResults;
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }

        public static List<ProcedureResult> GetProcedureList(List<string> lstProcedure)
        {
            try
            {
                List<ProcedureResult> _LstProcedureResult = new List<ProcedureResult>();

                return _LstProcedureResult;
            }
            catch (Exception)
            {
                throw;
            }
        }

        public static DataSet ExecuteHisoProcedure(string ProcedureName, HealthLinkSession objSession, List<HisoRequest> objListHisoRequests)
        {
            Logger.Logging.Instance.WriteEventLog($"[ExecuteHisoProcedure] Started | Procedure: {ProcedureName}  | PracticeId: {objSession?.PracticeId}");

            DataSet dsResults = new DataSet();

            try
            {
                bool useSecondNode = false;

                // Decide which connection to use
                if (!string.IsNullOrEmpty(ProcedureName) && (
                    ProcedureName.Equals("hiso.uspgetpatient_laboratoryreport", StringComparison.OrdinalIgnoreCase) ||
                    ProcedureName.Equals("hiso.uspgetpatient_radiologyreport", StringComparison.OrdinalIgnoreCase) ||
                    ProcedureName.Equals("hiso.uspgetpatient_attachment", StringComparison.OrdinalIgnoreCase) ||
                    ProcedureName.Equals("hiso.uspgetpatient_incomingletter", StringComparison.OrdinalIgnoreCase) ||
                    ProcedureName.Equals("hiso.uspgetpatient_outgoingletter", StringComparison.OrdinalIgnoreCase) ||
                    ProcedureName.Equals("hiso.uspgetpatient_problem", StringComparison.OrdinalIgnoreCase)))
                {
                    Logger.Logging.Instance.WriteEventLog($"[ExecuteHisoProcedure] Creating SECOND NODE connection for procedure {ProcedureName}");
                    CreateConnectionSecondNode();
                    useSecondNode = true;
                }
                else
                {
                    Logger.Logging.Instance.WriteEventLog($"[ExecuteHisoProcedure] Creating PRIMARY connection for procedure {ProcedureName}");
                    CreateConnection();
                    useSecondNode = false;
                }

                List<DynamicParam> _ParamList = GetParamList(ProcedureName, objSession);
                Logger.Logging.Instance.WriteEventLog($"[ExecuteHisoProcedure] Retrieved {_ParamList?.Count ?? 0} parameters for procedure {ProcedureName}");

                List<SqlParameter> sqlParams = new List<SqlParameter>();
                string maxval = null, minVal = null, fromDate = null, toDate = null, search = null, sortBy = null, pcode = null, ReferenceID = string.Empty;
                int maximumRows = 50000, startRowIndex = 0;

                // 🔹 Extract HisoRequest values
                try
                {
                    var objListHisoRequest = objListHisoRequests?.FirstOrDefault(x => x.ProcedureName == ProcedureName);
                    List<HisoRequest> temp = objListHisoRequests?.Where(x => x.ProcedureName == ProcedureName).ToList();

                    if (objListHisoRequest != null)
                    {
                        Logger.Logging.Instance.WriteEventLog($"[ExecuteHisoProcedure] Found HisoRequest for procedure {ProcedureName}");

                        maxval = string.IsNullOrEmpty(objListHisoRequest.GroupmaxVal) ? objListHisoRequest.FieldmaxVal : objListHisoRequest.GroupmaxVal;
                        minVal = string.IsNullOrEmpty(objListHisoRequest.GroupminVal) ? objListHisoRequest.FieldminVal : objListHisoRequest.GroupminVal;
                        fromDate = string.IsNullOrEmpty(objListHisoRequest.GroupminDateTime) ? objListHisoRequest.FieldminDateTime : objListHisoRequest.GroupminDateTime;
                        toDate = string.IsNullOrEmpty(objListHisoRequest.GroupmaxDateTime) ? objListHisoRequest.FieldmaxDateTime : objListHisoRequest.GroupmaxDateTime;
                        startRowIndex = objListHisoRequest.GroupStartRowIndex;
                        maximumRows = objListHisoRequest.GroupMaximumRows == 0
                            ? Convert.ToInt16(string.IsNullOrEmpty(objListHisoRequest.FieldnumRows) ? "0" : objListHisoRequest.FieldnumRows)
                            : objListHisoRequest.GroupMaximumRows;
                        search = string.IsNullOrEmpty(objListHisoRequest.GroupsearchString) ? objListHisoRequest.FieldsearchString : objListHisoRequest.GroupsearchString;
                        sortBy = string.IsNullOrEmpty(objListHisoRequest.Grouporder) ? objListHisoRequest.Fieldorder : objListHisoRequest.Grouporder;
                        pcode = string.IsNullOrEmpty(objListHisoRequest.FieldQualifierID) ? null : objListHisoRequest.FieldQualifierID;

                        foreach (var item in temp)
                        {
                            if (!string.IsNullOrEmpty(item.GroupreferenceID))
                                ReferenceID += item.GroupreferenceID + ",";
                        }
                        if (ReferenceID.EndsWith(",")) ReferenceID = ReferenceID.TrimEnd(',');

                        Logger.Logging.Instance.WriteEventLog($"[ExecuteHisoProcedure] HisoRequest values extracted | FromDate={fromDate}, ToDate={toDate}, MinVal={minVal}, MaxVal={maxval}, Rows={maximumRows}, StartIndex={startRowIndex}, Search={search}, SortBy={sortBy}, PCode={pcode}, RefId={ReferenceID}");
                    }
                    else
                    {
                        Logger.Logging.Instance.WriteEventLog($"[ExecuteHisoProcedure] No HisoRequest found for {ProcedureName}");
                    }
                }
                catch (Exception ex)
                {
                    Logger.Logging.Instance.WriteExceptionLog(ex, $"[ExecuteHisoProcedure] Error extracting HisoRequest values for {ProcedureName}");
                }

                // 🔹 Map SQL parameters
                if (_ParamList != null && _ParamList.Count > 0)
                {
                    foreach (var x in _ParamList)
                    {
                        try
                        {
                            switch (x.Parameter_name.ToLower())
                            {
                                case "@fromdate": x.ParamValue = string.IsNullOrEmpty(fromDate) ? null : fromDate; break;
                                case "@todate": x.ParamValue = string.IsNullOrEmpty(toDate) ? null : toDate; break;
                                case "@startrowindex": x.ParamValue = startRowIndex.ToString(); break;
                                case "@maximumrows": x.ParamValue = maximumRows.ToString(); break;
                                case "@search": x.ParamValue = string.IsNullOrEmpty(search) ? null : search; break;
                                case "@sortby": x.ParamValue = string.IsNullOrEmpty(sortBy) ? null : sortBy; break;
                                case "@minvalue": x.ParamValue = string.IsNullOrEmpty(minVal) ? null : minVal; break;
                                case "@maxvalue": x.ParamValue = string.IsNullOrEmpty(maxval) ? null : maxval; break;
                                case "@pcode": x.ParamValue = string.IsNullOrEmpty(pcode) ? null : pcode; break;
                                case "@vcode": x.ParamValue = string.IsNullOrEmpty(pcode) ? null : pcode; break;
                                case "@referenceid": x.ParamValue = string.IsNullOrEmpty(ReferenceID) ? null : ReferenceID; break;
                            }

                            sqlParams.Add(new SqlParameter(x.Parameter_name, x.ParamValue));
                            Logger.Logging.Instance.WriteEventLog($"[ExecuteHisoProcedure] Param mapped | {x.Parameter_name}={x.ParamValue}");
                        }
                        catch (Exception ex)
                        {
                            Logger.Logging.Instance.WriteExceptionLog(ex, $"[ExecuteHisoProcedure] Error assigning parameter {x.Parameter_name}");
                        }
                    }
                }
                else
                {
                    Logger.Logging.Instance.WriteEventLog($"[ExecuteHisoProcedure] No parameters defined for {ProcedureName}");
                }

                // 🔹 AWS check
                bool IsAwsEnabled = AWSDoc.IndiciDMS.CheckAWSIsEnabled(objSession.PracticeId);
                Logger.Logging.Instance.WriteEventLog($"[ExecuteHisoProcedure] AWS enabled: {IsAwsEnabled} | PracticeId={objSession.PracticeId}");

                var awsEnabledSPs = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "Hiso.uspGetPatient_Attachment",
            "Hiso.uspGetPatient_IncomingLetter",
            "Hiso.uspGetPatient_OutgoingLetter",
            "Hiso.uspGetPatient_OutgoingLetter_Author",
            "Hiso.uspGetPatient_LaboratoryReport",
            "Hiso.uspGetPatient_RadiologyReport"
        };

                string connectionToUse = useSecondNode ? strConnectionSecondNode : strConnection;
                Logger.Logging.Instance.WriteEventLog($"[ExecuteHisoProcedure] Using connection: {(useSecondNode ? "SECOND NODE" : "PRIMARY")}");

                // 🔹 Execute SP
                if (IsAwsEnabled && awsEnabledSPs.Contains(ProcedureName))
                {
                    string AWSProcedureName = ProcedureName + "_AWS";
                    Logger.Logging.Instance.WriteEventLog($"[ExecuteHisoProcedure] Executing AWS flow | Procedure={AWSProcedureName} | RefId={ReferenceID}");
                    dsResults = ExecuteAWSFlow(ReferenceID, connectionToUse, AWSProcedureName, ProcedureName, sqlParams, objSession.PracticeId);
                }
                else
                {
                    Logger.Logging.Instance.WriteEventLog($"[ExecuteHisoProcedure] Executing DB procedure | Procedure={ProcedureName} | Params={sqlParams.Count}");
                    dsResults = sqlParams.Count > 0
                        ? DALHelper.ExecuteDataset(connectionToUse, CommandType.StoredProcedure, ProcedureName, sqlParams.ToArray())
                        : DALHelper.ExecuteDataset(connectionToUse, CommandType.StoredProcedure, ProcedureName);
                }

                Logger.Logging.Instance.WriteEventLog($"[ExecuteHisoProcedure] Procedure executed successfully | Procedure={ProcedureName}");
            }
            catch (Exception ex)
            {
                Logger.Logging.Instance.WriteExceptionLog(ex, $"[ExecuteHisoProcedure] Failed executing {ProcedureName}");
                dsResults = null;
            }

            Logger.Logging.Instance.WriteEventLog($"[ExecuteHisoProcedure] Completed | Procedure={ProcedureName} | Result={(dsResults == null ? "NULL" : "OK")}");
            return dsResults;
        }


        private static DataSet ExecuteAWSFlow(string referenceId,string connectionToUse, string AWSProcedureName, string ProcedureName,
            List<SqlParameter> sqlParams,int practiceid)
        {
            DataSet dsResults = null;

            try
            {
                Logger.Logging.Instance.WriteEventLog($"Get record against referenceid/dmsid : {referenceId}");

                // 🔹 Decide which connection to use (same logic as main method)
               

              

                // 🔹 Execute AWS SP
                dsResults = (sqlParams.Count > 0)
                    ? DALHelper.ExecuteDataset(connectionToUse, CommandType.StoredProcedure, AWSProcedureName, sqlParams.ToArray())
                    : DALHelper.ExecuteDataset(connectionToUse, CommandType.StoredProcedure, AWSProcedureName);

                if (dsResults != null && dsResults.Tables.Count > 0)
                {
                    DataTable dtResult = dsResults.Tables[0];
                    if (dtResult.Rows.Count > 0)
                    {
                        try
                        {
                            EnrichWithAWS(dtResult, practiceid, referenceId);
                        }
                        catch (Exception ex)
                        {
                            Logger.Logging.Instance.WriteExceptionLog(ex, "Error enriching dataset in AWS flow.");
                        }
                    }
                    return dsResults;
                }

                return null;
            }
            catch (Exception ex)
            {
                Logger.Logging.Instance.WriteExceptionLog(ex, "AWS flow failed. Falling back to normal procedure.");

                // 🔹 fallback also respects connection routing
                bool useSecondNode = ProcedureName != string.Empty &&
                    (ProcedureName.Equals("hiso.uspgetpatient_laboratoryreport", StringComparison.OrdinalIgnoreCase) ||
                     ProcedureName.Equals("hiso.uspgetpatient_radiologyreport", StringComparison.OrdinalIgnoreCase) ||
                     ProcedureName.Equals("hiso.uspgetpatient_attachment", StringComparison.OrdinalIgnoreCase) ||
                     ProcedureName.Equals("hiso.uspgetpatient_incomingletter", StringComparison.OrdinalIgnoreCase) ||
                     ProcedureName.Equals("hiso.uspgetpatient_outgoingletter", StringComparison.OrdinalIgnoreCase) ||
                     ProcedureName.Equals("hiso.uspgetpatient_problem", StringComparison.OrdinalIgnoreCase));

                string connectionToUse_v2 = useSecondNode ? strConnectionSecondNode : strConnection;

                dsResults = DALHelper.ExecuteDataset(connectionToUse_v2, CommandType.StoredProcedure, ProcedureName, sqlParams.ToArray());
                return dsResults;
            }
        }

        private static void EnrichWithAWS(DataTable dtResult, int practiceid, string referenceId)
        {
            string idColumn = dtResult.Columns
                .Cast<DataColumn>()
                .FirstOrDefault(c => c.ColumnName.EndsWith("_ID", StringComparison.OrdinalIgnoreCase))?.ColumnName;

            if (string.IsNullOrEmpty(idColumn))
                return;

            string prefix = idColumn.Replace("Patient_", "").Replace("_ID", "");
            string idCol = $"Patient_{prefix}_ID";
            string contentCol = $"Patient_{prefix}_Content";
            string nameCol = $"Patient_{prefix}_Name";
            string dataTypeCol = $"Patient_{prefix}_DataType";
            string locationCol = $"Patient_{prefix}_Location";
            string formatCol = $"Patient_{prefix}_Format";
            string filenameCol = $"Patient_{prefix}_Filename";
            string sizeCol = $"Patient_{prefix}_Size";

            // 🔹 Ensure required columns exist before enrichment            
            EnsureColumnExists(dtResult, contentCol, typeof(byte[]));
            EnsureColumnExists(dtResult, sizeCol, typeof(Int64));
            EnsureColumnExists(dtResult, filenameCol, typeof(string));
            EnsureColumnExists(dtResult, dataTypeCol, typeof(string));
            EnsureColumnExists(dtResult, nameCol, typeof(string));
            EnsureColumnExists(dtResult, locationCol, typeof(string));
            EnsureColumnExists(dtResult, formatCol, typeof(string));

            foreach (DataRow row in dtResult.Rows)
            {
                try
                {
                    if (row[idCol] == DBNull.Value) continue;

                    var docKey = row[idCol].ToString().ToUpperInvariant();
                    if (string.IsNullOrWhiteSpace(docKey)) continue;

                    Logger.Logging.Instance.WriteEventLog(
                        $"[EnrichWithAWS] Processing DocumentKey={docKey}, PracticeID={practiceid}");

                    if (!string.IsNullOrEmpty(referenceId))
                    {
                        var docResult = AWSDoc.IndiciDMS.DocumentGetByDocumentKeyJsonResult(docKey, practiceid);
                        if (string.IsNullOrEmpty(docResult))
                        {
                            Logger.Logging.Instance.WriteEventLog($"[EnrichWithAWS] No JSON result for DocumentKey={docKey}");
                            continue;
                        }
                        var objDocumentFile = JsonConvert.DeserializeObject<DocumentFile>(docResult);
                        if (objDocumentFile == null ||
                            objDocumentFile.DocumentName == null ||
                            objDocumentFile.DocumentData == null ||
                            objDocumentFile.DocumentData.Length <= 0)
                        {
                            Logger.Logging.Instance.WriteEventLog($"[EnrichWithAWS] Invalid DocumentFile for DocumentKey={docKey}");
                            continue;
                        }

                        // 🔹 Assign values with logging
                        row[contentCol] = objDocumentFile.DocumentData;
                        row[sizeCol] = objDocumentFile.DocumentData.Length;
                        row[filenameCol] = objDocumentFile.DocumentName;
                        Logger.Logging.Instance.WriteEventLog(
                      $"[EnrichWithAWS] DocumentKey={docKey} | Filename={objDocumentFile.DocumentName} | Size={objDocumentFile.DocumentData.Length}");
                    }
                    

                    var docResultmimeTypes = IndiciDMS.GetDocumentStatusFromIndici(docKey, practiceid);
                    if (docResultmimeTypes == null)
                    {
                        Logger.Logging.Instance.WriteEventLog($"[EnrichWithAWS] No MIME type result for DocumentKey={docKey}");
                        continue;
                    }
                    if (string.IsNullOrEmpty(referenceId) || string.IsNullOrWhiteSpace(referenceId))
                    {
                        row[filenameCol] = docResultmimeTypes.DocumentName;
                        row[sizeCol] = docResultmimeTypes.DocumentSize;
                        Logger.Logging.Instance.WriteEventLog(
                       $"[EnrichWithAWS] DocumentName={docResultmimeTypes.DocumentName}");
                    }




                    if (docResultmimeTypes.DocumentType == null) continue;

                    var docTypeKey = docResultmimeTypes.DocumentType.ToUpperInvariant();

                    var resolvedMime = _mimeTypes.TryGetValue(docTypeKey, out var contentType) ? contentType : null;

                    row[dataTypeCol] = resolvedMime;
                    row[nameCol] = (row[nameCol] == DBNull.Value ? "" : row[nameCol].ToString()) + docTypeKey;
                    row[locationCol] = (row[locationCol] == DBNull.Value ? "" : row[locationCol].ToString()) + docTypeKey;
                    row[formatCol] = docTypeKey;

                    Logger.Logging.Instance.WriteEventLog(
                        $"[EnrichWithAWS] DocumentKey={docKey} | MimeType={resolvedMime} | Format={docTypeKey}");
                }
                catch (Exception ex)
                {
                    Logger.Logging.Instance.WriteExceptionLog(ex,
                        "[EnrichWithAWS] Error enriching a single row from AWS.");
                }
            }
        }


        private static void EnsureColumnExists(DataTable table, string columnName, Type expectedType)
        {
            Logger.Logging.Instance.WriteEventLog($"[EnsureColumnExists] Checking column {columnName} in DataTable.");

            // Determine if this is a patient content-related column
            bool isPatientContent = columnName.StartsWith("Patient_", StringComparison.OrdinalIgnoreCase)
                                    && columnName.EndsWith("_Content", StringComparison.OrdinalIgnoreCase);

            // ✅ Handle all Patient_* columns with strict type validation
            if (isPatientContent || columnName.StartsWith("Patient_", StringComparison.OrdinalIgnoreCase))
            {
                if (table.Columns.Contains(columnName))
                {
                    var currentType = table.Columns[columnName].DataType;

                    if (currentType != expectedType)
                    {
                        Logger.Logging.Instance.WriteEventLog(
                            $"[EnsureColumnExists] Column {columnName} exists but type={currentType}, expected={expectedType}. Rebuilding...");

                        // Backup values
                        var tempValues = table.AsEnumerable()
                            .Select(r => r[columnName])
                            .ToList();

                        // Remove and recreate column
                        table.Columns.Remove(columnName);
                        table.Columns.Add(columnName, expectedType);

                        // Attempt to restore compatible values
                        for (int i = 0; i < table.Rows.Count; i++)
                        {
                            var val = tempValues[i];
                            if (val == DBNull.Value || val == null)
                            {
                                table.Rows[i][columnName] = DBNull.Value;
                                continue;
                            }

                            try
                            {
                                if (expectedType == typeof(byte[]))
                                {
                                    if (val is byte[] bytes)
                                        table.Rows[i][columnName] = bytes;
                                    else if (val is string s)
                                        table.Rows[i][columnName] = Convert.FromBase64String(s);
                                }
                                else if (expectedType == typeof(string))
                                {
                                    table.Rows[i][columnName] = val.ToString();
                                }
                                else if (expectedType == typeof(int))
                                {
                                    table.Rows[i][columnName] = Convert.ToInt32(val);
                                }
                                else
                                {
                                    table.Rows[i][columnName] = DBNull.Value;
                                }
                            }
                            catch
                            {
                                table.Rows[i][columnName] = DBNull.Value;
                            }
                        }

                        Logger.Logging.Instance.WriteEventLog($"[EnsureColumnExists] Column {columnName} recreated as {expectedType.Name}.");
                    }
                    else
                    {
                        Logger.Logging.Instance.WriteEventLog($"[EnsureColumnExists] Column {columnName} already correct ({expectedType.Name}).");
                    }
                }
                else
                {
                    table.Columns.Add(columnName, expectedType);
                    Logger.Logging.Instance.WriteEventLog($"[EnsureColumnExists] Column {columnName} added dynamically as {expectedType.Name}.");
                }
            }
            else
            {
                // Non-patient columns
                if (!table.Columns.Contains(columnName))
                {
                    table.Columns.Add(columnName, expectedType);
                    Logger.Logging.Instance.WriteEventLog($"[EnsureColumnExists] Column {columnName} added as {expectedType.Name}.");
                }
            }
        }




        public static List<DynamicParam> GetParamList(string ProcedureName, HealthLinkSession objSession)
        {
            try
            {
                CreateConnection();

                DataTable dtResults = new DataTable();
                List<DynamicParam> _ParamList = new List<DynamicParam>();
                List<SqlParameter> sqlParams = new List<SqlParameter>();
                sqlParams.Add(new SqlParameter("@pProcedureName", ProcedureName));

                dtResults = DALHelper.ExecuteDataTable(strConnection, CommandType.StoredProcedure, "[Hiso].[USPGetProcedureParamList]", sqlParams.ToArray());
                if (dtResults.Rows.Count > 0)
                {
                    _ParamList = Utitlity.ToGenList<DynamicParam>(dtResults);

                    // Map param list with Hiso Session Keys
                    MapParamList(_ParamList, objSession);
                }
                return _ParamList;
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }

        public static void MapParamList(List<DynamicParam> lstParam, HealthLinkSession objSession)
        {
            try
            {
                Logger.Logging.Instance.WriteEventLog("MapParamList" );
                lstParam.ForEach(x =>
                {
                    switch (x.Parameter_name.ToLower())
                    {
                        case "@patientid":
                        case "@ppatientid":
                            x.ParamValue = objSession.PatientId.ToString();
                            break;
                        case "@loggedinproviderId":
                        case "@providerid":
                        case "@pproviderid":
                            x.ParamValue = objSession.ProviderId.ToString();
                            break;

                        case "@appointmentid":
                        case "@pappointmentid":
                            x.ParamValue = objSession.AppointmentId.ToString();
                            break;

                        case "@practiceid":
                        case "@ppracticeid":
                            x.ParamValue = objSession.PracticeId.ToString();
                            break;

                        case "@acc45id":
                        case "@pacc45id":
                            x.ParamValue = objSession.ReferenceId.ToString();
                            break;

                        case "@ppracticelocationid": // Change to get location based address and HPI
                            if (objSession.PracticeLocationID != null)
                            {
                                x.ParamValue = objSession.PracticeLocationID.ToString();
                            }
                            else
                            {
                                x.ParamValue = null; 
                            }
                            
                            break;

                       

                            //case "@referenceID":
                            //    x.ParamValue = objSession.ReferenceId.ToString();
                            //    break;
                    }
                }
                );

            }
            catch (Exception ex)
            {
                throw ex;
            }
        }


        public static void CreateConnection()
        {
            try
            {
                strConnection = System.Configuration.ConfigurationManager.ConnectionStrings["ConectionStringPMS_NZ"].ToString();
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }
        public static void CreateConnectionSecondNode()
        {
            try
            {
                strConnectionSecondNode = System.Configuration.ConfigurationManager.ConnectionStrings["ConectionStringPMS_NZ_SecondNode"].ToString();
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }
        
    }
    public class DocumentFile
    {
        [DataMember]
        public int DocumentID { get; set; }
        [DataMember]
        public string DocumentName { get; set; }
        [DataMember]
        public string DocumentType { get; set; }
        [DataMember]
        public string Description { get; set; }
        [DataMember]
        public byte[] DocumentData { get; set; }
        [DataMember]
        public string Base64String { get; set; }
        [DataMember]
        public int Counter { get; set; }
        [DataMember]
        public string AppendGuid { get; set; }
        [DataMember]
        public string FilePath { get; set; }
        [DataMember]
        public bool IsAWS { get; set; }
        [DataMember]
        public string AWSTransactionID { get; set; }

        [DataMember]
        public string DocumentKey { get; set; }

        [DataMember]
        public string AWSUrl { get; set; }

        [DataMember]
        public string DMSAPIPublicKey { get; set; }
        [DataMember]
        public string DMSAPIPrivateKey { get; set; }
        [DataMember]
        public int PracticeID { get; set; }

    }
}
