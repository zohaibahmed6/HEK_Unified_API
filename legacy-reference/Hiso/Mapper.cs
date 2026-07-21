using Hiso.ConceptMapper;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Data.SqlClient;
using System.IO;
using System.Linq;
using System.Text;
using System.Web;
using System.Web.Hosting;
using System.Xml;
using System.Xml.Linq;
using System.Xml.Serialization;
using System.Xml.XPath;

namespace Hiso
{
    public class Mapper
    {   public static List<string> PrepareConceptsFromRequest(ref XmlDocument xDocRequest, ref List<ConceptMapper.HisoConceptDetail> _LstConcept,ref List<HisoRequest> objListHisoRequests, HealthLinkSession objSessionKey, string formStatus) {
            foreach (HisoRequest req in objListHisoRequests)
            {
                var objConcept = _LstConcept.FirstOrDefault();
                objConcept = _LstConcept.FirstOrDefault(f => f.QualifierID != null && f.QualifierID == req.FieldQualifierID);
                if (objConcept != null && !objConcept.HisoConceptDetailName.Equals(req.FieldConceptName))
                {
                    if (objConcept.ConceptName.ToLower().Equals("measurements") && req.SectionName.Equals("dynamicConcept"))
                    {
                        objConcept = null;
                    }
                }

                if (objConcept == null)
                {
                    objConcept = _LstConcept.FirstOrDefault(f => (!string.IsNullOrEmpty(f.HisoID) && !string.IsNullOrEmpty(req.FieldConceptID) && f.HisoID.ToString().Equals(req.FieldConceptID)) || (!string.IsNullOrEmpty(f.HisoConceptDetailName)) && !string.IsNullOrEmpty(req.FieldConceptName) && f.HisoConceptDetailName.ToString().Equals(req.FieldConceptName));
                }

                req.ProcedureName = objConcept != null ? objConcept.ProcedureName : string.Empty;

                //****** Empty Group Query *************
                if (objConcept == null && req.SectionName == "dynamicConcept" && string.IsNullOrEmpty(req.FieldConceptName))
                {

                    objConcept = _LstConcept.FirstOrDefault(f => (!string.IsNullOrEmpty(f.HisoGroupID) && !string.IsNullOrEmpty(req.GroupConceptID) && f.HisoGroupID.ToString().Equals(req.GroupConceptID)) || (!string.IsNullOrEmpty(f.ConceptName)) && !string.IsNullOrEmpty(req.GroupConceptName) && f.ConceptName.ToString().Equals(req.GroupConceptName));

                    if (objConcept != null)
                    {
                        var _TempList = _LstConcept.Where(f => (!string.IsNullOrEmpty(f.HisoGroupID) && !string.IsNullOrEmpty(req.GroupConceptID) && f.HisoGroupID.ToString().Equals(req.GroupConceptID)) || (!string.IsNullOrEmpty(f.ConceptName)) && !string.IsNullOrEmpty(req.GroupConceptName) && f.ConceptName.ToString().Equals(req.GroupConceptName));

                        XmlNodeList groupList = xDocRequest.DocumentElement.GetElementsByTagName("group");

                        List<XmlNode> processedNodes = new List<XmlNode>();

                        for (int i = 0; i < groupList.Count; i++)
                        {
                            foreach (var item in _TempList)
                            {
                                var newNode = groupList[i].ChildNodes[0].Clone();
                                newNode.Attributes["conceptID"].Value = item.HisoID;
                                newNode.Attributes["conceptName"].Value = item.HisoConceptDetailName;
                                processedNodes.Add(newNode);
                            }
                        }

                        for (int i = 0; i < groupList.Count; i++)
                        {
                            foreach (XmlNode node in processedNodes)
                            {
                                groupList[i].AppendChild(node);
                            }
                        }

                        objListHisoRequests = Hiso.ConceptMapper.HisoRequest.GetHisoRequest(xDocRequest);
                        objListHisoRequests.ForEach(x => { x.ProcedureName = objConcept.ProcedureName; });

                    }
                }
            }
            //**********  Get Distinct Procedure Name from the list that need to be executed.
            if(formStatus != "N")// if prepare concepts for Old Froms then only call procedures for current user  fields/concepts.
            objListHisoRequests.RemoveAll(x => x.ProcedureName.ToLower().Contains("currentuser") == false);
            List<string> TempList = objListHisoRequests.Select(o => o.ProcedureName).Distinct().ToList();
            TempList.RemoveAll(r => r == null || r.Length == 0);
            return TempList;
        }
        public static string GetFromXml(FormData fd)
        {
            string strXml = string.Empty;
            try
            {
                using (var memoryStream = new MemoryStream())
                {
                    using (TextWriter streamWriter = new StreamWriter(memoryStream))
                    {
                        XmlSerializer XmlSerial = new XmlSerializer(typeof(FormData));
                        XmlSerial.Serialize(memoryStream, fd);
                        strXml = System.Text.Encoding.ASCII.GetString(memoryStream.ToArray());
                    }
                }
            }
            catch (Exception)
            {
                throw;
            }
            return strXml;
        }
        public static FormData MakeFormData(string xml)
        {
            FormData objFDT = null;
            try
            {

                using (var memoryStream = new MemoryStream(System.Text.Encoding.UTF8.GetBytes(xml)))
                {
                    using (TextWriter streamWriter = new StreamWriter(memoryStream))
                    {
                        XmlSerializer XmlSerial = new XmlSerializer(typeof(FormData));
                        objFDT = (FormData)XmlSerial.Deserialize(memoryStream);
                    }
                }
            }
            catch (Exception)
            {
                throw;
            }
            return objFDT;
        }

