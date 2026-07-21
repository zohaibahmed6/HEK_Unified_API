using DAL;
using DAL.Pegasus;
using ERMSWebAPI.Pegasus_Models;
using Logger;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using System.Web.Http;
using System.Web.Http.Cors;
using static ERMSWebAPI.Models.APIModels;

namespace ERMSWebAPI.Controllers
{
    [EnableCors(origins: "*", headers: "*", methods: "*")]
    public class COLController : ApiController
    {
        [AcceptVerbs("POST")]
        public async Task<HttpResponseMessage> Authenticate()
        {
            string error = string.Empty;
            string result = string.Empty;
            string practiceid = string.Empty;
            DataTable dtResults = new DataTable();
            PegasusAPIModel.UserAuthenticate objAuth = new PegasusAPIModel.UserAuthenticate();
            try
            {
                result = await Request.Content.ReadAsStringAsync();
                Credential objCredentials = JsonConvert.DeserializeObject<Credential>(result);
                double expiryInDays = Utility.Instance.ToDouble(ConfigurationManager.AppSettings["ExpiryInDays"]);

                if (!string.IsNullOrEmpty(objCredentials.EncounterId) && objCredentials.EncounterId.Contains("_"))
                {
                    string Delimiter = "__";
                    string[] splitEncounter = objCredentials.EncounterId.Split(new[] { Delimiter }, StringSplitOptions.None);
                    if (splitEncounter.Length == 1) { splitEncounter = objCredentials.EncounterId.Split('_'); }
                    if (splitEncounter.Length > 0)
                    {
                        objCredentials.EncounterId = GetDcrptValue(splitEncounter[0]);
                        practiceid = "_" + splitEncounter[1];
                        if (splitEncounter.Length > 2)
                        {
                            practiceid = "_" + splitEncounter[2];
                        }
                    }
                    else
                    {
                        objCredentials.EncounterId = GetDcrptValue(objCredentials.EncounterId);
                    }
                }
                objCredentials.PatientId = GetDcrptValue(objCredentials.PatientId);
                WriteLog("In Authenticate: PatientId/EncounterId/Username >> " + objCredentials.PatientId + "/" + objCredentials.EncounterId + "/" + objCredentials.Username);
                dtResults = HSSDA.InsertAndValidateToken(objCredentials.Username, objCredentials.Password,
                                                         objCredentials.PatientId, objCredentials.EncounterId, practiceid,
                                                         string.Empty, expiryInDays, "", out error);

                if (dtResults.Rows.Count > 0
                    && string.IsNullOrWhiteSpace(Utility.Instance.ToString(dtResults.Rows[0]["StatusMessage"])))
                {
                    DateTime dtExpiry = DateTime.MinValue;
                    DateTime.TryParse(Utility.Instance.ToString(dtResults.Rows[0]["Expiry"]), out dtExpiry);

                    if (!dtExpiry.Equals(DateTime.MinValue))
                    {
                        objAuth.Token = Utility.Instance.ToString(dtResults.Rows[0]["Token"]);
                        objAuth.Expiry = dtExpiry.ToString("yyyy-MM-ddTHH:mm:ssz");
                        objAuth.PracticeId = Utility.Instance.ToString(dtResults.Rows[0]["PracticeId"]);
                    }
                    else
                        error = "Invalid credentials!";
                }
                else
                    error = "Authentication failed!";
            }
            catch (Exception ex)
            {
                WriteLog("Authenticate: " + result, ex);
                error = ex.Message;
            }
            objAuth.error = error;
            string res = JsonConvert.SerializeObject(objAuth);
            WriteLog("In Authenticate: ResponseResult : " + res);
            var response = new HttpResponseMessage(HttpStatusCode.OK);
            response.Content = new StringContent(res, Encoding.UTF8, "application/json");

            return response;
        }

