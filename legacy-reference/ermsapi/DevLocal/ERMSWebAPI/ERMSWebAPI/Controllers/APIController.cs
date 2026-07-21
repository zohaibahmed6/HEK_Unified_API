using DAL;
using ERMSWebAPI.Helpers;
using Logger;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using System.Web.Http;
using System.Web.Http.Cors;
using System.Xml;
using System.Xml.Serialization;
using static ERMSWebAPI.Models.APIModels;

namespace ERMSWebAPI.Controllers
{
    //[EnableCors(origins: "*", headers: "*", methods: "*")]
    public class APIController : ApiController
    {
        [AcceptVerbs("GET")]
        public HttpResponseMessage Ping()
        {
            string result = "<?xml version=\"1.0\" encoding=\"utf-16\"?>" +
                            "<Ping>Success!</Ping>";

            return SetToXml(result, string.Empty);
        }
        [AcceptVerbs("POST")]
        public async Task<HttpResponseMessage> Authenticate()
        {
            string error = string.Empty;
            string result = string.Empty;
            string practiceid = string.Empty;
            DataTable dtResults = new DataTable();
            string pho = string.Empty;

            try
            {                
                result = await Request.Content.ReadAsStringAsync();
                Credential objCredentials = new Credential();
                XmlSerializer xmlSerialize = new XmlSerializer(typeof(Credential));
                double expiryInDays = Utility.Instance.ToDouble(ConfigurationManager.AppSettings["ExpiryInDays"]);

                using (XmlReader xmlReader = XmlReader.Create(new StringReader(result)))
                {
                    objCredentials = (Credential)xmlSerialize.Deserialize(xmlReader);
                }

                objCredentials.EncounterId = GetBase64Value(objCredentials.EncounterId);
                objCredentials.PatientId = GetBase64Value(objCredentials.PatientId);
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
                            practiceid += "_" + splitEncounter[2];
                        }
                        if (splitEncounter.Length > 3)
                        {
                            practiceid = "_" + splitEncounter[3];
                            pho = splitEncounter[3];
                        }
                    }
                    else
                    {
                        objCredentials.EncounterId = GetDcrptValue(objCredentials.EncounterId);
                    }
                }

                if (ConfigurationManager.AppSettings["EnableAzureERMSAPI"] == "1" && !string.IsNullOrEmpty(pho) && pho.ToLower().Contains("azure"))
                    return await ERMSAPIProxy.ForwardtoERMSAzureAPIPOST(Request, "/api/Authenticate");
                
                objCredentials.PatientId = GetDcrptValue(objCredentials.PatientId);


                WriteLog("In Authenticate: PatientId/EncounterId/Username/Practiceid >> " + objCredentials.PatientId + "/" + objCredentials.EncounterId + "/" + objCredentials.Username + "/" + practiceid);
                dtResults = HSSDA.InsertAndValidateToken(objCredentials.Username, objCredentials.Password,
                                                         objCredentials.PatientId, objCredentials.EncounterId, practiceid,
                                                         string.Empty, expiryInDays, "", out error);
                WriteLog("In Authenticate: dtResults >> " + dtResults);
                WriteLog("In Authenticate: error >> " + error);
                if (dtResults.Rows.Count > 0
                    && string.IsNullOrWhiteSpace(Utility.Instance.ToString(dtResults.Rows[0]["StatusMessage"])))
                {
                    DateTime dtExpiry = DateTime.MinValue;
                    DateTime.TryParse(Utility.Instance.ToString(dtResults.Rows[0]["Expiry"]), out dtExpiry);

                    if (!dtExpiry.Equals(DateTime.MinValue))
                    {
                        result = "<?xml version=\"1.0\" encoding=\"utf-16\"?>" +
                                 "<Authentication>" +
                                 "<Token>" + Utility.Instance.ToString(dtResults.Rows[0]["Token"]) + "</Token>" +
                                 //"<Expiry>" + String.Format("yyyy-mm-ddTHH:mm:ss+|-HH:mm", dtExpiry.ToUniversalTime()) + "</Expiry>" +
                                 "<Expiry>" + dtExpiry.ToString("yyyy-MM-ddTHH:mm:ssz") + "</Expiry>" +
                                 "<PracticeId>" + Utility.Instance.ToString(dtResults.Rows[0]["PracticeId"]) + "</PracticeId>" +
                                 "</Authentication>";
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
            WriteLog("In Authenticate: ResponseResult : " + result.ToString());
            return SetToXml(result, error);
        }
        [AcceptVerbs("GET")]
        public async Task<HttpResponseMessage> GetAccidents(string pmsPatientId, string pmsEncounterId, string pmsOrder = "desc",
                                                string pmsMinDateTime = "", string pmsMaxDateTime = "")
        {
            string error = string.Empty;
            string result = string.Empty;
            string authentication = GetAuthorizationToken();
            DateTime dtMin = DateTime.MinValue;
            DateTime dtMax = DateTime.MinValue;

            DateTime.TryParse(pmsMinDateTime, out dtMin);
            DateTime.TryParse(pmsMaxDateTime, out dtMax);
            string pho = string.Empty;

            try
            {

                string practiceid = string.Empty;
                pmsEncounterId = GetBase64Value(pmsEncounterId);
                pmsPatientId = GetBase64Value(pmsPatientId);
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
                            practiceid += "_" + splitEncounter[2];
                        }
                        if (splitEncounter.Length > 3)
                        {
                            practiceid = "_" + splitEncounter[3];
                            pho = splitEncounter[3];
                        }
                    }
                }

                if (ConfigurationManager.AppSettings["EnableAzureERMSAPI"] == "1" && !string.IsNullOrEmpty(pho) && pho.ToLower().Contains("azure"))
                    return await ERMSAPIProxy.ForwardtoERMSAzureAPIGET(Request, "/api/GetAccidents");