        /*********************** New Methods ****************************/
        private DataTable GetGroupTableByGroup(XmlNode group, HealthLinkSession objSessionKey)
        {
            bool groupRefresh = false;
            string order = string.Empty;
            int startPosition = 0;
            int numRows = 50000;
            string minDateTime = string.Empty;
            string maxDateTime = string.Empty;
            string searchString = string.Empty;
            double minVal = 0;
            double maxVal = 0;
            DataSet dsResult = new DataSet();
            XmlNode parentGroup = null;
            DataTable dt = null;
            if (IsNodeHasModifiers(group))
            {

                if (group.Attributes["refresh"] != null && group.Attributes["refresh"].Value.ToLower() == "true")
                {
                    groupRefresh = true;
                }
                if (group.Attributes["order"] != null && string.IsNullOrEmpty(group.Attributes["order"].Value) == false)
                {
                    order = group.Attributes["order"].Value;
                }
                if (group.Attributes["startPosition"] != null && string.IsNullOrEmpty(group.Attributes["startPosition"].Value) == false)
                {
                    startPosition = int.Parse(group.Attributes["startPosition"].Value) < 0 ? 0 : int.Parse(group.Attributes["startPosition"].Value);
                }
                if (group.Attributes["numRows"] != null && string.IsNullOrEmpty(group.Attributes["numRows"].Value) == false)
                {
                    numRows = int.Parse(group.Attributes["numRows"].Value) <= 0 ? 0 : int.Parse(group.Attributes["numRows"].Value);
                }
                if (group.Attributes["minDateTime"] != null && string.IsNullOrEmpty(group.Attributes["minDateTime"].Value) == false)
                {
                    minDateTime = group.Attributes["minDateTime"].Value.Replace("T", " ");
                }
                if (group.Attributes["maxDateTime"] != null && string.IsNullOrEmpty(group.Attributes["maxDateTime"].Value) == false)
                {
                    maxDateTime = group.Attributes["maxDateTime"].Value.Replace("T", " ");
                }
                if (group.Attributes["searchString"] != null && string.IsNullOrEmpty(group.Attributes["searchString"].Value) == false)
                {
                    searchString = group.Attributes["searchString"].Value;
                }

                if (group.Attributes["minVal"] != null && string.IsNullOrEmpty(group.Attributes["minVal"].Value) == false)
                {
                    minVal = double.Parse(group.Attributes["minVal"].Value);
                }
                if (group.Attributes["maxVal"] != null && string.IsNullOrEmpty(group.Attributes["maxVal"].Value) == false)
                {
                    maxVal = double.Parse(group.Attributes["maxVal"].Value);
                }
            }
            SqlConnection con = new SqlConnection(System.Configuration.ConfigurationManager.ConnectionStrings["ConectionStringPMS_NZ"].ToString());
            SqlCommand cmd = new SqlCommand();

            cmd.Connection = con;
            if (group.Attributes["conceptName"].Value == "Patient_SocialHistory" || group.Attributes["conceptName"].Value == "Patient_PastHistory" || group.Attributes["conceptName"].Value == "Patient_FamilyHistory" || group.Attributes["conceptName"].Value == "Patient_Problem" || group.Attributes["conceptName"].Value == "Patient_LaboratoryReport" || group.Attributes["conceptName"].Value == "Patient_RadiologyReport" || group.Attributes["conceptName"].Value == "Patient_MedicalWarning" || group.Attributes["conceptName"].Value == "Patient_Accident" || group.Attributes["conceptName"].Value == "Patient_RegularMedication" || group.Attributes["conceptName"].Value == "Patient_PrescribedMedication" || group.Attributes["conceptName"].Value == "Patient_OutgoingLetter" || group.Attributes["conceptName"].Value == "Patient_Attachment" || group.Attributes["conceptName"].Value.ToLower().StartsWith("patient_postaladdress") || group.Attributes["conceptName"].Value.ToLower().StartsWith("patient_residentialaddress") || group.Attributes["conceptName"].Value.ToLower().StartsWith("patient_accident_diagnosis") || group.Attributes["conceptName"].Value == "Patient_Accident_Location" || group.Attributes["conceptName"].Value == "Patient_Accident_Scene" || group.Attributes["conceptName"].Value == "Patient_Accident_Sport" || group.Attributes["conceptName"].Value == "Patient_Accident_FitForWork" || group.Attributes["conceptName"].Value == "Patient_Accident_DiagnosisSide" || group.Attributes["conceptName"].Value == "Patient_Accident_DiagnosisQualifier" || group.Attributes["conceptName"].ToString().ToLower().StartsWith("patient_accident_referral") || group.Attributes["name"].ToString().ToLower() == "accident.referral")
            {
                cmd.Parameters.Add(new SqlParameter("@AppointmentId", objSessionKey.AppointmentId));
                cmd.Parameters.Add(new SqlParameter("@PatientId", objSessionKey.PatientId));




                if (string.IsNullOrEmpty(minDateTime) == false)
                    cmd.Parameters.Add(new SqlParameter("@FromDate", DateTime.Parse(minDateTime)));

                if (string.IsNullOrEmpty(maxDateTime) == false)
                    cmd.Parameters.Add(new SqlParameter("@ToDate", DateTime.Parse(maxDateTime)));

                if (startPosition > 0)
                    cmd.Parameters.Add(new SqlParameter("@StartRowIndex", startPosition));

                if (numRows != 50000)
                    cmd.Parameters.Add(new SqlParameter("@MaximumRows", numRows));

                if (string.IsNullOrEmpty(searchString) == false)
                    cmd.Parameters.Add(new SqlParameter("@Search", searchString));

                if (string.IsNullOrEmpty(order) == false)
                    cmd.Parameters.Add(new SqlParameter("@SortBy", order));

                if (minVal > 0.00)
                    cmd.Parameters.Add(new SqlParameter("@minVal", minVal));

                if (maxVal > 0.00)
                    cmd.Parameters.Add(new SqlParameter("@maxVal", maxVal));
            }

            if (group.Attributes["conceptName"].Value == "Patient_SocialHistory")
            {
                cmd.CommandText = "Appointment.uspGetHLFormSocialHistoryDetailPMS";
                cmd.CommandType = CommandType.StoredProcedure;
            }
            else if (group.Attributes["conceptName"].Value == "Patient_PastHistory")
            {
                cmd.CommandText = "Appointment.uspGetHLFormSurgicalHistoryDetailPMS";
                cmd.CommandType = CommandType.StoredProcedure;
            }
            else if (group.Attributes["conceptName"].Value == "Patient_FamilyHistory")
            {
                cmd.CommandText = "Appointment.uspGetHLFormFamilyHistoryDetailPMS";
                cmd.CommandType = CommandType.StoredProcedure;
            }
            else if (group.Attributes["conceptName"].Value == "Patient_Problem")
            {
                cmd.CommandText = "Appointment.uspGetHLFormPatientProblemDetailPMS";
                cmd.CommandType = CommandType.StoredProcedure;
            }
            else if (group.Attributes["conceptName"].Value == "Patient_LaboratoryReport")
            {
                cmd.CommandText = "Appointment.uspGetHLFormLabReportsDetailPMS";
                cmd.CommandType = CommandType.StoredProcedure;
            }
            else if (group.Attributes["conceptName"].Value == "Patient_RadiologyReport")
            {
                cmd.CommandText = "Appointment.uspGetHLFormRadReportsDetailPMS";
                cmd.CommandType = CommandType.StoredProcedure;
            }
            else if (group.Attributes["conceptName"].Value == "Patient_MedicalWarning")
            {
                cmd.CommandText = "Appointment.uspGetHLFormWarningsDetailPMS";
                cmd.CommandType = CommandType.StoredProcedure;
            }
            else if (group.Attributes["conceptName"].Value == "Patient_Accident" || group.Attributes["conceptName"].Value == "Patient_Accident_Location" || group.Attributes["conceptName"].Value == "Patient_Accident_Scene" || group.Attributes["conceptName"].Value == "Patient_Accident_Sport" || group.Attributes["conceptName"].Value == "Patient_Accident_FitForWork" || group.Attributes["conceptName"].Value == "Patient_Accident_DiagnosisSide" || group.Attributes["conceptName"].Value == "Patient_Accident_DiagnosisQualifier")
            {
                if (objSessionKey.ReferenceId > 0)
                    cmd.Parameters.Add(new SqlParameter("@ACC45ID", objSessionKey.ReferenceId));
                cmd.CommandText = "Appointment.uspGetHLFormAccidentsDetailPMS";
                cmd.CommandType = CommandType.StoredProcedure;
            }
            else if (group.Attributes["conceptName"].Value == "Patient_PrescribedMedication")
            {
                cmd.CommandText = "Appointment.uspGetHLFormPrescribedMedicationDetailPMS";
                cmd.CommandType = CommandType.StoredProcedure;
            }
            else if (group.Attributes["conceptName"].Value == "Patient_RegularMedication")
            {
                cmd.CommandText = "Appointment.uspGetHLFormLongtermMedicationDetailPMS";
                cmd.CommandType = CommandType.StoredProcedure;
            }
            else if (group.Attributes["conceptName"].Value == "Patient_OutgoingLetter")
            {
                cmd.CommandText = "Appointment.uspGetHLFormGeneratedDocsDetailPMS";
                cmd.CommandType = CommandType.StoredProcedure;
            }
            else if (group.Attributes["conceptName"].Value == "Patient_Attachment")
            {
                cmd.CommandText = "Appointment.uspGetHLFormUploadedDocsDetailPMS";
                cmd.CommandType = CommandType.StoredProcedure;
            }
            else if (group.Attributes["conceptName"].Value.ToLower().StartsWith("patient_postaladdress"))
            {
                cmd.CommandText = "Appointment.uspGetHLFormPatientPostalAddressPMS";
                cmd.CommandType = CommandType.StoredProcedure;
            }
            else if (group.Attributes["conceptName"].Value.ToLower().StartsWith("patient_residentialaddress"))
            {
                cmd.CommandText = "Appointment.uspGetHLFormPatientResidentialAddressPMS";
                cmd.CommandType = CommandType.StoredProcedure;
            }
            else if (group.Attributes["conceptName"].Value.ToLower().StartsWith("patient_accident_diagnosis") || group.Attributes["conceptName"].Value == "Patient_Accident_Diagnosis" || group.Attributes["conceptName"].Value.ToLower().StartsWith("patient_accident_diagnosis1") || group.Attributes["conceptName"].Value.ToLower().StartsWith("patient_accident_diagnosis2") || group.Attributes["conceptName"].Value.ToLower().StartsWith("patient_accident_diagnosis3"))
            {
                cmd.CommandText = "Appointment.uspGetHLFormDiagnosisDetailPMS";
                cmd.CommandType = CommandType.StoredProcedure;
            }
            else
            {
                return dt;
            }
            try
            {
                con.Open();
                SqlDataAdapter adapter = new SqlDataAdapter(cmd);
                adapter.Fill(dsResult);
            }
            catch (Exception ex)
            {
                throw ex;
            }
            finally
            {
                con.Close();
            }
            if (dsResult.Tables.Count > 0)
            {
                if (group.Attributes["conceptName"].Value.ToLower().StartsWith("patient_accident_diagnosis1") || group.Attributes["conceptName"].Value.ToLower().StartsWith("patient_accident_diagnosis2") || group.Attributes["conceptName"].Value.ToLower().StartsWith("patient_accident_diagnosis3")) {
                    dt = MakeNumberedDiagnosisTable(dsResult.Tables[0]);
                }
                else dt = dsResult.Tables[0];
            }
                
            return dt;
        }
        private DataTable MakeNumberedDiagnosisTable(DataTable dt) {
            DataTable dtNumberedDiagnosis = new DataTable();
            string numberedColumNames = "Patient_Accident_Diagnosis1_Code,Patient_Accident_Diagnosis1_Description,Patient_Accident_Diagnosis1_Side_Code,Patient_Accident_Diagnosis1_Side_Description,Patient_Accident_Diagnosis1_Qualifier,Patient_Accident_Diagnosis2_Code,Patient_Accident_Diagnosis2_Description,Patient_Accident_Diagnosis2_Side_Code,Patient_Accident_Diagnosis2_Side_Description,Patient_Accident_Diagnosis2_Qualifier,Patient_Accident_Diagnosis3_Code,Patient_Accident_Diagnosis3_Description,Patient_Accident_Diagnosis3_Side_Code,Patient_Accident_Diagnosis3_Side_Description,Patient_Accident_Diagnosis3_Qualifier";
            string[] array = numberedColumNames.Split(',');
            DataRow dr = dtNumberedDiagnosis.NewRow();
            for (int i = 0; i < array.Length; i++)
            {
                dtNumberedDiagnosis.Columns.Add(array[i]);
                dr[array[i]] = DBNull.Value;
            }
            if (dt.Rows.Count > 0)
            {
                for(int counter=0;counter<3;counter++){
                    DataRow row = dt.Rows[counter];
                    switch (counter)
                    {
                        case 0:
                            dr["Patient_Accident_Diagnosis1_Code"] = row["Patient_Accident_Diagnosis_Code"];
                            dr["Patient_Accident_Diagnosis1_Description"] = row["Patient_Accident_Diagnosis_Description"];
                            dr["Patient_Accident_Diagnosis1_Side_Code"] = row["Patient_Accident_DiagnosisSide_Code"];
                            dr["Patient_Accident_Diagnosis1_Side_Description"] = row["Patient_Accident_DiagnosisSide_Description"];
                            dr["Patient_Accident_Diagnosis1_Qualifier"] = row["Patient_Accident_DiagnosisQualifier_Code"];
                            break;
                        case 1:
                            dr["Patient_Accident_Diagnosis2_Code"] = row["Patient_Accident_Diagnosis_Code"];
                            dr["Patient_Accident_Diagnosis2_Description"] = row["Patient_Accident_Diagnosis_Description"];
                            dr["Patient_Accident_Diagnosis2_Side_Code"] = row["Patient_Accident_DiagnosisSide_Code"];
                            dr["Patient_Accident_Diagnosis2_Side_Description"] = row["Patient_Accident_DiagnosisSide_Description"];
                            dr["Patient_Accident_Diagnosis2_Qualifier"] = row["Patient_Accident_DiagnosisQualifier_Code"];
                            break;
                        case 2:
                            dr["Patient_Accident_Diagnosis3_Code"] = row["Patient_Accident_Diagnosis_Code"];
                            dr["Patient_Accident_Diagnosis3_Description"] = row["Patient_Accident_Diagnosis_Description"];
                            dr["Patient_Accident_Diagnosis3_Side_Code"] = row["Patient_Accident_DiagnosisSide_Code"];
                            dr["Patient_Accident_Diagnosis3_Side_Description"] = row["Patient_Accident_DiagnosisSide_Description"];
                            dr["Patient_Accident_Diagnosis3_Qualifier"] = row["Patient_Accident_DiagnosisQualifier_Code"];
                            break;
                    }
                }
            }
            if (dr != null)
            dtNumberedDiagnosis.Rows.Add(dr);
            return dtNumberedDiagnosis;
        }
        private XmlNode MakeGroupFieldsFromTable(XmlNode group, Int64 apptID, DataRow row, DataColumnCollection dtCol)
        {
            group.InnerXml = string.Empty;
            StringBuilder sb = new StringBuilder();
            for (int counter = 0; counter < dtCol.Count; counter++)
            {
                string value = GetColumnValue(row, dtCol[counter].ColumnName);
                sb.Append("<field conceptName='" + dtCol[counter].ColumnName + "'>" + value + "</field>");
            }
            group.InnerXml = sb.ToString();
            return group;
        }
        private XmlNode FillExistingGroupFields(XmlNode group, Int64 apptID, DataRow row, DataColumnCollection dtCol)
        {
            IEnumerable<XmlNode> fieldList = group.ChildNodes.OfType<XmlNode>().Where(e => e.LocalName == "field");
            if (group.Attributes["referenceID"] != null && group.Attributes["referenceID"].Value == string.Empty)
            {
                group.Attributes["referenceID"].Value = row["referenceID"].ToString();
            }
            // Following if block is a chappy to handle referrals in other way.
            StringBuilder sb = new StringBuilder();
            if ((group.Attributes["name"] != null && group.Attributes["name"].Value.ToString().ToLower() == "patient_accident") || (group.Attributes["conceptName"] != null && group.Attributes["conceptName"].Value.ToString().ToLower() == "patient_accident"))
            {
                sb.Append(group.InnerXml);
                foreach (XmlNode field in fieldList)
                {
                    XmlNode n = field.Clone();
                    string conceptName = field.Attributes["conceptName"].Value.ToLower();
                    if (conceptName == "patient_accident_referral" || conceptName == "patient_accident_referralcomments")
                    {
                        if (row[conceptName + "1"] != DBNull.Value && string.IsNullOrEmpty(row[conceptName+ "1"].ToString()) == false)
                        {

                            sb.Append("<field conceptName='" + conceptName + "'/>");
                        }
                        if (row[conceptName + "2"] != DBNull.Value && string.IsNullOrEmpty(row[conceptName + "2"].ToString()) == false)
                        {

                            sb.Append("<field conceptName='" + conceptName + "'/>");
                        }
                        if (row[conceptName + "3"] != DBNull.Value && string.IsNullOrEmpty(row[conceptName + "3"].ToString()) == false)
                        {

                            sb.Append("<field conceptName='" + conceptName + "'/>");
                        }
                    }
                }
            }
            group.InnerXml = sb.ToString();
            int referralcounter = 1;
            foreach (XmlNode field in fieldList)
            {
                //XmlNode node = field.Clone();
                //string conceptName = node.Attributes["conceptName"].Value.ToLower();
                string conceptName = field.Attributes["conceptName"].Value.ToLower();
                string value = string.Empty;
                if (dtCol[conceptName] != null)
                {
                    if (row[conceptName] != DBNull.Value && string.IsNullOrEmpty(row[conceptName].ToString()) == false)
                    {
                        value = row[conceptName].ToString();
                        if (conceptName.ToLower().Contains("date"))
                        {
                            string datepart, timepart = string.Empty;
                            datepart = Convert.ToDateTime(value).ToString("yyyy-MM-dd");
                            if (conceptName != "patient_dateofbirth")
                            {
                                timepart = "T" + Convert.ToDateTime(value).ToString("hh:mm:ss");
                            }
                            value = datepart + timepart;
                        }
                        //node.InnerText = value;
                        field.InnerText = value;
                    }
                }
                // Following elseif block is a chappy to handle referrals in other way.
                else if (conceptName.StartsWith("patient_accident_referral") && dtCol[conceptName + referralcounter.ToString()] != null)
                {
                    if (row[conceptName + referralcounter.ToString()] != DBNull.Value && string.IsNullOrEmpty(row[conceptName + referralcounter.ToString()].ToString()) == false)
                        value = row[conceptName + referralcounter.ToString()].ToString();
                    field.InnerText = value;
                    referralcounter++;
                }
                

                if (field.Attributes["referenceID"] != null && field.Attributes["referenceID"].Value == string.Empty)
                {
                    //if (dt.Rows.Count > 0)
                    field.Attributes["referenceID"].Value = row["referenceID"].ToString();
                }
                //parentGroup.AppendChild(node);

            }
            return group;
        }