        [AcceptVerbs("GET")]
        public HttpResponseMessage GetCurrentPatientData(string pmsPatientId, string pmsEncounterId)
        {
            string error = string.Empty;
            string result = string.Empty;
            string practiceid = string.Empty;
            string authentication = GetAuthorizationToken();

            try
            {
                if (!string.IsNullOrEmpty(pmsEncounterId) && pmsEncounterId.Contains("_"))
                {
                    string Delimiter = "__";
                    string[] splitEncounter = pmsEncounterId.Split(new[] { Delimiter }, StringSplitOptions.None);
                    if (splitEncounter.Length == 1) { splitEncounter = pmsEncounterId.Split('_'); }
                    if (splitEncounter.Length > 0)
                    {
                        pmsEncounterId = GetDcrptValue(splitEncounter[0]);
                        practiceid = "_" + splitEncounter[1];
                        if (splitEncounter.Length > 2)
                        {
                            practiceid = "_" + splitEncounter[2];
                        }
                    }
                }
                pmsPatientId = GetDcrptValue(pmsPatientId);
                WriteLog("In GetCurrentPatientData: PatientId: " + pmsPatientId + " ---- EncounterId: " + pmsEncounterId);

                if (HSSDA.InsertAndValidateToken(pmsPatientId, pmsEncounterId, practiceid, authentication, out error))
                {
                    List<PegasusAPIModel.PatientData> objPatientData = new List<PegasusAPIModel.PatientData>();
                    objPatientData = Utility.instance.DataTableToList<PegasusAPIModel.PatientData>(PHCO.GetCurrentPatientData(pmsPatientId, practiceid, out error));
                    if (objPatientData.Count > 0)
                        result = JsonConvert.SerializeObject(objPatientData);
                    else
                        result = JsonConvert.SerializeObject(new PegasusAPIModel.PatientData());

                }
                else if (string.IsNullOrWhiteSpace(error))
                    error = "Invalid token value!";
            }
            catch (Exception ex)
            {
                WriteLog("GetCurrentPatientData: ", ex);
                error = ex.Message;
            }
            if (!string.IsNullOrEmpty(error))
                result = error;
            var response = new HttpResponseMessage(HttpStatusCode.OK);
            response.Content = new StringContent(result, Encoding.UTF8, "application/json");
            return response;
        }
        [AcceptVerbs("GET")]
        public HttpResponseMessage GetSessionData(string pmsPatientId, string pmsEncounterId)
        {
            string error = string.Empty;
            string result = string.Empty;
            string practiceid = string.Empty;
            string authentication = GetAuthorizationToken();

            try
            {
                if (!string.IsNullOrEmpty(pmsEncounterId) && pmsEncounterId.Contains("_"))
                {
                    string Delimiter = "__";
                    string[] splitEncounter = pmsEncounterId.Split(new[] { Delimiter }, StringSplitOptions.None);
                    if (splitEncounter.Length == 1) { splitEncounter = pmsEncounterId.Split('_'); }
                    if (splitEncounter.Length > 0)
                    {
                        pmsEncounterId = GetDcrptValue(splitEncounter[0]);
                        practiceid = "_" + splitEncounter[1];
                        if (splitEncounter.Length > 2)
                        {
                            practiceid = "_" + splitEncounter[2];
                        }
                    }
                }
                pmsPatientId = GetDcrptValue(pmsPatientId);
                WriteLog("In GetSessionData: PatientId: " + pmsPatientId + " ---- EncounterId: " + pmsEncounterId);

                if (HSSDA.InsertAndValidateToken(pmsPatientId, pmsEncounterId, practiceid, authentication, out error))
                {

                    List<PegasusAPIModel.SessionData> objSessionData = new List<PegasusAPIModel.SessionData>();
                    objSessionData = Utility.instance.DataTableToList<PegasusAPIModel.SessionData>(PHCO.GetSessionData(pmsPatientId, pmsEncounterId, practiceid, out error));
                    if (objSessionData.Count > 0)
                        result = JsonConvert.SerializeObject(objSessionData);
                    else
                        result = JsonConvert.SerializeObject(new PegasusAPIModel.SessionData());

                }
                else if (string.IsNullOrWhiteSpace(error))
                    error = "Invalid token value!";
            }
            catch (Exception ex)
            {
                WriteLog("GetSessionData: ", ex);
                error = ex.Message;
            }
            if (!string.IsNullOrEmpty(error))
                result = error;
            var response = new HttpResponseMessage(HttpStatusCode.OK);
            response.Content = new StringContent(result, Encoding.UTF8, "application/json");
            return response;
        }
        [AcceptVerbs("GET")]
        public HttpResponseMessage GetProviderData(string pmsPatientId, string pmsEncounterId)
        {
            string error = string.Empty;
            string result = string.Empty;
            string practiceid = string.Empty;
            string authentication = GetAuthorizationToken();

            try
            {
                if (!string.IsNullOrEmpty(pmsEncounterId) && pmsEncounterId.Contains("_"))
                {
                    string Delimiter = "__";
                    string[] splitEncounter = pmsEncounterId.Split(new[] { Delimiter }, StringSplitOptions.None);
                    if (splitEncounter.Length == 1) { splitEncounter = pmsEncounterId.Split('_'); }
                    if (splitEncounter.Length > 0)
                    {
                        pmsEncounterId = GetDcrptValue(splitEncounter[0]);
                        practiceid = "_" + splitEncounter[1];
                        if (splitEncounter.Length > 2)
                        {
                            practiceid = "_" + splitEncounter[2];
                        }
                    }
                }
                pmsPatientId = GetDcrptValue(pmsPatientId);
                WriteLog("In GetProviderData: PatientId: " + pmsPatientId + " ---- EncounterId: " + pmsEncounterId);

                if (HSSDA.InsertAndValidateToken(pmsPatientId, pmsEncounterId, practiceid, authentication, out error))
                {
                    List<PegasusAPIModel.ProviderData> objProviderData = new List<PegasusAPIModel.ProviderData>();
                    objProviderData = Utility.instance.DataTableToList<PegasusAPIModel.ProviderData>(PHCO.GetProviderData(pmsPatientId, practiceid, out error));
                    if (objProviderData.Count > 0)
                        result = JsonConvert.SerializeObject(objProviderData);
                    else
                        result = JsonConvert.SerializeObject(new PegasusAPIModel.ProviderData());
                }
                else if (string.IsNullOrWhiteSpace(error))
                    error = "Invalid token value!";
            }
            catch (Exception ex)
            {
                WriteLog("GetProviderData: ", ex);
                error = ex.Message;
            }
            if (!string.IsNullOrEmpty(error))
                result = error;
            var response = new HttpResponseMessage(HttpStatusCode.OK);
            response.Content = new StringContent(result, Encoding.UTF8, "application/json");
            return response;
        }
        [AcceptVerbs("GET")]
        public HttpResponseMessage GetSurgeryData(string pmsPatientId, string pmsEncounterId, string LocationId = "")
        {
            string error = string.Empty;
            string result = string.Empty;
            string practiceid = string.Empty;
            string authentication = GetAuthorizationToken();

            try
            {
                if (!string.IsNullOrEmpty(pmsEncounterId) && pmsEncounterId.Contains("_"))
                {
                    string Delimiter = "__";
                    string[] splitEncounter = pmsEncounterId.Split(new[] { Delimiter }, StringSplitOptions.None);
                    if (splitEncounter.Length == 1) { splitEncounter = pmsEncounterId.Split('_'); }
                    if (splitEncounter.Length > 0)
                    {
                        pmsEncounterId = GetDcrptValue(splitEncounter[0]);
                        practiceid = "_" + splitEncounter[1];
                        if (splitEncounter.Length > 2)
                        {
                            practiceid = "_" + splitEncounter[2];
                        }
                    }
                }
                pmsPatientId = GetDcrptValue(pmsPatientId);
                WriteLog("In GetSurgeryData: PatientId: " + pmsPatientId + " ---- EncounterId" + pmsEncounterId + " ---- Practiceid" + practiceid + " ---- LocationId: " + LocationId);

                if (HSSDA.InsertAndValidateToken(pmsPatientId, pmsEncounterId, practiceid, authentication, out error))
                {
                    List<PegasusAPIModel.SurgeryData> objSurgeryData = new List<PegasusAPIModel.SurgeryData>();
                    objSurgeryData = Utility.instance.DataTableToList<PegasusAPIModel.SurgeryData>(PHCO.GetSurgeryData(pmsPatientId, LocationId, pmsEncounterId, practiceid, out error));
                    if (objSurgeryData.Count > 0)
                        result = JsonConvert.SerializeObject(objSurgeryData);
                    else
                        result = JsonConvert.SerializeObject(new PegasusAPIModel.SurgeryData());
                }
                else if (string.IsNullOrWhiteSpace(error))
                    error = "Invalid token value!";
            }
            catch (Exception ex)
            {
                WriteLog("GetSurgeryData: ", ex);
                error = ex.Message;
            }
            if (!string.IsNullOrEmpty(error))
                result = error;
            var response = new HttpResponseMessage(HttpStatusCode.OK);
            response.Content = new StringContent(result, Encoding.UTF8, "application/json");
            return response;
        }