                pmsPatientId = GetDcrptValue(pmsPatientId);
                WriteLog("In GetAccidents: PatientId: " + pmsPatientId + " ---- EncounterId: " + pmsEncounterId + " ---- Practiceid: " + practiceid + " ---- Order: " + pmsOrder + " ---- MinDateTime: " + pmsMinDateTime + " ---- MaxDateTime: " + pmsMaxDateTime);
                if (HSSDA.InsertAndValidateToken(pmsPatientId, pmsEncounterId, practiceid, authentication, out error))
                {
                    Accidents objAccidents = new Accidents();
                    List<Accident> ListAccidents = ERMSDataTableToListHiso<Accident>(HSSDA.GetACC45(pmsPatientId, pmsOrder.ToUpper(), dtMin, dtMax, practiceid,
                                                                                                  out error));
                    if (ListAccidents != null && ListAccidents.Count > 0)
                    {
                        ListAccidents.ForEach(x => x.DiagnosisDescription.Text = (x.DiagnosisDescription.Text != null) ?
                        System.Text.RegularExpressions.Regex.Replace(x.DiagnosisDescription.Text, @"<(.|\n)*?>", "").Replace("&nbsp;", " ") : x.DiagnosisDescription.Text);
                        ListAccidents.ForEach(x => x.Order = (pmsOrder.Equals("desc") ? "dateDescend" : "dateAscend"));

                        if (!dtMin.Equals(DateTime.MinValue))
                            ListAccidents.ForEach(x => x.MinDateTime = dtMin.ToString(Utility.Instance.ToString(ConfigurationManager.AppSettings["DateFormat"])));
                        if (!dtMax.Equals(DateTime.MinValue))
                            ListAccidents.ForEach(x => x.MaxDateTime = dtMax.ToString(Utility.Instance.ToString(ConfigurationManager.AppSettings["DateFormat"])));


                        objAccidents.Accident.AddRange(ListAccidents);
                    }
                    result = PrepareXml<Accidents>(objAccidents);

                }
                else if (string.IsNullOrWhiteSpace(error))
                    error = "Invalid token value!";
            }
            catch (Exception ex)
            {
                WriteLog("GetAccidents: ", ex);
                error = ex.Message;
            }
            WriteLog("In GetAccidents: ResponseResult : " + result.ToString());
            return SetToXml(result, error);
        }
        [AcceptVerbs("GET")]
        public async Task<HttpResponseMessage> GetClassifications(string pmsPatientId, string pmsEncounterId, string pmsOrder = "desc",
                                                      string pmsMinDateTime = "", string pmsMaxDateTime = "")
        {
            string error = string.Empty;
            string result = string.Empty;
            string authentication = GetAuthorizationToken();
            DateTime dtMin = DateTime.MinValue;
            DateTime dtMax = DateTime.MinValue;

            DateTime.TryParse(pmsMinDateTime, out dtMin);
            DateTime.TryParse(pmsMaxDateTime, out dtMax);
            string pho = string.Empty;

            try
            {

                string practiceid = string.Empty;
                pmsEncounterId = GetBase64Value(pmsEncounterId);
                pmsPatientId = GetBase64Value(pmsPatientId);
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
                            practiceid += "_" + splitEncounter[2];
                        }
                        if (splitEncounter.Length > 3)
                        {
                            practiceid = "_" + splitEncounter[3];
                            pho = splitEncounter[3];
                        }
                    }
                }

                if (ConfigurationManager.AppSettings["EnableAzureERMSAPI"] == "1" && !string.IsNullOrEmpty(pho) && pho.ToLower().Contains("azure"))
                    return await ERMSAPIProxy.ForwardtoERMSAzureAPIGET(Request, "/api/GetClassifications");

                pmsPatientId = GetDcrptValue(pmsPatientId);
                WriteLog("In GetClassifications: PatientId: " + pmsPatientId + " ---- EncounterId: " + pmsEncounterId + " ---- Practiceid: " + practiceid + " ---- Order: " + pmsOrder + " ---- MinDateTime: " + pmsMinDateTime + " ---- MaxDateTime: " + pmsMaxDateTime);
                if (HSSDA.InsertAndValidateToken(pmsPatientId, pmsEncounterId, practiceid, authentication, out error))
                {
                    Problems objProblems = new Problems();
                    List<PatientProblem> ListPatientProblem = ERMSDataTableToListHiso<PatientProblem>(
                                                              HSSDA.GetConditions(pmsPatientId, pmsOrder.ToUpper(), dtMin, dtMax, practiceid, out error));
                    if (ListPatientProblem != null && ListPatientProblem.Count > 0)
                    {
                        ListPatientProblem.ForEach(x => x.Order = (pmsOrder.Equals("desc") ? "dateDescend" : "dateAscend"));

                        if (!dtMin.Equals(DateTime.MinValue))
                            ListPatientProblem.ForEach(x => x.MinDateTime = dtMin.ToString(Utility.Instance.ToString(ConfigurationManager.AppSettings["DateFormat"])));
                        if (!dtMax.Equals(DateTime.MinValue))
                            ListPatientProblem.ForEach(x => x.MaxDateTime = dtMax.ToString(Utility.Instance.ToString(ConfigurationManager.AppSettings["DateFormat"])));

                        objProblems.Problem.AddRange(ListPatientProblem);
                    }
                    result = PrepareXml<Problems>(objProblems);

                }
                else if (string.IsNullOrWhiteSpace(error))
                    error = "Invalid token value!";
            }
            catch (Exception ex)
            {
                WriteLog("GetClassifications: ", ex);
                error = ex.Message;
            }
            WriteLog("In GetClassifications: ResponseResult : " + result.ToString());
            return SetToXml(result, error);
        }
        [AcceptVerbs("GET")]
        public async Task<HttpResponseMessage> GetConsultNotes(string pmsPatientId, string pmsEncounterId, string pmsOrder = "desc",
                                                   string pmsMinDateTime = "", string pmsMaxDateTime = "")
        {
            string error = string.Empty;
            string result = string.Empty;
            string authentication = GetAuthorizationToken();
            DateTime dtMin = DateTime.MinValue;
            DateTime dtMax = DateTime.MinValue;

            DateTime.TryParse(pmsMinDateTime, out dtMin);
            DateTime.TryParse(pmsMaxDateTime, out dtMax);

            if (string.IsNullOrEmpty(pmsMinDateTime))
            {
                dtMin = DateTime.Now.AddMonths(-24);
            }
            if (string.IsNullOrEmpty(pmsMaxDateTime))
            {
                dtMax = DateTime.Now;
            }

            string pho = string.Empty;

            try
            {

                string practiceid = string.Empty;
                pmsEncounterId = GetBase64Value(pmsEncounterId);
                pmsPatientId = GetBase64Value(pmsPatientId);
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
                            practiceid += "_" + splitEncounter[2];
                        }
                        if (splitEncounter.Length > 3)
                        {
                            practiceid = "_" + splitEncounter[3];
                            pho = splitEncounter[3];
                        }
                    }
                }

                if (ConfigurationManager.AppSettings["EnableAzureERMSAPI"] == "1" && !string.IsNullOrEmpty(pho) && pho.ToLower().Contains("azure"))
                    return await ERMSAPIProxy.ForwardtoERMSAzureAPIGET(Request, "/api/GetConsultNotes");

                pmsPatientId = GetDcrptValue(pmsPatientId);
                WriteLog("In GetConsultNotes: PatientId: " + pmsPatientId + " ---- EncounterId: " + pmsEncounterId + " ---- Practiceid: " + practiceid + " ---- Order: " + pmsOrder + " ---- MinDateTime: " + pmsMinDateTime + " ---- MaxDateTime: " + pmsMaxDateTime);
                if (HSSDA.InsertAndValidateToken(pmsPatientId, pmsEncounterId, practiceid, authentication, out error))
                {
                    ConsultNotes objConsultNotes = new ConsultNotes();
                    List<Consult> ListConsult = ERMSDataTableToListHiso<Consult>(
                                                HSSDA.GetConsultNotes(pmsPatientId, pmsOrder.ToUpper(), dtMin, dtMax, practiceid,
                                                out error));
                    if (ListConsult != null && ListConsult.Count > 0)
                    {
                        ListConsult.ForEach(x => x.Order = (pmsOrder.Equals("desc") ? "dateDescend" : "dateAscend"));

                        //if (!dtMin.Equals(DateTime.MinValue))
                        //    ListConsult.ForEach(x => x.MinDateTime = dtMin.ToString(Utility.Instance.ToString(ConfigurationManager.AppSettings["DateFormat"])));
                        //if (!dtMax.Equals(DateTime.MinValue))
                        //    ListConsult.ForEach(x => x.MaxDateTime = dtMax.ToString(Utility.Instance.ToString(ConfigurationManager.AppSettings["DateFormat"])));

                        objConsultNotes.Consult.AddRange(ListConsult);
                    }
                    result = PrepareXml<ConsultNotes>(objConsultNotes);

                }
                else if (string.IsNullOrWhiteSpace(error))
                    error = "Invalid token value!";
            }
            catch (Exception ex)
            {
                WriteLog("GetConsultNotes: ", ex);
                error = ex.Message;
            }
            WriteLog("In GetConsultNotes: ResponseResult : " + result.ToString());
            return SetToXml(result, error);
        }
        [AcceptVerbs("GET")]
        public async Task<HttpResponseMessage> GetCurrentUser(string pmsPatientId, string pmsEncounterId, string LocationId = "", string pmsUserId = "")
        {
            string error = string.Empty;
            string result = string.Empty;
            string authentication = GetAuthorizationToken();
            string pho = string.Empty;

            try
            {
                
                string practiceid = string.Empty;
                pmsEncounterId = GetBase64Value(pmsEncounterId);
                pmsPatientId = GetBase64Value(pmsPatientId);
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
                            practiceid += "_" + splitEncounter[2];
                        }
                        if (splitEncounter.Length > 3)
                        {
                            practiceid = "_" + splitEncounter[3];
                            pho = splitEncounter[3];
                        }
                    }
                }

                if (ConfigurationManager.AppSettings["EnableAzureERMSAPI"] == "1" && !string.IsNullOrEmpty(pho) && pho.ToLower().Contains("azure"))
                    return await ERMSAPIProxy.ForwardtoERMSAzureAPIGET(Request, "/api/GetCurrentUser");

                pmsPatientId = GetDcrptValue(pmsPatientId);
                pmsUserId = GetDcrptValue(pmsUserId);
                LocationId = GetDcrptValue(LocationId);
                WriteLog("In GetCurrentUser: PatientId: " + pmsPatientId + " ---- EncounterId: " + pmsEncounterId + " ---- Practiceid: " + practiceid + " ---- LocationId: " + LocationId + " ---- UserId: " + pmsUserId);
                if (HSSDA.InsertAndValidateToken(pmsPatientId, pmsEncounterId, practiceid, authentication, out error))
                {
                    List<CurrentUser> ListProvider = ERMSDataTableToListHiso<CurrentUser>(HSSDA.GetProvider(pmsPatientId, pmsUserId, LocationId, practiceid, out error, pmsEncounterId));

                    result = PrepareXml<CurrentUser>((ListProvider.Count > 0) ?
                                                      ListProvider[0] : new CurrentUser());
                }
                else if (string.IsNullOrWhiteSpace(error))
                    error = "Invalid token value!";
            }
            catch (Exception ex)
            {
                WriteLog("GetCurrentUser: ", ex);
                error = ex.Message;
            }
            WriteLog("In GetCurrentUser: ResponseResult : " + result.ToString());
            return SetToXml(result, error);
        }
        [AcceptVerbs("GET")]
        public async Task<HttpResponseMessage> GetDischargeSummaryReportList(string pmsPatientId, string pmsEncounterId, string pmsOrder = "desc",
                                                string pmsMinDateTime = "", string pmsMaxDateTime = "")
        {
            string error = string.Empty;
            string result = string.Empty;
            string authentication = GetAuthorizationToken();
            DateTime dtMin = DateTime.MinValue;
            DateTime dtMax = DateTime.MinValue;

            DateTime.TryParse(pmsMinDateTime, out dtMin);
            DateTime.TryParse(pmsMaxDateTime, out dtMax);
            string pho = string.Empty;

            try
            {
                int actualPracticeID = 0;
                string practiceid = string.Empty;
                pmsEncounterId = GetBase64Value(pmsEncounterId);
                pmsPatientId = GetBase64Value(pmsPatientId);
                if (!string.IsNullOrEmpty(pmsEncounterId) && pmsEncounterId.Contains("_"))
                {
                    string Delimiter = "__";
                    string[] splitEncounter = pmsEncounterId.Split(new[] { Delimiter }, StringSplitOptions.None);
                    if (splitEncounter.Length == 1) { splitEncounter = pmsEncounterId.Split('_'); }
                    if (splitEncounter.Length > 0)
                    {
                        pmsEncounterId = GetDcrptValue(splitEncounter[0]);
                        practiceid = "_" + splitEncounter[1];
                        actualPracticeID = Convert.ToInt32(splitEncounter[1]);
                        if (splitEncounter.Length > 2)
                        {
                            practiceid += "_" + splitEncounter[2];
                        }
                        if (splitEncounter.Length > 3)
                        {
                            practiceid = "_" + splitEncounter[3];
                            pho = splitEncounter[3];
                        }
                    }
                }

                if (ConfigurationManager.AppSettings["EnableAzureERMSAPI"] == "1" && !string.IsNullOrEmpty(pho) && pho.ToLower().Contains("azure"))
                    return await ERMSAPIProxy.ForwardtoERMSAzureAPIGET(Request, "/api/GetDischargeSummaryReportList");

                pmsPatientId = GetDcrptValue(pmsPatientId);
                WriteLog("In GetDischargeSummaryReportList: PatientId: " + pmsPatientId + " ---- EncounterId: " + pmsEncounterId + " ---- Practiceid: " + practiceid + " ---- Order: " + pmsOrder + " ---- MinDateTime: " + pmsMinDateTime + " ---- MaxDateTime: " + pmsMaxDateTime + "---actualPracticeID "+ actualPracticeID);
                if (HSSDA.InsertAndValidateToken(pmsPatientId, pmsEncounterId, practiceid, authentication, out error))
                {
                    DischargeReports objDischargeReports = new DischargeReports();
                    List<Group> ListDischargeReports = ERMSDataTableToListHiso<Group>(HSSDA.GetOtherDocs(pmsPatientId, pmsOrder.ToUpper(), dtMin, dtMax, practiceid,
                                                                                                  true, actualPracticeID, out error));
                    if (ListDischargeReports != null && ListDischargeReports.Count > 0)
                    {
                        ListDischargeReports.ForEach(x => x.Order = (pmsOrder.Equals("desc") ? "dateDescend" : "dateAscend"));

                        if (!dtMin.Equals(DateTime.MinValue))
                            ListDischargeReports.ForEach(x => x.MinDateTime = dtMin.ToString(Utility.Instance.ToString(ConfigurationManager.AppSettings["DateFormat"])));
                        if (!dtMax.Equals(DateTime.MinValue))
                            ListDischargeReports.ForEach(x => x.MaxDateTime = dtMax.ToString(Utility.Instance.ToString(ConfigurationManager.AppSettings["DateFormat"])));

                        objDischargeReports.Group.AddRange(ListDischargeReports);
                    }
                    result = PrepareXml<DischargeReports>(objDischargeReports);

                }
                else if (string.IsNullOrWhiteSpace(error))
                    error = "Invalid token value!";
            }
            catch (Exception ex)
            {
                WriteLog("GetDischargeSummaryReportList: ", ex);
                error = ex.Message;
            }
            WriteLog("In GetDischargeSummaryReportList: ResponseResult : " + result.ToString());
            return SetToXml(result, error);
        }
        [AcceptVerbs("GET")]
        public async Task<HttpResponseMessage> GetDischargeSummaryDetails(string pmsPatientId, string pmsEncounterId, string pmsReferenceId)
        {
            string error = string.Empty;
            string result = string.Empty;
            string authentication = GetAuthorizationToken();
            string pho = string.Empty;
            int actualPracticeID = 0;
            try
            {

                string practiceid = string.Empty;
                pmsEncounterId = GetBase64Value(pmsEncounterId);
                pmsPatientId = GetBase64Value(pmsPatientId);
                if (!string.IsNullOrEmpty(pmsEncounterId) && pmsEncounterId.Contains("_"))
                {
                    string Delimiter = "__";
                    string[] splitEncounter = pmsEncounterId.Split(new[] { Delimiter }, StringSplitOptions.None);
                    if (splitEncounter.Length == 1) { splitEncounter = pmsEncounterId.Split('_'); }
                    if (splitEncounter.Length > 0)
                    {
                        pmsEncounterId = GetDcrptValue(splitEncounter[0]);
                        practiceid = "_" + splitEncounter[1];
                        actualPracticeID = Convert.ToInt32(splitEncounter[1]);
                        if (splitEncounter.Length > 2)
                        {
                            practiceid += "_" + splitEncounter[2];
                        }
                        if (splitEncounter.Length > 3)
                        {
                            practiceid = "_" + splitEncounter[3];
                            pho = splitEncounter[3];
                        }
                    }
                }

                if (ConfigurationManager.AppSettings["EnableAzureERMSAPI"] == "1" && !string.IsNullOrEmpty(pho) && pho.ToLower().Contains("azure"))
                    return await ERMSAPIProxy.ForwardtoERMSAzureAPIGET(Request, "/api/GetDischargeSummaryDetails");

                pmsPatientId = GetDcrptValue(pmsPatientId);
                WriteLog("In GetDischargeSummaryDetails: PatientId: " + pmsPatientId + " ---- EncounterId: " + pmsEncounterId + " ---- Practiceid: " + practiceid + " ---- ReferenceId: " + pmsReferenceId);
                if (HSSDA.InsertAndValidateToken(pmsPatientId, pmsEncounterId, practiceid, authentication, out error))
                {
                    DischargeSummaryContents objDischargeSummaryContents = new DischargeSummaryContents();
                    List<DischargeSummaryContent> ListDischargeSummaryContent = ERMSDataTableToListHiso<DischargeSummaryContent>(HSSDA.GetDocResults(pmsReferenceId, string.Empty, true, practiceid, actualPracticeID, out error));

                    if (ListDischargeSummaryContent.Count > 0 &&
                       !string.IsNullOrWhiteSpace(ListDischargeSummaryContent[0].Content.Text))
                    {
                        objDischargeSummaryContents.DischargeSummaryContent.Add(ListDischargeSummaryContent[0]);
                    }
                    else
                        objDischargeSummaryContents.DischargeSummaryContent.Add(new DischargeSummaryContent());

                    result = PrepareXml<DischargeSummaryContents>(objDischargeSummaryContents);
                }
                else if (string.IsNullOrWhiteSpace(error))
                    error = "Invalid token value!";
            }
            catch (Exception ex)
            {
                WriteLog("GetDischargeSummaryDetails: ", ex);
                error = ex.Message;
            }
            WriteLog("In GetDischargeSummaryDetails: ResponseResult : " + result.ToString());
            return SetToXml(result, error);
        }
        [AcceptVerbs("GET")]
        public async Task<HttpResponseMessage> GetLaboratoryReportList(string pmsPatientId, string pmsEncounterId, string pmsOrder = "desc",
                                               string pmsMinDateTime = "", string pmsMaxDateTime = "")
        {
            string error = string.Empty;
            string result = string.Empty;
            string authentication = GetAuthorizationToken();
            DateTime dtMin = DateTime.MinValue;
            DateTime dtMax = DateTime.MinValue;

            DateTime.TryParse(pmsMinDateTime, out dtMin);
            DateTime.TryParse(pmsMaxDateTime, out dtMax);
            string pho = string.Empty;

            try
            {

                string practiceid = string.Empty;
                pmsEncounterId = GetBase64Value(pmsEncounterId);
                pmsPatientId = GetBase64Value(pmsPatientId);
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
                            practiceid += "_" + splitEncounter[2];
                        }
                        if (splitEncounter.Length > 3)
                        {
                            practiceid = "_" + splitEncounter[3];
                            pho = splitEncounter[3];
                        }
                    }
                }

                if (ConfigurationManager.AppSettings["EnableAzureERMSAPI"] == "1" && !string.IsNullOrEmpty(pho) && pho.ToLower().Contains("azure"))
                    return await ERMSAPIProxy.ForwardtoERMSAzureAPIGET(Request, "/api/GetLaboratoryReportList");

                pmsPatientId = GetDcrptValue(pmsPatientId);
                WriteLog("In GetLaboratoryReportList: PatientId: " + pmsPatientId + " ---- EncounterId: " + pmsEncounterId + " ---- Practiceid: " + practiceid + " ---- Order: " + pmsOrder + " ---- MinDateTime: " + pmsMinDateTime + " ---- MaxDateTime: " + pmsMaxDateTime);
                if (HSSDA.InsertAndValidateToken(pmsPatientId, pmsEncounterId, practiceid, authentication, out error))
                {
                    LaboratoryReports objLaboratoryReports = new LaboratoryReports();
                    List<LaboratoryReport> ListLaboratoryReport = ERMSDataTableToListHiso<LaboratoryReport>(HSSDA.GetLabs(pmsPatientId, pmsOrder.ToUpper(), dtMin, dtMax, practiceid,
                                                                                                  out error));
                    if (ListLaboratoryReport != null && ListLaboratoryReport.Count > 0)
                    {
                        ListLaboratoryReport.ForEach(x => x.Order = (pmsOrder.Equals("desc") ? "dateDescend" : "dateAscend"));

                        if (!dtMin.Equals(DateTime.MinValue))
                            ListLaboratoryReport.ForEach(x => x.MinDateTime = dtMin.ToString(Utility.Instance.ToString(ConfigurationManager.AppSettings["DateFormat"])));
                        if (!dtMax.Equals(DateTime.MinValue))
                            ListLaboratoryReport.ForEach(x => x.MaxDateTime = dtMax.ToString(Utility.Instance.ToString(ConfigurationManager.AppSettings["DateFormat"])));


                        objLaboratoryReports.LaboratoryReport.AddRange(ListLaboratoryReport);
                    }
                    result = PrepareXml<LaboratoryReports>(objLaboratoryReports);

                }
                else if (string.IsNullOrWhiteSpace(error))
                    error = "Invalid token value!";
            }
            catch (Exception ex)
            {
                WriteLog("GetLaboratoryReportList: ", ex);
                error = ex.Message;
            }
            WriteLog("In GetLaboratoryReportList: ResponseResult : " + result.ToString());
            return SetToXml(result, error);
        }
        [AcceptVerbs("GET")]
        public async Task<HttpResponseMessage> GetLaboratoryReportDetails(string pmsPatientId, string pmsEncounterId, string pmsReferenceId)
        {
            string error = string.Empty;
            string result = string.Empty;
            string authentication = GetAuthorizationToken();
            string pho = string.Empty;

            try
            {

                string practiceid = string.Empty;
                pmsEncounterId = GetBase64Value(pmsEncounterId);
                pmsPatientId = GetBase64Value(pmsPatientId);
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
                            practiceid += "_" + splitEncounter[2];
                        }
                        if (splitEncounter.Length > 3)
                        {
                            practiceid = "_" + splitEncounter[3];
                            pho = splitEncounter[3];
                        }
                    }
                }

                if (ConfigurationManager.AppSettings["EnableAzureERMSAPI"] == "1" && !string.IsNullOrEmpty(pho) && pho.ToLower().Contains("azure"))
                    return await ERMSAPIProxy.ForwardtoERMSAzureAPIGET(Request, "/api/GetLaboratoryReportDetails");

                pmsPatientId = GetDcrptValue(pmsPatientId);
                WriteLog("In GetLaboratoryReportDetails: PatientId: " + pmsPatientId + " ---- EncounterId: " + pmsEncounterId + " ---- Practiceid: " + practiceid + " ---- ReferenceId: " + pmsReferenceId);
                if (HSSDA.InsertAndValidateToken(pmsPatientId, pmsEncounterId, practiceid, authentication, out error))
                {
                    LaboratoryReportsContent objLaboratoryReportsContent = new LaboratoryReportsContent();
                    List<LaboratoryReportContent> ListLaboratoryReport = ERMSDataTableToListHiso<LaboratoryReportContent>(HSSDA.GetLabResults(pmsPatientId, pmsReferenceId, practiceid, out error));

                    if (ListLaboratoryReport.Count > 0 &&
                       !string.IsNullOrWhiteSpace(ListLaboratoryReport[0].Content.Text))
                    {
                        ListLaboratoryReport[0].Content.Text = Utility.Instance.ConvertString2RTF(ListLaboratoryReport[0].Content.Text, true);
                        objLaboratoryReportsContent.LaboratoryReportContent.Add(ListLaboratoryReport[0]);
                    }
                    else
                        objLaboratoryReportsContent.LaboratoryReportContent.Add(new LaboratoryReportContent());

                    result = PrepareXml<LaboratoryReportsContent>(objLaboratoryReportsContent);
                }
                else if (string.IsNullOrWhiteSpace(error))
                    error = "Invalid token value!";
            }
            catch (Exception ex)
            {
                WriteLog("GetLaboratoryReportDetails: ", ex);
                error = ex.Message;
            }
            WriteLog("In GetLaboratoryReportDetails: ResponseResult : " + result.ToString());
            return SetToXml(result, error);
        }
        [AcceptVerbs("GET")]
        public async Task<HttpResponseMessage> GetMedicalAllergies(string pmsPatientId, string pmsEncounterId, string pmsOrder = "desc",
                                                       string pmsMinDateTime = "", string pmsMaxDateTime = "")
        {
            string error = string.Empty;
            string result = string.Empty;
            string authentication = GetAuthorizationToken();
            DateTime dtMin = DateTime.MinValue;
            DateTime dtMax = DateTime.MinValue;

            DateTime.TryParse(pmsMinDateTime, out dtMin);
            DateTime.TryParse(pmsMaxDateTime, out dtMax);
            string pho = string.Empty;

            try
            {

                string practiceid = string.Empty;
                pmsEncounterId = GetBase64Value(pmsEncounterId);
                pmsPatientId = GetBase64Value(pmsPatientId);
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
                            practiceid += "_" + splitEncounter[2];
                        }
                        if (splitEncounter.Length > 3)
                        {
                            practiceid = "_" + splitEncounter[3];
                            pho = splitEncounter[3];
                        }
                    }
                }

                if (ConfigurationManager.AppSettings["EnableAzureERMSAPI"] == "1" && !string.IsNullOrEmpty(pho) && pho.ToLower().Contains("azure"))
                    return await ERMSAPIProxy.ForwardtoERMSAzureAPIGET(Request, "/api/GetMedicalAllergies");

                pmsPatientId = GetDcrptValue(pmsPatientId);
                WriteLog("In GetMedicalAllergies: PatientId: " + pmsPatientId + " ---- EncounterId: " + pmsEncounterId + " ---- Practiceid: " + practiceid + " ---- Order: " + pmsOrder + " ---- MinDateTime: " + pmsMinDateTime + " ---- MaxDateTime: " + pmsMaxDateTime);
                if (HSSDA.InsertAndValidateToken(pmsPatientId, pmsEncounterId, practiceid, authentication, out error))
                {
                    MedicalWarnings objMedicalWarnings = new MedicalWarnings();
                    List<MedicalWarning> ListMedicalWarnings = ERMSDataTableToListHiso<MedicalWarning>(
                                                       HSSDA.GetMedicalAllergies(pmsPatientId, pmsOrder.ToUpper(), dtMin, dtMax, practiceid,
                                                        out error));
                    if (ListMedicalWarnings != null && ListMedicalWarnings.Count > 0)
                    {
                        ListMedicalWarnings.ForEach(x => x.Order = (pmsOrder.Equals("desc") ? "dateDescend" : "dateAscend"));

                        if (!dtMin.Equals(DateTime.MinValue))
                            ListMedicalWarnings.ForEach(x => x.MinDateTime = dtMin.ToString(Utility.Instance.ToString(ConfigurationManager.AppSettings["DateFormat"])));
                        if (!dtMax.Equals(DateTime.MinValue))
                            ListMedicalWarnings.ForEach(x => x.MaxDateTime = dtMax.ToString(Utility.Instance.ToString(ConfigurationManager.AppSettings["DateFormat"])));

                        objMedicalWarnings.MedicalWarning.AddRange(ListMedicalWarnings);
                    }
                    result = PrepareXml<MedicalWarnings>(objMedicalWarnings);

                }
                else if (string.IsNullOrWhiteSpace(error))
                    error = "Invalid token value!";
            }
            catch (Exception ex)
            {
                WriteLog("GetMedicalAllergies: ", ex);
                error = ex.Message;
            }
            WriteLog("In GetMedicalAllergies: ResponseResult : " + result.ToString());
            return SetToXml(result, error);
        }
        [AcceptVerbs("GET")]
        public async Task<HttpResponseMessage> GetNextOfKin(string pmsPatientId, string pmsEncounterId)
        {
            string error = string.Empty;
            string result = string.Empty;
            string authentication = GetAuthorizationToken();
            string pho = string.Empty;

            try
            {

                string practiceid = string.Empty;
                pmsEncounterId = GetBase64Value(pmsEncounterId);
                pmsPatientId = GetBase64Value(pmsPatientId);
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
                            practiceid += "_" + splitEncounter[2];
                        }
                        if (splitEncounter.Length > 3)
                        {
                            practiceid = "_" + splitEncounter[3];
                            pho = splitEncounter[3];
                        }
                    }
                }

                if (ConfigurationManager.AppSettings["EnableAzureERMSAPI"] == "1" && !string.IsNullOrEmpty(pho) && pho.ToLower().Contains("azure"))
                    return await ERMSAPIProxy.ForwardtoERMSAzureAPIGET(Request, "/api/GetNextOfKin");

                pmsPatientId = GetDcrptValue(pmsPatientId);
                WriteLog("In GetNextOfKin: PatientId: " + pmsPatientId + " ---- EncounterId: " + pmsEncounterId + " ---- Practiceid: " + practiceid);
                if (HSSDA.InsertAndValidateToken(pmsPatientId, pmsEncounterId, practiceid, authentication, out error))
                {
                    NextOfKin objNextOfKin = new NextOfKin();
                    List<PatientNOK> ListPatientNOK = ERMSDataTableToListHiso<PatientNOK>(HSSDA.GetNextOfKin(pmsPatientId, practiceid, out error));

                    if (ListPatientNOK != null && ListPatientNOK.Count > 0)
                    {
                        objNextOfKin.PatientNOK.AddRange(ListPatientNOK);
                    }
                    result = PrepareXml<NextOfKin>(objNextOfKin);

                }
                else if (string.IsNullOrWhiteSpace(error))
                    error = "Invalid token value!";
            }
            catch (Exception ex)
            {
                WriteLog("GetNextOfKin: ", ex);
                error = ex.Message;
            }
            WriteLog("In GetNextOfKin: ResponseResult : " + result.ToString());
            return SetToXml(result, error);
        }
        [AcceptVerbs("GET")]
        public async Task<HttpResponseMessage> GetPatientData(string pmsPatientId, string pmsEncounterId)
        {
            string error = string.Empty;
            string result = string.Empty;
            string authentication = GetAuthorizationToken();
            string pho = string.Empty;

            try
            {

                string practiceid = string.Empty;
                pmsEncounterId = GetBase64Value(pmsEncounterId);
                pmsPatientId = GetBase64Value(pmsPatientId);
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
                            practiceid += "_" + splitEncounter[2];
                        }
                        if (splitEncounter.Length > 3)
                        {
                            practiceid = "_" + splitEncounter[3];
                            pho = splitEncounter[3];
                        }
                    }
                }

                if (ConfigurationManager.AppSettings["EnableAzureERMSAPI"] == "1" && !string.IsNullOrEmpty(pho) && pho.ToLower().Contains("azure"))
                    return await ERMSAPIProxy.ForwardtoERMSAzureAPIGET(Request, "/api/GetPatientData");

                pmsPatientId = GetDcrptValue(pmsPatientId);
                WriteLog("In GetPatientData: PatientId: " + pmsPatientId + " ---- EncounterId: " + pmsEncounterId + " ---- Practiceid: " + practiceid);
                if (HSSDA.InsertAndValidateToken(pmsPatientId, pmsEncounterId, practiceid, authentication, out error))
                {
                    List<PatientData> ListPatientData = ERMSDataTableToListHiso<PatientData>(HSSDA.GetDemographics(pmsPatientId, practiceid, out error));

                    result = PrepareXml<PatientData>((ListPatientData.Count > 0) ?
                                                      ListPatientData[0] : new PatientData());
                }
                else if (string.IsNullOrWhiteSpace(error))
                    error = "Invalid token value!";
            }
            catch (Exception ex)
            {
                WriteLog("GetPatientData: ", ex);
                error = ex.Message;
            }
            WriteLog("In GetPatientData: ResponseResult : " + result.ToString());
            return SetToXml(result, error);
        }
        [AcceptVerbs("GET")]
        public async Task<HttpResponseMessage> GetPatientMeasurement(string pmsPatientId, string pmsEncounterId)
        {
            string error = string.Empty;
            string result = string.Empty;
            string authentication = GetAuthorizationToken();
            string pho = string.Empty;

            try
            {

                string practiceid = string.Empty;
                pmsEncounterId = GetBase64Value(pmsEncounterId);
                pmsPatientId = GetBase64Value(pmsPatientId);
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
                            practiceid += "_" + splitEncounter[2];
                        }
                        if (splitEncounter.Length > 3)
                        {
                            practiceid = "_" + splitEncounter[3];
                            pho = splitEncounter[3];
                        }
                    }
                }

                if (ConfigurationManager.AppSettings["EnableAzureERMSAPI"] == "1" && !string.IsNullOrEmpty(pho) && pho.ToLower().Contains("azure"))
                    return await ERMSAPIProxy.ForwardtoERMSAzureAPIGET(Request, "/api/GetPatientMeasurement");

                pmsPatientId = GetDcrptValue(pmsPatientId);
                WriteLog("In GetPatientMeasurement: PatientId: " + pmsPatientId + " ---- EncounterId: " + pmsEncounterId + " ---- Practiceid: " + practiceid);
                if (HSSDA.InsertAndValidateToken(pmsPatientId, pmsEncounterId, practiceid, authentication, out error))
                {
                    List<Measurement> ListMeasurement = ERMSDataTableToListHiso<Measurement>(HSSDA.GetMeasurement(pmsPatientId, practiceid, string.Empty, out error));

                    result = PrepareXml<Measurement>((ListMeasurement.Count > 0) ?
                                                      ListMeasurement[0] : new Measurement());
                }
                else if (string.IsNullOrWhiteSpace(error))
                    error = "Invalid token value!";
            }
            catch (Exception ex)
            {
                WriteLog("GetPatientMeasurement: ", ex);
                error = ex.Message;
            }
            WriteLog("In GetPatientMeasurement: ResponseResult : " + result.ToString());
            return SetToXml(result, error);
        }
        [AcceptVerbs("GET")]
        public async Task<HttpResponseMessage> GetPrescribedMedications(string pmsPatientId, string pmsEncounterId, string pmsOrder = "desc",
                                                            string pmsMinDateTime = "", string pmsMaxDateTime = "")
        {
            string error = string.Empty;
            string result = string.Empty;
            string authentication = GetAuthorizationToken();
            DateTime dtMin = DateTime.MinValue;
            DateTime dtMax = DateTime.MinValue;

            DateTime.TryParse(pmsMinDateTime, out dtMin);
            DateTime.TryParse(pmsMaxDateTime, out dtMax);
            string pho = string.Empty;

            try
            {

                string practiceid = string.Empty;
                pmsEncounterId = GetBase64Value(pmsEncounterId);
                pmsPatientId = GetBase64Value(pmsPatientId);
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
                            practiceid += "_" + splitEncounter[2];
                        }
                        if (splitEncounter.Length > 3)
                        {
                            practiceid = "_" + splitEncounter[3];
                            pho = splitEncounter[3];
                        }
                    }
                }

                if (ConfigurationManager.AppSettings["EnableAzureERMSAPI"] == "1" && !string.IsNullOrEmpty(pho) && pho.ToLower().Contains("azure"))
                    return await ERMSAPIProxy.ForwardtoERMSAzureAPIGET(Request, "/api/GetPrescribedMedications");

                pmsPatientId = GetDcrptValue(pmsPatientId);
                WriteLog("In GetPrescribedMedications: PatientId: " + pmsPatientId + " ---- EncounterId: " + pmsEncounterId + " ---- Practiceid: " + practiceid + " ---- Order: " + pmsOrder + " ---- MinDateTime: " + pmsMinDateTime + " ---- MaxDateTime: " + pmsMaxDateTime);
                if (HSSDA.InsertAndValidateToken(pmsPatientId, pmsEncounterId, practiceid, authentication, out error))
                {
                    PrescribedMedications objMedications = new PrescribedMedications();
                    List<PrescribedMedication> ListMedications = ERMSDataTableToListHiso<PrescribedMedication>(
                                                              HSSDA.GetMedications(pmsPatientId, pmsOrder.ToUpper(), dtMin, dtMax, practiceid,
                                                              false, out error));
                    if (ListMedications != null && ListMedications.Count > 0)
                    {
                        ListMedications.ForEach(x => x.Order = (pmsOrder.Equals("desc") ? "dateDescend" : "dateAscend"));

                        if (!dtMin.Equals(DateTime.MinValue))
                            ListMedications.ForEach(x => x.MinDateTime = dtMin.ToString(Utility.Instance.ToString(ConfigurationManager.AppSettings["DateFormat"])));
                        if (!dtMax.Equals(DateTime.MinValue))
                            ListMedications.ForEach(x => x.MaxDateTime = dtMax.ToString(Utility.Instance.ToString(ConfigurationManager.AppSettings["DateFormat"])));

                        objMedications.Medication.AddRange(ListMedications);
                    }
                    result = PrepareXml<PrescribedMedications>(objMedications);

                }
                else if (string.IsNullOrWhiteSpace(error))
                    error = "Invalid token value!";
            }
            catch (Exception ex)
            {
                WriteLog("GetPrescribedMedications: ", ex);
                error = ex.Message;
            }
            WriteLog("In GetPrescribedMedications: ResponseResult : " + result.ToString());
            return SetToXml(result, error);
        }
        [AcceptVerbs("GET")]
        public async Task<HttpResponseMessage> GetRegularMedications(string pmsPatientId, string pmsEncounterId, string pmsOrder = "desc",
                                                         string pmsMinDateTime = "", string pmsMaxDateTime = "")
        {
            string error = string.Empty;
            string result = string.Empty;
            string authentication = GetAuthorizationToken();
            DateTime dtMin = DateTime.MinValue;
            DateTime dtMax = DateTime.MinValue;

            DateTime.TryParse(pmsMinDateTime, out dtMin);
            DateTime.TryParse(pmsMaxDateTime, out dtMax);
            string pho = string.Empty;

            try
            {

                string practiceid = string.Empty;
                pmsEncounterId = GetBase64Value(pmsEncounterId);
                pmsPatientId = GetBase64Value(pmsPatientId);
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
                            practiceid += "_" + splitEncounter[2];
                        }
                        if (splitEncounter.Length > 3)
                        {
                            practiceid = "_" + splitEncounter[3];
                            pho = splitEncounter[3];
                        }
                    }
                }

                if (ConfigurationManager.AppSettings["EnableAzureERMSAPI"] == "1" && !string.IsNullOrEmpty(pho) && pho.ToLower().Contains("azure"))
                    return await ERMSAPIProxy.ForwardtoERMSAzureAPIGET(Request, "/api/GetRegularMedications");

                pmsPatientId = GetDcrptValue(pmsPatientId);
                WriteLog("In GetRegularMedications: PatientId: " + pmsPatientId + " ---- EncounterId: " + pmsEncounterId + " ---- Practiceid: " + practiceid + " ---- Order: " + pmsOrder + " ---- MinDateTime: " + pmsMinDateTime + " ---- MaxDateTime: " + pmsMaxDateTime);
                if (HSSDA.InsertAndValidateToken(pmsPatientId, pmsEncounterId, practiceid, authentication, out error))
                {
                    RegularMedications objMedications = new RegularMedications();
                    List<RegularMedication> ListMedications = ERMSDataTableToListHiso<RegularMedication>(
                                                              HSSDA.GetMedications(pmsPatientId, pmsOrder.ToUpper(), dtMin, dtMax, practiceid,
                                                              true, out error));
                    if (ListMedications != null && ListMedications.Count > 0)
                    {
                        ListMedications.ForEach(x => x.Order = (pmsOrder.Equals("desc") ? "dateDescend" : "dateAscend"));

                        if (!dtMin.Equals(DateTime.MinValue))
                            ListMedications.ForEach(x => x.MinDateTime = dtMin.ToString(Utility.Instance.ToString(ConfigurationManager.AppSettings["DateFormat"])));
                        if (!dtMax.Equals(DateTime.MinValue))
                            ListMedications.ForEach(x => x.MaxDateTime = dtMax.ToString(Utility.Instance.ToString(ConfigurationManager.AppSettings["DateFormat"])));

                        objMedications.RegularMedication.AddRange(ListMedications);
                    }
                    result = PrepareXml<RegularMedications>(objMedications);

                }
                else if (string.IsNullOrWhiteSpace(error))
                    error = "Invalid token value!";
            }
            catch (Exception ex)
            {
                WriteLog("GetRegularMedications: ", ex);
                error = ex.Message;
            }
            WriteLog("In GetRegularMedications: ResponseResult : " + result.ToString());
            return SetToXml(result, error);
        }
        [AcceptVerbs("GET")]
        public async Task<HttpResponseMessage> GetRadiologyReportList(string pmsPatientId, string pmsEncounterId, string pmsOrder = "desc",
                                                string pmsMinDateTime = "", string pmsMaxDateTime = "")
        {
            string error = string.Empty;
            string result = string.Empty;
            string authentication = GetAuthorizationToken();
            DateTime dtMin = DateTime.MinValue;
            DateTime dtMax = DateTime.MinValue;

            DateTime.TryParse(pmsMinDateTime, out dtMin);
            DateTime.TryParse(pmsMaxDateTime, out dtMax);
            string pho = string.Empty;

            try
            {

                string practiceid = string.Empty;
                pmsEncounterId = GetBase64Value(pmsEncounterId);
                pmsPatientId = GetBase64Value(pmsPatientId);
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
                            practiceid += "_" + splitEncounter[2];
                        }
                        if (splitEncounter.Length > 3)
                        {
                            practiceid = "_" + splitEncounter[3];
                            pho = splitEncounter[3];
                        }
                    }
                }

                if (ConfigurationManager.AppSettings["EnableAzureERMSAPI"] == "1" && !string.IsNullOrEmpty(pho) && pho.ToLower().Contains("azure"))
                    return await ERMSAPIProxy.ForwardtoERMSAzureAPIGET(Request, "/api/GetRadiologyReportList");

                pmsPatientId = GetDcrptValue(pmsPatientId);
                WriteLog("In GetRadiologyReportList: PatientId: " + pmsPatientId + " ---- EncounterId: " + pmsEncounterId + " ---- Practiceid: " + practiceid + " ---- Order: " + pmsOrder + " ---- MinDateTime: " + pmsMinDateTime + " ---- MaxDateTime: " + pmsMaxDateTime);
                if (HSSDA.InsertAndValidateToken(pmsPatientId, pmsEncounterId, practiceid, authentication, out error))
                {
                    RadiologyReports objRadiologyReports = new RadiologyReports();
                    List<RadiologyGroup> ListRadiologyReport = ERMSDataTableToListHiso<RadiologyGroup>(HSSDA.GetRads(pmsPatientId, pmsOrder.ToUpper(), dtMin, dtMax, practiceid,
                                                                                                  out error));
                    if (ListRadiologyReport != null && ListRadiologyReport.Count > 0)
                    {
                        ListRadiologyReport.ForEach(x => x.Order = (pmsOrder.Equals("desc") ? "dateDescend" : "dateAscend"));

                        if (!dtMin.Equals(DateTime.MinValue))
                            ListRadiologyReport.ForEach(x => x.MinDateTime = dtMin.ToString(Utility.Instance.ToString(ConfigurationManager.AppSettings["DateFormat"])));
                        if (!dtMax.Equals(DateTime.MinValue))
                            ListRadiologyReport.ForEach(x => x.MaxDateTime = dtMax.ToString(Utility.Instance.ToString(ConfigurationManager.AppSettings["DateFormat"])));


                        objRadiologyReports.RadiologyGroup.AddRange(ListRadiologyReport);
                    }
                    result = PrepareXml<RadiologyReports>(objRadiologyReports);

                }
                else if (string.IsNullOrWhiteSpace(error))
                    error = "Invalid token value!";
            }
            catch (Exception ex)
            {
                WriteLog("GetRadiologyReportList: ", ex);
                error = ex.Message;
            }
            WriteLog("In GetRadiologyReportList: ResponseResult : " + result.ToString());
            return SetToXml(result, error);
        }
        [AcceptVerbs("GET")]
        public async Task<HttpResponseMessage> GetRadiologyReportDetails(string pmsPatientId, string pmsEncounterId, string pmsReferenceId)
        {
            string error = string.Empty;
            string result = string.Empty;
            string authentication = GetAuthorizationToken();
            string pho = string.Empty;

            try
            {

                string practiceid = string.Empty;
                pmsEncounterId = GetBase64Value(pmsEncounterId);
                pmsPatientId = GetBase64Value(pmsPatientId);
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
                            practiceid += "_" + splitEncounter[2];
                        }
                        if (splitEncounter.Length > 3)
                        {
                            practiceid = "_" + splitEncounter[3];
                            pho = splitEncounter[3];
                        }
                    }
                }

                if (ConfigurationManager.AppSettings["EnableAzureERMSAPI"] == "1" && !string.IsNullOrEmpty(pho) && pho.ToLower().Contains("azure"))
                    return await ERMSAPIProxy.ForwardtoERMSAzureAPIGET(Request, "/api/GetRadiologyReportDetails");

                pmsPatientId = GetDcrptValue(pmsPatientId);
                WriteLog("In GetRadiologyReportDetails: PatientId: " + pmsPatientId + " ---- EncounterId: " + pmsEncounterId + " ---- Practiceid: " + practiceid + " ---- ReferenceId: " + pmsReferenceId);
                if (HSSDA.InsertAndValidateToken(pmsPatientId, pmsEncounterId, practiceid, authentication, out error))
                {
                    RadiologyReportContents objRadiologyReportsContent = new RadiologyReportContents();
                    List<RadiologyReportContent> ListRadiologyReport = ERMSDataTableToListHiso<RadiologyReportContent>(HSSDA.GetRadResults(pmsPatientId, pmsReferenceId, practiceid, out error));

                    if (ListRadiologyReport.Count > 0 &&
                       !string.IsNullOrWhiteSpace(ListRadiologyReport[0].Content.Text))
                    {
                        ListRadiologyReport[0].Content.Text = Utility.Instance.ConvertString2RTF(ListRadiologyReport[0].Content.Text, true);
                        objRadiologyReportsContent.RadiologyReportContent.Add(ListRadiologyReport[0]);
                    }
                    else
                        objRadiologyReportsContent.RadiologyReportContent.Add(new RadiologyReportContent());

                    result = PrepareXml<RadiologyReportContents>(objRadiologyReportsContent);
                }
                else if (string.IsNullOrWhiteSpace(error))
                    error = "Invalid token value!";
            }
            catch (Exception ex)
            {
                WriteLog("GetRadiologyReportDetails: ", ex);
                error = ex.Message;
            }
            WriteLog("In GetRadiologyReportDetails: ResponseResult : " + result.ToString());
            return SetToXml(result, error);
        }
        [AcceptVerbs("GET")]
        public async Task<HttpResponseMessage> GetRegisteredPractitioners(string pmsPatientId, string pmsEncounterId, string pmsLocationId = "")
        {
            string error = string.Empty;
            string result = string.Empty;
            string authentication = GetAuthorizationToken();
            string pho = string.Empty;

            try
            {

                string practiceid = string.Empty;
                pmsEncounterId = GetBase64Value(pmsEncounterId);
                pmsPatientId = GetBase64Value(pmsPatientId);
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
                            practiceid += "_" + splitEncounter[2];
                        }
                        if (splitEncounter.Length > 3)
                        {
                            practiceid = "_" + splitEncounter[3];
                            pho = splitEncounter[3];
                        }
                    }
                }

                if (ConfigurationManager.AppSettings["EnableAzureERMSAPI"] == "1" && !string.IsNullOrEmpty(pho) && pho.ToLower().Contains("azure"))
                    return await ERMSAPIProxy.ForwardtoERMSAzureAPIGET(Request, "/api/GetRegisteredPractitioners");

                pmsPatientId = GetDcrptValue(pmsPatientId);
                WriteLog("In GetRegisteredPractitioner: PatientId: " + pmsPatientId + " ---- EncounterId: " + pmsEncounterId + " ---- Practiceid: " + practiceid + " ---- LocationId: " + pmsLocationId);
                if (HSSDA.InsertAndValidateToken(pmsPatientId, pmsEncounterId, practiceid, authentication, out error))
                {
                    RegisteredPractitioners objPractitioners = new RegisteredPractitioners();
                    List<RegisteredPractitioner> ListPractitioners = ERMSDataTableToListHiso<RegisteredPractitioner>(HSSDA.GetRegisteredPractitioners(pmsPatientId, pmsLocationId, practiceid, out error));

                    objPractitioners.RegisteredPractitioner.AddRange(ListPractitioners);
                    result = PrepareXml<RegisteredPractitioners>(objPractitioners);
                }
                else if (string.IsNullOrWhiteSpace(error))
                    error = "Invalid token value!";
            }
            catch (Exception ex)
            {
                WriteLog("GetRegisteredPractitioner: ", ex);
                error = ex.Message;
            }
            WriteLog("In GetRegisteredPractitioners: ResponseResult : " + result.ToString());
            return SetToXml(result, error);
        }
        [AcceptVerbs("GET")]
        public async Task<HttpResponseMessage> GetScannedList(string pmsPatientId, string pmsEncounterId, string pmsOrder = "desc",
                                                string pmsMinDateTime = "", string pmsMaxDateTime = "")
        {
            string error = string.Empty;
            string result = string.Empty;
            string authentication = GetAuthorizationToken();
            DateTime dtMin = DateTime.MinValue;
            DateTime dtMax = DateTime.MinValue;

            DateTime.TryParse(pmsMinDateTime, out dtMin);
            DateTime.TryParse(pmsMaxDateTime, out dtMax);
            string pho = string.Empty;

            try
            {
               

                int actualPracticeID = 0;
                string practiceid = string.Empty;
                pmsEncounterId = GetBase64Value(pmsEncounterId);
                pmsPatientId = GetBase64Value(pmsPatientId);
                if (!string.IsNullOrEmpty(pmsEncounterId) && pmsEncounterId.Contains("_"))
                {
                    string Delimiter = "__";
                    string[] splitEncounter = pmsEncounterId.Split(new[] { Delimiter }, StringSplitOptions.None);
                    if (splitEncounter.Length == 1) { splitEncounter = pmsEncounterId.Split('_'); }
                    if (splitEncounter.Length > 0)
                    {
                        pmsEncounterId = GetDcrptValue(splitEncounter[0]);
                        practiceid = "_" + splitEncounter[1];
                        actualPracticeID = Convert.ToInt32(splitEncounter[1]);
                        if (splitEncounter.Length > 2)
                        {
                            practiceid += "_" + splitEncounter[2];
                        }
                        if (splitEncounter.Length > 3)
                        {
                            practiceid = "_" + splitEncounter[3];
                            pho = splitEncounter[3];
                        }
                    }
                }

                if (ConfigurationManager.AppSettings["EnableAzureERMSAPI"] == "1" && !string.IsNullOrEmpty(pho) && pho.ToLower().Contains("azure"))
                    return await ERMSAPIProxy.ForwardtoERMSAzureAPIGET(Request, "/api/GetScannedList");

                pmsPatientId = GetDcrptValue(pmsPatientId);
                WriteLog("In GetScannedList: PatientId: " + pmsPatientId + " ---- EncounterId: " + pmsEncounterId + " ---- Practiceid: " + practiceid + " ---- Order: " + pmsOrder + " ---- MinDateTime: " + pmsMinDateTime + " ---- MaxDateTime: " + pmsMaxDateTime + "---Actual Practiceid "+  actualPracticeID );
                if (HSSDA.InsertAndValidateToken(pmsPatientId, pmsEncounterId, practiceid, authentication, out error))
                {
                    ScanDocumentReports objScannedReports = new ScanDocumentReports();
                    List<ScannedGroup> ListScannedGroup = ERMSDataTableToListHiso<ScannedGroup>(HSSDA.GetOtherDocs(pmsPatientId, pmsOrder.ToUpper(), dtMin, dtMax, practiceid,
                                                                                                  false, actualPracticeID, out error));
                    if (ListScannedGroup != null && ListScannedGroup.Count > 0)
                    {
                        ListScannedGroup.ForEach(x => x.Order = (pmsOrder.Equals("desc") ? "dateDescend" : "dateAscend"));

                        if (!dtMin.Equals(DateTime.MinValue))
                            ListScannedGroup.ForEach(x => x.MinDateTime = dtMin.ToString(Utility.Instance.ToString(ConfigurationManager.AppSettings["DateFormat"])));
                        if (!dtMax.Equals(DateTime.MinValue))
                            ListScannedGroup.ForEach(x => x.MaxDateTime = dtMax.ToString(Utility.Instance.ToString(ConfigurationManager.AppSettings["DateFormat"])));

                        objScannedReports.ScannedGroup.AddRange(ListScannedGroup);
                    }
                    result = PrepareXml<ScanDocumentReports>(objScannedReports);

                }
                else if (string.IsNullOrWhiteSpace(error))
                    error = "Invalid token value!";
            }
            catch (Exception ex)
            {
                WriteLog("GetScannedList: ", ex);
                error = ex.Message;
            }
            WriteLog("In GetScannedList: ResponseResult : " + result.ToString());
            return SetToXml(result, error);
        }
        [AcceptVerbs("GET")]
        public async Task<HttpResponseMessage> GetScannedDetails(string pmsPatientId, string pmsEncounterId, string pmsReferenceId)
        {
            int actualPracticeID = 0;
            string error = string.Empty;
            string result = string.Empty;
            string authentication = GetAuthorizationToken();
            string pho = string.Empty;

            try
            {

                string practiceid = string.Empty;
                pmsEncounterId = GetBase64Value(pmsEncounterId);
                pmsPatientId = GetBase64Value(pmsPatientId);
                if (!string.IsNullOrEmpty(pmsEncounterId) && pmsEncounterId.Contains("_"))
                {
                    string Delimiter = "__";
                    string[] splitEncounter = pmsEncounterId.Split(new[] { Delimiter }, StringSplitOptions.None);
                    if (splitEncounter.Length == 1) { splitEncounter = pmsEncounterId.Split('_'); }
                    if (splitEncounter.Length > 0)
                    {
                        pmsEncounterId = GetDcrptValue(splitEncounter[0]);
                        practiceid = "_" + splitEncounter[1];
                        actualPracticeID = Convert.ToInt32(splitEncounter[1]);
                        if (splitEncounter.Length > 2)
                        {
                            practiceid += "_" + splitEncounter[2];
                        }
                        if (splitEncounter.Length > 3)
                        {
                            practiceid = "_" + splitEncounter[3];
                            pho = splitEncounter[3];
                        }
                    }
                }

                if (ConfigurationManager.AppSettings["EnableAzureERMSAPI"] == "1" && !string.IsNullOrEmpty(pho) && pho.ToLower().Contains("azure"))
                    return await ERMSAPIProxy.ForwardtoERMSAzureAPIGET(Request, "/api/GetScannedDetails");

                pmsPatientId = GetDcrptValue(pmsPatientId);
                WriteLog("In GetScannedDetails: PatientId: " + pmsPatientId + " ---- EncounterId: " + pmsEncounterId + " ---- Practiceid: " + practiceid + " ---- ReferenceId: " + pmsReferenceId);
                if (HSSDA.InsertAndValidateToken(pmsPatientId, pmsEncounterId, practiceid, authentication, out error))
                {
                    ScanReportContent objScanReportContent = new ScanReportContent();
                    List<ScannedGroup> ListDischargeSummaryContent = ERMSDataTableToListHiso<ScannedGroup>(HSSDA.GetDocResults(pmsReferenceId, string.Empty, false, practiceid, actualPracticeID, out error));

                    if (ListDischargeSummaryContent.Count > 0 &&
                       !string.IsNullOrWhiteSpace(ListDischargeSummaryContent[0].Content.Text))
                    {
                        objScanReportContent.ScannedGroup.Add(ListDischargeSummaryContent[0]);
                    }
                    else
                        objScanReportContent.ScannedGroup.Add(new ScannedGroup());

                    result = PrepareXml<ScanReportContent>(objScanReportContent);
                }
                else if (string.IsNullOrWhiteSpace(error))
                    error = "Invalid token value!";
            }
            catch (Exception ex)
            {
                WriteLog("GetDischargeSummaryDetails: ", ex);
                error = ex.Message;
            }
            WriteLog("In GetScannedDetails: ResponseResult : " + result.ToString());
            return SetToXml(result, error);
        }
        [AcceptVerbs("GET")]
        public async Task<HttpResponseMessage> GetSmokingStatus(string pmsPatientId, string pmsEncounterId)
        {
            string error = string.Empty;
            string result = string.Empty;
            string authentication = GetAuthorizationToken();
            string pho = string.Empty;

            try
            {

                string practiceid = string.Empty;
                pmsEncounterId = GetBase64Value(pmsEncounterId);
                pmsPatientId = GetBase64Value(pmsPatientId);
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
                            practiceid += "_" + splitEncounter[2];
                        }
                        if (splitEncounter.Length > 3)
                        {
                            practiceid = "_" + splitEncounter[3];
                            pho = splitEncounter[3];
                        }
                    }
                }

                if (ConfigurationManager.AppSettings["EnableAzureERMSAPI"] == "1" && !string.IsNullOrEmpty(pho) && pho.ToLower().Contains("azure"))
                    return await ERMSAPIProxy.ForwardtoERMSAzureAPIGET(Request, "/api/GetSmokingStatus");

                pmsPatientId = GetDcrptValue(pmsPatientId);
                WriteLog("In GetSmokingStatus: PatientId: " + pmsPatientId + " ---- EncounterId: " + pmsEncounterId + " ---- Practiceid: " + practiceid);
                if (HSSDA.InsertAndValidateToken(pmsPatientId, pmsEncounterId, practiceid, authentication, out error))
                {
                    SmokingStatus objSmokingStatus = new SmokingStatus();
                    List<Smoking> ListSmoking = ERMSDataTableToListHiso<Smoking>(HSSDA.GetSmokingStatus(pmsPatientId, practiceid, out error));

                    objSmokingStatus.Smoking.AddRange(ListSmoking);
                    result = PrepareXml<SmokingStatus>(objSmokingStatus);
                }
                else if (string.IsNullOrWhiteSpace(error))
                    error = "Invalid token value!";
            }
            catch (Exception ex)
            {
                WriteLog("GetSmokingStatus: ", ex);
                error = ex.Message;
            }
            WriteLog("In GetSmokingStatus: ResponseResult : " + result.ToString());
            return SetToXml(result, error);
        }
        [AcceptVerbs("POST")]
        public async Task<HttpResponseMessage> SaveDocument()
        {
            string result = string.Empty;
            string error = string.Empty;
            string authentication = GetAuthorizationToken();
            DateTime dtResult = DateTime.MinValue;
            string pho = string.Empty;

            //WriteLog("In SaveDocument");

            XmlSerializer serializer = new XmlSerializer(typeof(ReferralDocument));
            ReferralDocument objDocument = new Models.APIModels.ReferralDocument();

            try
            {
                result = await Request.Content.ReadAsStringAsync();
                WriteLog("DEBUG: SaveDocument: " + result);

                using (StringReader reader = new StringReader(result))
                {
                    objDocument = (ReferralDocument)serializer.Deserialize(reader);

                }
                string practiceid = string.Empty;
                string practiceid2 = string.Empty;
                objDocument.PatiendID = GetBase64Value(objDocument.PatiendID);
                objDocument.EncounterID = GetBase64Value(objDocument.EncounterID);
                objDocument.ProviderID = GetBase64Value(objDocument.ProviderID);
                if (!string.IsNullOrEmpty(objDocument.EncounterID) && objDocument.EncounterID.Contains("_"))
                {
                    string Delimiter = "__";
                    string[] splitEncounter = objDocument.EncounterID.Split(new[] { Delimiter }, StringSplitOptions.None);
                    if (splitEncounter.Length == 1) { splitEncounter = objDocument.EncounterID.Split('_'); }
                    if (splitEncounter.Length > 0)
                    {
                        objDocument.EncounterID = GetDcrptValue(splitEncounter[0]);
                        practiceid = "_" + splitEncounter[1];
                        if (splitEncounter.Length > 2)
                        {
                            practiceid += "_" + splitEncounter[2];
                        }
                        if (splitEncounter.Length > 3)
                        {
                            practiceid = "_" + splitEncounter[3];
                            pho = splitEncounter[3];
                        }
                        practiceid2 = splitEncounter[1];
                    }
                }

                if (ConfigurationManager.AppSettings["EnableAzureERMSAPI"] == "1" && !string.IsNullOrEmpty(pho) && pho.ToLower().Contains("azure"))
                    return await ERMSAPIProxy.ForwardtoERMSAzureAPIPOST(Request, "/api/SaveDocument");


                objDocument.PatiendID = GetDcrptValue(objDocument.PatiendID);
                objDocument.ProviderID = GetDcrptValue(objDocument.ProviderID);
                WriteLog("In SaveDocument: PatientId: " + objDocument.PatiendID + " ---- EncounterId: " + objDocument.EncounterID + " ---- Practiceid: " + practiceid + " ---- Userid:" + objDocument.ProviderID);
                if (HSSDA.InsertAndValidateToken(objDocument.PatiendID, objDocument.EncounterID, practiceid, authentication, out error))
                {
                    string dmsGuidKey = string.Empty;
                    string referralId = string.Empty;
                    byte[] base64Value = Convert.FromBase64String(objDocument.Content);

                    if (base64Value.Length > 0
                        && !string.IsNullOrWhiteSpace(objDocument.ContentType)
                        && !string.IsNullOrWhiteSpace(objDocument.Type))
                    {
                        referralId = objDocument.ID + "_" + objDocument.DocumentID;
                        //DataTable dtDMS = HSSDA.GetDocResults(string.Empty, referralId, false, out error);

                        //if (dtDMS.Rows.Count > 0
                        //&& string.IsNullOrWhiteSpace(Utility.Instance.ToString(dtDMS.Rows[0]["DMSID"])))
                        //{
                        //    HSSDA.DocumentDelete(Utility.Instance.ToString(dtDMS.Rows[0]["DMSID"]), out error);
                        //}

                        HSSDA.UpdateExistingDocument(referralId, practiceid, practiceid2, out error);
                        WriteLog("DEBUG: SaveDocument: UpdateExistingDocument out error: " + error);
                        dmsGuidKey = SaveToDMS(base64Value, objDocument.ContentType, objDocument.Type,
                                                    objDocument.ItemType.ToLower().Equals("in") ? 17 : 18, referralId, practiceid, practiceid2);
                        WriteLog("DEBUG: SaveDocument: SaveToDMS DMSID: " + dmsGuidKey);

                    }
                    else
                    {
                        error = "Invalid passed values!";
                    }

                    if (!string.IsNullOrWhiteSpace(dmsGuidKey))
                    {
                        DateTime.TryParse(objDocument.CreatedDate, out dtResult);
                        HSSDA.InsertDocument(objDocument.PatiendID, objDocument.EncounterID, objDocument.Type, dmsGuidKey,
                                             objDocument.ItemType.ToLower().Equals("in") ? 1 : 2, dtResult, "23", referralId, objDocument.ProviderID, practiceid, out error);
                        WriteLog("DEBUG: SaveDocument: InsertDocument out error: " + error);
                    }
                }
                else if (string.IsNullOrEmpty(error))
                    error = "Invalid token value!";
            }
            catch (Exception ex)
            {
                WriteLog("SaveDocument: ", ex);
                error = ex.Message;
            }
            finally
            {
                objDocument.ErrorText = error;
                objDocument.ContentType = string.Empty;
                objDocument.DescriptionType = string.Empty;
                objDocument.Description = string.Empty;
                objDocument.Encoding = string.Empty;
                objDocument.Source = string.Empty;
                objDocument.Content = string.Empty;
            }

            using (StringWriter writer = new StringWriter())
            {
                if (!string.IsNullOrEmpty(error))
                {
                    var res = Request.CreateResponse(HttpStatusCode.BadRequest);
                    res.Content = new StringContent(HttpStatusCode.BadRequest.ToString(), System.Text.Encoding.UTF8, "application/xml");
                    return res;
                }
                else
                {
                    serializer.Serialize(writer, objDocument);
                    result = writer.ToString();
                }
            };
            WriteLog("In SaveDocument: ResponseResult : " + result.ToString());
            return SetToXml(result, string.Empty);
        }
        private HttpResponseMessage SetToXml(string xmlString, string error)
        {
            if (!string.IsNullOrWhiteSpace(error))
                xmlString = "<?xml version=\"1.0\" encoding=\"utf-16\"?>" +
                            "<Error><Message>" + error + "</Message></Error>";

            var response = new HttpResponseMessage(HttpStatusCode.OK);
            response.Content = new StringContent(xmlString, Encoding.UTF8, "application/xml");

            return response;
        }
        private string PrepareXml<T>(T objRoot)
        {
            string result = string.Empty;
            XmlSerializer xmlSerialize = new XmlSerializer(typeof(T));

            try
            {
                using (StringWriter sw = new StringWriter())
                {
                    using (XmlWriter xm = XmlWriter.Create(sw))
                    {
                        xmlSerialize.Serialize(xm, objRoot);
                        result = sw.ToString();
                    }
                }
            }
            catch
            {
                throw;
            }

            return result;
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
        private string SaveToDMS(byte[] contentData, string postedType, string messageSubject, int categoryId, string description, string practiceid, string practiceid2)
        {
            string documentKey = Guid.NewGuid().ToString();
            string error = string.Empty;

            try
            {
                if (postedType.Contains("/"))
                    postedType = Utility.Instance.GetBetweenString("/", ";", postedType, false).Trim();

                int documentTypeID = DocumentTypeID(postedType);
                Int64 DMSID = HSSDA.DocumentSave(categoryId, messageSubject, documentTypeID, description, documentKey, contentData, practiceid, practiceid2, out error);

                if (!string.IsNullOrWhiteSpace(error))
                    throw new Exception(error);
                else if (DMSID <= 0)
                    documentKey = string.Empty;
            }
            catch
            {
                throw;
            }

            return documentKey;
        }
        private int DocumentTypeID(string postedType)
        {
            int id = -1;

            try
            {
                string[] docTypes = Utility.Instance.ToString(ConfigurationManager.AppSettings["DMSDocTypes"]).ToLower().Split('|');
                string[] results = Array.FindAll(docTypes, s => s.Contains(postedType.ToLower()));
                id = Convert.ToInt32(results[0].Split(',')[0]);
            }
            catch { }

            return id;
        }
        public List<T> ERMSDataTableToListHiso<T>(DataTable table) where T : class, new()
        {
            try
            {
                List<T> list = new List<T>();
                Type typeMain = typeof(T);

                foreach (var row in table.AsEnumerable())
                {
                    var mainInstance = Activator.CreateInstance(typeMain);
                    foreach (var prop in mainInstance.GetType().GetProperties())
                    {
                        try
                        {
                            string conceptId = string.Empty;
                            string text = string.Empty;
                            string name = string.Empty;
                            string qualifierId = string.Empty;
                            string qualifierName = string.Empty;
                            string dateTaken = string.Empty;
                            if (!(table.Columns.Contains(prop.Name)))
                                continue;
                            if (string.IsNullOrEmpty(row.Field<string>(prop.Name)))
                                continue;
                            string[] retVal = row.Field<string>(prop.Name).Split(new string[] { "|&|" }, StringSplitOptions.RemoveEmptyEntries);

                            if (retVal.Length > 0)
                            {
                                conceptId = retVal[0];
                                text = retVal.Length > 1 ? retVal[1] : text;

                                if (text.Contains("|?|"))
                                {
                                    string[] innerVal = text.Split(new string[] { "|?|" }, StringSplitOptions.RemoveEmptyEntries);
                                    text = Utility.Instance.ToString(innerVal[0]);

                                    try
                                    {
                                        name = Utility.Instance.ToString(innerVal[1]);
                                        qualifierId = Utility.Instance.ToString(innerVal[2]);
                                        qualifierName = Utility.Instance.ToString(innerVal[3]);
                                        dateTaken = Utility.Instance.ToString(innerVal[4]);
                                    }
                                    catch (IndexOutOfRangeException)
                                    { }
                                }
                            }

                            if (prop.Name.Equals("ReferenceId", StringComparison.OrdinalIgnoreCase))
                            {
                                typeMain.GetProperty("ReferenceId").SetValue(mainInstance, text);
                                typeMain.GetProperty("ConceptID").SetValue(mainInstance, conceptId);
                            }
                            else if (prop.Name.Equals("ConceptID", StringComparison.OrdinalIgnoreCase))
                            {

                                typeMain.GetProperty("ConceptID").SetValue(mainInstance, conceptId);
                            }
                            else
                            {
                                Type nested = prop.PropertyType;
                                var nestedProperty = Activator.CreateInstance(nested);
                                nested.GetProperty("ConceptID").SetValue(nestedProperty, conceptId);

                                if (!string.IsNullOrWhiteSpace(text))
                                    nested.GetProperty("Text").SetValue(nestedProperty, text);

                                if (!string.IsNullOrWhiteSpace(name))
                                    nested.GetProperty("Name").SetValue(nestedProperty, name);

                                if (!string.IsNullOrWhiteSpace(qualifierId))
                                    nested.GetProperty("QualifierID").SetValue(nestedProperty, qualifierId);

                                if (!string.IsNullOrWhiteSpace(qualifierName))
                                    nested.GetProperty("QualifierName").SetValue(nestedProperty, qualifierName);

                                if (!string.IsNullOrWhiteSpace(dateTaken))
                                    nested.GetProperty("DateTaken").SetValue(nestedProperty, dateTaken);

                                typeMain.GetProperty(prop.Name).SetValue(mainInstance, nestedProperty);
                            }
                        }
                        catch
                        {
                            continue;
                        }
                    }

                    list.Add((T)mainInstance);
                }

                return list;
            }
            catch
            {
                return null;
            }
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
        public string GetBase64Value(string value)
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
                    byte[] temEnc = Convert.FromBase64String(value);
                    return System.Text.Encoding.UTF8.GetString(temEnc);
                }

            }
            catch (Exception ex)
            {
                return value;

            }
        }
    }
}