        private XmlDocument InsertGroups(XmlDocument xSourceDoc, HealthLinkSession objSessionKey)
        {
            XmlNodeList groupList = xSourceDoc.DocumentElement.GetElementsByTagName("group");
            XmlDocument xTargetDoc = xSourceDoc;
            XmlNodeList sectionList = xTargetDoc.DocumentElement.GetElementsByTagName("section");
            Dictionary<string, List<XmlNode>> dictGroups = new Dictionary<string, List<XmlNode>>();
            foreach (XmlNode group in groupList)
            {
                DataTable dt = GetGroupTableByGroup(group, objSessionKey);
                int count = 1;
                string sectionName = group.ParentNode.Attributes["name"].Value;
                List<XmlNode> _groupList = new List<XmlNode>();
                if (dt != null)
                {
                    foreach (DataRow row in dt.Rows)
                    {
                        XmlNode cloneGrope = group.Clone();
                        if (group.HasChildNodes == true && group.ChildNodes.Count == 1 && group.ChildNodes.Item(0).Attributes["conceptName"].Value == "")
                        {
                            cloneGrope = MakeGroupFieldsFromTable(cloneGrope, objSessionKey.AppointmentId, row, dt.Columns);
                        }
                        //else
                        //{
                        //    cloneGrope = FillExistingGroupFields(cloneGrope, objSessionKey.AppointmentId, row, dt.Columns);
                        //}
                        cloneGrope = FillExistingGroupFields(cloneGrope, objSessionKey.AppointmentId, row, dt.Columns);
                        _groupList.Add(cloneGrope);
                    }
                }
                if (dictGroups.ContainsKey(sectionName))
                {
                    List<XmlNode> lstNodes = dictGroups[sectionName];
                    foreach (XmlNode n in _groupList)
                    {
                        lstNodes.Add(n);
                    }
                    dictGroups[sectionName] = lstNodes;
                }
                else
                    dictGroups.Add(sectionName, _groupList);
                count++;
            }
            foreach (XmlNode section in sectionList)
            {
                string sectionName = section.Attributes["name"].Value;
                if (dictGroups.ContainsKey(sectionName))
                {
                    List<XmlNode> lstNodes = dictGroups[sectionName];
                    foreach (XmlNode n in lstNodes)
                    {
                        section.AppendChild(n);
                    }
                }
            }

            //return xTargetDoc =  RemoveEmptyGroups(xTargetDoc);
            return xTargetDoc;
        }
        private XmlDocument RemoveEmptyGroups(XmlDocument xSource)
        {

            XmlNodeList groupList = xSource.DocumentElement.GetElementsByTagName("group");
            foreach (XmlNode group in groupList)
            {
                if (group.HasChildNodes == true && group.ChildNodes.Count == 1 && group.ChildNodes.Item(0).Attributes["conceptName"].Value == "")
                {
                    XmlAttribute xa = xSource.CreateAttribute("del");
                    xa.Value = "1";
                    group.Attributes.Append(xa);
                    //xTargetDoc.RemoveChild(group);
                }
            }

            XDocument xDoc = XDocument.Parse(xSource.OuterXml);
            //xDoc.Descendants().Where(item => item.Element("del").Value.Equals("1")).ToList().ForEach(item => item.Remove());


            var xmlDocument = new XmlDocument();
            using (var xmlReader = xDoc.CreateReader())
            {
                xmlDocument.Load(xmlReader);
            }
            return xmlDocument;
        }
        public static void SaveFile(XmlDocument xdoc, string fileName, byte type)
        {
            string path = "D:\\Projects\\HISO-ServiceProject\\Hiso\\Hiso\\data\\SubmittedData\\";
            if (type == 1)
            {
                path = path + fileName + "-Request.xml";
            }
            else
            {
                path = path + fileName + "-Response.xml";
            }

            //System.IO.File.Create()

            //System.IO.File.Create(path);
            System.IO.File.WriteAllText(path, xdoc.OuterXml);
        }
        public XmlDocument FillXml(XmlDocument xDoc, HealthLinkSession objSessionKey)
        {
            string fileName = DateTime.Now.Ticks.ToString();
            //SaveFile(xDoc, fileName, 1);
            /* Data Classes */

            xDoc = InsertGroups(xDoc, objSessionKey);
            XmlNodeList fieldList = xDoc.DocumentElement.GetElementsByTagName("field");
            DataSet dsResult = new DataSet();
            SqlConnection con = new SqlConnection(System.Configuration.ConfigurationManager.ConnectionStrings["ConectionStringPMS_NZ"].ToString());
            try
            {
                SqlCommand cmd = new SqlCommand();
                cmd.CommandText = "Appointment.uspGetHLFormDetailPMS";
                cmd.CommandType = CommandType.StoredProcedure;
                cmd.Connection = con;
                cmd.Parameters.Add(new SqlParameter("@LoggedInProviderId", objSessionKey.ProviderId)); //186238
                cmd.Parameters.Add(new SqlParameter("@AppointmentId", objSessionKey.AppointmentId)); //3500,3488
                cmd.Parameters.Add(new SqlParameter("@PatientId", objSessionKey.PatientId)); //3500,3488
                cmd.Parameters.Add(new SqlParameter("@PracticeId", objSessionKey.PracticeId)); //3500,3488
                con.Open();
                SqlDataAdapter adapter = new SqlDataAdapter(cmd);
                adapter.Fill(dsResult);
            }
            catch (Exception ex)
            {
                throw ex;
            }
            finally
            {
                con.Close();
            }
            DataTable dtReferrer = dsResult.Tables[0];
            DataTable dtPatient = dsResult.Tables[1];
            DataTable dtPatientDisabilities = dsResult.Tables[2];
            DataTable dtSettings = dsResult.Tables[3];
            DataTable dtPractioners = dsResult.Tables[4];

            DataSet dsMeasurements = null;

            dsResult = null;
            foreach (XmlNode field in fieldList)
            {
                //field.InnerText = " ";
                if (field.Attributes["conceptName"] != null /*&& field.ParentNode.Name.ToLower()!="group"*/)
                {
                    string conceptName = field.Attributes["conceptName"].Value.ToLower();
                    string value = string.Empty;
                    DataRow row = null;
                    if (conceptName.StartsWith("currentuser") && dtReferrer.Rows.Count > 0 && dtReferrer.Columns[conceptName] != null)
                    {
                        row = dtReferrer.Rows[0];
                    }
                    else if (conceptName == "patient_measurement" || (field.Attributes["name"] != null && field.Attributes["name"].Value.StartsWith("measurement.")))
                    {
                        DataTable dt = null;
                        if (dsMeasurements == null)
                        {
                            dsMeasurements = GetPatientMeasurements(objSessionKey);
                        }
                        if (dsMeasurements.Tables.Count > 0)
                        {
                            string name = string.Empty;
                            if (field.Attributes["name"] != null)
                                name = field.Attributes["name"].Value.ToLower();
                            if (string.IsNullOrEmpty(name) == false)
                            {
                                if (name.StartsWith("measurement.bloodpressure"))
                                {
                                    dt = dsMeasurements.Tables[0];
                                }
                                else if (name.Equals("measurement.weight") || name.Equals("measurement.height") || name.Equals("measurement.bmi"))
                                {
                                    dt = dsMeasurements.Tables[1];
                                }
                                else if (name.EndsWith("serumcreatinine"))
                                {
                                    dt = dsMeasurements.Tables[2];
                                }
                                else if (name.EndsWith("microalbumincreaturine"))
                                {
                                    dt = dsMeasurements.Tables[3];
                                }
                                else if (name.StartsWith("measurement.cholesterol"))
                                {
                                    dt = dsMeasurements.Tables[4];
                                }
                                else if (name.EndsWith("hba1c(dcct)"))
                                {
                                    dt = dsMeasurements.Tables[5];
                                }
                                else if (name.EndsWith("hba1c(mmol/mol)"))
                                {
                                    dt = dsMeasurements.Tables[6];
                                }
                                else if (name.Equals("measurement.egfr"))
                                {
                                    dt = dsMeasurements.Tables[8];
                                }
                                else if (name.Equals("measurement.diabetestype1") || name.Equals("measurement.diabetestype2"))
                                {
                                    dt = dsMeasurements.Tables[10];
                                }
                                if (dt != null && dt.Rows.Count > 0 && dt.Columns[name] != null)
                                    field.InnerText = GetColumnValue(dt.Rows[0], name);
                                continue;
                            }
                        }
                    }
                    else if (conceptName.StartsWith("registeredpractitioner"))
                    {
                        if (dtPractioners.Rows.Count > 0)
                            row = dtPractioners.Rows[0];
                    }
                    else if (IsGroupConcept(field) == false && conceptName.StartsWith("patient") && dtPatient.Rows.Count > 0 && dtPatient.Columns[conceptName] != null)
                    {
                        //if (conceptName != "patient_iscommunicationimpaired" ||
                        //   conceptName != "Patient_IsVisionImpaired" ||
                        //   conceptName != "Patient_IsIntellectuallyImpaired" ||
                        //   conceptName != "Patient_IsMobilityImpaired")
                        //{
                        //    if(dtPatientDisabilities.Rows.Count>0)
                        //        row = dtPatientDisabilities.Rows[0];
                        //}
                        //else
                        row = dtPatient.Rows[0];
                    }
                    else if (dtSettings.Rows.Count > 0 && conceptName.StartsWith("pms_"))
                    {
                        if (conceptName == "pms_application version")
                            conceptName = "pms_applicationversion";
                        if (dtSettings.Columns[conceptName] != null)
                            row = dtSettings.Rows[0];
                    }
                    else
                    {
                        continue;
                    }
                    XmlDocument xx = xDoc;
                    field.InnerText = GetColumnValue(row, conceptName);
                }
            }
            dtReferrer = null;
            dtPatient = null;
            dtSettings = null;
            //SaveFile(xDoc, fileName, 2);
            return xDoc;
        }
        private string GetColumnValue(DataRow row, string columnName)
        {
            string value = string.Empty;
            if (row == null)
                return value;
            if(row.Table.Columns.Contains(columnName)==false)
                return value;
            if (row[columnName] != DBNull.Value && string.IsNullOrEmpty(row[columnName].ToString()) == false)
            {
                value = row[columnName].ToString();
                if (columnName.ToLower().Contains("date"))
                {
                    string datepart, timepart = string.Empty;
                    datepart = Convert.ToDateTime(value).ToString("yyyy-MM-dd");
                    if (columnName != "patient_dateofbirth")
                    {
                        timepart = "T" + Convert.ToDateTime(value).ToString("hh:mm:ss");
                    }
                    value = datepart + timepart;
                }
                else if (columnName.ToLower().Equals("patient_ethnicity1codelevel2"))
                {
                    if (int.Parse(value) < 10)
                    {
                        value = "0" + value;
                    }
                }
            }
            return value;
        }
        public static bool IsGroupConcept(XmlNode node)
        {
            bool isgroupconcept = false;
            if (node.ParentNode.Name.ToLower() == "group" || node.Name.ToLower() == "group")
                isgroupconcept = true;
            return isgroupconcept;
        }
        private bool IsNodeAttributeNull(XmlNode node, string attribute)
        {
            return node.Attributes[attribute] == null ? true : false;
        }
        private bool IsNodeHasModifiers(XmlNode node)
        {
            if (node.Attributes["refresh"] != null ||
                node.Attributes["order"] != null ||
                node.Attributes["startPosition"] != null ||
                node.Attributes["numRows"] != null ||
                node.Attributes["minDateTime"] != null ||
                node.Attributes["maxDateTime"] != null ||
                node.Attributes["searchString"] != null ||
                node.Attributes["minVal"] != null ||
                node.Attributes["maxVal"] != null ||
                node.Attributes["referenceID"] != null)
                return true;
            else return false;
        }
        private DataSet GetPatientMeasurements(HealthLinkSession objSessionKey)
        {
            DataSet dsResult = new DataSet();
            SqlConnection con = new SqlConnection(System.Configuration.ConfigurationManager.ConnectionStrings["ConectionStringPMS_NZ"].ToString());
            try
            {
                SqlCommand cmd = new SqlCommand();
                cmd.CommandText = "Appointment.uspGetHLFormMeasurmentsDetailPMS";
                cmd.CommandType = CommandType.StoredProcedure;
                cmd.Connection = con;
                cmd.Parameters.Add(new SqlParameter("@AppointmentId", objSessionKey.AppointmentId)); //3500,3488
                cmd.Parameters.Add(new SqlParameter("@PatientId", objSessionKey.PatientId)); //3500,3488
                con.Open();
                SqlDataAdapter adapter = new SqlDataAdapter(cmd);
                adapter.Fill(dsResult);
            }
            catch (Exception ex)
            {
                throw ex;
            }
            finally
            {
                con.Close();
            }
            return dsResult;
        }
        public static DataTable GetConceptMappingTable()
        {
            DataSet dsResult = new DataSet();
            SqlConnection con = new SqlConnection(System.Configuration.ConfigurationManager.ConnectionStrings["ConectionStringPMS_NZ"].ToString());
            try
            {
                SqlCommand cmd = new SqlCommand();
                cmd.CommandText = "Appointment.uspGetHL7ConceptMapping";
                cmd.CommandType = CommandType.StoredProcedure;
                cmd.Connection = con;
                con.Open();
                SqlDataAdapter adapter = new SqlDataAdapter(cmd);
                adapter.Fill(dsResult);
            }
            catch (Exception ex)
            {
                throw ex;
            }
            finally
            {
                con.Close();
            }
            if (dsResult.Tables.Count > 0)
                return dsResult.Tables[0];
            else
                return null;
        }

