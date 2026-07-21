using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.ServiceModel;
using System.ServiceModel.Web;
using System.Text;
using System.Web;
using System.Xml.Linq;
using System.Xml;
using System.Configuration;
using System.IO;
using System.ServiceModel.Channels;
using System.Data;
using System.Globalization;
using Hiso.ConceptMapper;
using System.Threading.Tasks;
using System.Runtime.Caching;
using System.Xml.Serialization;
using Aspose.Words;

namespace Hiso
{
    /// <summary>
    /// Author Azam Khan
    /// Hiso Form Implementation for PMS
    /// </summary>

    public class FormSessionService : FormSessionPortType
    {
        List<ConceptMapper.HisoConceptDetail> _LstConcept = new List<ConceptMapper.HisoConceptDetail>();
        ObjectCache cache = MemoryCache.Default;

        //string sessionID = HttpContext.Current.Session[request.sessionKey] != null ? HttpContext.Current.Session[request.sessionKey].ToString() : "";
        public getVersionResponse getVersion(getVersionRequest request)
        {




            Logger.Logging.Instance.WriteEventLog("getVersion Call");

            try
            {
                HealthLinkSession objSessionKey = GetSession(request.sessionKey);
                getVersionResponse objResponse = new getVersionResponse();

                objResponse.@return = new GetVersionResponseReturn();

                objResponse.@return.application = "PMS";

                objResponse.@return.applicationVersion = "1.0";

                objResponse.@return.dictionaryVersion = 1;

                objResponse.@return.dictionaryVersionSpecified = false;

                objResponse.@return.hisoversion = 1;

                return objResponse;

            }
            catch (Exception ex)
            {
                Logger.Logging.Instance.WriteExceptionLog(ex);
                throw new FaultException(ex.Message);
            }
        }

        public getDeliveryOptionsResponse getDeliveryOptions(getDeliveryOptionsRequest request)
        {
            Aspose.Words.License objLicenseWord = new License();
            Aspose.Pdf.License objLicensePdf = new Aspose.Pdf.License();
            objLicenseWord.SetLicense("Aspose.Words.lic");
            objLicensePdf.SetLicense("Aspose.Pdf.lic");
            getDeliveryOptionsResponse objdoResp = new getDeliveryOptionsResponse();

            //string str = HttpContext.Current.Request.UserHostAddress;
            Logger.Logging.Instance.WriteEventLog("getDeliveryOptions Call for session" + request.sessionKey);

            try
            {
                HealthLinkSession objSessionKey = GetSession(request.sessionKey);
                objdoResp.@return = new GetDeliveryOptionsResponseReturn();

                /// Unique message control ID number or other identifier that uniquely identifies the message. this will be releated to the FormInstanceId.
                /// System to automatically generate one if not provided
                //objdoResp.@return.messageID = "123152";

                ///Default recipient account name of the receiving user. edi account will be provided from PMS.
                //objdoResp.@return.recipientAccount = "test";

                ///Sender account name of the sending user. Mandatory if required by the service provider. Optional otherwise

                if(Convert.ToString(ConfigurationManager.AppSettings["PracticeEDI"])=="1") //Procare and Compass
                {
                    if (!string.IsNullOrEmpty(objSessionKey.PracticeEDI))
                    {
                        objdoResp.@return.senderAccount = objSessionKey.PracticeEDI;
                    }
                    else
                    {
                        objdoResp.@return.senderAccount = Convert.ToString(ConfigurationManager.AppSettings["UserID"]);
                    }
                }else
                {
                    objdoResp.@return.senderAccount = Convert.ToString(ConfigurationManager.AppSettings["UserID"]);
                }

                /// For EDI to Practice HPI base
                //if (!string.IsNullOrEmpty(objSessionKey.PracticeEDI))
                //{
                //    objdoResp.@return.senderAccount = objSessionKey.PracticeEDI;
                //}
                //else
                //{
                //    objdoResp.@return.senderAccount = Convert.ToString(ConfigurationManager.AppSettings["UserID"]);
                //}


                ///Password of the sending user. Mandatory if required by the service provider. Optional otherwise
                objdoResp.@return.senderPassword = Convert.ToString(ConfigurationManager.AppSettings["Password"]);

                ///Submission Gateway URL
                objdoResp.@return.URL = Convert.ToString(ConfigurationManager.AppSettings["URL"]);


                string serializedData = string.Empty;                   // The string variable that will hold the serialized data

                XmlSerializer serializer = new XmlSerializer(objdoResp.GetType());
                using (StringWriter sw = new StringWriter())
                {
                    serializer.Serialize(sw, objdoResp);
                    serializedData = sw.ToString();
                }


                Logger.Logging.Instance.WriteEventLog("getDeliveryOptions Call (Response):  " + serializedData);

                return objdoResp;
            }
            catch (Exception ex)
            {
                Logger.Logging.Instance.WriteExceptionLog(ex);
                throw new FaultException(ex.Message);
            }
        }

