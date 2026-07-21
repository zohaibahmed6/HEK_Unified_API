using DAL.HelperClasses;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DAL.UI
{
    public class GUIDA
    {
        DataSet ds = null;
        public string MessageId = string.Empty;
        public DataSet GetMessageValues() 
        {
            ds = new DataSet();
            Hashtable sqlParams = new Hashtable();
            sqlParams.Add("@pMessageID", null);
            sqlParams.Add("@pMessageType", null);
            sqlParams.Add("@pMessageStatus", null);
            if (ds.Tables.Contains("MessageValues"))
            {
                ds.Tables["MessageValues"].Clear();
                DbAccess.selectStoredProcedure(ds, "[dbo].[uspGetMessageHeader]", sqlParams, "MessageValues");
            }
            else
            {
                DbAccess.selectStoredProcedure(ds, "[dbo].[uspGetMessageHeader]", sqlParams, "MessageValues");
            }
            return ds;
        }

        public DataSet GetFieldValues()
        {
            Hashtable sqlParams = new Hashtable();
            sqlParams.Add("@pMessageID", MessageId);
            sqlParams.Add("@pMessageType", null);
            if (ds.Tables.Contains("FieldValues"))
            {
                ds.Tables["FieldValues"].Clear();
                DbAccess.selectStoredProcedure(ds, "[dbo].[uspGetFieldValues]", sqlParams, "FieldValues");
            }
            else
            {
                DbAccess.selectStoredProcedure(ds, "[dbo].[uspGetFieldValues]", sqlParams, "FieldValues");
            }
            return ds;
        }
    }
}
