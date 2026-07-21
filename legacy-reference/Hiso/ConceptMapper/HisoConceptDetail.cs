using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Data;
using System.Xml;
using System.Xml.Linq;
using System.Threading.Tasks;
using System.Configuration;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.Threading;

namespace Hiso.ConceptMapper
{
    public class HisoConceptDetail
    {
        public string HisoConceptID { get; set; }
        public string ConceptName { get; set; }
        public string HisoGroupID { get; set; }
        public string HisoConceptDetailName { get; set; }
        public string HisoCategory { get; set; }
        public string HisoID { get; set; }
        public string Defination { get; set; }
        public string HisoDataType { get; set; }
        public string TableName { get; set; }
        public string TableAlias { get; set; }
        public string TableColumn { get; set; }
        public string ColumnAlias { get; set; }
        public string IsActive { get; set; }
        public string CodingSystem { get; set; }
        public string QualifierID { get; set; }
        public string QualifierName { get; set; }
        public string ProcedureName { get; set; }
        public bool IsFixed { get; set; }
        public string Sort { get; set; }
        //public static string[] QualifierList = { "271649006", "271650006", "75367002", "27113001", "50373000", "60621009", "14647-2", "14646-4", "14927-8", "39469-2", "14682-9", "14959-1", "33914-3", "4548-4", "XNZ0436", "73211009", "46635009", "44054006", "99006", "6W", "3M", "5M", "15M", "11Y" };
        public static string[] QualifierList = ConfigurationManager.AppSettings["QualifierList"].ToString().Split(',');

        public static string[] MeasurementGroup = { "Patient_Measurement", "Patient_TestResult", "Patient_Condition_Exists", "Patient_DateLastImmunised", "Patient_FullyImmunised" };

        public static List<ConceptMapper.HisoConceptDetail> GetHisoConcept(DataTable dt)
        {
            return Utitlity.ToGenList<HisoConceptDetail>(dt);
        }

        public static List<ProcedureResult> GetProcedureList(
     List<string> lstProcedure,
     HealthLinkSession objSession,
     List<HisoRequest> objListHisoRequests)
        {
            var results = new List<ProcedureResult>();

            try
            {
                if (lstProcedure.Count == 1)
                {
                    // 🔹 Single procedure → Direct execution (no foreach)
                    string procName = lstProcedure[0];
                    var sw = Stopwatch.StartNew();

                    try
                    {
                        Logger.Logging.Instance.WriteEventLog(
                            $"[GetProcedureList] START | Procedure={procName} | Thread={Thread.CurrentThread.ManagedThreadId}");

                        var ds = DBMessages.ExecuteHisoProcedure(procName, objSession, objListHisoRequests);

                        results.Add(new ProcedureResult { ProcedureName = procName, dsResult = ds });

                        Logger.Logging.Instance.WriteEventLog(
                            $"[GetProcedureList] SUCCESS | Procedure={procName} | Duration={sw.ElapsedMilliseconds}ms | Thread={Thread.CurrentThread.ManagedThreadId}");
                    }
                    catch (Exception ex)
                    {
                        Logger.Logging.Instance.WriteExceptionLog(ex,
                            $"[GetProcedureList] ERROR | Procedure={procName} | Thread={Thread.CurrentThread.ManagedThreadId}");
                    }
                }
                else
                {
                    // 🔹 Multiple procedures → Parallel + ConcurrentBag
                    var bag = new ConcurrentBag<ProcedureResult>();

                    Parallel.ForEach(lstProcedure, procName =>
                    {
                        var sw = Stopwatch.StartNew();
                        try
                        {
                            Logger.Logging.Instance.WriteEventLog(
                                $"[GetProcedureList] START | Procedure={procName} | Thread={Thread.CurrentThread.ManagedThreadId}");

                            var ds = DBMessages.ExecuteHisoProcedure(procName, objSession, objListHisoRequests);

                            bag.Add(new ProcedureResult { ProcedureName = procName, dsResult = ds });

                            Logger.Logging.Instance.WriteEventLog(
                                $"[GetProcedureList] SUCCESS | Procedure={procName} | Duration={sw.ElapsedMilliseconds}ms | Thread={Thread.CurrentThread.ManagedThreadId}");
                        }
                        catch (Exception ex)
                        {
                            Logger.Logging.Instance.WriteExceptionLog(ex,
                                $"[GetProcedureList] ERROR | Procedure={procName} | Thread={Thread.CurrentThread.ManagedThreadId}");
                        }
                    });

                    results = bag.ToList();
                }
            }
            catch (Exception ex)
            {
                Logger.Logging.Instance.WriteExceptionLog(ex, "[GetProcedureList] General failure");
            }

            return results;
        }


