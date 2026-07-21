using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Linq;
using System.Web;
using System.Xml;

namespace Hiso
{
    public class Task
    {
        public string Code { get; set; }
        public DateTime DueDate { get; set; }
        public string Description { get; set; }
        public decimal RecurrenceInterval { get; set; }

        public string IntervalUnit { get; set; }
        public bool Complete { get; set; }

        public static Task processTask(processActionRequest request, HealthLinkSession objSessionKey)
        {
            string actionType = request.actionId.ToLower();
            XmlElement xElem = request.actionContainer;
            Task objTask = new Task();
            if (xElem.GetElementsByTagName("code")[0].InnerText != "")
                objTask.Code = xElem.GetElementsByTagName("code")[0].InnerText;

            if (xElem.GetElementsByTagName("taskDescription")[0].InnerText != "")
                objTask.Description = xElem.GetElementsByTagName("taskDescription")[0].InnerText;
            if (xElem.GetElementsByTagName("recurrenceInterval")[0].InnerText != "")
                objTask.RecurrenceInterval = decimal.Parse(xElem.GetElementsByTagName("recurrenceInterval")[0].InnerText);
            if (xElem.GetElementsByTagName("intervalUnit")[0].InnerText != "")
                objTask.IntervalUnit = xElem.GetElementsByTagName("intervalUnit")[0].InnerText;
            if (xElem.GetElementsByTagName("dueDate")[0].InnerText != "")
            {
                string[] dateParts = xElem.GetElementsByTagName("dueDate")[0].InnerText.Split('-');
                DateTime dd = new DateTime(int.Parse(dateParts[2]), int.Parse(dateParts[1]), int.Parse(dateParts[0]));
                objTask.DueDate = dd;
            }
            objTask.Complete = bool.Parse(xElem.GetElementsByTagName("complete")[0].InnerText);
            return objTask;
        }
        public bool AddTask(Task objTask, HealthLinkSession objSessionKey)
        {
            bool retVal = false;
            SqlConnection con = new SqlConnection(System.Configuration.ConfigurationManager.ConnectionStrings["ConectionStringPMS_NZ"].ToString());
            try
            {

                string subject = GetConceptNameByReadCode(objTask, objSessionKey);
                subject = subject + "<br/>" + objTask.Description;
                SqlCommand cmd = new SqlCommand();
                cmd.CommandText = "Task.uspAddTaskExternal";
                cmd.CommandType = CommandType.StoredProcedure;
                cmd.Parameters.Add(new SqlParameter("@pTaskSubject", subject));//done
                cmd.Parameters.Add(new SqlParameter("@pProviderID", objSessionKey.ProviderId)); //done
                cmd.Parameters.Add(new SqlParameter("@pStartDate", objTask.DueDate)); //done
                cmd.Parameters.Add(new SqlParameter("@pEndDate", objTask.DueDate));//done
                cmd.Parameters.Add(new SqlParameter("@pPracticeID", objSessionKey.PracticeId));//done
                cmd.Parameters.Add(new SqlParameter("@pIsShowOnTimeline", true));//done
                //cmd.Parameters.Add(new SqlParameter("@pIsCarePlan", false));
                //cmd.Parameters.Add(new SqlParameter("@pCarePlanID", DBNull.Value));
                //cmd.Parameters.Add(new SqlParameter("@pAppointmentID", objSessionKey.AppointmentId));
                cmd.Parameters.Add(new SqlParameter("@pACC45ID", objSessionKey.ReferenceId));
                cmd.Parameters.Add(new SqlParameter("@pTaskTypeID", int.Parse(System.Configuration.ConfigurationManager.AppSettings["ACC45TaskTypeTypeId"].ToString()))); // ACC45 //done
                cmd.Parameters.Add(new SqlParameter("@pIsConfidential", false));//done
                //cmd.Parameters.Add(new SqlParameter("@pIsDeleted", false));
                cmd.Parameters.Add(new SqlParameter("@pUpdatedBy", objSessionKey.ProviderId));//done

                //cmd.Parameters.Add(new SqlParameter("@pIsActive", true));
                //cmd.Parameters.Add(new SqlParameter("@pPrimaryAssignTo", objSessionKey.ProviderId));
                cmd.Parameters.Add(new SqlParameter("@pProirtyID", int.Parse(System.Configuration.ConfigurationManager.AppSettings["TaskPriorityId"].ToString()))); //done
                if (objTask.Complete== false)
                    cmd.Parameters.Add(new SqlParameter("@pLastStatusID", int.Parse(System.Configuration.ConfigurationManager.AppSettings["TaskStatusActive"].ToString()))); // Active
                else
                    cmd.Parameters.Add(new SqlParameter("@pLastStatusID", int.Parse(System.Configuration.ConfigurationManager.AppSettings["TaskStatusCompleted"].ToString()))); // Active

                cmd.Parameters.Add(new SqlParameter("@pPatientID", objSessionKey.PatientId)); // Active//done

                cmd.Parameters.Add(new SqlParameter("@pUserLoggingID", 1)); // Active//done
                cmd.Parameters.Add(new SqlParameter("@pAddtoPrompt", false));//done
                cmd.Parameters.Add(new SqlParameter("@pTaskMedicationIds", ""));//done
                cmd.Parameters.Add(new SqlParameter("@pDataSourceID", 10));//done
                cmd.Parameters.Add(new SqlParameter("@pPharmacyID", DBNull.Value));//done
                cmd.Parameters.Add(new SqlParameter("@pOtherPharmacy", DBNull.Value));//done
                cmd.Parameters.Add(new SqlParameter("@pIsSelfPickup", false));//done
                cmd.Parameters.Add(new SqlParameter("@pPrescriberProviderID", DBNull.Value));//done
                //cmd.Parameters.Add(new SqlParameter("@pIsConfidential", false));//done
                cmd.Parameters.Add(new SqlParameter("@pPrimaryAssignFrom", DBNull.Value));//done
                cmd.Parameters.Add(new SqlParameter("@pMasterServiceSubServiceID", DBNull.Value));//done
                cmd.Parameters.Add(new SqlParameter("@pPrice", DBNull.Value));//done
                cmd.Parameters.Add(new SqlParameter("@pNoCharged", false));//done





                //                PriorityID
                //                    IsRead
                //                    IsActive bit Unchecked
                //IsDeleted   bit Unchecked
                //InsertedBy  int Unchecked
                //UpdatedBy   int Unchecked
                //InsertedAt datetime    Unchecked
                //UpdatedAt   datetime Unchecked

                //SqlParameter param = new SqlParameter("@pOutputParam", 0);
                //param.Direction = ParameterDirection.Output;
                //cmd.Parameters.Add(param);

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
        public string GetConceptNameByReadCode(Task objTask, HealthLinkSession objSessionKey)
        {
            string subject = string.Empty;
            DataSet dsResult = new DataSet();
            SqlConnection con = new SqlConnection(System.Configuration.ConfigurationManager.ConnectionStrings["ConectionStringPMS_NZ"].ToString());
            try
            {

                SqlCommand cmd = new SqlCommand();
                cmd.CommandText = "Billing.usptblConcept_GetByCode";
                cmd.CommandType = CommandType.StoredProcedure;
                cmd.Parameters.Add(new SqlParameter("@SNOMEDID", DBNull.Value));
                cmd.Parameters.Add(new SqlParameter("@ReadCode", objTask.Code));
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
            {
                if (dsResult.Tables[0].Rows.Count > 0)
                    subject = dsResult.Tables[0].Rows[0]["FullySpecifiedName"].ToString();
            }
            return subject;
        }
    }
    

}
