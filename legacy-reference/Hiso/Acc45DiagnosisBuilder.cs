using System;
using System.Collections.Generic;
using System.Linq;
using System.Data;
using System.Xml;

namespace Hiso
{
    public class Acc45DiagnosisBuilder
    {
        public DataTable GenerateTable(XmlDocument xDoc, HealthLinkSession objSessionKey)
        {
            string[] ACC45DiadColumns = { "Acc45DiagnosisID", "ACC45ID", "DiagnosisDescription", "CodeSystem", "Code", "SideCode", "SideDescription", "QualifierCode", "QualifierDescription", "IsActive", "IsDeleted", "InsertedBy", "UpdatedBy", "InsertedAt", "UpdatedAt" };
            DataTable dtMapping = Mapper.GetConceptMappingTable();
            // Make ACC45 Diagnostics Table and add columns to datatable
            DataTable dtACC45Diag = new DataTable();
            DataRow drDiag = null;//dtACC45Diag.NewRow();
            Dictionary<string, string> dictAcc45DiagData = new Dictionary<string, string>();
            for (int i = 0; i < ACC45DiadColumns.Length; i++)
            {
                dtACC45Diag.Columns.Add(ACC45DiadColumns[i]);
                //drDiag[ACC45DiadColumns[i]] = DBNull.Value;
            }
            XmlNodeList groups = xDoc.DocumentElement.GetElementsByTagName("group");
            foreach (XmlNode xn in groups)
            {
                XmlNodeList groupChilds = null;
                if (xn.Attributes["name"] != null && xn.Attributes["name"].Value == "accident.diagnosis")
                {
                    drDiag = dtACC45Diag.NewRow();
                    groupChilds = xn.ChildNodes;

                }
                else if (xn.Attributes["conceptName"] != null && xn.Attributes["conceptName"].Value == "Patient_Accident_Diagnosis")
                {
                    drDiag = dtACC45Diag.NewRow();
                    groupChilds = xn.ChildNodes;
                }
                else {
                    drDiag = null;
                }
                if (drDiag != null && groupChilds.Count > 0)
                {
                    drDiag["ACC45ID"] = 0;
                    drDiag["Acc45DiagnosisID"] = 0;
                    drDiag["IsActive"] = true;
                    drDiag["IsDeleted"] = false;
                    drDiag["InsertedBy"] = objSessionKey.ProviderId;
                    drDiag["UpdatedBy"] = objSessionKey.ProviderId;
                    drDiag["InsertedAt"] = DateTime.Now.ToString("s");
                    drDiag["UpdatedAt"] = DateTime.Now.ToString("s");
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
                            if (dtACC45Diag.Columns[columnName] != null)
                            {
                                if (attrName == "side" && Utitlity.IsNumeric(value) == false)
                                {
                                    drDiag[columnName] = value.Split(',')[0];
                                    drDiag["SideDescription"] = value.Split(',')[1];
                                }
                                else
                                {
                                    drDiag[columnName] = value;
                                }
                            }
                        }
                    }
                    dtACC45Diag.Rows.Add(drDiag);
                }
            }
            return dtACC45Diag;



        }
    }
}