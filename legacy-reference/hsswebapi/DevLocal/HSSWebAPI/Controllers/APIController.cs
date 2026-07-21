using DAL;
using Logger;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json.Serialization;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using System.Web;
using System.Web.Http;
using System.Web.Http.Cors;
using static HSSWebAPI.Models.APIModels;

namespace HSSWebAPI.Controllers
{
    [EnableCors(origins: "*", headers: "*", methods: "*")]
    public class APIController : ApiController
    {
        [AcceptVerbs("GET")]
        public HttpResponseMessage Ping()
        {
            return SetToJson("{\"status\":\"success\"}", string.Empty);
        }
        [AcceptVerbs("GET")]
        public HttpResponseMessage Authenticate(string username, string password, string patientId, string encounterId, string system, string pho)
        {
            string error = string.Empty;
            string result = string.Empty;

            DataTable dtResults = new DataTable();

            try
            {

                string practiceid = string.Empty;
                if (!string.IsNullOrEmpty(encounterId) && encounterId.Contains("_"))
                {
                    string Delimiter = "__";
                    string[] splitEncounter = encounterId.Split(new[] { Delimiter }, StringSplitOptions.None);
                    if (splitEncounter.Length == 1) { splitEncounter = encounterId.Split('_'); }
                    if (splitEncounter.Length > 0)
                    {
                        encounterId = GetDcrptValue(splitEncounter[0]);
                        practiceid = "_" + splitEncounter[1];
                        if (splitEncounter.Length > 2)
                        {
                            practiceid += "_" + splitEncounter[2];
                        }
                        if (splitEncounter.Length > 3)
                        {
                            practiceid = "_" + splitEncounter[3];
                        }
                    }
                }
                patientId = GetDcrptValue(patientId);
                WriteLog("In Authenticate: PatientId/EncounterId/practiceid/Username/Password/system/pho >> " + patientId + "/" + encounterId + "/" + practiceid + "/" + username + "/" + password + "/" + system + "/" + pho);
                dtResults = HSSDA.InsertAndValidateToken(username, password, patientId, encounterId, practiceid, string.Empty, pho, out error);

                if (dtResults.Rows.Count > 0
                    && string.IsNullOrWhiteSpace(Utility.Instance.ToString(dtResults.Rows[0]["StatusMessage"])))
                {
                    DateTime dtExpiry = DateTime.MinValue;
                    DateTime.TryParse(Utility.Instance.ToString(dtResults.Rows[0]["Expiry"]), out dtExpiry);

                    if (!dtExpiry.Equals(DateTime.MinValue))
                    {
                        result = "{\"status\": \"success\",\"token\": \""
                                + Utility.Instance.ToString(dtResults.Rows[0]["Token"]) + "\",\"expiry\": \""
                                + String.Format("{0:s}", dtExpiry) + "\",\"practiceId\":\""
                                + Utility.Instance.ToString(dtResults.Rows[0]["PracticeId"]) + "\"}";
                    }
                    else
                        error = "Invalid credentials!";
                }
                else
                    error = "Authentication failed!";
            }
            catch (Exception ex)
            {
                WriteLog("Authenticate: ", ex);
                error = ex.Message;
            }

            return SetToJson(result, error);
        }
        [AcceptVerbs("POST")]
        public async Task<HttpResponseMessage> Authenticate()
        {
            string error = string.Empty;
            string result = string.Empty;
            string practiceid = string.Empty;
            DataTable dtResults = new DataTable();

            try
            {
                result = await Request.Content.ReadAsStringAsync();
                Credential objCredentials = JsonConvert.DeserializeObject<Credential>(result);

                //WriteLog("In Authenticate: PatientId/EncounterId/Username/password/system/pho >> " + objCredentials.PatientId + "/" + objCredentials.EncounterId + "/" + objCredentials.Username + "/" + objCredentials.Password + "/" + objCredentials.System + "/" + objCredentials.Pho);
                WriteLog("In Authenticate:" + result);
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
                        }
                    }
                    else
                    {
                        objCredentials.EncounterId = GetDcrptValue(objCredentials.EncounterId);
                    }
                }
                objCredentials.PatientId = GetDcrptValue(objCredentials.PatientId);
                WriteLog("In Authenticate: PatientId/EncounterId/Username/password/system/pho >> " + objCredentials.PatientId + "/" + objCredentials.EncounterId + "/" + objCredentials.Username + "/" + objCredentials.Password + "/" + objCredentials.System + "/" + objCredentials.Pho);
                dtResults = HSSDA.InsertAndValidateToken(objCredentials.Username, objCredentials.Password, objCredentials.PatientId, objCredentials.EncounterId, practiceid, string.Empty, objCredentials.Pho, out error);

