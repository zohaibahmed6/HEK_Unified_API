using AWSDoc;
using MHN.DAL.Extentions;
using MHN.Entity.Inbox;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Data.SqlClient;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NLog;

namespace MHN.DAL.DMSAWS
{
    public class DMSAWS
    {
        //public static async Task<DocumentFile> DocumentGetByDocumentKeyAWS(string DocumentKey, int PracticeID = 0)
        //{            
        //    MHN.Utilities.Exception_Handler.ApplicationExceptions.SaveActivityLog("DocumentGetByDocumentKeyAWS DAL .." + "\n DucumentKey : " + DocumentKey);
        //    string DMSID = "";            
        //    DocumentFile objListProviderInboxMenu = new DocumentFile();
        //    AWSDoc.DataModel.DocumentFile documentFile = new AWSDoc.DataModel.DocumentFile();
        //    documentFile = IndiciDMS.GetDocumentStatusFromIndici(DocumentKey, PracticeID);
        //    if (documentFile.IsAWS)
        //    {
        //        AWSDoc.DataModel.DocumentDownlaodInfo documentDownlaodInfo = new AWSDoc.DataModel.DocumentDownlaodInfo();
        //        documentDownlaodInfo = await AWSDoc.DocumentManager.Download(Guid.Parse(DocumentKey));
        //        objListProviderInboxMenu.DocumentData = documentDownlaodInfo.DocumentBytes;
        //        objListProviderInboxMenu.DocumentID = documentFile.DocumentID;
        //        objListProviderInboxMenu.DocumentName = documentFile.DocumentName;
        //        objListProviderInboxMenu.Description = documentFile.Description;
        //        objListProviderInboxMenu.DocumentType = documentFile.DocumentType;
        //    }
        //    else
        //    {
        //        string DMSServer = ConfigurationManager.AppSettings["DMSServer"].ToString();
        //        SqlConnection conn = new SqlConnection(DMSServer);                
        //        List<SqlParameter> sqlParams = new List<SqlParameter>();
        //        if (DMSID == "")
        //        {
        //            sqlParams.Add(new SqlParameter("@pDocumentKey", DocumentKey));
        //        }
        //        else
        //        {
        //            sqlParams.Add(new SqlParameter("@pDocumentKey", DMSID));
        //        }
        //        sqlParams.Add(new SqlParameter("@pPracticeID", PracticeID));
        //        DataSet dsResult = DalHelper.ExecuteDataset(conn, CommandType.StoredProcedure, "[dbo].[uspDocumentGetByDocumentKey]", sqlParams.ToArray());

        //        if (dsResult != null && dsResult.Tables.Count > 0 && dsResult.Tables[0].Rows.Count > 0)
        //            objListProviderInboxMenu = Helper.DataTableToListOld<DocumentFile>(dsResult.Tables[0]).FirstOrDefault();

                
        //    }
        //    return objListProviderInboxMenu;
        //}

        private static bool _IsEnableAWS = false;
        public static bool IsEnableAWS
        {
            get
            {
                _IsEnableAWS = System.Configuration.ConfigurationManager.AppSettings.Get("IsEnableAWS") == "0" ? false : true; 
                return _IsEnableAWS;
            }
            set
            {
                ConfigurationManager.AppSettings.Set("IsEnableAWS", value.ToString());
            }
        }
        public static  DocumentFile DocumentGetByDocumentKey(string DocumentKey, int PracticeID = 0)
        {
            MHN.Utilities.Exception_Handler.ApplicationExceptions.SaveActivityLog("DocumentGetByDocumentKeyAWS DAL .." + "\n DucumentKey : " + DocumentKey);
            string DMSID = "";
            DocumentFile objListProviderInboxMenu = new DocumentFile();
            AWSDoc.DataModel.DocumentFile documentFile = new AWSDoc.DataModel.DocumentFile();
            documentFile = IndiciDMS.GetDocumentStatusFromIndici(DocumentKey, PracticeID);
            if (documentFile.IsAWS)
            {
                AWSDoc.DataModel.DocumentDownlaodInfo documentDownlaodInfo = new AWSDoc.DataModel.DocumentDownlaodInfo();
                documentDownlaodInfo = AWSDoc.DocumentManager.Download(Guid.Parse(DocumentKey)).GetAwaiter().GetResult();
                objListProviderInboxMenu.DocumentData = documentDownlaodInfo.DocumentBytes;
                objListProviderInboxMenu.DocumentID = documentFile.DocumentID;
                objListProviderInboxMenu.DocumentName = documentFile.DocumentName;
                objListProviderInboxMenu.Description = documentFile.Description;
                objListProviderInboxMenu.DocumentType = documentFile.DocumentType;
            }
            else
            {
                string DMSServer = ConfigurationManager.AppSettings["DMSServer"].ToString();
                SqlConnection conn = new SqlConnection(DMSServer);
                List<SqlParameter> sqlParams = new List<SqlParameter>();
                if (DMSID == "")
                {
                    sqlParams.Add(new SqlParameter("@pDocumentKey", DocumentKey));
                }
                else
                {
                    sqlParams.Add(new SqlParameter("@pDocumentKey", DMSID));
                }
                sqlParams.Add(new SqlParameter("@pPracticeID", PracticeID));
                DataSet dsResult = DalHelper.ExecuteDataset(conn, CommandType.StoredProcedure, "[dbo].[uspDocumentGetByDocumentKey]", sqlParams.ToArray());

                if (dsResult != null && dsResult.Tables.Count > 0 && dsResult.Tables[0].Rows.Count > 0)
                    objListProviderInboxMenu = Helper.DataTableToListOld<DocumentFile>(dsResult.Tables[0]).FirstOrDefault();


            }
            return objListProviderInboxMenu;
        }
    }
}
