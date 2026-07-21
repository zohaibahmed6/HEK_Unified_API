using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using DMSProxy.DMSService;


namespace Hiso
{
    public class DocumentHandler
    {
        public static Guid AddDocument(string base64string, string extension, string fileName,int PracticeID)
        {
            //DocumentCollection documentCollection;
            Guid GUID = Guid.NewGuid();
            if (string.IsNullOrEmpty(base64string.Trim()))
                return Guid.Empty;
            
            
            //GUID = Guid.NewGuid();
            byte[] viewBytes = null;
            int DMSDocumentTypeId = 0;
            if (extension == ".html")
            {
                viewBytes = System.Text.Encoding.UTF8.GetBytes(base64string);
                DMSDocumentTypeId = int.Parse(System.Configuration.ConfigurationManager.AppSettings["DMSHTMLTypeId"].ToString());
            }
            else if (extension == ".pdf")
            {
                viewBytes = Convert.FromBase64String(base64string);
                DMSDocumentTypeId = int.Parse(System.Configuration.ConfigurationManager.AppSettings["DMSPDFTypeId"].ToString());
            }
            else {
                viewBytes = System.Text.Encoding.UTF8.GetBytes(base64string);
                DMSDocumentTypeId = int.Parse(System.Configuration.ConfigurationManager.AppSettings["DMSHTMLTypeId"].ToString());
            }
            Logger.Logging.Instance.WriteEventLog("AddDirectDMS: " + Convert.ToString(System.Configuration.ConfigurationManager.AppSettings["AddDirectDMS"]));
            if (Convert.ToString(System.Configuration.ConfigurationManager.AppSettings["AddDirectDMS"]) == "1")
            {
                try
                {
                    Logger.Logging.Instance.WriteEventLog("AddDirectDMS: Start");
                    Logger.Logging.Instance.WriteEventLog("GUID before: "+ GUID.ToString());
                    Mapper.SaveDocumentToDMS(3, 3, fileName, DMSDocumentTypeId, "", GUID.ToString(), viewBytes, "", false, PracticeID);
                    Logger.Logging.Instance.WriteEventLog("GUID before: " + GUID.ToString());
                }
                catch (Exception ex)
                {
                    Logger.Logging.Instance.WriteExceptionLog(ex);
                }
                return GUID;
            }
            else
            {
                DMSProxy.DMSService.DocumentCollection documentCollection;
                documentCollection = new DMSProxy.DMSService.DocumentCollection()
                {
                    DocumentKey = GUID,
                    CategoryID = 1,
                    DocumentTypeID = DMSDocumentTypeId,
                    DocumentName = fileName,
                    Description = fileName,
                    ProfileID = "1",
                };

                documentCollection.DocumentData = viewBytes;

                ////try
                ////{
                ////    return new Guid(
                //    DMSProxy.DMSProxy.GetInstance().SaveDocumentToDMS(fileName, viewBytes, extension));
                ////}
                ////catch (Exception ex)
                ////{
                ////    throw ex;
                ////}

                try
                {
                    if (!DMSProxy.DMSProxy.InstanceDMSProxy.FileUploaded)
                    {
                        DMSProxy.DMSProxy.InstanceDMSProxy.SaveDocument(documentCollection, false, fileName);
                        return GUID;
                    }
                    else
                    {
                        return Guid.Empty;
                    }
                }
                catch (Exception ex)
                {
                    throw ex;
                }
            }


            //documentCollection = new DocumentCollection()
            //{
            //    DocumentKey = GUID,
            //    CategoryID = 1,
            //    DocumentTypeID = DMSDocumentTypeId,
            //    DocumentName = fileName,
            //    Description = fileName,
            //    ProfileID = "1",
            //};
            //documentCollection.DocumentData = viewBytes;
        }
        public string GetDocumentData(FormMetaDataFormInstanceId objFormInstanceId) {
            return "";
        }
    }
}