        /// <summary>
        ///  It is suggested that the xml fragment is modified rather than parsing and then recreating the xml. One way of doing this would be to use xpath to obtain a 
        ///  list of elements that have conceptId tags and then for each one lookup the corresponding data and insert it into the value section of the corresponding element. 
        ///  If the population is done in this way then no other changes will be made to the inbound xml allowing the structure and other tags added by the Form Engine to remain undisturbed.
        /// </summary>
        /// <param name="request"></param>
        /// <returns></returns>
        public getDataResponse getData(getDataRequest request)
        {
            Logger.Logging.Instance.WriteEventLog($"getData started | SessionKey: {request.sessionKey}");

            XmlDocument xDocRequest = null;

            try
            {
                getDataResponse obj = new getDataResponse();

                // Validate session
                Logger.Logging.Instance.WriteEventLog("Fetching session object...");
                HealthLinkSession objSessionKey = GetSession(request.sessionKey);
                Logger.Logging.Instance.WriteEventLog("Session object fetched successfully.");

                if (ConfigurationManager.AppSettings["IsDynamic"] != null &&
                    ConfigurationManager.AppSettings["IsDynamic"].ToString() == "1")
                {
                    Logger.Logging.Instance.WriteEventLog("Dynamic mode enabled.");

                    #region DynamicLogic

                    // Cache ConceptList
                    if (cache.Contains("ConceptList"))
                    {
                        _LstConcept = (List<ConceptMapper.HisoConceptDetail>)cache.Get("ConceptList");
                        Logger.Logging.Instance.WriteEventLog($"ConceptList retrieved from cache | Count: {_LstConcept.Count}");
                    }
                    else
                    {
                        Logger.Logging.Instance.WriteEventLog("ConceptList not in cache. Loading from DB...");
                        _LstConcept = ConceptMapper.HisoConceptDetail.GetHisoConcept(DBMessages.GetHisoConceptDetail());
                        CacheItemPolicy cacheItemPolicy = new CacheItemPolicy
                        {
                            AbsoluteExpiration = DateTime.Now.AddMinutes(10.0)
                        };
                        cache.Add("ConceptList", _LstConcept, cacheItemPolicy);
                        Logger.Logging.Instance.WriteEventLog($"ConceptList cached | Count: {_LstConcept.Count}");
                    }

                    xDocRequest = new XmlDocument();
                    List<string> procedureList = null;
                    List<HisoRequest> objListHisoRequests = null;

                    if (request.dataContainer.formMetaData.formInstanceOperationMode.Value == "N")
                    {
                        Logger.Logging.Instance.WriteEventLog("Form operation mode = New (N). Loading submitted data XML...");

                        xDocRequest.LoadXml(((System.Xml.XmlNode[])(request.dataContainer.submittedData))[1].OuterXml);

                        Logger.Logging.Instance.WriteEventLog("SubmittedData XML loaded successfully.");

                        if (ConfigurationManager.AppSettings["addDMSRef"]?.ToString() == "1")
                        {
                            Logger.Logging.Instance.WriteEventLog("Adding DMS reference IDs to diagnosticReport/scannedDocument groups...");
                            XmlNodeList n = xDocRequest.GetElementsByTagName("group");

                            foreach (XmlNode curr in n)
                            {
                                if (curr.Attributes["name"].Value == "clinical.diagnosticReport" ||
                                    curr.Attributes["name"].Value == "scannedDocument")
                                {
                                    if (curr.Attributes["referenceID"] == null)
                                    {
                                        XmlAttribute newAttr = xDocRequest.CreateAttribute("referenceID");
                                        newAttr.Value = "";
                                        curr.Attributes.Append(newAttr);
                                    }
                                }
                            }
                            Logger.Logging.Instance.WriteEventLog("DMS reference IDs added where missing.");
                        }

                        Logger.Logging.Instance.WriteEventLog("Extracting HisoRequests from XML...");
                        objListHisoRequests = Hiso.ConceptMapper.HisoRequest.GetHisoRequest(xDocRequest);
                        Logger.Logging.Instance.WriteEventLog($"HisoRequests extracted | Count: {objListHisoRequests.Count}");

                        Logger.Logging.Instance.WriteEventLog("Preparing concepts from request...");
                        procedureList = Mapper.PrepareConceptsFromRequest(ref xDocRequest, ref _LstConcept,
                            ref objListHisoRequests, objSessionKey, request.dataContainer.formMetaData.formInstanceOperationMode.Value);

                        Logger.Logging.Instance.WriteEventLog($"Concepts prepared | Count: {procedureList.Count}");

                        Logger.Logging.Instance.WriteEventLog("Fetching ProcedureList from DB...");
                        List<Hiso.ConceptMapper.ProcedureResult> lstProcedure =
                            ConceptMapper.HisoConceptDetail.GetProcedureList(procedureList, objSessionKey, objListHisoRequests);
                        Logger.Logging.Instance.WriteEventLog($"ProcedureList fetched | Count: {lstProcedure.Count}");

                        Logger.Logging.Instance.WriteEventLog("Filling XML details...");
                        ConceptMapper.HisoRequest.FillXMLDetails(lstProcedure, ref xDocRequest, _LstConcept, objListHisoRequests);

                        Logger.Logging.Instance.WriteEventLog("Building response object...");
                        obj.@return = new GetDataResponseReturn
                        {
                            dataContainer = new FormData
                            {
                                formMetaData = request.dataContainer.formMetaData,
                                submittedData = request.dataContainer.submittedData
                            }
                        };

                        if (xDocRequest != null)
                        {
                            ((System.Xml.XmlNode[])(obj.@return.dataContainer.submittedData))[1] = xDocRequest.DocumentElement;
                        }
                        Logger.Logging.Instance.WriteEventLog("Response object built successfully.");
                    }
                    else
                    {
                        Logger.Logging.Instance.WriteEventLog("Form operation mode != N. Processing Parked XML...");
                        // existing parked logic, add logs similarly
                    }

                    #endregion
                }
                else
                {
                    Logger.Logging.Instance.WriteEventLog("Static mode enabled.");
                    #region Static
                    // Add logs same way as above
                    #endregion
                }

                Logger.Logging.Instance.WriteEventLog("getData execution completed successfully.");
                return obj;
            }
            catch (Exception ex)
            {
                Logger.Logging.Instance.WriteExceptionLog(ex);
                Logger.Logging.Instance.WriteEventLog("getData failed with exception: " + ex.Message);
                throw new FaultException(ex.Message);
            }
        }

