using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace HSSWebAPI.Models
{
    public class APIModels
    {

        public class Demographic
        {
            public List<DemographicInfo> ListDemographicInfo = new List<DemographicInfo>();
            public List<CardInfo> Listcardtype = new List<CardInfo>();
        }
        public class DemographicInfo
        {
            public string Nhi { get; set; }
            public string BirthDate { get; set; }
            public string Type { get; set; }
            public string TitleCode { get; set; }
            public string Given { get; set; }
            public string Family { get; set; }
            public string Gender { get; set; }
            public int Ethnicity1 { get; set; }
            public int Ethnicity2 { get; set; }
            public int Ethnicity3 { get; set; }
            public string Quintile { get; set; }
            public string Meshblock { get; set; }
            public string CellNumber { get; set; }
            public string DayPhone { get; set; }
            public string Email { get; set; }
            public string FullAddress { get; set; }
            public string EnrolmentStatus { get; set; }
            public string SmokingStatus { get; set; }
            public string Street { get; set; }
            public string Suburb { get; set; }
            public string City { get; set; }
            public string PostCode { get; set; }
            public string IsNZResident { get; set; }
            public string DateOfEnrolment { get; set; }
            public string EndEnrolmentDate { get; set; }
            
        }
        public class CardInfo
        {
            public string cardtype { get; set; }
            public string startdate { get; set; }
            public string expirydate { get; set; }
            public string cardnumber { get; set; }
        }
        public class Provider
        {
            public string Type { get; set; }
            public string Nzmc { get; set; }
            public string BirthDate { get; set; }
            public string TitleCode { get; set; }
            public string Given { get; set; }
            public string Family { get; set; }
            public string Gender { get; set; }
            public string DayPhone { get; set; }
            public string Email { get; set; }
        }
        public class ConsultNotes
        {
            public string SubjectiveNotes { get; set; }
            public string ObjectiveNotes { get; set; }
            public string Assessment { get; set; }
            public string Plans { get; set; }
            public string AppointmentAdvice { get; set; }
            public string Date { get; set; }
        }
        public class ConsultNote : ConsultNotes
        {
            public string EncounterId { get; set; }
            public string PatientId { get; set; }
            public string UserId { get; set; }
        }
        public class Medications
        {
            public string Sctid { get; set; }
            public string MedicineName { get; set; }
            public string Dosage { get; set; }
            public string Route { get; set; }
            public string ExpectedDuration { get; set; }
            public string StartDate { get; set; }
            public string IsLongterm { get; set; }
            public string Directions { get; set; }
        }
        public class LabResults
        {
            public string MessageSubject { get; set; }
            public string Title { get; set; }
            public string Code { get; set; }
            public string EffectiveDateTime { get; set; }
            public string Value { get; set; }
        }
        public class ScreeningCodes
        {
            public string ConceptId { get; set; }
            public string ScreeningShortName { get; set; }
            public string ScreeningName { get; set; }
        }
        public class Observation
        {
            public string ObservationDate { get; set; }
            public string ConceptId { get; set; }
            public string ShortName { get; set; }
            public string Name { get; set; }
            public string Value { get; set; }
            public string Units { get; set; }
        }
        public class Diagnosis
        {
            public string DiagnosisDate { get; set; }
            public string ConceptId { get; set; }
            public string Name { get; set; }
            public string FSN { get; set; }
            public string Type { get; set; }
            public string OnSetDate { get; set; }
            public string Summary { get; set; }
            public string IsLongTerm { get; set; }
        }
        public class Condition : Diagnosis
        {
            public string PatientId { get; set; }
            public string EncounterId { get; set; }
            public string UserId { get; set; }
            public string ResourceType { get; set; }
            public string System { get; set; }
        }
        public class Root<T>
        {
            public Root()
            {
                Entry = new List<T>();
            }
            public Root(string resourceType, string system)
            {
                this.ResourceType = resourceType;
                this.System = system;
                Entry = new List<T>();
            }
            public string PatientId { get; set; }
            public string ResourceType { get; set; }
            public string System { get; set; }
            public List<T> Entry { get; set; }
        }
        public class Auth
        {
            public string Status { get; set; }
            public string Token { get; set; }
            public string Expiry { get; set; }
            public string PracticeId { get; set; }
            public string Message { get; set; }
        }
        public class Credential
        {
            public string Username { get; set; }
            public string Password { get; set; }
            public string PatientId { get; set; }
            public string EncounterId { get; set; }
            public string System { get; set; }
            public string Pho { get; set; }
        }
        public class Documents
        {
            public string CreatedDateTime { get; set; }
            public string MessageSubject { get; set; }
            public string Identifier { get; set; }
            public string MessageTitle { get; set; }
            public byte[] MessageData { get; set; }
            public string ContentType { get; set; }
        }
        public class Document : Documents
        {
            public string PatientId { get; set; }
            public string EncounterId { get; set; }
            public string ResourceType { get; set; }
            public string System { get; set; }
            public string ContentType { get; set; }
            public string ItemType { get; set; }
        }

        public class PatientDMS
        {
            public string AttachmentReferenceID { get; set; }
            public string AttachmentSubject { get; set; }
            public string AttachmentComments { get; set; }
            public string AttachmentType { get; set; }
            public string AttachmentDataType { get; set; }
            public string AttachmentCreationDate { get; set; }
            public byte[] AttachmentContent { get; set; }
            public string AttachmentSize { get; set; }
        }
        public class PatientDMS2
        {
            public string AttachmentReferenceID { get; set; }
            public string AttachmentSubject { get; set; }
            public string AttachmentComments { get; set; }
            public string AttachmentType { get; set; }
            public string AttachmentDataType { get; set; }
            public string AttachmentCreationDate { get; set; }
            public string AttachmentContent { get; set; }
            public string AttachmentSize { get; set; }
        }
        public class Recalls
        {
            public string Group { get; set; }
            public string CategoryId { get; set; }
            public string Priority { get; set; }
            public string DueDate { get; set; }
            public string Notes { get; set; }
            public string Reason { get; set; }
        }
        public class Recall : Recalls
        {
            public string PatientId { get; set; }
            public string EncounterId { get; set; }
            public string UserId { get; set; }
            public string ResourceType { get; set; }
            public string System { get; set; }
        }
        public class Observations
        {
            public string PatientId { get; set; }
            public string EncounterId { get; set; }
            public string UserId { get; set; }
            public string ResourceType { get; set; }
            public string System { get; set; }
            public string Temperature { get; set; }
            public string WaistCircumference { get; set; }
            public string Height { get; set; }
            public string Weight { get; set; }
            public string BPSys { get; set; }
            public string BPDia { get; set; }
            public string HeartRate { get; set; }
            public string Risk { get; set; }
            public string Framingham { get; set; }
            public string Notes { get; set; }
        }
        public class PostedData
        {
            public string NameWithoutIdentifier { get; set; }
            public string OriginalName { get; set; }
            public string FieldId { get; set; }
            public string FieldColumnDB { get; set; }
            public string NameWithSpace { get; set; }
            public string Value { get; set; }
            public string Type { get; set; }
        }
        public class RecallCategories
        {
            public string Id { get; set; }
            public string Name { get; set; }
            public string Code { get; set; }
        }
        public class Invoice
        {
            public string PatientId { get; set; }
            public string EncounterId { get; set; }
            public string UserId { get; set; }
            public string ResourceType { get; set; }
            public string System { get; set; }
            public string LocationId { get; set; }
            public string Name { get; set; }
            public string Code { get; set; }
            public string ClaimType { get; set; }
            public string Fee { get; set; }
            //public string copayment { get; set; }
            public string payee { get; set; }
        }
    }
}