        public static string FormatValue(string value, string type)
        {
            if (!string.IsNullOrEmpty(type.Trim()) && !string.IsNullOrEmpty(value))
            {
                try
                {
                    switch (type.ToUpper())
                    {
                        case "DATE":
                            return Convert.ToDateTime(value).ToString("yyyy-MM-dd");

                        case "DATETIME":
                            return Convert.ToDateTime(value).ToString("yyyy-MM-ddThh:mm:ss");

                        case "INTEGER":
                            return Convert.ToInt16(Convert.ToDecimal(value)).ToString();

                        case "DECIMAL":
                            return string.Format("{0:F1}", Convert.ToDecimal(value));

                            //case "Decimal":
                            //    return Convert.ToDecimal(value).ToString("#.#");

                            //return Convert.ToDateTime(value).ToString("yyy-MM-ddThh:mm:ss:sss");
                    }
                }
                catch (Exception)
                {
                    // Logger.Logging.Instance.WriteExceptionLog(ex, "FormatValue: Value: " + value + " Type: " + type);
                }
            }
            return value;
        }
    }


    public class HisoRequest
    {
        public string ProcedureName { get; set; }

        public string SectionName { get; set; }

        public string GroupName { get; set; }
        public string GroupConceptID { get; set; }
        public string GroupConceptName { get; set; }
        public string GroupLaboratoryReport { get; set; }
        public int GroupMaximumRows { get; set; }
        public string Grouporder { get; set; }
        public string GroupreferenceID { get; set; }
        public string GroupQualifierName { get; set; }
        public string GroupQualifierID { get; set; }
        public string GroupCodingSystem { get; set; }
        public string Grouprefresh { get; set; }
        public int GroupStartRowIndex { get; set; }
        public string GroupminDateTime { get; set; }
        public string GroupmaxDateTime { get; set; }
        public string GroupsearchString { get; set; }
        public string GroupminVal { get; set; }
        public string GroupmaxVal { get; set; }


        public string FieldName { get; set; }
        public string FieldConceptID { get; set; }
        public string FieldConceptName { get; set; }
        public string FieldQualifierName { get; set; }
        public string FieldQualifierID { get; set; }
        public string FieldCodingSystem { get; set; }
        public string Fieldrefresh { get; set; }
        public string Fieldorder { get; set; }
        public string FieldstartPosition { get; set; }
        public string FieldnumRows { get; set; }
        public string FieldminDateTime { get; set; }
        public string FieldmaxDateTime { get; set; }
        public string FieldsearchString { get; set; }
        public string FieldminVal { get; set; }
        public string FieldmaxVal { get; set; }
        public string FieldreferenceID { get; set; }