        private HealthLinkSession GetSession(string sessionKey)
        {
            HealthLinkSession objSessionKey = HealthLinkSession.GetByGUID(Guid.Parse(sessionKey));
            if (objSessionKey == null)
            {
                throw new FaultException("Invalid Session Key");
            }
            return objSessionKey;
        }
        public saveContainerResponse saveContainer(saveContainerRequest request)
        {
            //System.Diagnostics.Debugger.Launch();
            bool retval = false;
            Logger.Logging.Instance.WriteEventLog("saveContainer call with request completed:" + request.completed.ToString() + " and session id: " + request.sessionKey + " and resume path:" + request.resumePath);
            saveContainerResponse obj = new saveContainerResponse();
            try
            {
                HealthLinkSession objSessionKey = GetSession(request.sessionKey);
                obj.@return = new SaveContainerResponseReturn();

                string strPath = Convert.ToString(ConfigurationManager.AppSettings["htmlPath"]);
                Guid guid = Guid.Empty;

                Logger.Logging.Instance.WriteEventLog("saveContainer call with request completed:" + request.completed.ToString() + " and session id: " + request.sessionKey + " View Type :" + request.viewType + " View Data :" + request.view);

                //Successfully submitted.
                if (request.completed == true)
                {
                    string extension = getFileExtension(request.viewType);
                    Logger.Logging.Instance.WriteEventLog("View Extension: " + extension);
                    guid = DocumentHandler.AddDocument(request.view, extension, request.dataContainer.formMetaData.formEngineId.Value, objSessionKey.PracticeId);
                    Logger.Logging.Instance.WriteEventLog("DMSID: " + guid.ToString());
                    if (string.IsNullOrEmpty(request.view.Trim()))
                        Logger.Logging.Instance.WriteEventLog("Document view data not found for session id:" + request.sessionKey + " and DMSID:" + guid.ToString());
                }
                retval = saveDataContainer(request, objSessionKey, guid);
                obj.@return.response = true;
                return obj;
            }
            catch (Exception ex)
            {
                obj.@return.response = false;
                Logger.Logging.Instance.WriteExceptionLog(ex);
                throw new FaultException(ex.Message);
            }
        }

