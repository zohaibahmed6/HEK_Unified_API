using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Data;
using System.Xml;

namespace Hiso
{
    public class Acc45Builder
    {
        public DataTable GenerateTable(FormMetaData objMetaData, XmlDocument xDoc, HealthLinkSession objSession, ref int mode)
        {
            string[] strColumns = Utitlity.GetColumnNameByTableName("UDT_tblACC45Detail");
            DataTable dtACC45 = new DataTable();
            
            DataRow dr = dtACC45.NewRow();
            for (int i = 0; i < strColumns.Length; i++) {
                string colName = strColumns[i];
                dtACC45.Columns.Add(colName);
                if(colName== "IsContactACC")
                    dr[colName] = false;
                else
                    dr[colName] = DBNull.Value;
            }
            dr["ACC45ID"] = 0;
            if (objSession.ReferenceId > 0) {
                dr["ACC45ID"] = objSession.ReferenceId;
            }
            XmlNodeList sectionList = xDoc.DocumentElement.GetElementsByTagName("section");
            Dictionary<string, string> dictAcc45Data = new Dictionary<string, string>();
            DataTable dtMapping = Mapper.GetConceptMappingTable();
            foreach (XmlNode section in sectionList)
            {
                if (section.Attributes["name"] != null && (section.Attributes["name"].Value == "patientDemographicsWriteBack" || section.Attributes["name"].Value == "accidentWriteBack" || section.Attributes["name"].Value == "patientAndAccident" || section.Attributes["name"].Value == "injuryDiagnosis" || section.Attributes["name"].Value == "administrative"))
                {
                    XmlNodeList xnFields = section.ChildNodes;
                    foreach (XmlNode xn in xnFields)
                    {
                        if (xn.Name == "field")
                        {
                            if (xn.Attributes["conceptName"] != null && xn.Attributes["conceptName"].Value != "")
                            {
                                dictAcc45Data.Add(xn.Attributes["conceptName"].Value, xn.InnerText);
                            }
                            else if (xn.Attributes["name"] != null && xn.Attributes["name"].Value != "")
                            {
                                dictAcc45Data.Add(xn.Attributes["name"].Value, xn.InnerText);
                            }
                        }
                    }



                    foreach (KeyValuePair<string, string> item in dictAcc45Data)
                    {
                        DataRow[] rows = dtMapping.Select("ConceptName = '" + item.Key + "' OR Description = '" + item.Key + "'");
                        if (rows.Length > 0)
                        {
                            string columnName = rows[0]["ColumnName"].ToString();
                            if (dtACC45.Columns[columnName] != null)
                            {
                                if (item.Value == "Y")
                                    dr[columnName] = true;
                                else if (item.Value == "N")
                                    dr[columnName] = false;
                                else if (columnName.ToLower() == "employmentstatus")
                                {
                                    if (Hiso.Utitlity.IsNumeric(item.Value))
                                    {
                                        dr[columnName] = item.Value;
                                    }
                                    else if (item.Value.Contains(","))
                                    {
                                        dr[columnName] = int.Parse(item.Value.Split(',')[0]);
                                    }
                                    else if (item.Value.Contains(";"))
                                    {
                                        dr[columnName] = int.Parse(item.Value.Split(';')[0]);
                                    }
                                }
                                else
                                    dr[columnName] = item.Value;
                            }
                        }
                    }
                    //if (section.Attributes["name"].Value == "patientDemographicsWriteBack")
                    //{
                    //    mode = 3;
                    //    continue;
                    //}
                    //else if (section.Attributes["name"].Value == "accidentWriteBack")
                    //{
                    //    mode = 2;
                    //    continue;
                    //}
                    //else
                    //{
                    //    mode = 1;
                    //    continue;
                    //}
                }
                
            }
            dtACC45.Rows.Add(dr);
            return dtACC45;
        }
        
    }
}
public class Enumeration
{

    public enum ACC45Status { Pending = 1, InProgress = 2, Completed = 3 }
}
public class FormSettings
{

    public string ViewType { get; set; }
    public string ViewSignature { get; set; }
    public string ResumePath { get; set; }
    public bool Completed { get; set; }
    public bool ContinueSession { get; set; }
    public string DMSDocumentID { get; set; }
    public string View { get; set; }
    public string FormXML { get; set; }

}