        public bool SaveAccidentInformation(FormMetaData objMetaData, DataTable dt, DataTable dt2, DataTable dtRef,int mode)
        {
            bool retVal = false;
            SqlConnection con = new SqlConnection(System.Configuration.ConfigurationManager.ConnectionStrings["ConectionStringPMS_NZ"].ToString());
            try
            {
                SqlCommand cmd = new SqlCommand();
                cmd.CommandText = "Appointment.usptblACC45Detail_InsertUpdate_New";
                cmd.CommandType = CommandType.StoredProcedure;
                cmd.Parameters.Add(new SqlParameter("@tblACC45Detail", dt));
                cmd.Parameters.Add(new SqlParameter("@tblAcc45Diagnosis", dt2));
                cmd.Parameters.Add(new SqlParameter("@FormInstanceVersion", objMetaData.formInstanceVersion.Value));
                cmd.Parameters.Add(new SqlParameter("@FormEngineID", objMetaData.formEngineId.Value));
                cmd.Parameters.Add(new SqlParameter("@Mode", mode));
                cmd.Parameters.Add(new SqlParameter("@tblAcc45Referral", dtRef));
                cmd.Connection = con;
                con.Open();
                cmd.ExecuteNonQuery();
                retVal = true;
            }
            catch (Exception ex)
            {
                retVal = false;
                throw ex;
            }
            finally
            {

                con.Close();
            }
            return retVal;
        }