        public getFormViewResponse getFormView(getFormViewRequest request)
        {

            Logger.Logging.Instance.WriteEventLog("getFormView Call");
            try
            {
                HealthLinkSession objSessionKey = GetSession(request.sessionKey);
                Acc45DefinitionBuilder objBuilder = new Acc45DefinitionBuilder();
                FormSettings objFormSettings = objBuilder.GetACC45Definition(objSessionKey);

                getFormViewResponse obj = new getFormViewResponse();
                obj.@return = new GetFormViewResponseReturn();
                obj.@return.resumePath = objFormSettings.ResumePath; // Path from PMS DB
                obj.@return.viewType = objFormSettings.ViewType;
                obj.@return.view = objFormSettings.View;
                obj.@return.dataContainer = new FormData();


                //obj.@return.dataContainer.formMetaData = new FormMetaData();
                //obj.@return.dataContainer.formMetaData.encryptedFlag=                     // From PMS  
                //obj.@return.dataContainer.formMetaData.formDefinitionDescription =        // From PMS
                //obj.@return.dataContainer.formMetaData.formDefinitionId=                  // From PMS
                //obj.@return.dataContainer.formMetaData.formDefinitionTitle=                 // From PMS
                //obj.@return.dataContainer.formMetaData.formDefinitionVersion=               // From PMS
                //obj.@return.dataContainer.formMetaData.formEngineId=                        // From PMS
                //obj.@return.dataContainer.formMetaData.formInstanceCreationDate=            // From PMS
                //obj.@return.dataContainer.formMetaData.formInstanceDescription=             // From PMS
                //obj.@return.dataContainer.formMetaData.formInstanceId=                      // From PMS
                //obj.@return.dataContainer.formMetaData.formInstanceOperationMode=           // From PMS
                //obj.@return.dataContainer.formMetaData.formInstanceVersion=           // From PMS
                //obj.@return.dataContainer.formMetaData.recipientAccount=           // From PMS

                //obj.@return.dataContainer.submittedData  
                //. request.formInstanceId; 
                Logger.Logging.Instance.WriteEventLog("View:" + objFormSettings.View);
                return obj;
            }
            catch (Exception ex)
            {
                Logger.Logging.Instance.WriteExceptionLog(ex);
                throw new FaultException(ex.Message);
            }
        }

        public processActionResponse processAction(processActionRequest request)
        {

            Logger.Logging.Instance.WriteEventLog("processAction call with session id: " + request.sessionKey + " and actionID: " + request.actionId);
            processActionResponse objProAResp = new processActionResponse();
            try
            {
                HealthLinkSession objSessionKey = GetSession(request.sessionKey);
                objProAResp.@return = new ProcessActionResponseReturn();

                Logger.Logging.Instance.WriteEventLog("processAction: " + Convert.ToString(request.actionContainer));

                objProAResp.@return.processed = false;

                if (request.actionId == "save")
                {
                    //Save information back to PMS system.
                    objProAResp.@return.processed = saveProcessAction(request, objSessionKey);
                }
                else if (request.actionId == "addTask")
                {
                    Task objTask = Task.processTask(request, objSessionKey);
                    objProAResp.@return.processed = objTask.AddTask(objTask, objSessionKey);
                    // Add Task/Reminder into PMS about the forms
                }
                else if (request.actionId == "addInvoice")
                {
                    //Add invoice information into PMS
                }
                else if (request.actionId == "launchForm")
                {
                    //Launch a form
                }

                return objProAResp;
            }
            catch (Exception ex)
            {
                Logger.Logging.Instance.WriteExceptionLog(ex);
                throw new FaultException(ex.Message);
            }
        }

