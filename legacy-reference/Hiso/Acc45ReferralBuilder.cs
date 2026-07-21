using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Web;
using System.Xml;

namespace Hiso
{
    public class Acc45ReferralBuilder
    {

        public DataTable GenerateTable(XmlDocument xDoc, HealthLinkSession objSessionKey)
        {
            string[] ACC45DiadColumns = { "ACCRefID", "Notes", "UpdatedBy" };
            DataTable dtMapping = Mapper.GetConceptMappingTable();
            // Make ACC45 Diagnostics Table and add columns to datatable
            DataTable dtReferral = new DataTable();
            DataRow drRef = null;//dtACC45Diag.NewRow();
            Dictionary<string, string> dictAcc45DiagData = new Dictionary<string, string>();
            for (int i = 0; i < ACC45DiadColumns.Length; i++)
            {
                dtReferral.Columns.Add(ACC45DiadColumns[i]);
                //drRef[ACC45DiadColumns[i]] = DBNull.Value;
            }
            XmlNodeList groups = xDoc.DocumentElement.GetElementsByTagName("group");
            foreach (XmlNode xn in groups)
            {
                XmlNodeList groupChilds = null;
                if (xn.Attributes["name"] != null && xn.Attributes["name"].Value == "accident.referral")
                {
                    drRef = dtReferral.NewRow();
                    groupChilds = xn.ChildNodes;

                }
                else
                {
                    drRef = null;
                }
                if (drRef != null && groupChilds.Count > 0)
                {
                    drRef["UpdatedBy"] = objSessionKey.ProviderId;
                    foreach (XmlNode gc in groupChilds)
                    {
                        string attrName = string.Empty;
                        string value = string.Empty;
                        if (gc.Attributes["conceptName"] != null && gc.Attributes["conceptName"].Value != "")
                        {
                            attrName = gc.Attributes["conceptName"].Value;

                        }
                        else if (gc.Attributes["name"] != null)
                        {
                            attrName = gc.Attributes["name"].Value;
                        }
                        value = gc.InnerText;
                        DataRow[] rows = dtMapping.Select("ConceptName='" + attrName + "' OR Description = '" + attrName + "'");
                        if (rows.Length > 0)
                        {
                            string columnName = rows[0]["ColumnName"].ToString();
                            if (dtReferral.Columns[columnName] != null)
                            {
                                if (attrName == "treatmentProvider" && Utitlity.IsNumeric(value) == false)
                                {
                                    drRef[columnName] = value.Split(',')[0].Trim();
                                }
                                else
                                {
                                    drRef[columnName] = value;
                                }
                            }
                        }
                    }
                    dtReferral.Rows.Add(drRef);
                }
            }
            return dtReferral;



        }
    }
}