        #region Add DMS data
        
        public static int SaveDocumentToDMS(int clientID, int categoryID, string documentName, int documentTypeID, string description, string documentKey, byte[] contentData, string filePath = "", bool IsSaveOnClud = false, int PracticeID = 0, int ProfileID = 0)
        {
            int result = 0;
            try
            {
                Logger.Logging.Instance.WriteEventLog("AddDirectDMS (Connection string): "+ System.Configuration.ConfigurationManager.ConnectionStrings["ConectionStringPMS_NZ_DMS"].ToString());
                SqlConnection con = new SqlConnection(System.Configuration.ConfigurationManager.ConnectionStrings["ConectionStringPMS_NZ_DMS"].ToString());

                //using (SqlConnection con = new SqlConnection(connectionString))
                //{
                    con.Open();

                    SqlCommand sCommand = new SqlCommand("[dbo].[uspDocumentSave]", con);
                    sCommand.CommandType = CommandType.StoredProcedure;
                    sCommand.Parameters.AddWithValue("@pDocumentID", 0);
                    sCommand.Parameters.AddWithValue("@pClientID", clientID);
                    sCommand.Parameters.AddWithValue("@pCategoryID", categoryID);
                    sCommand.Parameters.AddWithValue("@pDocumentName", documentName);
                    sCommand.Parameters.AddWithValue("@pDocumentTypeID", documentTypeID);
                    sCommand.Parameters.AddWithValue("@pDescription", description);
                    sCommand.Parameters.AddWithValue("@pDocumentKey", documentKey);
                    sCommand.Parameters.AddWithValue("@pDocumentSize", contentData.Length);
                    sCommand.Parameters.AddWithValue("@pProfileID", ProfileID);
                    sCommand.Parameters.AddWithValue("@pPracticeID", PracticeID);

                    if (filePath != "")
                    {
                        sCommand.Parameters.AddWithValue("@pFilePath", filePath);
                    }
                    sCommand.Parameters.AddWithValue("@pIsSaveOnCloud", IsSaveOnClud);



                    sCommand.Parameters.AddWithValue("@pDocumentData", contentData);

                    SqlParameter sOutParam = new SqlParameter("@pDocumentIDOut", SqlDbType.Int);
                    sOutParam.Direction = ParameterDirection.Output;
                    sOutParam.Value = -1;

                    sCommand.Parameters.Add(sOutParam);

                    sCommand.ExecuteNonQuery();
                    result = sOutParam.Value == DBNull.Value ? result : Convert.ToInt32(sOutParam.Value);

                    Logger.Logging.Instance.WriteEventLog("AddDirectDMS Result: "+ result.ToString());
                //}
            }
            catch (Exception ex)
            {
                //Utility.Log("Exception", "Exption occured in SaveDMS " + ex.StackTrace + Environment.NewLine);
                result = 0;
                Logger.Logging.Instance.WriteEventLog("AddDirectDMS Result: " + ex.ToString());
                throw ex;

            }

            return result;
        }
        #endregion