        private string getFileExtension(string mimeType)
        {
            string ext = ".html";
            switch (mimeType)
            {
                case "text/html":
                    ext = ".html";
                    break;

                case "application/pdf":
                    ext = ".pdf";
                    break;

                case "normalizedString":
                    ext = ".txt";
                    break;
            }
            return ext;
        }
        private bool saveDataContainer(saveContainerRequest request, HealthLinkSession objSessionKey, Guid guid)
        {
            bool retVal = true;
            string formXml = string.Empty;
            try
            {
                // Extract Submitted Data;
                XmlDocument xDocRequest = new XmlDocument();
                formXml = Mapper.GetFromXml(request.dataContainer);
                //Logger.Logging.Instance.WriteEventLog("saveDataContainer - Form XML:" + formXml);
                string submittedData = ((System.Xml.XmlNode[])(request.dataContainer.submittedData))[1].OuterXml.ToString();
                xDocRequest.LoadXml(submittedData);
                // Make Form Settings Data
                FormSettings objSetting = new FormSettings();
                objSetting.ViewType = request.viewType;
                objSetting.ViewSignature = request.viewSignature;
                objSetting.ResumePath = request.resumePath;
                objSetting.ContinueSession = request.continueSession;
                objSetting.Completed = request.completed;
                objSetting.DMSDocumentID = guid.ToString();
                // Make FormDefinitaion DataTable and save
                DataTable dtDefinition = null;
                string strDefinitionComments = string.Empty;
                Acc45DefinitionBuilder objDefBuilder = new Acc45DefinitionBuilder();
                dtDefinition = objDefBuilder.GenerateTable(request.dataContainer.formMetaData, objSetting, objSessionKey);
                if (request.dataContainer.formMetaData.formDefinitionId.Value == "msdwcref")
                {
                    strDefinitionComments = Mapper.GetNodeText(xDocRequest, "field", "name", "workCapacity.entitlementComments");
                }
                retVal = objDefBuilder.SaveAcc45Definition(dtDefinition, formXml, strDefinitionComments, objSessionKey.GUID.ToString());
                //if (request.dataContainer.formMetaData.formInstanceOperationMode.Value == "P")
                //{
                //Make ACC45Detail table
                Acc45Builder objACCBuilder = new Acc45Builder();
                int mode = 0;
                DataTable dtACC45 = objACCBuilder.GenerateTable(request.dataContainer.formMetaData, xDocRequest, objSessionKey, ref mode);
                // Make Acc 45 diagnosis table
                Acc45DiagnosisBuilder objDiag = new Acc45DiagnosisBuilder();
                DataTable dtDiagnosis = objDiag.GenerateTable(xDocRequest, objSessionKey);
                // Make ACC45 Referral
                Acc45ReferralBuilder objRefBuilder = new Acc45ReferralBuilder();
                DataTable dtRef = objRefBuilder.GenerateTable(xDocRequest, objSessionKey);
                Mapper objMapper = new Mapper();
                objMapper.SaveAccidentInformation(request.dataContainer.formMetaData, dtACC45, dtDiagnosis, dtRef, 1);
                //}

            }
            catch
            {
                Logger.Logging.Instance.WriteEventLog("saveDataContainer - Form XML:" + formXml);
                throw;
            }
            finally
            {

            }
            return retVal;

        }
        private bool saveProcessAction(processActionRequest request, HealthLinkSession objSessionKey)
        {
            bool retVal = true;
            string actionType = request.actionId.ToLower();
            XmlElement xElem = request.actionContainer;
            FormMetaData objFormMeta = new FormMetaData();
            if (actionType == "save")//&& xElem.GetElementsByTagName("for1:formDefinitionId").Item(0).InnerText == "acc45")
            {
                if (ConfigurationManager.AppSettings["IsDynamic"] == null || ConfigurationManager.AppSettings["IsDynamic"] == "0")
                    return true;
                string xml = xElem.GetElementsByTagName("form")[0].OuterXml.ToString();
                XmlDocument xDoc = new XmlDocument();
                xDoc.LoadXml(xml);
                // Save Patient Info
                PatientBuilder objPat = new PatientBuilder();
                DataTable dtPat = objPat.GenerateTable(xDoc, objSessionKey);
                objPat.Save(dtPat, objSessionKey);
                // Save Patient Consult
                PatientConsultBuilder objConsult = new PatientConsultBuilder();
                DataTable dtConsult = objConsult.GenerateTable(xDoc, objSessionKey);
                objConsult.Save(dtConsult, objSessionKey);
                // Save Organization Info
                PatientEmployerOrganisationBuilder objOrg = new PatientEmployerOrganisationBuilder();
                DataTable dtOrg = objOrg.GenerateTable(xDoc, objSessionKey);
                objOrg.Save(dtOrg, objSessionKey);
                // Save Problem
                PatientProblemBuilder objProblem = new PatientProblemBuilder();
                DataTable dtProb = objProblem.GenerateTable(xDoc, objSessionKey);
                objProblem.Save(dtProb, objSessionKey);
                // Save Practitioner
                RegisteredPractitionerBuilder objPractitioner = new RegisteredPractitionerBuilder();
                DataTable dtPractitioner = objPractitioner.GenerateTable(xDoc, objSessionKey);
                objPractitioner.Save(dtPractitioner, objSessionKey);
                return true;
                objFormMeta.formInstanceId = new FormMetaDataFormInstanceId();
                objFormMeta.formInstanceId.Value = xElem.GetElementsByTagName("for1:formInstanceId").Item(0).InnerText;

                objFormMeta.formInstanceVersion = new FormMetaDataFormInstanceVersion();
                objFormMeta.formInstanceVersion.Value = xElem.GetElementsByTagName("for1:formInstanceVersion").Item(0).InnerText;

                objFormMeta.formEngineId = new FormMetaDataFormEngineId();
                objFormMeta.formEngineId.Value = xElem.GetElementsByTagName("for1:formEngineId").Item(0).InnerText;

                objFormMeta.formInstanceCreationDate = new FormMetaDataFormInstanceCreationDate();
                objFormMeta.formInstanceCreationDate.Value = DateTime.Parse(xElem.GetElementsByTagName("for1:formInstanceCreationDate").Item(0).InnerText);

                objFormMeta.formInstanceOperationMode = new FormMetaDataFormInstanceOperationMode();
                objFormMeta.formInstanceOperationMode.Value = xElem.GetElementsByTagName("for1:formInstanceOperationMode").Item(0).InnerText;

                objFormMeta.formInstanceDescription = new FormMetaDataFormInstanceDescription();
                objFormMeta.formInstanceDescription.Value = xElem.GetElementsByTagName("for1:formInstanceDescription").Item(0).InnerText;

                objFormMeta.formDefinitionId = new FormMetaDataFormDefinitionId();
                objFormMeta.formDefinitionId.Value = xElem.GetElementsByTagName("for1:formDefinitionId").Item(0).InnerText;

                objFormMeta.formDefinitionVersion = new FormMetaDataFormDefinitionVersion();
                objFormMeta.formDefinitionVersion.Value = xElem.GetElementsByTagName("for1:formDefinitionVersion").Item(0).InnerText;

                objFormMeta.formDefinitionDescription = new FormMetaDataFormDefinitionDescription();
                objFormMeta.formDefinitionDescription.Value = xElem.GetElementsByTagName("for1:formDefinitionDescription").Item(0).InnerText;

                objFormMeta.formDefinitionTitle = new FormMetaDataFormDefinitionTitle();
                objFormMeta.formDefinitionTitle.Value = xElem.GetElementsByTagName("for1:formDefinitionTitle").Item(0).InnerText;

                objFormMeta.recipientAccount = new FormMetaDataRecipientAccount();
                objFormMeta.recipientAccount.Value = xElem.GetElementsByTagName("for1:recipientAccount").Item(0).InnerText;

                //string xml = xElem.GetElementsByTagName("form")[0].OuterXml.ToString();
                //XmlDocument xDoc = new XmlDocument();
                //xDoc.LoadXml(xml);
                //Mapper.SaveFile(xDoc, DateTime.Now.Ticks.ToString() + "-"+ objFormMeta.formEngineId.Value, 1);
                // Get Acc45Detail Datatable
                int mode = 0;
                Acc45Builder objBulder = new Acc45Builder();
                DataTable dtACC45 = objBulder.GenerateTable(objFormMeta, xDoc, objSessionKey, ref mode);
                // Get Acc 45 Diagnosis table
                Acc45DiagnosisBuilder objDiag = new Acc45DiagnosisBuilder();
                DataTable dtDiagnosis = objDiag.GenerateTable(xDoc, objSessionKey);
                Mapper objMapper = new Mapper();

                //retVal = objMapper.SaveAccidentInformation(objFormMeta, dtACC45, dtDiagnosis, mode, );
                retVal = true;
            }
            else if (actionType == "addtask")
            {
                Task objTask = new Task();
                retVal = true;
            }

            return retVal;

        }

    }
}