        /// <summary>
        /// Returns HisoRequest List object populated from xml sent
        /// </summary>
        /// <param name="doc"></param>
        /// <returns></returns>
        public static List<HisoRequest> GetHisoRequest(XmlDocument doc)
        {
            XDocument xDoc = new XDocument();

            using (var nodeReader = new XmlNodeReader(doc))
            {
                nodeReader.MoveToContent();
                xDoc = XDocument.Load(nodeReader);
            }

            IEnumerable<HisoRequest> results = from c in xDoc.Descendants().Where(x => x.Parent != null && x.Name.LocalName == "field")
                                               select new HisoRequest()
                                               {
                                                   SectionName = c.Parent.Name.LocalName == "section" ? (string)c.Parent.Attribute("name") : (c.Parent.Parent.Name.LocalName == "section" ? (string)c.Parent.Parent.Attribute("name") : ""),

                                                   GroupName = (c.Parent != null && c.Parent.Name.LocalName == "group" ? (string)c.Parent.Attribute("name") : ""),
                                                   GroupConceptID = (c.Parent != null && c.Parent.Name.LocalName == "group" ? (string)c.Parent.Attribute("conceptID") : ""),
                                                   GroupConceptName = (c.Parent != null && c.Parent.Name.LocalName == "group" ? (string)c.Parent.Attribute("conceptName") : ""),
                                                   GroupLaboratoryReport = (c.Parent != null && c.Parent.Name.LocalName == "group" ? (string)c.Parent.Attribute("LaboratoryReport") : ""),
                                                   GroupMaximumRows = (c.Parent != null && c.Parent.Name.LocalName == "group" && c.Parent.Attribute("numRows") != null ? (int)c.Parent.Attribute("numRows") : 50000),
                                                   Grouporder = (c.Parent != null && c.Parent.Name.LocalName == "group" ? (string)c.Parent.Attribute("order") : ""),
                                                   GroupreferenceID = (c.Parent != null && c.Parent.Name.LocalName == "group" ? (string)c.Parent.Attribute("referenceID") : ""),
                                                   Grouprefresh = (c.Parent != null && c.Parent.Name.LocalName == "group" ? (string)c.Parent.Attribute("refresh") : ""),
                                                   GroupStartRowIndex = (c.Parent != null && c.Parent.Name.LocalName == "group" && c.Parent.Attribute("startPosition") != null ? (int)c.Parent.Attribute("startPosition") : 0),
                                                   GroupminDateTime = (c.Parent != null && c.Parent.Name.LocalName == "group" ? (string)c.Parent.Attribute("minDateTime") : ""),
                                                   GroupmaxDateTime = (c.Parent != null && c.Parent.Name.LocalName == "group" ? (string)c.Parent.Attribute("maxDateTime") : ""),
                                                   GroupsearchString = (c.Parent != null && c.Parent.Name.LocalName == "group" ? (string)c.Parent.Attribute("searchString") : ""),
                                                   GroupminVal = (c.Parent != null && c.Parent.Name.LocalName == "group" ? (string)c.Parent.Attribute("minVal") : ""),
                                                   GroupmaxVal = (c.Parent != null && c.Parent.Name.LocalName == "group" ? (string)c.Parent.Attribute("maxVal") : ""),

                                                   FieldName = (string)c.Attribute("name"),
                                                   FieldConceptID = (string)c.Attribute("conceptID"),
                                                   FieldConceptName = (string)c.Attribute("conceptName"),
                                                   FieldQualifierName = (string)c.Attribute("qualifierName"),
                                                   FieldQualifierID = (string)c.Attribute("qualifierID"),
                                                   FieldCodingSystem = (string)c.Attribute("codingSystem"),
                                                   Fieldrefresh = (string)c.Attribute("refresh"),
                                                   Fieldorder = (string)c.Attribute("order"),
                                                   FieldstartPosition = (string)c.Attribute("startPosition"),
                                                   FieldnumRows = (string)c.Attribute("numRows"),
                                                   FieldminDateTime = (string)c.Attribute("minDateTime"),
                                                   FieldmaxDateTime = (string)c.Attribute("maxDateTime"),
                                                   FieldsearchString = (string)c.Attribute("searchString"),
                                                   FieldminVal = (string)c.Attribute("minVal"),
                                                   FieldmaxVal = (string)c.Attribute("maxVal"),
                                                   FieldreferenceID = (string)c.Attribute("referenceID")
                                               };
            return results.ToList();
        }


        private static string getNodeType(XmlNode node, List<ConceptMapper.HisoConceptDetail> _LstConcept, out string ProcdureName, out string ConceptDataType)
        {
            string nodeName = string.Empty;
            ProcdureName = string.Empty;
            ConceptDataType = string.Empty;

            try
            {
                HisoConceptDetail concept = new HisoConceptDetail();
                if (node.Attributes["qualifierID"] != null && node.Attributes["qualifierID"].Value != string.Empty)
                {
                    nodeName = node.Attributes["qualifierID"].Value;
                    concept = _LstConcept.FirstOrDefault(n => n.QualifierID == nodeName);
                    if (concept != null && concept.HisoConceptID != "" && concept.HisoConceptID != "0")
                    {
                        //"Patient_Measurement,Patient_TestResult,Patient_Condition_Exists,Patient_DateLastImmunised,Patient_FullyImmunised" ?
                        //nodeName = concept.HisoConceptDetailName == "Patient_Measurement,Patient_TestResult,Patient_Condition_Exists,Patient_DateLastImmunised,Patient_FullyImmunised" ? nodeName : concept.QualifierName;
                        if (node.Attributes["conceptName"] != null && (node.Attributes["conceptName"].Value.ToLower() != "patient_measurementgroup_datetaken"
                                                                    && node.Attributes["conceptName"].Value.ToLower() != "patient_measurementgroup_value"))
                        {
                            nodeName = HisoConceptDetail.MeasurementGroup.Any(concept.HisoConceptDetailName.Equals) ? nodeName : concept.QualifierName;
                            ProcdureName = concept.ProcedureName;
                            ConceptDataType = concept.HisoDataType;
                            return nodeName;
                        }
                        else return nodeName;
                    }
                }

                if ((node.Attributes["conceptName"] != null && node.Attributes["conceptName"].Value != string.Empty && nodeName == "") || (concept != null && !string.IsNullOrEmpty(concept.HisoConceptID) && concept.HisoConceptID != "0"))
                {
                    nodeName = node.Attributes["conceptName"].Value;

                    concept = _LstConcept.FirstOrDefault(n => n.HisoConceptDetailName == nodeName);
                    if (concept != null && concept.ConceptName != "")
                    {
                        ProcdureName = concept.ProcedureName;
                        ConceptDataType = concept.HisoDataType;
                    }
                    return nodeName;
                }

                if (node.Attributes["conceptID"] != null && node.Attributes["conceptID"].Value != string.Empty && nodeName == "")
                {
                    nodeName = node.Attributes["conceptID"].Value;
                    concept = _LstConcept.FirstOrDefault(n => n.HisoID == nodeName);
                    if (concept != null && concept.HisoConceptID != "")
                    {
                        nodeName = concept.HisoConceptDetailName;
                        ProcdureName = concept.ProcedureName;
                        ConceptDataType = concept.HisoDataType;
                    }
                }
            }
            catch (Exception ex)
            {
                Logger.Logging.Instance.WriteExceptionLog(ex, "getNodeType Method: " + node.InnerXml);
            }
            return nodeName;
        }