        public DataTable GetAccidentInformation(HealthLinkSession objSessionKey)
        {
            DataSet dsResult = new DataSet();
            DataTable dt = null;
            SqlConnection con = new SqlConnection(System.Configuration.ConfigurationManager.ConnectionStrings["ConectionStringPMS_NZ"].ToString());
            SqlCommand cmd = new SqlCommand();

            try
            {
                cmd.Connection = con;
                cmd.Parameters.Add(new SqlParameter("@AppointmentId", objSessionKey.AppointmentId));
                cmd.Parameters.Add(new SqlParameter("@PatientId", objSessionKey.PatientId));

                if (objSessionKey.ReferenceId > 0)
                    cmd.Parameters.Add(new SqlParameter("@ACC45ID", objSessionKey.ReferenceId));
                cmd.CommandText = "Appointment.uspGetHLFormAccidentsDetailPMS";
                cmd.CommandType = CommandType.StoredProcedure;

                con.Open();
                SqlDataAdapter adapter = new SqlDataAdapter(cmd);
                adapter.Fill(dsResult);

                if (dsResult.Tables.Count > 0)
                    dt = dsResult.Tables[0];
            }
            catch (Exception)
            {
             
            }
            finally
            {
                con.Close();
            }
            return dt;
        }
        public bool IsInDictionary(string key, Dictionary<string, string> objDict)
        {
            return objDict.ContainsKey(key);

        }
        public static string GetNodeText(XmlDocument xDoc, string nodeType, string attributeName, string attributeValue)
        {
            string retVal = string.Empty;
            XmlNodeList xnl = xDoc.GetElementsByTagName(nodeType);
            foreach (XmlNode n in xnl)
            {
                if (n.Attributes[attributeName] != null && n.Attributes[attributeName].Value == attributeValue)
                {
                    retVal = n.InnerText;
                    break;
                }
            }
            return retVal;
        }
    }