        [AcceptVerbs("GET")]
        public HttpResponseMessage GetDiagnosisData(string pmsPatientId, string pmsEncounterId, string pmsOrder = "desc", string pmsMinDateTime = "", string pmsMaxDateTime = "")
        {
            string error = string.Empty;
            string result = string.Empty;
            string authentication = GetAuthorizationToken();
            DateTime dtMin = DateTime.MinValue;
            DateTime dtMax = DateTime.MinValue;
            string practiceid = string.Empty;

            DateTime.TryParse(pmsMinDateTime, out dtMin);
            DateTime.TryParse(pmsMaxDateTime, out dtMax);

            try
            {
                if (!string.IsNullOrEmpty(pmsEncounterId) && pmsEncounterId.Contains("_"))
                {
                    string Delimiter = "__";
                    string[] splitEncounter = pmsEncounterId.Split(new[] { Delimiter }, StringSplitOptions.None);
                    if (splitEncounter.Length == 1) { splitEncounter = pmsEncounterId.Split('_'); }
                    if (splitEncounter.Length > 0)
                    {
                        pmsEncounterId = GetDcrptValue(splitEncounter[0]);
                        practiceid = "_" + splitEncounter[1];
                        if (splitEncounter.Length > 2)
                        {
                            practiceid = "_" + splitEncounter[2];
                        }
                    }
                }
                pmsPatientId = GetDcrptValue(pmsPatientId);
                WriteLog("In GetDiagnosisData: PatientId: " + pmsPatientId + " ---- EncounterId: " + pmsEncounterId);

                if (HSSDA.InsertAndValidateToken(pmsPatientId, pmsEncounterId, practiceid, authentication, out error))
                {
                    List<PegasusAPIModel.DiagnosisData> objDiagnosisData = new List<PegasusAPIModel.DiagnosisData>();
                    objDiagnosisData = Utility.instance.DataTableToList<PegasusAPIModel.DiagnosisData>(PHCO.GetDiagnosisData(pmsPatientId, pmsOrder, dtMin, dtMax, practiceid, out error));
                    if (objDiagnosisData.Count > 0)
                        result = JsonConvert.SerializeObject(objDiagnosisData);
                    else
                        result = JsonConvert.SerializeObject(new PegasusAPIModel.DiagnosisData());
                }
                else if (string.IsNullOrWhiteSpace(error))
                    error = "Invalid token value!";
            }
            catch (Exception ex)
            {
                WriteLog("GetDiagnosisData: ", ex);
                error = ex.Message;
            }
            if (!string.IsNullOrEmpty(error))
                result = error;
            var response = new HttpResponseMessage(HttpStatusCode.OK);
            response.Content = new StringContent(result, Encoding.UTF8, "application/json");
            return response;
        }