                if (dtResults.Rows.Count > 0
                    && string.IsNullOrWhiteSpace(Utility.Instance.ToString(dtResults.Rows[0]["StatusMessage"])))
                {
                    DateTime dtExpiry = DateTime.MinValue;
                    DateTime.TryParse(Utility.Instance.ToString(dtResults.Rows[0]["Expiry"]), out dtExpiry);

                    if (!dtExpiry.Equals(DateTime.MinValue))
                    {
                        result = "{\"status\": \"success\",\"token\": \""
                                + Utility.Instance.ToString(dtResults.Rows[0]["Token"]) + "\",\"expiry\": \""
                                + String.Format("{0:s}", dtExpiry) + "\",\"practiceId\":\""
                                + Utility.Instance.ToString(dtResults.Rows[0]["PracticeId"]) + "\"}";
                    }
                    else
                        error = "Invalid credentials!";
                }
                else
                    error = "Authentication failed!";
                WriteLog("In Authenticate:" + result);
            }
            catch (Exception ex)
            {
                WriteLog("Authenticate: " + result, ex);
                error = ex.Message;
            }

            return SetToJson(result, error);
        }
        [AcceptVerbs("GET")]
        public HttpResponseMessage GetClinicalNotes(string system, string pho, string patientId, string encounterId)
        {
            string error = string.Empty;
            string result = string.Empty;
            string authentication = GetAuthorizationToken();

            WriteLog("In GetClinicalNotes");

            try
            {

                string practiceid = string.Empty;
                if (!string.IsNullOrEmpty(encounterId) && encounterId.Contains("_"))
                {
                    string Delimiter = "__";
                    string[] splitEncounter = encounterId.Split(new[] { Delimiter }, StringSplitOptions.None);
                    if (splitEncounter.Length == 1) { splitEncounter = encounterId.Split('_'); }
                    if (splitEncounter.Length > 0)
                    {
                        encounterId = GetDcrptValue(splitEncounter[0]);
                        practiceid = "_" + splitEncounter[1];
                        if (splitEncounter.Length > 2)
                        {
                            practiceid += "_" + splitEncounter[2];
                        }
                        if (splitEncounter.Length > 3)
                        {
                            practiceid = "_" + splitEncounter[3];
                        }
                    }
                }
                patientId = GetDcrptValue(patientId);
                WriteLog("In GetClinicalNotes: PatientId: " + patientId + "--EncounterId:" + encounterId + "--Practiceid:" + practiceid + " --system: " + system + "--pho:" + pho);
                if (HSSDA.InsertAndValidateToken(patientId, encounterId, authentication, practiceid, pho, out error))
                {
                    Root<ConsultNotes> objRoot = new Root<ConsultNotes>("Notes", "hss");
                    objRoot.PatientId = patientId;

                    List<ConsultNotes> ListConsultNotes = Utility.Instance.DataTableToList<ConsultNotes>(HSSDA.GetConsultNotes(patientId, practiceid, out error));
                    objRoot.Entry.AddRange(ListConsultNotes);

                    result = PrepareJSON(objRoot);
                }
                else if (string.IsNullOrEmpty(error))
                    error = "Invalid token value!";
            }
            catch (Exception ex)
            {
                WriteLog("GetClinicalNotes: ", ex);
                error = ex.Message;
            }

            return SetToJson(result, error);
        }
        [AcceptVerbs("GET")]
        public HttpResponseMessage GetConditions(string system, string pho, string patientId, string encounterId)
        {
            string error = string.Empty;
            string result = string.Empty;
            string authentication = GetAuthorizationToken();

            WriteLog("In GetConditions");

            try
            {
                WriteLog("In GetConditions Request:PatientID:" + patientId + "EncounterID:" + encounterId);
                string practiceid = string.Empty;
                if (!string.IsNullOrEmpty(encounterId) && encounterId.Contains("_"))
                {
                    string Delimiter = "__";
                    string[] splitEncounter = encounterId.Split(new[] { Delimiter }, StringSplitOptions.None);
                    if (splitEncounter.Length == 1) { splitEncounter = encounterId.Split('_'); }
                    if (splitEncounter.Length > 0)
                    {
                        encounterId = GetDcrptValue(splitEncounter[0]);
                        practiceid = "_" + splitEncounter[1];
                        if (splitEncounter.Length > 2)
                        {
                            practiceid += "_" + splitEncounter[2];
                        }
                        if (splitEncounter.Length > 3)
                        {
                            practiceid = "_" + splitEncounter[3];
                        }
                    }
                }
                patientId = GetDcrptValue(patientId);
                WriteLog("In GetConditions: PatientId: " + patientId + "--EncounterId:" + encounterId + "--Practiceid:" + practiceid + " --system: " + system + "--pho:" + pho);
                if (HSSDA.InsertAndValidateToken(patientId, encounterId, authentication, practiceid, pho, out error))
                {
                    Root<Diagnosis> objRoot = new Root<Diagnosis>("Conditions", "hss");
                    objRoot.PatientId = patientId;

                    List<Diagnosis> ListDiagnosis = Utility.Instance.DataTableToList<Diagnosis>(HSSDA.GetConditions(patientId, practiceid, out error));
                    objRoot.Entry.AddRange(ListDiagnosis);

                    result = PrepareJSON(objRoot);
                }
                else if (string.IsNullOrEmpty(error))
                    error = "Invalid token value!";
            }
            catch (Exception ex)
            {
                WriteLog("GetConditions: ", ex);
                error = ex.Message;
            }
            WriteLog("In GetConditions: " + result);
            return SetToJson(result, error);
        }
        [AcceptVerbs("GET")]
        public HttpResponseMessage GetDemographics(string system, string pho, string patientId, string encounterId)
        {
            string error = string.Empty;
            string result = string.Empty;
            string authentication = GetAuthorizationToken();

            WriteLog("In GetDemographics");

            try
            {
                WriteLog("In GetDemographics: PatientId: " + patientId + "--EncounterId:" + encounterId + " --system: " + system + "--pho:" + pho);
                string practiceid = string.Empty;
                if (!string.IsNullOrEmpty(encounterId) && encounterId.Contains("_"))
                {
                    string Delimiter = "__";
                    string[] splitEncounter = encounterId.Split(new[] { Delimiter }, StringSplitOptions.None);
                    if (splitEncounter.Length == 1) { splitEncounter = encounterId.Split('_'); }
                    if (splitEncounter.Length > 0)
                    {
                        encounterId = GetDcrptValue(splitEncounter[0]);
                        practiceid = "_" + splitEncounter[1];
                        if (splitEncounter.Length > 2)
                        {
                            practiceid += "_" + splitEncounter[2];
                        }
                        if (splitEncounter.Length > 3)
                        {
                            practiceid = "_" + splitEncounter[3];
                        }
                    }
                }
                patientId = GetDcrptValue(patientId);
                WriteLog("In GetDemographics: PatientId: " + patientId + "--EncounterId:" + encounterId + "--Practiceid:" + practiceid + " --system: " + system + "--pho:" + pho);
                if (HSSDA.InsertAndValidateToken(patientId, encounterId, authentication, practiceid, pho, out error))
                {
                    Root<Demographic> objRoot = new Root<Demographic>("Patient", "hss");
                    objRoot.PatientId = patientId;
                    DataSet ds = new DataSet();
                    Demographic DemoInfo = new Demographic();
                    List<DemographicInfo> ListDemographic = new List<DemographicInfo>();
                    List<CardInfo> ListCardInfo = new List<CardInfo>();

                    ds = HSSDA.GetHSSDemographics(patientId, practiceid, out error);
                    if (ds.Tables[0].Rows.Count > 0)
                        ListDemographic = Utility.Instance.DataTableToList<DemographicInfo>(ds.Tables[0]);

                    if (ListDemographic.Count > 0)
                        DemoInfo.ListDemographicInfo.Add(ListDemographic[0]);

                    if (ds.Tables[1].Rows.Count > 0)
                        ListCardInfo = Utility.Instance.DataTableToList<CardInfo>(ds.Tables[1]);

                    if (ListCardInfo.Count > 0)
                    {
                        if (!string.IsNullOrEmpty(ListCardInfo[0].cardtype.ToString()))
                            DemoInfo.Listcardtype.Add(ListCardInfo[0]);
                        if (!string.IsNullOrEmpty(ListCardInfo[1].cardtype.ToString()))
                            DemoInfo.Listcardtype.Add(ListCardInfo[1]);
                    }
                    if (DemoInfo != null)
                        objRoot.Entry.Add(DemoInfo);

                    result = PrepareJSON(objRoot);
                }
                else if (string.IsNullOrEmpty(error))
                    error = "Invalid token value!";
            }
            catch (Exception ex)
            {
                WriteLog("GetDemographics: ", ex);
                error = ex.Message;
            }
            WriteLog("In GetDemographics: " + result);
            return SetToJson(result, error);
        }
        [AcceptVerbs("GET")]
        public HttpResponseMessage GetDocuments(string system, string pho, string patientId, string encounterId, string identifier = "")
        {
            string error = string.Empty;
            string result = string.Empty;
            string authentication = GetAuthorizationToken();

            WriteLog("In GetDocuments");

            try
            {
                //WriteLog("In GetDocuments: PatientId: " + patientId + "--EncounterId:" + encounterId + " --system: " + system + "--pho:" + pho);
                string practiceid = string.Empty;
                int actualPracticeID = 0;
                if (!string.IsNullOrEmpty(encounterId) && encounterId.Contains("_"))
                {
                    string Delimiter = "__";
                    string[] splitEncounter = encounterId.Split(new[] { Delimiter }, StringSplitOptions.None);
                    if (splitEncounter.Length == 1) { splitEncounter = encounterId.Split('_'); }
                    if (splitEncounter.Length > 0)
                    {
                        encounterId = GetDcrptValue(splitEncounter[0]);
                        practiceid = "_" + splitEncounter[1];
                        actualPracticeID = Convert.ToInt32(splitEncounter[1]);
                        if (splitEncounter.Length > 2)
                        {
                            practiceid += "_" + splitEncounter[2];
                        }
                        if (splitEncounter.Length > 3)
                        {
                            practiceid = "_" + splitEncounter[3];
                        }
                    }
                }
                patientId = GetDcrptValue(patientId);
                WriteLog("In GetDocuments: PatientId: " + patientId + "--EncounterId:" + encounterId + "--Practiceid:" + practiceid + " --system: " + system + "--pho:" + pho + "--Actual PracticeID:" + actualPracticeID) ;
                if (HSSDA.InsertAndValidateToken(patientId, encounterId, authentication, practiceid, pho, out error))
                {
                    Root<Documents> objRoot = new Root<Documents>("Documents", "hss");
                    objRoot.PatientId = patientId;

                    List<Documents> ListDocuments = Utility.Instance.DataTableToList<Documents>(HSSDA.GetDocuments(patientId, identifier, practiceid, actualPracticeID,
                                                                                                                   out error));
                    objRoot.Entry.AddRange(ListDocuments);
                    result = PrepareJSON(objRoot);
                }
                else if (string.IsNullOrEmpty(error))
                    error = "Invalid token value!";
            }
            catch (Exception ex)
            {
                WriteLog("GetDocuments: ", ex);
                error = ex.Message;
            }

            return SetToJson(result, error);
        }
        [AcceptVerbs("GET")]
        public HttpResponseMessage GetEncounterSummary(string system, string pho, string patientId, string encounterId, string identifier)
        {
            string result = string.Empty;
            string error = string.Empty;
            string authentication = GetAuthorizationToken();

            WriteLog("In GetEncounterSummary");
            //WriteLog("In GetEncounterSummary: PatientId: " + patientId + "--EncounterId:" + encounterId + " --system: " + system + "--pho:" + pho);
            string practiceid = string.Empty;
            if (!string.IsNullOrEmpty(encounterId) && encounterId.Contains("_"))
            {
                string Delimiter = "__";
                string[] splitEncounter = encounterId.Split(new[] { Delimiter }, StringSplitOptions.None);
                if (splitEncounter.Length == 1) { splitEncounter = encounterId.Split('_'); }
                if (splitEncounter.Length > 0)
                {
                    encounterId = GetDcrptValue(splitEncounter[0]);
                    practiceid = "_" + splitEncounter[1];
                    if (splitEncounter.Length > 2)
                    {
                        practiceid += "_" + splitEncounter[2];
                    }
                    if (splitEncounter.Length > 3)
                    {
                        practiceid = "_" + splitEncounter[3];
                    }
                }
            }
            patientId = GetDcrptValue(patientId);
            WriteLog("In GetEncounterSummary: PatientId: " + patientId + "--EncounterId:" + encounterId + "--Practiceid:" + practiceid + " --system: " + system + "--pho:" + pho);
            if (HSSDA.InsertAndValidateToken(patientId, encounterId, authentication, practiceid, pho, out error))
            {
                if (identifier.ToLower().Equals("diap"))
                    result = "{\"patientId\":\"941272\",\"resourceType\":\"EncounterSummary\",\"identifier\":\"DIAP\",\"dateTimeRecorded\":\"2016-05-23T13:30:00\",\"entry\":[{\"DIAP_OUTCOME\":\"D\",\"DIAP_TYPE\":\"Type 1\",\"DIAP_YOD\":2016,\"DIAP_HEIGHT\":175,\"DIAP_WEIGHT\":75,\"DIAP_RETINAL\":2015,\"DIAP_BP_SYS\":120,\"DIAP_BP_DIA\":80,\"DIAP_HBA1C\":50,\"DIAP_ALBUMIN\":45,\"DIAP_DIPSTICK\":\"Positive\",\"DIAP_CHOL\":5,\"DIAP_HDL\":7,\"DIAP_TRIG\":9,\"DIAP_ETHNICITY\":11,\"DIAP_NHI\":\"NZZ3100\",\"DIAP_SMOKER\":\"Recently Quit\",\"DIAP_CONSENT\":true,\"DIAP_MAILING\":false,\"DIAP_INSULIN\":\"Yes\",\"DIAP_ORAL\":\"No\",\"DIAP_DIET\":\"Yes\",\"DIAP_ACE\":\"No\",\"DIAP_BBLOCKER\":\"Yes\",\"DIAP_STATIN\":\"No\",\"DIAP_OTHERMEDS\":\"Yes\",\"DIAP_FOOT\":\"No\",\"DIAP_PVD\":\"No\",\"DIAP_FOOD\":\"Yes\",\"DIAP_ACTIVITY\":\"Yes\",\"DIAP_RISK\":\"Moderate\"}]}";
                else if (identifier.ToLower().Equals("cvra"))
                    result = "{\"patientId\":\"941272\",\"resourceType\":\"EncounterSummary\",\"identifier\":\"CVRA\",\"dateTimeRecorded\":\"2016-05-23T13:30:00\",\"entry\":[{\"Risk\":\"20\",\"Framingham\":\"30\"}]}";
                else
                    result = "{}";
            }
            else
                result = "{\"status\":\"fail\",\"message\":\"Invalid token value!\"}";

            return SetToJson(result, error);
        }
        [AcceptVerbs("GET")]
        public HttpResponseMessage GetLabResults(string system, string pho, string patientId, string encounterId)
        {
            string error = string.Empty;
            string result = string.Empty;
            string authentication = GetAuthorizationToken();

            WriteLog("In GetLabResults");

            try
            {
                //WriteLog("In GetLabResults: PatientId: " + patientId + "--EncounterId:" + encounterId + " --system: " + system + "--pho:" + pho);
                string practiceid = string.Empty;
                if (!string.IsNullOrEmpty(encounterId) && encounterId.Contains("_"))
                {
                    string Delimiter = "__";
                    string[] splitEncounter = encounterId.Split(new[] { Delimiter }, StringSplitOptions.None);
                    if (splitEncounter.Length == 1) { splitEncounter = encounterId.Split('_'); }
                    if (splitEncounter.Length > 0)
                    {
                        encounterId = GetDcrptValue(splitEncounter[0]);
                        practiceid = "_" + splitEncounter[1];
                        if (splitEncounter.Length > 2)
                        {
                            practiceid += "_" + splitEncounter[2];
                        }
                        if (splitEncounter.Length > 3)
                        {
                            practiceid = "_" + splitEncounter[3];
                        }
                    }
                }
                patientId = GetDcrptValue(patientId);
                WriteLog("In GetLabResults: PatientId: " + patientId + "--EncounterId:" + encounterId + "--Practiceid:" + practiceid + " --system: " + system + "--pho:" + pho);
                if (HSSDA.InsertAndValidateToken(patientId, encounterId, authentication, practiceid, pho, out error))
                {
                    Root<LabResults> objRoot = new Root<LabResults>("Labs", "hss");
                    objRoot.PatientId = patientId;

                    List<LabResults> ListLabResults = Utility.Instance.DataTableToList<LabResults>(HSSDA.GetLabResults(patientId, practiceid, out error));
                    objRoot.Entry.AddRange(ListLabResults);

                    result = PrepareJSON(objRoot);
                }
                else if (string.IsNullOrEmpty(error))
                    error = "Invalid token value!";
            }
            catch (Exception ex)
            {
                WriteLog("GetLabResults: ", ex);
                error = ex.Message;
            }

            return SetToJson(result, error);
        }
        [AcceptVerbs("GET")]
        public HttpResponseMessage GetMedications(string system, string pho, string patientId, string encounterId)
        {
            string error = string.Empty;
            string result = string.Empty;
            string authentication = GetAuthorizationToken();

            WriteLog("In GetMedications");

            try
            {
                WriteLog("In GetMedications: PatientId: " + patientId + "--EncounterId:" + encounterId + " --system: " + system + "--pho:" + pho);
                string practiceid = string.Empty;
                if (!string.IsNullOrEmpty(encounterId) && encounterId.Contains("_"))
                {
                    string Delimiter = "__";
                    string[] splitEncounter = encounterId.Split(new[] { Delimiter }, StringSplitOptions.None);
                    if (splitEncounter.Length == 1) { splitEncounter = encounterId.Split('_'); }
                    if (splitEncounter.Length > 0)
                    {
                        encounterId = GetDcrptValue(splitEncounter[0]);
                        practiceid = "_" + splitEncounter[1];
                        if (splitEncounter.Length > 2)
                        {
                            practiceid += "_" + splitEncounter[2];
                        }
                        if (splitEncounter.Length > 3)
                        {
                            practiceid = "_" + splitEncounter[3];
                        }
                    }
                }
                patientId = GetDcrptValue(patientId);
                WriteLog("In GetMedications: PatientId: " + patientId + "--EncounterId:" + encounterId + "--Practiceid:" + practiceid + " --system: " + system + "--pho:" + pho);
                if (HSSDA.InsertAndValidateToken(patientId, encounterId, authentication, practiceid, pho, out error))
                {
                    Root<Medications> objRoot = new Root<Medications>("Medications", "hss");
                    objRoot.PatientId = patientId;

                    List<Medications> ListMedications = Utility.Instance.DataTableToList<Medications>(HSSDA.GetMedications(patientId, practiceid, out error));
                    objRoot.Entry.AddRange(ListMedications);

                    result = PrepareJSON(objRoot);
                }
                else if (string.IsNullOrEmpty(error))
                    error = "Invalid token value!";
            }
            catch (Exception ex)
            {
                WriteLog("GetMedications: ", ex);
                error = ex.Message;
            }
            WriteLog("In GetMedications: " + result);
            return SetToJson(result, error);
        }
        [AcceptVerbs("GET")]
        public HttpResponseMessage GetObservations(string system, string pho, string patientId, string encounterId, string conceptId = "")
        {
            string error = string.Empty;
            string result = string.Empty;
            string authentication = GetAuthorizationToken();

            WriteLog("In GetObservations");

            try
            {
                WriteLog("In GetObservations: PatientId: " + patientId + "--EncounterId:" + encounterId + " --system: " + system + "--pho:" + pho);
                string practiceid = string.Empty;
                if (!string.IsNullOrEmpty(encounterId) && encounterId.Contains("_"))
                {
                    string Delimiter = "__";
                    string[] splitEncounter = encounterId.Split(new[] { Delimiter }, StringSplitOptions.None);
                    if (splitEncounter.Length == 1) { splitEncounter = encounterId.Split('_'); }
                    if (splitEncounter.Length > 0)
                    {
                        encounterId = GetDcrptValue(splitEncounter[0]);
                        practiceid = "_" + splitEncounter[1];
                        if (splitEncounter.Length > 2)
                        {
                            practiceid += "_" + splitEncounter[2];
                        }
                        if (splitEncounter.Length > 3)
                        {
                            practiceid = "_" + splitEncounter[3];
                        }
                    }
                }
                patientId = GetDcrptValue(patientId);
                WriteLog("In GetObservations: PatientId: " + patientId + "--EncounterId:" + encounterId + "--Practiceid:" + practiceid + " --system: " + system + "--pho:" + pho);
                if (HSSDA.InsertAndValidateToken(patientId, encounterId, authentication, practiceid, pho, out error))
                {
                    Root<Observation> objRoot = new Root<Observation>("Screening", "hss");
                    objRoot.PatientId = patientId;

                    List<Observation> ListObservation = Utility.Instance.DataTableToList<Observation>(HSSDA.GetObservations(patientId, conceptId, practiceid, out error));
                    objRoot.Entry.AddRange(ListObservation);

                    result = PrepareJSON(objRoot);
                }
                else if (string.IsNullOrEmpty(error))
                    error = "Invalid token value!";
            }
            catch (Exception ex)
            {
                WriteLog("GetObservations: ", ex);
                error = ex.Message;
            }
            WriteLog("In GetObservations: " + result);
            return SetToJson(result, error);
        }
        [AcceptVerbs("GET")]
        public HttpResponseMessage GetProvider(string system, string pho, string patientId, string encounterId, string userId = "")
        {
            string error = string.Empty;
            string result = string.Empty;
            string authentication = GetAuthorizationToken();

            WriteLog("In GetProvider");

            try
            {
                WriteLog("In GetProvider: PatientId: " + patientId + "--EncounterId:" + encounterId + " --system: " + system + "--pho:" + pho);
                string practiceid = string.Empty;
                if (!string.IsNullOrEmpty(encounterId) && encounterId.Contains("_"))
                {
                    string Delimiter = "__";
                    string[] splitEncounter = encounterId.Split(new[] { Delimiter }, StringSplitOptions.None);
                    if (splitEncounter.Length == 1) { splitEncounter = encounterId.Split('_'); }
                    if (splitEncounter.Length > 0)
                    {
                        encounterId = GetDcrptValue(splitEncounter[0]);
                        practiceid = "_" + splitEncounter[1];
                        if (splitEncounter.Length > 2)
                        {
                            practiceid += "_" + splitEncounter[2];
                        }
                        if (splitEncounter.Length > 3)
                        {
                            practiceid = "_" + splitEncounter[3];
                        }
                    }
                }
                patientId = GetDcrptValue(patientId);
                userId = GetDcrptValue(userId);
                WriteLog("In GetProvider: PatientId: " + patientId + "--EncounterId:" + encounterId + "--Practiceid:" + practiceid + " --system: " + system + "--pho:" + pho);
                if (HSSDA.InsertAndValidateToken(patientId, encounterId, authentication, practiceid, pho, out error))
                {
                    Root<Provider> objRoot = new Root<Provider>("Provider", "hss");
                    objRoot.PatientId = patientId;

                    List<Provider> ListProvider = Utility.Instance.DataTableToList<Provider>(HSSDA.GetProvider(patientId, userId, string.Empty, practiceid, out error));

                    if (ListProvider.Count > 0)
                        objRoot.Entry.Add(ListProvider[0]);

                    result = PrepareJSON(objRoot);
                }
                else if (string.IsNullOrEmpty(error))
                    error = "Invalid token value!";
            }
            catch (Exception ex)
            {
                WriteLog("GetProvider: ", ex);
                error = ex.Message;
            }
            WriteLog("In GetProviders: " + result);
            return SetToJson(result, error);
        }
        [AcceptVerbs("GET")]
        public HttpResponseMessage GetRecallCategories(string system, string pho, string patientId, string encounterId, string group)
        {
            string error = string.Empty;
            string result = string.Empty;
            string authentication = GetAuthorizationToken();

            WriteLog("In GetRecallCategories");

            try
            {
                //WriteLog("In GetRecallCategories: PatientId: " + patientId + "--EncounterId:" + encounterId + " --system: " + system + "--pho:" + pho);
                string practiceid = string.Empty;
                string practiceid2 = string.Empty;
                if (!string.IsNullOrEmpty(encounterId) && encounterId.Contains("_"))
                {
                    string Delimiter = "__";
                    string[] splitEncounter = encounterId.Split(new[] { Delimiter }, StringSplitOptions.None);
                    if (splitEncounter.Length == 1) { splitEncounter = encounterId.Split('_'); }
                    if (splitEncounter.Length > 0)
                    {
                        encounterId = GetDcrptValue(splitEncounter[0]);
                        practiceid = "_" + splitEncounter[1];
                        if (splitEncounter.Length > 2)
                        {
                            practiceid += "_" + splitEncounter[2];
                        }
                        if (splitEncounter.Length > 3)
                        {
                            practiceid = "_" + splitEncounter[3];
                        }
                    }
                }
                patientId = GetDcrptValue(patientId);
                WriteLog("In GetRecallCategories: PatientId: " + patientId + "--EncounterId:" + encounterId + "--Practiceid:" + practiceid + " --system: " + system + "--pho:" + pho);
                if (HSSDA.InsertAndValidateToken(patientId, encounterId, authentication, practiceid, pho, out error))
                {
                    Root<RecallCategories> objRoot = new Root<RecallCategories>("RecallCategories", "hss");
                    objRoot.PatientId = patientId;

                    List<RecallCategories> ListRecallCategories = Utility.Instance.DataTableToList<RecallCategories>(HSSDA.GetRecallCategories(group, practiceid, practiceid2, out error));
                    objRoot.Entry.AddRange(ListRecallCategories);

                    result = PrepareJSON(objRoot);
                }
                else if (string.IsNullOrEmpty(error))
                    error = "Invalid token value!";
            }
            catch (Exception ex)
            {
                WriteLog("GetRecallCategories: ", ex);
                error = ex.Message;
            }

            return SetToJson(result, error);
        }
        [AcceptVerbs("GET")]
        public HttpResponseMessage GetRecalls(string system, string pho, string patientId, string encounterId)
        {
            string error = string.Empty;
            string result = string.Empty;
            string authentication = GetAuthorizationToken();

            WriteLog("In GetRecalls");

            try
            {
                //WriteLog("In GetRecalls: PatientId: " + patientId + "--EncounterId:" + encounterId + " --system: " + system + "--pho:" + pho);
                string practiceid = string.Empty;
                if (!string.IsNullOrEmpty(encounterId) && encounterId.Contains("_"))
                {
                    string Delimiter = "__";
                    string[] splitEncounter = encounterId.Split(new[] { Delimiter }, StringSplitOptions.None);
                    if (splitEncounter.Length == 1) { splitEncounter = encounterId.Split('_'); }
                    if (splitEncounter.Length > 0)
                    {
                        encounterId = GetDcrptValue(splitEncounter[0]);
                        practiceid = "_" + splitEncounter[1];
                        if (splitEncounter.Length > 2)
                        {
                            practiceid += "_" + splitEncounter[2];
                        }
                        if (splitEncounter.Length > 3)
                        {
                            practiceid = "_" + splitEncounter[3];
                        }
                    }
                }
                patientId = GetDcrptValue(patientId);
                WriteLog("In GetRecalls: PatientId: " + patientId + "--EncounterId:" + encounterId + "--Practiceid:" + practiceid + " --system: " + system + "--pho:" + pho);
                if (HSSDA.InsertAndValidateToken(patientId, encounterId, authentication, practiceid, pho, out error))
                {
                    Root<Recalls> objRoot = new Root<Recalls>("Recalls", "hss");
                    objRoot.PatientId = patientId;

                    List<Recalls> ListRecalls = Utility.Instance.DataTableToList<Recalls>(HSSDA.GetRecalls(patientId, practiceid, out error));
                    objRoot.Entry.AddRange(ListRecalls);

                    result = PrepareJSON(objRoot);
                }
                else if (string.IsNullOrEmpty(error))
                    error = "Invalid token value!";
            }
            catch (Exception ex)
            {
                WriteLog("GetRecalls: ", ex);
                error = ex.Message;
            }

            return SetToJson(result, error);
        }
        [AcceptVerbs("GET")]
        public HttpResponseMessage GetScreeningCodes(string system, string pho, string patientId, string encounterId)
        {
            string error = string.Empty;
            string result = string.Empty;
            string authentication = GetAuthorizationToken();

            WriteLog("In GetScreeningCodes");

            try
            {
                //WriteLog("In GetScreeningCodes: PatientId: " + patientId + "--EncounterId:" + encounterId + " --system: " + system + "--pho:" + pho);
                string practiceid = string.Empty;
                if (!string.IsNullOrEmpty(encounterId) && encounterId.Contains("_"))
                {
                    string Delimiter = "__";
                    string[] splitEncounter = encounterId.Split(new[] { Delimiter }, StringSplitOptions.None);
                    if (splitEncounter.Length == 1) { splitEncounter = encounterId.Split('_'); }
                    if (splitEncounter.Length > 0)
                    {
                        encounterId = GetDcrptValue(splitEncounter[0]);
                        practiceid = "_" + splitEncounter[1];
                        if (splitEncounter.Length > 2)
                        {
                            practiceid += "_" + splitEncounter[2];
                        }
                        if (splitEncounter.Length > 3)
                        {
                            practiceid = "_" + splitEncounter[3];
                        }
                    }
                }
                patientId = GetDcrptValue(patientId);
                WriteLog("In GetScreeningCodes: PatientId: " + patientId + "--EncounterId:" + encounterId + "--Practiceid:" + practiceid + " --system: " + system + "--pho:" + pho);
                if (HSSDA.InsertAndValidateToken(patientId, encounterId, authentication, practiceid, pho, out error))
                {
                    Root<ScreeningCodes> objRoot = new Root<ScreeningCodes>("ScreeningCodes", "hss");
                    objRoot.PatientId = patientId;

                    List<ScreeningCodes> ListScreeningCodes = Utility.Instance.DataTableToList<ScreeningCodes>(HSSDA.GetScreeningCodes("6", practiceid, out error));
                    objRoot.Entry.AddRange(ListScreeningCodes);

                    result = PrepareJSON(objRoot);
                }
                else if (string.IsNullOrEmpty(error))
                    error = "Invalid token value!";
            }
            catch (Exception ex)
            {
                WriteLog("GetScreeningCodes: ", ex);
                error = ex.Message;
            }

            return SetToJson(result, error);
        }
        [AcceptVerbs("POST")]
        public async Task<HttpResponseMessage> SaveClinicalNotes()
        {
            string result = string.Empty;
            string error = string.Empty;
            string authentication = GetAuthorizationToken();

            try
            {
                WriteLog("In SaveClinicalNotes");

                result = await Request.Content.ReadAsStringAsync();
                ConsultNote objConsultNote = JsonConvert.DeserializeObject<ConsultNote>(result);
                string practiceid = string.Empty;
                string pho = string.Empty;
                if (!string.IsNullOrEmpty(objConsultNote.EncounterId) && objConsultNote.EncounterId.Contains("_"))
                {
                    string Delimiter = "__";
                    string[] splitEncounter = objConsultNote.EncounterId.Split(new[] { Delimiter }, StringSplitOptions.None);
                    if (splitEncounter.Length == 1) { splitEncounter = objConsultNote.EncounterId.Split('_'); }
                    if (splitEncounter.Length > 0)
                    {
                        objConsultNote.EncounterId = GetDcrptValue(splitEncounter[0]);
                        practiceid = "_" + splitEncounter[1];
                        if (splitEncounter.Length > 2)
                        {
                            practiceid += "_" + splitEncounter[2];
                        }
                        if (splitEncounter.Length > 3)
                        {
                            practiceid = "_" + splitEncounter[3];
                        }
                    }
                }
                objConsultNote.PatientId = GetDcrptValue(objConsultNote.PatientId);
                objConsultNote.UserId = GetDcrptValue(objConsultNote.UserId);
                WriteLog("In SaveClinicalNotes: UserID "+ objConsultNote.UserId+" PatientId: " + objConsultNote.PatientId + "--EncounterId:" + objConsultNote.EncounterId + "--PracticeId:" + practiceid + " --AppointmentAdvice: " + objConsultNote.AppointmentAdvice + " --AppointmentAdvice: " + objConsultNote.Assessment
                    + " --ObjectiveNotes: " + objConsultNote.ObjectiveNotes + " --SubjectiveNotes: " + objConsultNote.SubjectiveNotes + " --Plans: " + objConsultNote.Plans);
                if (HSSDA.InsertAndValidateToken(objConsultNote.PatientId, objConsultNote.EncounterId, authentication, practiceid, pho, out error))
                {
                    WriteLog("DEBUG: SaveClinicalNotes: " + result);
                    Int64 saveNotes = HSSDA.InsertUpdateConsultNotes(objConsultNote.PatientId, objConsultNote.EncounterId, objConsultNote.SubjectiveNotes, objConsultNote.ObjectiveNotes,
                                                      objConsultNote.Assessment, objConsultNote.Plans, practiceid, objConsultNote.UserId, out error);
                    WriteLog("DEBUG: SaveReturnClinicalNotes: " + saveNotes);
                    if (saveNotes > 0)
                    {

                        result = "{\"status\":\"success\",\"message\":\"\"}";
                    }
                    else if (string.IsNullOrWhiteSpace(error))
                    {
                        error = "Unable to Save Notes.Please try again or contact INDICI support.";
                    }
                }
                else if (string.IsNullOrEmpty(error))
                    error = "Invalid token value!";
            }
            catch (Exception ex)
            {
                WriteLog("SaveClinicalNotes: ", ex);
                error = ex.Message;
            }

            return SetToJson(result, error);
        }
        [AcceptVerbs("POST")]
        public async Task<HttpResponseMessage> SaveCondition()
        {
            string result = string.Empty;
            string error = string.Empty;
            string authentication = GetAuthorizationToken();
            DateTime dtOnsetDate = DateTime.MinValue;

            try
            {
                WriteLog("In SaveCondition");

                result = await Request.Content.ReadAsStringAsync();
                Condition objCondition = JsonConvert.DeserializeObject<Condition>(result);

                DateTime.TryParse(objCondition.OnSetDate, out dtOnsetDate);
                string practiceid = string.Empty;
                string pho = string.Empty;
                if (!string.IsNullOrEmpty(objCondition.EncounterId) && objCondition.EncounterId.Contains("_"))
                {
                    string Delimiter = "__";
                    string[] splitEncounter = objCondition.EncounterId.Split(new[] { Delimiter }, StringSplitOptions.None);
                    if (splitEncounter.Length == 1) { splitEncounter = objCondition.EncounterId.Split('_'); }
                    if (splitEncounter.Length > 0)
                    {
                        objCondition.EncounterId = GetDcrptValue(splitEncounter[0]);
                        practiceid = "_" + splitEncounter[1];
                        if (splitEncounter.Length > 2)
                        {
                            practiceid += "_" + splitEncounter[2];
                        }
                        if (splitEncounter.Length > 3)
                        {
                            practiceid = "_" + splitEncounter[3];
                        }
                    }
                }
                objCondition.PatientId = GetDcrptValue(objCondition.PatientId);
                objCondition.UserId = GetDcrptValue(objCondition.UserId);
                WriteLog("In SaveCondition: PatientId: " + objCondition.PatientId + "--EncounterId:" + objCondition.EncounterId + "--Practiceid:" + practiceid);
                if (HSSDA.InsertAndValidateToken(objCondition.PatientId, objCondition.EncounterId, authentication, practiceid, pho, out error))
                {
                    WriteLog("DEBUG: SaveCondition: " + result);
                    int saveDiagnosis = HSSDA.InsertUpdateDiagnosis(objCondition.PatientId, objCondition.EncounterId, objCondition.UserId, objCondition.Type, dtOnsetDate,
                                                    objCondition.Summary, (objCondition.IsLongTerm.ToLower().Equals("true") ? true : false), objCondition.ConceptId, objCondition.Name,
                                                    objCondition.FSN, practiceid, out error);
                    WriteLog("DEBUG: ReturnSaveCondition: " + saveDiagnosis);
                    if (saveDiagnosis > 0)
                    {

                        result = "{\"status\":\"success\",\"message\":\"\"}";
                    }
                    else if (saveDiagnosis == -5)
                    {
                        result = "{\"status\":\"success\",\"message\":\"Diagnosis already exits against current Appointment.\" }";
                    }
                    else if (string.IsNullOrWhiteSpace(error))
                    {
                        error = "Unable to Save Diagnosis.Please try again or contact INDICI support.";
                    }
                }
                else if (string.IsNullOrEmpty(error))
                    error = "Invalid token value!";
            }
            catch (Exception ex)
            {
                WriteLog("SaveCondition: ", ex);
                error = ex.Message;
            }

            return SetToJson(result, error);
        }
        [AcceptVerbs("POST")]
        public async Task<HttpResponseMessage> SaveSummary()
        {
            string result = string.Empty;
            string successStatus = string.Empty;
            string message = string.Empty;
            string json = string.Empty;
            string error = string.Empty;

            string authentication = GetAuthorizationToken();

            try
            {
                WriteLog("PutSummary called >> ");

                string jsonContent = await Request.Content.ReadAsStringAsync();// Read JSON Object

                dynamic jsonParsed = JObject.Parse(jsonContent); // Parse JSON Object
                WriteLog("PutSummary >> " + Utility.Instance.ToString(jsonParsed));

                string patientId = string.Empty;
                string encounterId = string.Empty;
                string system = string.Empty;
                string identifier = string.Empty;

                foreach (var parsedItem in jsonParsed)
                {
                    string name = parsedItem.Name;

                    if (name.Trim().Equals("patientId", StringComparison.OrdinalIgnoreCase))
                    {
                        patientId = parsedItem.Value.ToString();
                    }
                    else if (name.Trim().Equals("encounterID", StringComparison.OrdinalIgnoreCase))
                    {
                        encounterId = parsedItem.Value.ToString();
                    }
                    else if (name.Trim().Equals("system", StringComparison.OrdinalIgnoreCase))
                    {
                        system = parsedItem.Value.ToString();
                    }
                    else if (name.Equals("identifier", StringComparison.OrdinalIgnoreCase))
                    {
                        identifier = parsedItem.Value.ToString();
                        WriteLog("PutSummary identifier >> " + identifier);
                    }
                }


                string practiceid = string.Empty;
                string pho = string.Empty;
                if (!string.IsNullOrEmpty(encounterId) && encounterId.Contains("_"))
                {
                    string Delimiter = "__";
                    string[] splitEncounter = encounterId.Split(new[] { Delimiter }, StringSplitOptions.None);
                    if (splitEncounter.Length == 1) { splitEncounter = encounterId.Split('_'); }
                    if (splitEncounter.Length > 0)
                    {
                        encounterId = GetDcrptValue(splitEncounter[0]);
                        practiceid = "_" + splitEncounter[1];
                        if (practiceid.Contains("302_F3H045"))
                            pho = "SCDHB";
                        if (splitEncounter.Length > 2)
                        {
                            practiceid += "_" + splitEncounter[2];
                        }
                        if (splitEncounter.Length > 3)
                        {
                            practiceid = "_" + splitEncounter[3];
                        }
                    }
                }
                patientId = GetDcrptValue(patientId);

                WriteLog("In PutSummary: PatientId: " + patientId + "--EncounterId:" + encounterId + "--Practiceid:" + practiceid);
                if (HSSDA.InsertAndValidateToken(patientId, encounterId, authentication, practiceid, pho, out error))
                {


                    WriteLog("PutSummary content >> " + jsonContent);


                    bool isUTC = false;

                    StringBuilder sb = new StringBuilder();

                    string name = string.Empty;
                    //string patientId = string.Empty;
                    //string encounterId = string.Empty;
                    string providerId = string.Empty;
                    //string identifier = string.Empty;
                    string dateTimeRecorded = string.Empty;
                    DataTable dtSchema = new DataTable();
                    DataTable dtSummary = CreateDataTable();

                    if (dtSummary.Columns.Count > 0)
                    {
                        bool isValidResponse = true;

                        foreach (var parsedItem in jsonParsed)
                        {
                            name = parsedItem.Name;

                            //if (name.Equals("patientId", StringComparison.OrdinalIgnoreCase))
                            //{
                            //    patientId = parsedItem.Value.ToString();
                            //}
                            //else if (name.Equals("encounterID", StringComparison.OrdinalIgnoreCase))
                            //{
                            //    encounterId = parsedItem.Value.ToString();
                            //}
                            if (name.Equals("providerID", StringComparison.OrdinalIgnoreCase))
                            {
                                providerId = parsedItem.Value.ToString();
                            }
                            else if (name.Equals("dateTimeRecorded", StringComparison.OrdinalIgnoreCase))
                            {
                                dateTimeRecorded = parsedItem.Value.ToString();
                            }
                            //else if (name.Equals("identifier", StringComparison.OrdinalIgnoreCase))
                            //{
                            //    identifier = parsedItem.Value.ToString();
                            //    WriteLog("PutSummary identifier >> " + identifier);
                            //}
                            else if (name.Trim().Equals("entry", StringComparison.OrdinalIgnoreCase))
                            {
                                //Logging.Instance.WriteEventLog("DEBUG: PutSummary GetTemplateSchema call");
                                dtSchema = HSSDA.GetTemplateSchema(patientId, identifier, practiceid);

                                WriteLog("PutSummary dtSchema Count >> " + dtSchema.Rows.Count);


                                if (dtSchema.Rows.Count > 0)
                                {
                                    //WriteLog("PutSummary dtSchema FieldCaption >> " + dtSchema.Rows[3]["FieldCaption"].ToString());
                                    var entries = jsonParsed.entry;
                                    foreach (var entry in entries)
                                    {
                                        WriteLog("PutSummary Entry  >> " + entry);
                                        isValidResponse = FillSummaryData(dtSchema, dtSummary, identifier, dateTimeRecorded, entry, out result, isUTC, practiceid);
                                    }
                                }
                                else
                                {
                                    WriteLog("DEBUG: PutSummary >> Schema not found" + dtSchema.Rows.Count);
                                    isValidResponse = false;
                                    error = "Invalid values passed!";
                                }
                            }
                        }

                        Logging.Instance.WriteEventLog("DEBUG: PutSummary (isValidResponse) >> " + isValidResponse + " : " + result);
                        if (isValidResponse)
                        {

                            WriteLog("DEBUG: PutSummary >> Summary Rows Count " + dtSummary.Rows.Count);
                            WriteLog("DEBUG: PutSummary >> Summary Columns Count " + dtSummary.Columns.Count);

                            if (dtSummary.Rows.Count > 0)
                            {
                                foreach (DataRow dr in dtSummary.Rows)
                                {
                                    foreach (DataColumn dc in dtSummary.Columns)
                                    {
                                        if (string.IsNullOrEmpty(Utility.Instance.ToString(dr[dc.ColumnName])))
                                            dr[dc.ColumnName] = DBNull.Value;
                                    }
                                }
                                WriteLog("DEBUG: PutSummary >> BEFORE Insert Summary " + identifier);
                                int tempSummaryReturn = InsertSummary(dtSummary, patientId, encounterId, providerId, identifier, dateTimeRecorded, practiceid);
                                if (tempSummaryReturn > 0)
                                {
                                    WriteLog("DEBUG: PutSummary >> AFTER Insert Summary " + identifier);
                                    result = "{\"status\":\"success\"}";
                                }
                                else
                                {
                                    if (tempSummaryReturn == -4)
                                    {
                                        error = "Invalid Identifier.Please send correct Identifier or contact INDIC support.";
                                    }
                                    else if (tempSummaryReturn == -5)
                                    {
                                        error = "Invalid OutCome.Please send correct Outcome or contact INDIC support.";
                                    }
                                    else
                                    {
                                        error = "Insertion Failed.Please contact INDIC support.";
                                    }
                                    WriteLog("DEBUG: PutSummary >> Insertion Failed Return Value. " + tempSummaryReturn);
                                }
                            }

                        }
                        else
                        {
                            error = "Error Parsing the JSON Data.Please send correct Data values or contact INDIC support.";
                        }
                    }
                }
                else
                {
                    error = " Invalid token value.";
                }
            }
            catch (Exception ex)
            {
                error = ex.Message;
                Logging.Instance.WriteExceptionLog("PutSummary: ", ex);
                WriteLog(" SaveSummary Input >> " + result, new Exception());
                WriteLog(" SaveSummary: ", ex);
            }
            return SetToJson(result, error);
        }

        [AcceptVerbs("GET")]
        public HttpResponseMessage GetPatientAttachment(string system, string pho, string patientId, string encounterId, string referenceID = "", string sortOrder = "dateDescend", string Subject = "", DateTime? dateFrom = null, DateTime? dateTo = null)
        {
            string error = string.Empty;
            string result = string.Empty;
            string authentication = GetAuthorizationToken();

            WriteLog("In GetPatientAttachment");
            try
            {
                //WriteLog("In GetPatientAttachment: PatientId: " + patientId + "--EncounterId:" + encounterId + " --system: " + system + "--pho:" + pho + "--ReferenceID:" + referenceID);
                string practiceid = string.Empty;
                string practiceid2 = string.Empty;
                DateTime dtfromDate = DateTime.MinValue;
                DateTime.TryParse(Utility.Instance.ToString(dateFrom), out dtfromDate);

                DateTime dtToDate = DateTime.MinValue;
                DateTime.TryParse(Utility.Instance.ToString(dateTo), out dtToDate);
                if (!string.IsNullOrEmpty(encounterId) && encounterId.Contains("_"))
                {
                    string Delimiter = "__";
                    string[] splitEncounter = encounterId.Split(new[] { Delimiter }, StringSplitOptions.None);
                    if (splitEncounter.Length == 1) { splitEncounter = encounterId.Split('_'); }
                    if (splitEncounter.Length > 0)
                    {
                        encounterId = GetDcrptValue(splitEncounter[0]);
                        practiceid = "_" + splitEncounter[1];
                        practiceid2 = practiceid.Replace("_", "");
                        if (splitEncounter.Length > 2)
                        {
                            practiceid += "_" + splitEncounter[2];
                        }
                        if (splitEncounter.Length > 3)
                        {
                            practiceid = "_" + splitEncounter[3];
                        }
                    }
                }
                patientId = GetDcrptValue(patientId);

                WriteLog("In GetPatientAttachment: PatientId: " + patientId + "--EncounterId:" + encounterId + " --system: " + system + "--pho:" + pho + "--ReferenceID:" + referenceID);

                if (HSSDA.InsertAndValidateToken(patientId, encounterId, authentication, practiceid, pho, out error))
                {
                    Root<PatientDMS2> objRoot = new Root<PatientDMS2>("PatientDMS", "hss");
                    objRoot.PatientId = patientId;
                    List<PatientDMS2> ListPatientDMS2 = new List<PatientDMS2>();
                    PatientDMS2 PatientDMS = new PatientDMS2();
                    List<PatientDMS> ListPatientDMS = Utility.Instance.DataTableToList<PatientDMS>(HSSDA.GetPatientAttachment(practiceid2, practiceid, patientId, referenceID, dtfromDate, dtToDate, sortOrder, Subject, out error));
                    foreach (var item in ListPatientDMS)
                    {
                        PatientDMS = new PatientDMS2();
                        PatientDMS.AttachmentComments = item.AttachmentComments;
                        PatientDMS.AttachmentContent = Convert.ToBase64String(item.AttachmentContent);
                        PatientDMS.AttachmentCreationDate = item.AttachmentCreationDate;
                        PatientDMS.AttachmentDataType = item.AttachmentDataType;
                        PatientDMS.AttachmentReferenceID = item.AttachmentReferenceID;
                        PatientDMS.AttachmentSize = item.AttachmentSize;
                        PatientDMS.AttachmentSubject = item.AttachmentSubject;
                        PatientDMS.AttachmentType = item.AttachmentType;
                        ListPatientDMS2.Add(PatientDMS);
                    }
                    objRoot.Entry.AddRange(ListPatientDMS2);

                    result = PrepareJSON(objRoot);
                }
                else if (string.IsNullOrEmpty(error))
                    error = "Invalid token value!";
            }
            catch (Exception ex)
            {
                WriteLog("GetPatientAttachment: ", ex);
                error = ex.Message;
            }

            return SetToJson(result, error);
        }

        private static int InsertSummary(DataTable dtResult, string patientID, string encounterID, string providerID, string identifier, string dateTimeRecorded, string practiceid)
        {
            Logging.Instance.WriteEventLog("PutSummary: InsertSummary Start Called");
            string outCome = Utility.Instance.ToString(dtResult.Rows[0]["OUTCOME"]);
            string ds = Utility.Instance.ToString(dtResult.Rows[0]["DS"]);
            string onSet = Utility.Instance.ToString(dtResult.Rows[0]["ONSET"]);

            dtResult.Columns.Remove("OUTCOME");
            dtResult.Columns.Remove("DS");
            dtResult.Columns.Remove("ONSET");


            Logging.Instance.WriteEventLog("PutSummary: InsertSummary Called");
            return HSSDA.InsertSummary(patientID, encounterID, providerID, identifier, dateTimeRecorded, outCome, ds, onSet, dtResult, practiceid);
        }

        private DataTable CreateDataTable()
        {
            DataTable dtResult = new DataTable();

            try
            {

                for (int i = 1; i <= 50; i++)
                    dtResult.Columns.Add("Field" + i, typeof(string));

                dtResult.Columns.AddRange(
                    new DataColumn[]
                    {
                         new DataColumn("PCode", typeof(string)),
                         new DataColumn("PCode2", typeof(string)),
                         new DataColumn("JsonData", typeof(string)),
                         new DataColumn("HtmlData", typeof(string)),
                         new DataColumn("EffectiveDateTime", typeof(DateTime)),
                         new DataColumn("IsConfidential", typeof(bool)),
                         new DataColumn("IsPatientPortal", typeof(bool)),
                         new DataColumn("Outcome", typeof(string)),
                         new DataColumn("DS", typeof(string)),
                         new DataColumn("ONSET", typeof(string))
                    });



            }
            catch (Exception ex)
            {
                Logging.Instance.WriteExceptionLog("CreateDataTable: ", ex);
            }

            return dtResult;
        }


        private bool FillSummaryData(DataTable dtSchema, DataTable dtSummary, string identifier, string dateTimeRecorded, dynamic entry, out string responseResult, bool isUTC, string practiceid)
        {
            try
            {
                responseResult = string.Empty;
                List<PostedData> listPosted = new List<PostedData>();
                PostedData objPosted = new PostedData();

                DataRow dr = dtSummary.NewRow();
                string[] excluded = new string[] { "OUTCOME", "DS", "ONSET" };
                string name = string.Empty;
                string dbType = string.Empty;
                string newDate = string.Empty;
                string successStatus = string.Empty;
                string message = string.Empty;
                DataRow[] drComparison = null;
                DataSet dsRetinalLookUp = new DataSet();
                //if (identifier.Trim().Equals("DS:RET", StringComparison.OrdinalIgnoreCase))
                //{
                //    dsRetinalLookUp = BPACDA.CFRetinalLookUp(connectionString);
                //}
                foreach (var entity in entry)
                {
                    objPosted = new PostedData();
                    var type = (JTokenType)(entity.Value).Type;

                    name = Utility.Instance.ToString(entity.Name);
                    //WriteLog("FillSummaryData Name >> " + name);
                    drComparison = dtSchema.Select("FieldCaption = '" + name + "' ");

                    //WriteLog("FillSummaryData drComparison count >> " + drComparison.Length);

                    objPosted.OriginalName = name;

                    if (name.Contains("_"))
                        objPosted.NameWithoutIdentifier = name = name.Substring(name.IndexOf("_") + 1);

                    if (drComparison.Length > 0 | excluded.Any(name.ToUpper().Equals))
                    {
                        dbType = string.Empty;
                        if (!excluded.Any(name.ToUpper().Equals))
                            dbType = Utility.Instance.ToString(drComparison[0]["ScnFieldType"]).Replace("N", "integer").Replace("B", "boolean").Replace("T", "string");

                        if (name.Equals("DATE", StringComparison.OrdinalIgnoreCase) || name.ToUpper().Contains("DATE"))
                        {
                            DateTime dt;
                            DateTime.TryParse(Utility.Instance.ToString(entity.Value), out dt);
                            dt = ConvertLocalDateTimeToUTC(dt.ToString(), isUTC);
                            if (dt > DateTime.MinValue)
                            {
                                newDate = dt.ToString("dd/MM/yyyy").Replace("-", "/");
                            }
                        }

                        if (excluded.Any(name.ToUpper().Equals))
                            dr[name] = Utility.Instance.ToString(entity.Value);
                        else
                        {
                            WriteLog("FillSummaryData Data >> " + drComparison[0]["FieldName"]);
                            objPosted.FieldColumnDB = Utility.Instance.ToString(drComparison[0]["FieldName"]);
                            objPosted.NameWithSpace = Utility.Instance.ToString(drComparison[0]["FieldCaption"]);
                        }

                        if (!string.IsNullOrEmpty(dbType))
                        {
                            if (Utility.Instance.ToString(type).Equals("Integer", StringComparison.OrdinalIgnoreCase)
                                | Utility.Instance.ToString(type).Equals("Float", StringComparison.OrdinalIgnoreCase))
                            {
                                objPosted.Value = Utility.Instance.ToString(entity.Value);
                                objPosted.Type = Utility.Instance.ToString(type).ToLower();
                                //DIAP_ALBUMIN
                                if (objPosted.OriginalName.Equals("DIAP_ALBUMIN"))
                                {
                                    WriteLog(string.Format("DIAP_ALBUMIN >> objPosted.Value >> objPosted.Type {0}", objPosted.Type));
                                    WriteLog(string.Format("DIAP_ALBUMIN >> dbType {0}", dbType));
                                }
                                if (!objPosted.Type.Equals(dbType)
                                    & !objPosted.Type.Equals("Float", StringComparison.OrdinalIgnoreCase))
                                {
                                    return false;
                                }
                            }
                            else if (Utility.Instance.ToString(type).Equals("Boolean", StringComparison.OrdinalIgnoreCase))
                            {
                                objPosted.Value = Utility.Instance.ToString(entity.Value).ToLower();
                                objPosted.Type = Utility.Instance.ToString(type).ToLower();

                                if (!objPosted.Type.Equals(dbType))
                                {
                                    return false;
                                }
                            }
                            else if (Utility.Instance.ToString(type).Equals("String", StringComparison.OrdinalIgnoreCase))
                            {
                                objPosted.Value = Utility.Instance.ToString(entity.Value);

                                WriteLog("FillSummaryData Value >> " + entity.Value);

                                //Added for JIRA-5582
                                if (identifier.Trim().Equals("DS:RET", StringComparison.OrdinalIgnoreCase))
                                {
                                    //if()
                                    //if (dsRetinalLookUp.Tables[0] != null)
                                    //{
                                    //    if (dsRetinalLookUp.Tables[0].Rows.Count > 0)
                                    //    {
                                    //        DataRow[] drRet = dsRetinalLookUp.Tables[0].Select("MappingCode = '" + objPosted.Value.Trim() + "'");
                                    //        if (drRet.Length > 0)
                                    //        {
                                    //            objPosted.Value = drRet[0]["MappingCode"].ToString() + " - " + drRet[0]["MappingDescription"].ToString();
                                    //        }
                                    //    }
                                    //}
                                }
                                if (!string.IsNullOrEmpty(newDate))
                                    objPosted.Value = newDate;

                                objPosted.Type = Utility.Instance.ToString(type).ToLower();
                            }
                            objPosted.FieldId = Utility.Instance.ToString(drComparison[0]["FieldId"]);
                            newDate = string.Empty;
                            listPosted.Add(objPosted);
                        }
                    }
                }

                List<string> listDBFields = new List<string>();
                foreach (DataRow drSummary in dtSchema.Rows)
                {
                    WriteLog("FillSummaryData drSummary >> " + drSummary["FieldCaption"]);
                    name = Utility.Instance.ToString(drSummary["FieldCaption"]);
                    listDBFields.Add(name);
                }

                WriteLog("FillSummaryData drSummary Count >> " + listDBFields.Count);
                string htmlData = string.Empty;
                string result = BuildJsonTimeLineData(listPosted, listDBFields, dr, dtSummary, dtSchema, out htmlData, identifier);

                dr["PCode"] = string.Empty;
                dr["PCode2"] = string.Empty;
                dr["JsonData"] = result;
                //dr["HtmlData"] = "<b>" + identifier + ":</b>\r" + htmlData;
                dr["HtmlData"] = "<b>" + identifier + ":</b>\r" + htmlData.Replace("true", "Yes").Replace("false", "No");
                if (string.IsNullOrEmpty(dateTimeRecorded))
                {
                    dateTimeRecorded = DateTime.Now.ToShortDateString();
                }
                dateTimeRecorded = ConvertLocalDateTimeToUTC(dateTimeRecorded, isUTC).ToString("yyyy-MM-ddTHH:mm:ss");

                dr["EffectiveDateTime"] = dateTimeRecorded;

                dr["IsConfidential"] = false;
                dr["IsPatientPortal"] = false;
                dtSummary.Rows.Add(dr);
            }
            catch (Exception ex)
            {
                throw;
            }

            return true;
        }
        private static string BuildJsonTimeLineData(List<PostedData> listPosted, List<string> listDBFields, DataRow dr, DataTable dtResult, DataTable dtSchema, out string htmlData, string identifier)
        {


            Logging.Instance.WriteEventLog("DEBUG: BuildJsonTimeLineData call >> ");
            StringBuilder sb = new StringBuilder();
            StringBuilder sbTimeline = new StringBuilder();

            htmlData = string.Empty;
            string result = string.Empty;
            string value = string.Empty;
            string labelname = string.Empty;
            string field = string.Empty;
            int fieldCounter = 1;

            try
            {
                sb.Append("[");
                foreach (string dbfield in listDBFields)
                {

                    //field = Regex.Replace(dbfield, @"\s+", "");
                    field = dbfield;
                    // Logging.Instance.WriteEventLog("DEBUG: BuildJsonTimeLineData call >> " + field + "/" + listPosted[fieldCounter-1].OriginalName);
                    PostedData data = listPosted.FirstOrDefault(x => x.OriginalName.Equals(field, StringComparison.OrdinalIgnoreCase));

                    if (data != null)
                    {


                        sb.Append("\r { \r");
                        string labelValue = "\"" + data.OriginalName + "\" : " + "\"" + data.Value + "\"" + ",\r";
                        sbTimeline.Append(data.OriginalName + " : " + data.Value + ",\r");//time line data
                        sb.Append(labelValue);
                        if (data.Type == "string")
                        {

                            value = "\"value\" : " + " \"" + data.Value.ToString() + "\",\r";
                            sb.Append(value);
                        }
                        else if (data.Type == "boolean")
                        {
                            bool bValue = Convert.ToBoolean(data.Value);

                            sb.Append("\"id\":" + "\"" + data.NameWithoutIdentifier + "-0\",\r");
                            sb.Append("\"" + data.NameWithoutIdentifier + "_selected\":" + "\"" + data.NameWithSpace + "\",\r");
                            sb.Append("\"Status\":" + "" + bValue.ToString().ToLower() + ",\r");

                            if (bValue)
                                value = "\"value\" : " + "\"Ticked\",\r";
                            else
                                value = "\"value\" : " + "\"\",\r";

                            sb.Append(value);
                        }
                        else if (data.Type == "integer")
                        {
                            int eValue = Convert.ToInt32(data.Value);
                            value = "\"value\" : " + eValue + ",\r";
                            sb.Append(value);
                        }

                        else if (data.Type == "float")
                        {
                            value = "\"value\" : " + data.Value + ",\r";
                            sb.Append(value);
                        }

                        //data.NameWithSpace = dbfield;
                        labelname = "\"label\" : " + " \"" + data.NameWithSpace + "\",\r";
                        sb.Append(labelname);
                        //appended id as per new JSon Request
                        string fieldId = "\"FieldID\" : " + " \"" + data.FieldId + "\"\r },";
                        sb.Append(fieldId);

                        //if (dtResult.Columns.Contains("Field" + fieldCounter))
                        if (dtResult.Columns.Contains(data.FieldColumnDB))
                        {
                            if (data.Type == "boolean")
                            {
                                bool bValue = Convert.ToBoolean(data.Value);
                                dr[data.FieldColumnDB] = bValue.ToString().ToLower();
                            }
                            else
                            {
                                dr[data.FieldColumnDB] = data.Value;
                            }
                        }
                    }
                    fieldCounter++;
                }
                result = sb.ToString().TrimEnd(',') + "\r]";
                Logging.Instance.WriteEventLog("DEBUG: BuildJsonTimeLineData JSON >>" + result);
                htmlData = sbTimeline.ToString().TrimEnd(',');
            }
            catch (Exception ex)
            {
                Logging.Instance.WriteExceptionLog("BuildJson: ", ex);
            }

            return result;
        }

        private DateTime ConvertLocalDateTimeToUTC(string dateTime, bool isUTC)
        {
            if (CheckDate(dateTime))
            {
                //if (isUTC)
                //{
                //    return TimeZoneInfo.ConvertTimeToUtc(Convert.ToDateTime(dateTime), TimeZoneInfo.FindSystemTimeZoneById(ConfigurationManager.AppSettings["NewZealandTimeZone"].ToString()));
                //}
                //else
                //{
                return Convert.ToDateTime(dateTime);
                // }
            }
            else
            {
                return DateTime.MinValue.Date;
            }
        }

        private bool CheckDate(String date)
        {
            DateTime dateout;
            try
            {
                bool success = DateTime.TryParse(date, out dateout);
                if (success) return true;
                else return false;
            }
            catch (Exception ex)
            {
                Logger.Logging.Instance.WriteExceptionLog("CheckDate >> ", ex);
                return false;
            }
        }


        [AcceptVerbs("POST")]
        public async Task<HttpResponseMessage> SaveDocument()
        {
            string result = string.Empty;
            string error = string.Empty;
            string authentication = GetAuthorizationToken();

            try
            {
                WriteLog("In SaveDocument");

                result = await Request.Content.ReadAsStringAsync();
                Document objDocument = JsonConvert.DeserializeObject<Document>(result);
                string practiceid = string.Empty;
                string practiceid2 = string.Empty;
                string pho = string.Empty;

                if (!string.IsNullOrEmpty(objDocument.EncounterId) && objDocument.EncounterId.Contains("_"))
                {
                    string Delimiter = "__";
                    string[] splitEncounter = objDocument.EncounterId.Split(new[] { Delimiter }, StringSplitOptions.None);
                    if (splitEncounter.Length == 1) { splitEncounter = objDocument.EncounterId.Split('_'); }
                    if (splitEncounter.Length > 0)
                    {
                        objDocument.EncounterId = GetDcrptValue(splitEncounter[0]);
                        practiceid = "_" + splitEncounter[1];
                        practiceid2 = splitEncounter[1];
                        if (practiceid.Contains("302_F3H045"))
                            pho = "SCDHB";
                        if (splitEncounter.Length > 2)
                        {
                            practiceid += "_" + splitEncounter[2];
                        }
                        if (splitEncounter.Length > 3)
                        {
                            practiceid = "_" + splitEncounter[3];
                        }
                    }
                }
                objDocument.PatientId = GetDcrptValue(objDocument.PatientId);
                WriteLog("In SaveDocument: PatientId: " + objDocument.PatientId + "--EncounterId:" + objDocument.EncounterId + "--Practiceid:" + practiceid);
                if (HSSDA.InsertAndValidateToken(objDocument.PatientId, objDocument.EncounterId, authentication, practiceid, pho, out error))
                {
                    WriteLog("DEBUG: SaveDocument: " + result);

                    string dmsGuidKey = SaveToDMS(objDocument.MessageData, objDocument.ContentType, objDocument.MessageTitle,
                                                  objDocument.ItemType.ToLower().Equals("in") ? 17 : 18, practiceid, practiceid2);
                    WriteLog("DEBUG: SaveDocument:DMSID " + dmsGuidKey);
                    Int64 saveDoc = HSSDA.InsertDocument(objDocument.PatientId, objDocument.EncounterId, objDocument.MessageSubject, dmsGuidKey,
                                                objDocument.ItemType.ToLower().Equals("in") ? 1 : 2, practiceid, out error);
                    WriteLog("DEBUG: ReturnSaveDocument: " + saveDoc);
                    if (!string.IsNullOrWhiteSpace(dmsGuidKey) && saveDoc > 0)
                    {

                        result = "{\"status\":\"success\",\"message\":\"" + dmsGuidKey + "\"}";
                    }
                    else if (string.IsNullOrWhiteSpace(error))
                        error = "Unable to save document. Please try again or contact INDICI support.";
                }
                else if (string.IsNullOrEmpty(error))
                    error = "Invalid token value!";
            }
            catch (Exception ex)
            {
                WriteLog("SaveDocument: ", ex);
                error = ex.Message;
            }

            return SetToJson(result, error);
        }
        [AcceptVerbs("POST")]
        public async Task<HttpResponseMessage> SaveInvoice()
        {
            string result = string.Empty;
            string error = string.Empty;
            string authentication = GetAuthorizationToken();
            int serviceMappingId = -1;

            WriteLog("In SaveInvoice");

            try
            {
                result = await Request.Content.ReadAsStringAsync();
                Invoice objInvoice = JsonConvert.DeserializeObject<Invoice>(result);
                string practiceid = string.Empty;
                string pho = string.Empty;

                if (!string.IsNullOrEmpty(objInvoice.EncounterId) && objInvoice.EncounterId.Contains("_"))
                {
                    string Delimiter = "__";
                    string[] splitEncounter = objInvoice.EncounterId.Split(new[] { Delimiter }, StringSplitOptions.None);
                    if (splitEncounter.Length == 1) { splitEncounter = objInvoice.EncounterId.Split('_'); }
                    if (splitEncounter.Length > 0)
                    {
                        objInvoice.EncounterId = GetDcrptValue(splitEncounter[0]);
                        practiceid = "_" + splitEncounter[1];
                        if (practiceid.Contains("302_F3H045"))
                            pho = "SCDHB";
                        if (splitEncounter.Length > 2)
                        {
                            practiceid += "_" + splitEncounter[2];
                        }
                        if (splitEncounter.Length > 3)
                        {
                            practiceid = "_" + splitEncounter[3];
                        }
                    }
                }
                objInvoice.PatientId = GetDcrptValue(objInvoice.PatientId);
                objInvoice.UserId = GetDcrptValue(objInvoice.UserId);
                WriteLog("In SaveInvoice: PatientId: " + objInvoice.PatientId + "--EncounterId:" + objInvoice.EncounterId + "--Practiceid:" + practiceid);
                if (HSSDA.InsertAndValidateToken(objInvoice.PatientId, objInvoice.EncounterId, authentication, practiceid, pho, out error))
                {
                    WriteLog("DEBUG: SaveInvoice: " + result);
                    result = "{\"status\":\"success\",\"message\":\"" + Utility.Instance.RandomDigits(6) + "\"}";

                    serviceMappingId = HSSDA.HSSInsertUpdateService(objInvoice.PatientId, objInvoice.EncounterId, "HSS Service", objInvoice.Name, objInvoice.Code, objInvoice.Fee,
                                                                 objInvoice.UserId, "167", practiceid, objInvoice.payee, out error);

                    WriteLog("serviceMappingId : " + serviceMappingId);

                    if (serviceMappingId > 0)
                    {
                        result = "{\"status\":\"success\",\"message\":\"" + serviceMappingId + "\"}";
                    }
                    else if (serviceMappingId == -3)
                    {
                        result = "{\"status\":\"success\",\"message\":\"Invoice already exits.\" }";
                    }
                    else if (string.IsNullOrWhiteSpace(error))
                    {
                        error = "Unable to Save Invoice.Please try again or contact INDICI support.";
                    }
                }
                else if (string.IsNullOrEmpty(error))
                    error = "Invalid token value!";

            }
            catch (Exception ex)
            {
                WriteLog("SaveInvoice: ", ex);
                error = ex.Message;
            }

            return SetToJson(result, error);
        }
        [AcceptVerbs("POST")]
        public async Task<HttpResponseMessage> SaveObservations()
        {
            string result = string.Empty;
            string error = string.Empty;
            string authentication = GetAuthorizationToken();

            try
            {
                WriteLog("In SaveObservations");

                result = await Request.Content.ReadAsStringAsync();
                Observations objObservations = JsonConvert.DeserializeObject<Observations>(result);
                string practiceid = string.Empty;
                string pho = string.Empty;


                if (!string.IsNullOrEmpty(objObservations.EncounterId) && objObservations.EncounterId.Contains("_"))
                {
                    string Delimiter = "__";
                    string[] splitEncounter = objObservations.EncounterId.Split(new[] { Delimiter }, StringSplitOptions.None);
                    if (splitEncounter.Length == 1) { splitEncounter = objObservations.EncounterId.Split('_'); }
                    if (splitEncounter.Length > 0)
                    {
                        objObservations.EncounterId = GetDcrptValue(splitEncounter[0]);
                        practiceid = "_" + splitEncounter[1];
                        if (practiceid.Contains("302_F3H045"))
                            pho = "SCDHB";
                        if (splitEncounter.Length > 2)
                        {
                            practiceid += "_" + splitEncounter[2];
                        }
                        if (splitEncounter.Length > 3)
                        {
                            practiceid = "_" + splitEncounter[3];
                        }
                    }
                }
                objObservations.PatientId = GetDcrptValue(objObservations.PatientId);
                objObservations.UserId = GetDcrptValue(objObservations.UserId);
                WriteLog("In SaveObservations: PatientId: " + objObservations.PatientId + "--EncounterId:" + objObservations.EncounterId + "--Practiceid:" + practiceid);
                if (HSSDA.InsertAndValidateToken(objObservations.PatientId, objObservations.EncounterId, authentication, practiceid, pho, out error))
                {
                    WriteLog("DEBUG: SaveObservations: " + result);
                    bool calltoApi = true;
                    int saveObs = 0;
                    if (string.IsNullOrEmpty(objObservations.BPDia) && string.IsNullOrEmpty(objObservations.BPSys) && string.IsNullOrEmpty(objObservations.Temperature) && string.IsNullOrEmpty(objObservations.WaistCircumference) &&
                        string.IsNullOrEmpty(objObservations.Height) && string.IsNullOrEmpty(objObservations.Weight) && string.IsNullOrEmpty(objObservations.HeartRate) &&
                        string.IsNullOrEmpty(objObservations.Risk) && string.IsNullOrEmpty(objObservations.Framingham))
                    {
                        calltoApi = false;
                    }
                    else
                    {
                        saveObs = HSSDA.InsertUpdateObservation(objObservations.PatientId, objObservations.EncounterId, objObservations.UserId, objObservations.Temperature,
                                                         objObservations.WaistCircumference, objObservations.Height, objObservations.Weight, objObservations.BPSys,
                                                         objObservations.BPDia, objObservations.HeartRate, objObservations.Notes,
                                                         objObservations.Risk, objObservations.Framingham, practiceid, out error);
                    }
                    WriteLog("DEBUG: ReturnSaveObservations: " + saveObs);
                    if (saveObs > 0)
                    {

                        result = "{\"status\":\"success\",\"message\":\"\"}";
                    }
                    else if (!calltoApi)
                    {
                        error = "Unable to Save Observation. Please Send at least one screening value to save Observation in the INDICI.";
                    }
                    else if (string.IsNullOrWhiteSpace(error))
                    {
                        error = "Unable to Save Observation.Please try again or contact INDICI support.";
                    }
                }
                else if (string.IsNullOrEmpty(error))
                    error = "Invalid token value!";
            }
            catch (Exception ex)
            {
                WriteLog("SaveObservations: ", ex);
                error = ex.Message;
            }

            return SetToJson(result, error);
        }
        [AcceptVerbs("POST")]
        public async Task<HttpResponseMessage> SaveRecall()
        {
            string result = string.Empty;
            string error = string.Empty;
            string authentication = GetAuthorizationToken();
            DateTime dtDueDate = DateTime.MinValue;

            try
            {
                WriteLog("In SaveRecall");

                result = await Request.Content.ReadAsStringAsync();
                Recall objRecall = JsonConvert.DeserializeObject<Recall>(result);

                DateTime.TryParse(objRecall.DueDate, out dtDueDate);
                string practiceid = string.Empty;
                string pho = string.Empty;

                if (!string.IsNullOrEmpty(objRecall.EncounterId) && objRecall.EncounterId.Contains("_"))
                {
                    string Delimiter = "__";
                    string[] splitEncounter = objRecall.EncounterId.Split(new[] { Delimiter }, StringSplitOptions.None);
                    if (splitEncounter.Length == 1) { splitEncounter = objRecall.EncounterId.Split('_'); }
                    if (splitEncounter.Length > 0)
                    {
                        objRecall.EncounterId = GetDcrptValue(splitEncounter[0]);
                        practiceid = "_" + splitEncounter[1];
                        if (practiceid.Contains("302_F3H045"))
                            pho = "SCDHB";
                        if (splitEncounter.Length > 2)
                        {
                            practiceid += "_" + splitEncounter[2];
                        }
                        if (splitEncounter.Length > 3)
                        {
                            practiceid = "_" + splitEncounter[3];
                        }
                    }
                }
                objRecall.PatientId = GetDcrptValue(objRecall.PatientId);
                objRecall.UserId = GetDcrptValue(objRecall.UserId);
                WriteLog("In SaveRecall: PatientId: " + objRecall.PatientId + "--EncounterId:" + objRecall.EncounterId + "--Practiceid:" + practiceid);
                if (HSSDA.InsertAndValidateToken(objRecall.PatientId, objRecall.EncounterId, authentication, practiceid, pho, out error))
                {
                    WriteLog("DEBUG: SaveRecall: " + result);
                    int saveRecall = HSSDA.InsertUpdateRecall(objRecall.PatientId, objRecall.EncounterId, objRecall.UserId, objRecall.Priority, objRecall.Group, dtDueDate, objRecall.Notes,
                                                 objRecall.CategoryId, practiceid, out error);
                    WriteLog("DEBUG: ReturnSaveRecall: " + saveRecall);
                    if (saveRecall > 0)
                    {
                        result = "{\"status\":\"success\",\"message\":\"\"}";
                    }
                    else if (string.IsNullOrWhiteSpace(error))
                    {
                        error = "Unable to Save Recall.Please try again or contact INDICI support.";
                    }
                }
                else if (string.IsNullOrEmpty(error))
                    error = "Invalid token value!";
            }
            catch (Exception ex)
            {
                WriteLog("SaveRecall: ", ex);
                error = ex.Message;
            }

            return SetToJson(result, error);
        }
        [AcceptVerbs("POST")]
        public async Task<HttpResponseMessage> SaveScreeningCode()
        {
            string result = string.Empty;
            string error = string.Empty;
            string authentication = GetAuthorizationToken();

            WriteLog("In SaveScreeningCode");

            try
            {
                result = await Request.Content.ReadAsStringAsync();
                WriteLog("DEBUG: SaveScreeningCode: " + result);

                result = "{\"status\":\"success\",\"message\":\"\"}";
            }
            catch (Exception ex)
            {
                WriteLog("SaveScreeningCode: ", ex);
                error = ex.Message;
            }

            return SetToJson(result, error);
        }
        private string SaveToDMS(byte[] contentData, string postedType, string messageSubject, int categoryId, string practiceid, string practiceid2)
        {
            string documentKey = Guid.NewGuid().ToString();
            string error = string.Empty;

            try
            {
                WriteLog("In SaveToDMS");

                postedType = Utility.Instance.GetBetweenString("/", ";", postedType, false).Trim();

                int documentTypeID = DocumentTypeID(postedType);
                int DMSID = HSSDA.DocumentSave(categoryId, messageSubject, documentTypeID, "HSS", documentKey, contentData, practiceid, practiceid2, out error);

                WriteLog("DEBUG: In SaveToDMS: " + DMSID);

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
        private int DocumentTypeID(string postedType)
        {
            int id = -1;

            try
            {
                string[] docTypes = Utility.Instance.ToString(ConfigurationManager.AppSettings["DMSDocTypes"]).ToLower().Split('|');
                string[] results = Array.FindAll(docTypes, s => s.Contains(postedType));
                id = Convert.ToInt32(results[0].Split(',')[0]);
            }
            catch { }

            return id;
        }
        private string PrepareJSON<T>(T objRoot)
        {
            JsonSerializerSettings jsonSettings = new JsonSerializerSettings();
            jsonSettings.ContractResolver = new CamelCasePropertyNamesContractResolver();
            jsonSettings.StringEscapeHandling = StringEscapeHandling.EscapeHtml;

            return JsonConvert.SerializeObject(objRoot, jsonSettings);
        }
        private HttpResponseMessage SetToJson(string jsonString, string error)
        {
            if (!string.IsNullOrEmpty(error))
                jsonString = "{\"status\":\"fail\",\"message\":\"" + error + "\"}";

            var response = new HttpResponseMessage(HttpStatusCode.OK);
            response.Content = new StringContent(jsonString, Encoding.UTF8, "application/json");

            return response;
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
                    return HSSWebAPI.Models.EncryptionManager.GetDecryptString(value);
                }
            }
            catch (Exception ex)
            {
                return "";
                throw ex;
            }
        }
    }
}