    public class HealthLinkSession
    {
        public int ProviderId { get; set; }
        public int PatientId { get; set; }
        public Int64 AppointmentId { get; set; }
        public int ReferenceId { get; set; }
        public int PracticeId { get; set; }
        public int? PracticeLocationID { get; set; }
        public string PracticeEDI { get; set; }

        public Guid GUID { get; set; }
        public static HealthLinkSession GetByGUID(Guid guid)
        {
            HealthLinkSession objHLSession = null;
            DataSet dsResult = new DataSet();
            DataTable dt = null;
            SqlConnection con = new SqlConnection(System.Configuration.ConfigurationManager.ConnectionStrings["ConectionStringPMS_NZ"].ToString());
            SqlCommand cmd = new SqlCommand();

            cmd.Connection = con;
            cmd.Parameters.Add(new SqlParameter("@SessionGUID", guid));
            cmd.CommandText = "Appointment.usptblHealthLinkSession_GetByGUID";
            cmd.CommandType = CommandType.StoredProcedure;

            try
            {
                con.Open();
                SqlDataAdapter adapter = new SqlDataAdapter(cmd);
                adapter.Fill(dsResult);
                if (dsResult.Tables.Count > 0)
                {
                    dt = dsResult.Tables[0];
                    if (dt.Rows.Count > 0)
                    {
                        objHLSession = new HealthLinkSession();
                        objHLSession.PatientId = int.Parse(dt.Rows[0]["PatientID"].ToString());
                        objHLSession.ProviderId = int.Parse(dt.Rows[0]["ProviderID"].ToString());
                        objHLSession.PracticeId = int.Parse(dt.Rows[0]["PracticeID"].ToString());
                        objHLSession.AppointmentId = Int64.Parse(dt.Rows[0]["AppointmentID"].ToString());

                        if (dt.Rows[0]["PracticeLocationID"] != DBNull.Value && !string.IsNullOrEmpty(Convert.ToString(dt.Rows[0]["PracticeLocationID"])))
                        {
                            objHLSession.PracticeLocationID = int.Parse(dt.Rows[0]["PracticeLocationID"].ToString());
                        }
                        else
                        {
                            objHLSession.PracticeLocationID =  null;
                        }
                        
                        objHLSession.ReferenceId = 0;
                        if (dt.Rows[0]["PMSReferenceID"] != DBNull.Value)
                            objHLSession.ReferenceId = int.Parse(dt.Rows[0]["PMSReferenceID"].ToString());
                        objHLSession.GUID = Guid.Parse(dt.Rows[0]["SessionGUID"].ToString());

                        objHLSession.PracticeEDI = dt.Rows[0]["EDIAccount"].ToString();
                    }
                }
            }
            catch (Exception)
            {
             
            }
            finally
            {
                con.Close();
                
            }
            return objHLSession;
        }
    }
    public class Acc45XMl
    {
        public int ACC45ID { get; set; }
        public string ACC45XML { get; set; }
        public string ErrorDescription { get; set; }
        public int OldProviderId { get; set; }
        public int OldProfileTypeId { get; set; }
        public int NewProviderId { get; set; }
        public int NewProfileTypeId { get; set; }
    }
    //public class ACC45
    //{
    //    "ACC45ID", "ACC45Number", "AppointmentID", "AccidentDate", "AccidentTime", "AccidentISWPA", "AccidentMotorVehicle", "AccidentMedicalMisadventure", "PatientID", "ProviderID", "ACC45StatusID", "ACC45StatusDate", "CauseOfInjury", "OccupationACCCode", "OccupationDescription", "AccidentLocation", "AccidentLocationCode", "SceneCode", "SceneDescription", "EmployerOrganisationName", "EmployerOrganisationAddress", "Comments", "InsurerName", "IsInNZ", "IsWorkedRelated", "IsMotorVehicleRelated", "IsGradualInjury", "IsMisadventure", "SportCode", "FitForWork", "FullIncapacityStartDate", "FullIncapacityEndDate", "FullIncapacityPeriod", "PartialIncapacityStartDate", "PartialIncapacityEndDate", "PartialIncapacityHours", "PartialIncapacityPeriod", "WorkRestrictions", "WorkRestrictionsComments", "FormInstanceID", "FormInstanceVersion", "FormEngineID", "FormInstanceCreationDate", "FormInstanceOperationMode", "FormInstanceDescription", "FormDefinitionId", "FormDefinitionVersion", "FormDefinitionDescription", "FormDefinitionTitle", "RecipientAccount"
    //}
    public static class AccidentsSections
    {
        static List<string> lstSectionList = new List<string>();
    }
    public abstract class TableBuilder
    {
        public abstract DataTable GenerateTable(FormMetaData objFormMetaData, object request, HealthLinkSession objSessionKey);
    }



}