        [AcceptVerbs("POST")]
        public async Task<HttpResponseMessage> SaveInvoice()
        {
            string result = string.Empty;
            string error = string.Empty;
            string authentication = GetAuthorizationToken();
            Int64 serviceMappingId = -1;
            string practiceid = string.Empty;
            WriteLog("In SaveInvoice");
            try
            {
                result = await Request.Content.ReadAsStringAsync();
                WriteLog("Result : " + result);
                SaveInvoice objInvoice = JsonConvert.DeserializeObject<SaveInvoice>(result);
                if (!string.IsNullOrEmpty(objInvoice.EncounterID) && objInvoice.EncounterID.Contains("_"))
                {
                    string Delimiter = "__";
                    string[] splitEncounter = objInvoice.EncounterID.Split(new[] { Delimiter }, StringSplitOptions.None);
                    if (splitEncounter.Length == 1) { splitEncounter = objInvoice.EncounterID.Split('_'); }
                    if (splitEncounter.Length > 0)
                    {
                        objInvoice.EncounterID = GetDcrptValue(splitEncounter[0]);
                        practiceid = "_" + splitEncounter[1];
                        if (splitEncounter.Length > 2)
                        {
                            practiceid = "_" + splitEncounter[2];
                        }
                    }
                }
                objInvoice.PatientID = GetDcrptValue(objInvoice.PatientID);
                WriteLog("DEBUG:  ClaimShortCode : " + objInvoice.ClaimShortCode);
                WriteLog("DEBUG: SaveInvoice: PatientID:" + objInvoice.PatientID + "/EncounterId:" + objInvoice.EncounterID + "/ServiceCode:" + objInvoice.ServiceCode +
                    "/ServiceName:" + objInvoice.ServiceName + "/Description:" + objInvoice.Description + "/AccountHolderID:" + objInvoice.AccountHolderID + "/Payee:" + objInvoice.Payee +
                    "/AmountInclGST:" + objInvoice.AmountInclGST + "/ServiceProvider:" + objInvoice.ServiceProvider + "/ServiceProviderType:" + objInvoice.ServiceProviderType + "/ServiceDate:" + objInvoice.ServiceDate +
                    "/PegasusReference:" + objInvoice.PegasusReference + "/Userid" + objInvoice.Userid + "/ClaimShortCode : " + objInvoice.ClaimShortCode);

                if (HSSDA.InsertAndValidateToken(objInvoice.PatientID, objInvoice.EncounterID, practiceid, authentication, out error))
                {
                    WriteLog("After token Validation: SaveInvoice: PatientID:" + objInvoice.PatientID + "/EncounterId:" + objInvoice.EncounterID);
                   result = "{\"status\":\"success\",\"message\":\"" + Utility.Instance.RandomDigits(6) + "\"}";
                    serviceMappingId = HSSDA.InsertUpdateService(objInvoice.PatientID, objInvoice.AccountHolderID, objInvoice.EncounterID, "COL", objInvoice.ServiceName, objInvoice.ServiceCode, objInvoice.AmountInclGST, "",
                        objInvoice.Description, objInvoice.Payee, objInvoice.ServiceProvider, objInvoice.ServiceProviderType, objInvoice.ServiceDate, objInvoice.PegasusReference
                        , practiceid, objInvoice.ClaimShortCode, out error);
                    WriteLog("After InsertUpdateService serviceMappingId : " + serviceMappingId);
                    if (serviceMappingId == -3)
                        result = "{\"status\":\"success\",\"message\":\" Invoice Already exist.}";
                    else if (serviceMappingId > 0)
                        result = "{\"status\":\"success\",\"message\":\"" + serviceMappingId + "\"}";
                    else if (string.IsNullOrWhiteSpace(error))
                        error = "Invalid values passed!";
                }
                else if (string.IsNullOrEmpty(error))
                    error = "Invalid token value!";
            }
            catch (Exception ex)
            {
                WriteLog("SaveInvoice: ", ex);
                error = ex.Message;
            }
            if (!string.IsNullOrEmpty(error))
                result = error;
            var response = new HttpResponseMessage(HttpStatusCode.OK);
            response.Content = new StringContent(result, Encoding.UTF8, "application/json");
            return response;
        }
        private string GetAuthorizationToken()
        {
            string authentication = string.Empty;

            try
            {
                authentication = Utility.Instance.ToString(Request.Headers.Authorization).Replace("Bearer", string.Empty).Trim();
            }
            catch { }

            return authentication;
        }
        private void WriteLog(string message, Exception ex = null)
        {
            if (ex == null)
                Logging.Instance.WriteEventLog(message, TypeEnums.LogType.Default);
            else
                Logging.Instance.WriteExceptionLog(message, ex);
        }
        public string GetDcrptValue(string value)
        {
            try
            {
                int res = 0;
                if (int.TryParse(value, out res))
                {
                    return value;
                }
                else
                {
                    return ERMSWebAPI.Models.EncryptionManager.GetDecryptString(value);
                }
            }
            catch (Exception ex)
            {
                return "";
                throw ex;
            }
        }
    }

    public class SaveInvoice
    {
        public string ServiceCode { get; set; }
        public string ServiceName { get; set; }
        public string AmountInclGST { get; set; }
        public string Description { get; set; }
        public string AccountHolderID { get; set; }
        public string Payee { get; set; }

        public string ServiceProvider { get; set; }
        public string ServiceProviderType { get; set; }
        public string ServiceDate { get; set; }
        public string PegasusReference { get; set; }
        public string EncounterID { get; set; }
        public string PatientID { get; set; }
        public string Userid { get; set; }
        public string ClaimShortCode { get; set; }
    }
}