        public static string NodeValueByNode(List<Hiso.ConceptMapper.ProcedureResult> lstProcedure, XmlNode node, List<ConceptMapper.HisoConceptDetail> LstConcept,
            List<HisoRequest> objListHisoRequests, out bool isMapped, int index = 0)
        {
            string ProcdureName = string.Empty;
            string ConceptDataType = string.Empty;
            isMapped = false;
            string nodeName = getNodeType(node, LstConcept, out ProcdureName, out ConceptDataType);
            try
            {
                if (!string.IsNullOrEmpty(ProcdureName))
                {
                    isMapped = true;
                    return NodeValue(lstProcedure, ProcdureName, nodeName, ConceptDataType, index);
                }
                return "";
            }
            catch (Exception ex)
            {
                Logger.Logging.Instance.WriteExceptionLog(ex, "NodeValue: " + node.InnerXml);
            }

            return "";
        }


        public static string NodeValue(List<Hiso.ConceptMapper.ProcedureResult> lstProcedure, string ProcedureName, string NodeName, string ConceptDataType, int index = 0)
        {
            try
            {
                if (!string.IsNullOrEmpty(ProcedureName))
                {
                    Hiso.ConceptMapper.ProcedureResult obj = lstProcedure.Where(x => x.ProcedureName == ProcedureName).SingleOrDefault();
                    if (obj != null && obj.dsResult != null && obj.dsResult.Tables[0].Rows.Count > 0)
                    {
                        if (obj.dsResult.Tables[0].Columns.Contains(NodeName) && obj.dsResult.Tables[0].Rows[index][NodeName] != null
                            && !String.IsNullOrEmpty(obj.dsResult.Tables[0].Rows[index][NodeName].ToString()))
                        {
                            //if (ConceptDataType.ToLower() != "base64binary")
                            //{
                            //    return HisoConceptDetail.FormatValue(obj.dsResult.Tables[0].Rows[index][NodeName].ToString(), ConceptDataType);
                            //}
                            //else
                            //    return Utitlity.ConvertByteToBase64((byte[])obj.dsResult.Tables[0].Rows[index][NodeName]);
                            if (ConceptDataType.ToLower() != "base64binary")
                            {
                                string mimeType = obj.dsResult.Tables[0].Rows[index][NodeName].ToString().ToLower().Trim();
                                Logger.Logging.Instance.WriteEventLog("Mime type Type: " + mimeType);
                                if (mimeType.EndsWith("html") || mimeType.EndsWith("png") || mimeType.Equals("bmp"))
                                {
                                    Logger.Logging.Instance.WriteEventLog("Converting html mime type to pdf");
                                    Logger.Logging.Instance.WriteEventLog("NodeName:" + NodeName);
                                    return HisoConceptDetail.FormatValue("application/pdf", ConceptDataType);
                                }
                                else if (mimeType.EndsWith("png"))
                                {
                                    Logger.Logging.Instance.WriteEventLog("Converting PNG-BMP mime type to pdf");
                                    Logger.Logging.Instance.WriteEventLog("NodeName:" + NodeName);
                                    return HisoConceptDetail.FormatValue("application/pdf", ConceptDataType);
                                }
                                else if (mimeType.EndsWith("xml"))
                                {
                                    Logger.Logging.Instance.WriteEventLog("Converting XML mime type to text");
                                    Logger.Logging.Instance.WriteEventLog("NodeName:" + NodeName);
                                    return HisoConceptDetail.FormatValue("text/plain", ConceptDataType);
                                }
                                else if (mimeType.EndsWith("doc") || mimeType.EndsWith("msword"))
                                {
                                    Logger.Logging.Instance.WriteEventLog("Converting doc mime type to rtf");
                                    Logger.Logging.Instance.WriteEventLog("NodeName:" + NodeName);
                                    return HisoConceptDetail.FormatValue("application/rtf", ConceptDataType);
                                }
                                else
                                {
                                    Logger.Logging.Instance.WriteEventLog("Last option");
                                    Logger.Logging.Instance.WriteEventLog("NodeName:" + NodeName);
                                    Logger.Logging.Instance.WriteEventLog("ConceptDataType:" + ConceptDataType.ToLower());
                                    return HisoConceptDetail.FormatValue(obj.dsResult.Tables[0].Rows[index][NodeName].ToString(), ConceptDataType);
                                }
                            }
                            else
                            {
                                string filetype = obj.dsResult.Tables[0].Rows[index][2].ToString().ToLower();
                                Logger.Logging.Instance.WriteEventLog("File Type: " + filetype);

                                Logger.Logging.Instance.WriteEventLog("Not base64binary: column Patient_Attachment_DataType Exit " + NodeName);


                                if (filetype.EndsWith("png") || filetype.EndsWith("bmp"))
                                {
                                    Logger.Logging.Instance.WriteEventLog("Enter PNG-BMP byte Convert to PDF");
                                    return Utitlity.ConvertByteToBase64(TypeConverter.CreatePDFromImage((byte[])obj.dsResult.Tables[0].Rows[index][NodeName]));

                                }
                                if (filetype.EndsWith("html"))
                                {
                                    //string temphtml = System.Text.Encoding.ASCII.GetString((byte[])obj.dsResult.Tables[0].Rows[index][NodeName]);

                                    Logger.Logging.Instance.WriteEventLog("Enter HTML byte Convert to PDF");
                                   // Logger.Logging.Instance.WriteEventLog("Bytes: "+ temphtml);

                                    //var Renderer = new IronPdf.HtmlToPdf();
                                    //var PDF = Renderer.RenderHtmlAsPdf(temphtml);
                                   // Logger.Logging.Instance.WriteEventLog("Successfull convert a  HTML byte Convert to PDF");
                                   // PDF.SaveAs(@"C:\Valentia Technologies\HisoHttp\Logs\test.pdf");
                                   // String val = Utitlity.ConvertByteToBase64(PDF.Stream.ToArray());

                                   // var htmlToPdf = new NReco.PdfGenerator.HtmlToPdfConverter();
                                   // System.IO.File.WriteAllBytes(@"C:\Valentia Technologies\HisoHttp\Logs\test.pdf", (byte[])obj.dsResult.Tables[0].Rows[index][NodeName]);
                                    string val=  Utitlity.ConvertByteToBase64(TypeConverter.ConvertHTMLToByte((byte[])obj.dsResult.Tables[0].Rows[index][NodeName]));
                                    //Logger.Logging.Instance.WriteEventLog("ConvertByteToBase64:" + val);
                                    return val;
                                    //return Utitlity.ConvertByteToBase64(htmlToPdf.GeneratePdf(temphtml));
                                }
                                else if (filetype.EndsWith("xml"))
                                {
                                    Logger.Logging.Instance.WriteEventLog("Enter XML byte Convert to text");
                                    return Utitlity.ConvertByteToBase64((byte[])obj.dsResult.Tables[0].Rows[index][NodeName]);
                                }
                                else
                                {
                                    Logger.Logging.Instance.WriteEventLog("Enter to Default Format Type");
                                    return Utitlity.ConvertByteToBase64((byte[])obj.dsResult.Tables[0].Rows[index][NodeName]);
                                }
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Logger.Logging.Instance.WriteEventLog(ex.ToString());
                Logger.Logging.Instance.WriteExceptionLog(ex);
            }

            return "";
        }

        public static void FillXMLDetails(List<Hiso.ConceptMapper.ProcedureResult> lstProcedure, ref XmlDocument xDocRequest,
                                          List<ConceptMapper.HisoConceptDetail> LstConcept, List<HisoRequest> objListHisoRequests)
        {
            try
            {
                Logger.Logging.Instance.WriteEventLog("FillXMLDetails: " + xDocRequest.InnerXml);

                XmlNodeList fieldList = xDocRequest.DocumentElement.GetElementsByTagName("section");
                List<XmlNode> ListProcessed = new List<XmlNode>();
                Dictionary<string, XmlNode> _DicNodes = new Dictionary<string, XmlNode>();
                
                //foreach (XmlNode item in fieldList)
                for (int i = 0; i < fieldList.Count; i++)
                {
                    //XmlNodeList GroupNodesList = item.ChildNodes;
                    //XmlNode NodeProcess = item.Clone();

                    XmlNodeList GroupNodesList = fieldList[i].ChildNodes;
                    XmlNode NodeProcess = fieldList[i].Clone();

                    NodeProcess.InnerXml = "";
                    
                    if (GroupNodesList.Count > 0)
                    {
                        foreach (XmlNode child in GroupNodesList)
                        {
                            if (child.Name.ToLower().Equals("group"))
                            {
                                try
                                {
                                    HisoRequest objRequestGruou = objListHisoRequests.FirstOrDefault(G => G.GroupConceptName == child.Attributes["conceptName"].Value && G.ProcedureName != "");

                                    if (objRequestGruou != null && !string.IsNullOrEmpty(objRequestGruou.ProcedureName))
                                    {
                                        Hiso.ConceptMapper.ProcedureResult objResult = lstProcedure.FirstOrDefault(x => x.ProcedureName == objRequestGruou.ProcedureName);

                                        if (objResult.dsResult != null && objResult.dsResult.Tables.Count > 0 && objResult.dsResult.Tables[0].Rows.Count > 0)
                                        {
                                            for (int x = 0; x < objResult.dsResult.Tables[0].Rows.Count; x++)
                                            {
                                                XmlNode CloneNode = child.Clone();
                                                foreach (XmlNode fields in CloneNode.ChildNodes)
                                                {
                                                    bool isMapped = false;
                                                    string value = NodeValueByNode(lstProcedure, fields, LstConcept, objListHisoRequests, out isMapped, x);
                                                    fields.InnerText = isMapped == true ? value : child.InnerText;
                                                }

                                                if (CloneNode.Attributes["referenceID"] != null)
                                                {

                                                    CloneNode.Attributes["referenceID"].Value = string.Empty;
                                                    if (objResult.dsResult.Tables[0].Columns.Contains("Patient_Attachment_ID") && objResult.dsResult.Tables[0].Rows[x]["Patient_Attachment_ID"] != null)
                                                    {
                                                        CloneNode.Attributes["referenceID"].Value = objResult.dsResult.Tables[0].Rows[x]["Patient_Attachment_ID"].ToString();
                                                    }
                                                    else if (objResult.dsResult.Tables[0].Columns.Contains("Patient_OutgoingLetter_ID") && objResult.dsResult.Tables[0].Rows[x]["Patient_OutgoingLetter_ID"] != null)
                                                    {
                                                        CloneNode.Attributes["referenceID"].Value = objResult.dsResult.Tables[0].Rows[x]["Patient_OutgoingLetter_ID"].ToString();
                                                    }
                                                    else if (objResult.dsResult.Tables[0].Columns.Contains("Patient_IncomingLetter_ID") && objResult.dsResult.Tables[0].Rows[x]["Patient_IncomingLetter_ID"] != null)
                                                    {
                                                        CloneNode.Attributes["referenceID"].Value = objResult.dsResult.Tables[0].Rows[x]["Patient_IncomingLetter_ID"].ToString();
                                                    }
                                                    else if (objResult.dsResult.Tables[0].Columns.Contains("Patient_LaboratoryReport_ID") && objResult.dsResult.Tables[0].Rows[x]["Patient_LaboratoryReport_ID"] != null)
                                                    {
                                                        CloneNode.Attributes["referenceID"].Value = objResult.dsResult.Tables[0].Rows[x]["Patient_LaboratoryReport_ID"].ToString();
                                                    }

                                                    else if (objResult.dsResult.Tables[0].Columns.Contains("Patient_RadiologyReport_ID") && objResult.dsResult.Tables[0].Rows[x]["Patient_RadiologyReport_ID"] != null)
                                                    {
                                                        CloneNode.Attributes["referenceID"].Value = objResult.dsResult.Tables[0].Rows[x]["Patient_RadiologyReport_ID"].ToString();
                                                    }
                                                }
                                                NodeProcess.AppendChild(CloneNode);
                                            }
                                        }
                                        else
                                        {
                                            //foreach (XmlNode fields in child.ChildNodes)
                                            //{
                                            //    fields.InnerText = "";
                                            //}
                                            NodeProcess.AppendChild(child.Clone());
                                            Logger.Logging.Instance.WriteEventLog("Not found any Store Procedure against: " + child.Attributes["conceptName"].Value);
                                        }
                                    }
                                    else { NodeProcess.AppendChild(child.Clone()); }

                                }
                                catch (Exception ex)
                                {
                                    Logger.Logging.Instance.WriteExceptionLog(ex, "FillXMLDetails: " + child.InnerXml);
                                }
                            }
                            else
                            {
                                // ********** Field ******************
                                bool isMapped = false;
                                string value = NodeValueByNode(lstProcedure, child, LstConcept, objListHisoRequests, out isMapped);
                                child.InnerText = isMapped == true ? value : child.InnerText;


                                // ********** Measurement Field ******************

                                if (child.ParentNode.Attributes.Count > 0 && child.ParentNode.Attributes["name"].Value == "measurements")
                                {
                                    string ProcdureName = string.Empty;
                                    string ConceptDataType = string.Empty;
                                    string nodeName = getNodeType(child, LstConcept, out ProcdureName, out ConceptDataType);

                                    if (HisoConceptDetail.QualifierList.Any(nodeName.Equals))
                                    {
                                        nodeName = nodeName + "_dateTaken";
                                        XmlAttribute xKey = xDocRequest.CreateAttribute("dateTaken");
                                        xKey.Value = "";

                                        if (!string.IsNullOrEmpty(ProcdureName))
                                        {
                                            Hiso.ConceptMapper.ProcedureResult obj = lstProcedure.Where(x => x.ProcedureName == ProcdureName).SingleOrDefault();
                                            if (obj != null && obj.dsResult != null && obj.dsResult.Tables[0].Rows.Count > 0)
                                            {
                                                if (obj.dsResult.Tables[0].Columns.Contains(nodeName) && obj.dsResult.Tables[0].Rows[0][nodeName] != null
                                                    && !String.IsNullOrEmpty(obj.dsResult.Tables[0].Rows[0][nodeName].ToString()))
                                                    xKey.Value = HisoConceptDetail.FormatValue(obj.dsResult.Tables[0].Rows[0][nodeName].ToString(), "DATETIME");
                                            }
                                        }
                                        child.Attributes.Append(xKey);
                                    }
                                }

                                NodeProcess.AppendChild(child.Clone());
                            }
                        }
                    }
                    ListProcessed.Add(NodeProcess);
                }

                List<XmlNode> nodes = ListProcessed.Cast<XmlNode>().ToList();
                for (int i = 0; i < fieldList.Count; i++)
                {
                    //fieldList[i].Attributes[""]
                    fieldList[i].InnerXml = "";
                    if (nodes.Count > i)
                        fieldList[i].InnerXml = nodes[i].InnerXml;


                    //fieldList[i].AppendChild(nodes[i]); // both counters should be same
                }
                Logger.Logging.Instance.WriteEventLog("FillXMLDetails After : " + xDocRequest.InnerXml);

            }
            catch (Exception ex)
            {
                Logger.Logging.Instance.WriteExceptionLog(ex, "FillXMLDetails: " + xDocRequest.InnerXml);
            }
        }

        public static void FillXMLDetails_Parked(List<Hiso.ConceptMapper.ProcedureResult> lstProcedure, ref XmlDocument xDocRequest,
                                       List<ConceptMapper.HisoConceptDetail> LstConcept, List<HisoRequest> objListHisoRequests)
        {
            try
            {
                Logger.Logging.Instance.WriteEventLog("FillXMLDetails_Parked: " + xDocRequest.InnerXml);

                XmlNodeList fieldList = xDocRequest.DocumentElement.GetElementsByTagName("section");
                List<XmlNode> ListProcessed = new List<XmlNode>();
                Dictionary<string, XmlNode> _DicNodes = new Dictionary<string, XmlNode>();

                //foreach (XmlNode item in fieldList)
                for (int i = 0; i < fieldList.Count; i++)
                {
                    //XmlNodeList GroupNodesList = item.ChildNodes;
                    //XmlNode NodeProcess = item.Clone();

                    XmlNodeList GroupNodesList = fieldList[i].ChildNodes;
                    XmlNode NodeProcess = fieldList[i].Clone();

                    NodeProcess.InnerXml = "";

                    if (GroupNodesList.Count > 0)
                    {
                        foreach (XmlNode child in GroupNodesList)
                        {
                            if (child.Name.ToLower().Equals("group"))
                            {
                                try
                                {
                                    HisoRequest objRequestGruou = objListHisoRequests.FirstOrDefault(G => G.GroupConceptName == child.Attributes["conceptName"].Value && G.ProcedureName != "");

                                    if (objRequestGruou != null && !string.IsNullOrEmpty(objRequestGruou.ProcedureName))
                                    {
                                        Hiso.ConceptMapper.ProcedureResult objResult = lstProcedure.FirstOrDefault(x => x.ProcedureName == objRequestGruou.ProcedureName);

                                        if (objResult.dsResult != null && objResult.dsResult.Tables.Count > 0 && objResult.dsResult.Tables[0].Rows.Count > 0)
                                        {
                                            for (int x = 0; x < objResult.dsResult.Tables[0].Rows.Count; x++)
                                            {
                                                XmlNode CloneNode = child.Clone();
                                                foreach (XmlNode fields in CloneNode.ChildNodes)
                                                {
                                                    bool isMapped = false;
                                                    if (fields.Attributes["conceptName"] != null && fields.Attributes["conceptName"].Value.ToLower().Contains("currentuser"))
                                                    {
                                                        string value = NodeValueByNode(lstProcedure, fields, LstConcept, objListHisoRequests, out isMapped, x);
                                                        fields.InnerText = isMapped == true ? value : child.InnerText;
                                                    }
                                                }
                                                NodeProcess.AppendChild(CloneNode);
                                            }
                                        }
                                        else
                                        {
                                            foreach (XmlNode fields in child.ChildNodes)
                                            {
                                                if (fields.Attributes["conceptName"] != null && fields.Attributes["conceptName"].Value.ToLower().Contains("currentuser"))
                                                    fields.InnerText = "";
                                            }
                                            NodeProcess.AppendChild(child.Clone());
                                            Logger.Logging.Instance.WriteEventLog("Not found any Store Procedure against: " + child.Attributes["conceptName"].Value);
                                        }
                                    }
                                    else { NodeProcess.AppendChild(child.Clone()); }

                                }
                                catch (Exception ex)
                                {
                                    Logger.Logging.Instance.WriteExceptionLog(ex, "FillXMLDetails_Parked: " + child.InnerXml);
                                }
                            }
                            else
                            {
                                // ********** Field ******************
                                bool isMapped = false;
                                if (child.Attributes["conceptName"] != null && child.Attributes["conceptName"].Value.ToLower().Contains("currentuser"))
                                {
                                    string value = NodeValueByNode(lstProcedure, child, LstConcept, objListHisoRequests, out isMapped);
                                    child.InnerText = isMapped == true ? value : child.InnerText;
                                }
                                NodeProcess.AppendChild(child.Clone());
                            }
                        }
                    }
                    ListProcessed.Add(NodeProcess);
                }
                List<XmlNode> nodes = ListProcessed.Cast<XmlNode>().ToList();
                for (int i = 0; i < fieldList.Count; i++)
                {
                    //fieldList[i].Attributes[""]
                    if (fieldList[i].Attributes["conceptName"] != null && fieldList[i].Attributes["conceptName"].Value.ToLower().Contains("currentuser"))
                    {
                        fieldList[i].InnerXml = "";
                        fieldList[i].InnerXml = nodes.Count > i ? nodes[i].InnerXml : string.Empty;
                    }
                    //fieldList[i].AppendChild(nodes[i]); // both counters should be same
                }
            }
            catch (Exception ex)
            {
                Logger.Logging.Instance.WriteExceptionLog(ex, "FillXMLDetails_Parked: " + xDocRequest.InnerXml);
            }
        }

    }

    public class DynamicParam
    {
        public string SP_name { get; set; }
        public string Parameter_name { get; set; }
        public string ParamValue { get; set; }
        public int Param_order { get; set; }
        public string Type { get; set; }
        public int Length { get; set; }
    }

    public class ProcedureResult
    {
        public string ProcedureName { get; set; }
        public DataSet dsResult { get; set; }
    }

    public enum DataType
    {
        Base64Binary,
        Boolean,
        Date,
        DateTime,
        Decimal,
        Integer,
        List,
        String,
    }






}