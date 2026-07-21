using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Xml.Serialization;

namespace ERMSWebAPI.Models
{
    public class APIModels
    {
        public class CurrentUserFirstName
        {
            [XmlAttribute(AttributeName = "conceptID")]
            public string ConceptID { get; set; }
            [XmlText]
            public string Text { get; set; }
        }
        public class CurrentUserSurname
        {
            [XmlAttribute(AttributeName = "conceptID")]
            public string ConceptID { get; set; }
            [XmlText]
            public string Text { get; set; }
        }
        public class CurrentUserMiddlename
        {
            [XmlAttribute(AttributeName = "conceptID")]
            public string ConceptID { get; set; }
            [XmlText]
            public string Text { get; set; }
        }
        public class CurrentUserFullName
        {
            [XmlAttribute(AttributeName = "conceptID")]
            public string ConceptID { get; set; }
            [XmlText]
            public string Text { get; set; }
        }
        public class RegisteringBody
        {
            [XmlAttribute(AttributeName = "conceptID")]
            public string ConceptID { get; set; }
            [XmlText]
            public string Text { get; set; }
        }
        public class RegistrationNumber
        {
            [XmlAttribute(AttributeName = "conceptID")]
            public string ConceptID { get; set; }
            [XmlText]
            public string Text { get; set; }
        }
        public class PersonalHPI
        {
            [XmlAttribute(AttributeName = "conceptID")]
            public string ConceptID { get; set; }
            [XmlText]
            public string Text { get; set; }
        }
        public class ApplicationUserID
        {
            [XmlAttribute(AttributeName = "conceptID")]
            public string ConceptID { get; set; }
            [XmlText]
            public string Text { get; set; }
        }
        public class FacilityHPI
        {
            [XmlAttribute(AttributeName = "conceptID")]
            public string ConceptID { get; set; }
            [XmlText]
            public string Text { get; set; }
        }
        public class HealthlinkEDI
        {
            [XmlAttribute(AttributeName = "conceptID")]
            public string ConceptID { get; set; }
            [XmlText]
            public string Text { get; set; }
        }
        [XmlRoot(ElementName = "CurrentUser")]
        public class CurrentUser
        {
            [XmlElement(ElementName = "CurrentUser_FirstName")]
            public CurrentUserFirstName FirstName { get; set; }  = new CurrentUserFirstName();
            [XmlElement(ElementName = "CurrentUser_Surname")]
            public CurrentUserSurname Surname { get; set; } = new CurrentUserSurname();
            [XmlElement(ElementName = "CurrentUser_Middlename")]
            public CurrentUserMiddlename Middlename { get; set; } = new CurrentUserMiddlename();
            [XmlElement(ElementName = "CurrentUser_FullName")]
            public CurrentUserFullName FullName { get; set; } = new CurrentUserFullName();
            [XmlElement(ElementName = "CurrentUser_RegisteringBody")]
            public RegisteringBody RegisteringBody { get; set; } = new RegisteringBody();
            [XmlElement(ElementName = "CurrentUser_RegistrationNumber")]
            public RegistrationNumber RegistrationNumber { get; set; } = new RegistrationNumber();
            [XmlElement(ElementName = "CurrentUser_PersonalHPI")]
            public PersonalHPI PersonalHPI { get; set; } = new PersonalHPI();
            [XmlElement(ElementName = "CurrentUser_Application_UserID")]
            public ApplicationUserID ApplicationUserID { get; set; } = new ApplicationUserID();
            [XmlElement(ElementName = "CurrentUserOrganisation_FacilityHPI")]
            public FacilityHPI FacilityHPI { get; set; } = new FacilityHPI();
            [XmlElement(ElementName = "CurrentUserOrganisation_HealthlinkEDI")]
            public HealthlinkEDI HealthlinkEDI { get; set; } = new HealthlinkEDI();
            [XmlElement(ElementName = "CurrentUser_PMSID")]
            public PMSID PMSID { get; set; } = new PMSID();
        }
        public class Credential
        {
            public string Username { get; set; }
            public string Password { get; set; }
            public string PatientId { get; set; }
            public string EncounterId { get; set; }
        }
        public class PatientSurname
        {
            [XmlAttribute(AttributeName = "conceptID")]
            public string ConceptID { get; set; }
            [XmlText]
            public string Text { get; set; }
        }
        public class PatientFirstName
        {
            [XmlAttribute(AttributeName = "conceptID")]
            public string ConceptID { get; set; }
            [XmlText]
            public string Text { get; set; }
        }
        public class PatientMiddleName
        {
            [XmlAttribute(AttributeName = "conceptID")]
            public string ConceptID { get; set; }
            [XmlText]
            public string Text { get; set; }
        }
        public class PatientNHI
        {
            [XmlAttribute(AttributeName = "conceptID")]
            public string ConceptID { get; set; }
            [XmlText]
            public string Text { get; set; }
        }
        public class PatientDateOfBirth
        {
            [XmlAttribute(AttributeName = "conceptID")]
            public string ConceptID { get; set; }
            [XmlText]
            public string Text { get; set; }
        }
        public class PatientGenderCode
        {
            [XmlAttribute(AttributeName = "conceptID")]
            public string ConceptID { get; set; }
            [XmlText]
            public string Text { get; set; }
        }
        public class RAStreetNumber
        {
            [XmlAttribute(AttributeName = "conceptID")]
            public string ConceptID { get; set; }
            [XmlText]
            public string Text { get; set; }
        }
        public class RAStreetName
        {
            [XmlAttribute(AttributeName = "conceptID")]
            public string ConceptID { get; set; }
            [XmlText]
            public string Text { get; set; }
        }
        public class RASuburb
        {
            [XmlAttribute(AttributeName = "conceptID")]
            public string ConceptID { get; set; }
            [XmlText]
            public string Text { get; set; }
        }
        public class RACity
        {
            [XmlAttribute(AttributeName = "conceptID")]
            public string ConceptID { get; set; }
            [XmlText]
            public string Text { get; set; }
        }
        public class RAPostcode
        {
            [XmlAttribute(AttributeName = "conceptID")]
            public string ConceptID { get; set; }
            [XmlText]
            public string Text { get; set; }
        }
        public class RAAdditionalLine
        {
            [XmlAttribute(AttributeName = "conceptID")]
            public string ConceptID { get; set; }
            [XmlText]
            public string Text { get; set; }
        }
        public class PAStreetNumber
        {
            [XmlAttribute(AttributeName = "conceptID")]
            public string ConceptID { get; set; }
            [XmlText]
            public string Text { get; set; }
        }
        public class PAStreetName
        {
            [XmlAttribute(AttributeName = "conceptID")]
            public string ConceptID { get; set; }
            [XmlText]
            public string Text { get; set; }
        }
        public class PASuburb
        {
            [XmlAttribute(AttributeName = "conceptID")]
            public string ConceptID { get; set; }
            [XmlText]
            public string Text { get; set; }
        }
        public class PACity
        {
            [XmlAttribute(AttributeName = "conceptID")]
            public string ConceptID { get; set; }
            [XmlText]
            public string Text { get; set; }
        }
        public class PAPostcode
        {
            [XmlAttribute(AttributeName = "conceptID")]
            public string ConceptID { get; set; }
            [XmlText]
            public string Text { get; set; }
        }
        public class PAAdditionalLine
        {
            [XmlAttribute(AttributeName = "conceptID")]
            public string ConceptID { get; set; }
            [XmlText]
            public string Text { get; set; }
        }
        public class Ethnicity1CodeLevel2
        {
            [XmlAttribute(AttributeName = "conceptID")]
            public string ConceptID { get; set; }
            [XmlText]
            public string Text { get; set; }
        }
        public class Ethnicity2CodeLevel2
        {
            [XmlAttribute(AttributeName = "conceptID")]
            public string ConceptID { get; set; }
            [XmlText]
            public string Text { get; set; }
        }
        public class Ethnicity3CodeLevel2
        {
            [XmlAttribute(AttributeName = "conceptID")]
            public string ConceptID { get; set; }
            [XmlText]
            public string Text { get; set; }
        }
        public class PatientEmailAddress
        {
            [XmlAttribute(AttributeName = "conceptID")]
            public string ConceptID { get; set; }
            [XmlText]
            public string Text { get; set; }
        }
        public class PatientMobile
        {
            [XmlAttribute(AttributeName = "conceptID")]
            public string ConceptID { get; set; }
            [XmlText]
            public string Text { get; set; }
        }
        public class PatientResidentialPhone
        {
            [XmlAttribute(AttributeName = "conceptID")]
            public string ConceptID { get; set; }
            [XmlText]
            public string Text { get; set; }
        }
        public class IsEligiblePublicFunds
        {
            [XmlAttribute(AttributeName = "conceptID")]
            public string ConceptID { get; set; }
            [XmlText]
            public string Text { get; set; }
        }
        public class PatientHUC
        {
            [XmlAttribute(AttributeName = "conceptID")]
            public string ConceptID { get; set; }
            [XmlText]
            public string Text { get; set; }
        }
        public class PatientHUCStartDate
        {
            [XmlAttribute(AttributeName = "conceptID")]
            public string ConceptID { get; set; }
            [XmlText]
            public string Text { get; set; }
        }
        public class PatientHUCEndDate
        {
            [XmlAttribute(AttributeName = "conceptID")]
            public string ConceptID { get; set; }
            [XmlText]
            public string Text { get; set; }
        }
        public class PatientCSC
        {
            [XmlAttribute(AttributeName = "conceptID")]
            public string ConceptID { get; set; }
            [XmlText]
            public string Text { get; set; }
        }
        public class PatientCSCStartDate
        {
            [XmlAttribute(AttributeName = "conceptID")]
            public string ConceptID { get; set; }
            [XmlText]
            public string Text { get; set; }
        }
        public class PatientCSCEndDate
        {
            [XmlAttribute(AttributeName = "conceptID")]
            public string ConceptID { get; set; }
            [XmlText]
            public string Text { get; set; }
        }
        public class InternalPMSID
        {
            [XmlAttribute(AttributeName = "conceptID")]
            public string ConceptID { get; set; }
            [XmlText]
            public string Text { get; set; }
        }
        [XmlRoot(ElementName = "PatientData")]
        public class PatientData
        {
            [XmlElement(ElementName = "Patient_Surname")]
            public PatientSurname Surname { get; set; } = new PatientSurname();
            [XmlElement(ElementName = "Patient_FirstName")]
            public PatientFirstName FirstName { get; set; } = new PatientFirstName();
            [XmlElement(ElementName = "Patient_MiddleName")]
            public PatientMiddleName MiddleName { get; set; } = new PatientMiddleName();
            [XmlElement(ElementName = "Patient_NHI")]
            public PatientNHI PatientNHI { get; set; } = new PatientNHI();
            [XmlElement(ElementName = "Patient_DateOfBirth")]
            public PatientDateOfBirth DateOfBirth { get; set; } = new PatientDateOfBirth();
            [XmlElement(ElementName = "Patient_Gender")]
            public PatientGenderCode GenderCode { get; set; } = new PatientGenderCode();
            [XmlElement(ElementName = "Patient_ResidentialAddress_StreetNumber")]
            public RAStreetNumber RAStreetNumber { get; set; } = new RAStreetNumber();
            [XmlElement(ElementName = "Patient_ResidentialAddress_StreetName")]
            public RAStreetName RAStreetName { get; set; } = new RAStreetName();
            [XmlElement(ElementName = "Patient_ResidentialAddress_Suburb")]
            public RASuburb RASuburb { get; set; } = new RASuburb();
            [XmlElement(ElementName = "Patient_ResidentialAddress_City")]
            public RACity RACity { get; set; } = new RACity();
            [XmlElement(ElementName = "Patient_ResidentialAddress_Postcode")]
            public RAPostcode RAPostcode { get; set; } = new RAPostcode();
            [XmlElement(ElementName = "Patient_ResidentialAddress_AdditionalLine")]
            public RAAdditionalLine RAAdditionalLine { get; set; } = new RAAdditionalLine();
            [XmlElement(ElementName = "Patient_PostalAddress_StreetNumber")]
            public PAStreetNumber PAStreetNumber { get; set; } = new PAStreetNumber();
            [XmlElement(ElementName = "Patient_PostalAddress_StreetName")]
            public PAStreetName PAStreetName { get; set; } = new PAStreetName();
            [XmlElement(ElementName = "Patient_PostalAddress_Suburb")]
            public PASuburb PASuburb { get; set; } = new PASuburb();
            [XmlElement(ElementName = "Patient_PostalAddress_City")]
            public PACity PACity { get; set; } = new PACity();
            [XmlElement(ElementName = "Patient_PostalAddress_Postcode")]
            public PAPostcode PAPostcode { get; set; } = new PAPostcode();
            [XmlElement(ElementName = "Patient_PostalAddress_AdditionalLine")]
            public PAAdditionalLine PAAdditionalLine { get; set; } = new PAAdditionalLine();
            [XmlElement(ElementName = "Patient_Ethnicity1CodeLevel2")]
            public Ethnicity1CodeLevel2 Ethnicity1CodeLevel2 { get; set; } = new Ethnicity1CodeLevel2();
            [XmlElement(ElementName = "Patient_Ethnicity2CodeLevel2")]
            public Ethnicity2CodeLevel2 Ethnicity2CodeLevel2 { get; set; } = new Ethnicity2CodeLevel2();
            [XmlElement(ElementName = "Patient_Ethnicity3CodeLevel2")]
            public Ethnicity3CodeLevel2 Ethnicity3CodeLevel2 { get; set; } = new Ethnicity3CodeLevel2();
            [XmlElement(ElementName = "Patient_Email")]
            public PatientEmailAddress PatientEmail { get; set; } = new PatientEmailAddress();
            [XmlElement(ElementName = "Patient_Mobile")]
            public PatientMobile PatientCellNumber { get; set; } = new PatientMobile();
            [XmlElement(ElementName = "Patient_ResidentialPhone")]
            public PatientResidentialPhone ResidentialPhone { get; set; } = new PatientResidentialPhone();
            [XmlElement(ElementName = "Patient_WorkPhone")]
            public WorkPhone WorkPhone { get; set; } = new WorkPhone();
            [XmlElement(ElementName = "Patient_IsEligiblePublicFunds")]
            public IsEligiblePublicFunds IsEligiblePublicFunds { get; set; } = new IsEligiblePublicFunds();
            [XmlElement(ElementName = "Patient_HUC")]
            public PatientHUC PatientHUC { get; set; } = new PatientHUC();
            [XmlElement(ElementName = "Patient_HUC_StartDate")]
            public PatientHUCStartDate PatientHUCStartDate { get; set; } = new PatientHUCStartDate();
            [XmlElement(ElementName = "Patient_HUC_EndDate")]
            public PatientHUCEndDate PatientHUCEndDate { get; set; } = new PatientHUCEndDate();
            [XmlElement(ElementName = "Patient_CSC")]
            public PatientCSC PatientCSC { get; set; } = new PatientCSC();
            [XmlElement(ElementName = "Patient_CSC_StartDate")]
            public PatientCSCStartDate PatientCSCStartDate { get; set; } = new PatientCSCStartDate();
            [XmlElement(ElementName = "Patient_CSC_EndDate")]
            public PatientCSCEndDate PatientCSCEndDate { get; set; } = new PatientCSCEndDate();
            [XmlElement(ElementName = "Patient_InternalPMSID")]
            public InternalPMSID InternalPMSID { get; set; } = new InternalPMSID();
        }
        public class ProblemComments
        {
            [XmlAttribute(AttributeName = "conceptID")]
            public string ConceptID { get; set; }
            [XmlText]
            public string Text { get; set; }
        }
        public class ProblemDescription
        {
            [XmlAttribute(AttributeName = "conceptID")]
            public string ConceptID { get; set; }
            [XmlText]
            public string Text { get; set; }
        }
        public class ProblemDateOfOnset
        {
            [XmlAttribute(AttributeName = "conceptID")]
            public string ConceptID { get; set; }
            [XmlText]
            public string Text { get; set; }
        }
        public class ProblemCode
        {
            [XmlAttribute(AttributeName = "conceptID")]
            public string ConceptID { get; set; }
            [XmlText]
            public string Text { get; set; }
        }
        public class ProblemCodingSystem
        {
            [XmlAttribute(AttributeName = "conceptID")]
            public string ConceptID { get; set; }
            [XmlText]
            public string Text { get; set; }
        }
        public class ProblemDateRecorded
        {
            [XmlAttribute(AttributeName = "conceptID")]
            public string ConceptID { get; set; }
            [XmlText]
            public string Text { get; set; }
        }
        public class ProblemIsLongTerm
        {
            [XmlAttribute(AttributeName = "conceptID")]
            public string ConceptID { get; set; }
            [XmlText]
            public string Text { get; set; }
        }
        public class PatientProblem
        {
            [XmlElement(ElementName = "Patient_Problem_Comments")]
            public ProblemComments Comments { get; set; } = new ProblemComments();
            [XmlElement(ElementName = "Patient_Problem_Description")]
            public ProblemDescription Description { get; set; } = new ProblemDescription();
            [XmlElement(ElementName = "Patient_Problem_DateOfOnset")]
            public ProblemDateOfOnset DateOfOnset { get; set; } = new ProblemDateOfOnset();
            [XmlElement(ElementName = "Patient_Problem_Code")]
            public ProblemCode Code { get; set; } = new ProblemCode();
            [XmlElement(ElementName = "Patient_Problem_CodingSystem")]
            public ProblemCodingSystem CodingSystem { get; set; } = new ProblemCodingSystem();
            [XmlElement(ElementName = "Patient_Problem_DateRecorded")]
            public ProblemDateRecorded DateRecorded { get; set; } = new ProblemDateRecorded();
            [XmlElement(ElementName = "Patient_Problem_IsLongTerm")]
            public ProblemIsLongTerm ProblemIsLongTerm { get; set; } = new ProblemIsLongTerm();
            [XmlAttribute(AttributeName = "order")]
            public string Order { get; set; }
            [XmlAttribute(AttributeName = "minDateTime")]
            public string MinDateTime { get; set; }
            [XmlAttribute(AttributeName = "maxDateTime")]
            public string MaxDateTime { get; set; }
            [XmlAttribute(AttributeName = "conceptName")]
            public string ConceptName { get; set; } = "Problems";
            [XmlAttribute(AttributeName = "conceptID")]
            public string ConceptID { get; set; }
            [XmlAttribute(AttributeName = "referenceID")]
            public string ReferenceId { get; set; }
        }
        [XmlRoot(ElementName = "Problems")]
        public class Problems
        {
            [XmlElement(ElementName = "Patient_Problem")]
            public List<PatientProblem> Problem { get; set; } = new List<PatientProblem>();
            [XmlAttribute(AttributeName = "name")] 
            public string Name { get; set; } = "Patient_Problem";
            [XmlAttribute(AttributeName = "conceptType")]
            public string ConceptType { get; set; } = "List";
        }
        public class MedStartedDate
        {
            [XmlAttribute(AttributeName = "conceptID")]
            public string ConceptID { get; set; }
            [XmlText]
            public string Text { get; set; }
        }
        public class MedName
        {
            [XmlAttribute(AttributeName = "conceptID")]
            public string ConceptID { get; set; }
            [XmlText]
            public string Text { get; set; }
        }
        public class MedCode
        {
            [XmlAttribute(AttributeName = "conceptID")]
            public string ConceptID { get; set; }
            [XmlText]
            public string Text { get; set; }
        }
        public class MedCodingSystem
        {
            [XmlAttribute(AttributeName = "conceptID")]
            public string ConceptID { get; set; }
            [XmlText]
            public string Text { get; set; }
        }
        public class MedDispenseQuantity
        {
            [XmlAttribute(AttributeName = "conceptID")]
            public string ConceptID { get; set; }
            [XmlText]
            public string Text { get; set; }
        }
        public class MedDispenseUnit
        {
            [XmlAttribute(AttributeName = "conceptID")]
            public string ConceptID { get; set; }
            [XmlText]
            public string Text { get; set; }
        }
        public class MedDosageQuantity
        {
            [XmlAttribute(AttributeName = "conceptID")]
            public string ConceptID { get; set; }
            [XmlText]
            public string Text { get; set; }
        }
        public class MedDosageUnit
        {
            [XmlAttribute(AttributeName = "conceptID")]
            public string ConceptID { get; set; }
            [XmlText]
            public string Text { get; set; }
        }
        public class MedAdministrationinstructions
        {
            [XmlAttribute(AttributeName = "conceptID")]
            public string ConceptID { get; set; }
            [XmlText]
            public string Text { get; set; }
        }
        public class MedLastPrescribedDate
        {
            [XmlAttribute(AttributeName = "conceptID")]
            public string ConceptID { get; set; }
            [XmlText]
            public string Text { get; set; }
        }
        public class RegularMedication
        {
            [XmlElement(ElementName = "Patient_RegularMedication_StartedDate")]
            public MedStartedDate StartedDate { get; set; } = new MedStartedDate();
            [XmlElement(ElementName = "Patient_RegularMedication_Name")]
            public MedName Name { get; set; } = new MedName();
            [XmlElement(ElementName = "Patient_RegularMedication_Code")]
            public MedCode Code { get; set; } = new MedCode();
            [XmlElement(ElementName = "Patient_RegularMedication_CodingSystem")]
            public MedCodingSystem CodingSystem { get; set; } = new MedCodingSystem();
            [XmlElement(ElementName = "Patient_RegularMedication_DispenseQuantity")]
            public MedDispenseQuantity DispenseQuantity { get; set; } = new MedDispenseQuantity();
            [XmlElement(ElementName = "Patient_RegularMedication_DispenseUnit")]
            public MedDispenseUnit DispenseUnit { get; set; } = new MedDispenseUnit();
            [XmlElement(ElementName = "Patient_RegularMedication_DosageQuantity")]
            public MedDosageQuantity DosageQuantity { get; set; } = new MedDosageQuantity();
            [XmlElement(ElementName = "Patient_RegularMedication_DosageUnit")]
            public MedDosageUnit DosageUnit { get; set; } = new MedDosageUnit();
            [XmlElement(ElementName = "Patient_RegularMedication_Administrationinstructions")]
            public MedAdministrationinstructions Administrationinstructions { get; set; } = new MedAdministrationinstructions();
            [XmlElement(ElementName = "Patient_RegularMedication_LastPrescribedDate")]
            public MedLastPrescribedDate LastPrescribedDate { get; set; } = new MedLastPrescribedDate();
            [XmlAttribute(AttributeName = "order")]
            public string Order { get; set; }
            [XmlAttribute(AttributeName = "minDateTime")]
            public string MinDateTime { get; set; }
            [XmlAttribute(AttributeName = "maxDateTime")]
            public string MaxDateTime { get; set; }
            [XmlAttribute(AttributeName = "conceptID")]
            public string ConceptID { get; set; }
            [XmlAttribute(AttributeName = "referenceID")]
            public string ReferenceId { get; set; }
        }
        [XmlRoot(ElementName = "RegularMedications")]
        public class RegularMedications
        {
            [XmlElement(ElementName = "Patient_RegularMedication")]
            public List<RegularMedication> RegularMedication { get; set; } = new List<RegularMedication>();
            [XmlAttribute(AttributeName = "conceptType")]
            public string ConceptType { get; set; } = "List";
        }
        public class PrescribedMedication
        {
            [XmlElement(ElementName = "Patient_PrescribedMedication_StartedDate")]
            public MedStartedDate StartedDate { get; set; } = new MedStartedDate();
            [XmlElement(ElementName = "Patient_PrescribedMedication_Name")]
            public MedName Name { get; set; } = new MedName();
            [XmlElement(ElementName = "Patient_PrescribedMedication_Code")]
            public MedCode Code { get; set; } = new MedCode();
            [XmlElement(ElementName = "Patient_PrescribedMedication_CodingSystem")]
            public MedCodingSystem CodingSystem { get; set; } = new MedCodingSystem();
            [XmlElement(ElementName = "Patient_PrescribedMedication_DispenseQuantity")]
            public MedDispenseQuantity DispenseQuantity { get; set; } = new MedDispenseQuantity();
            [XmlElement(ElementName = "Patient_PrescribedMedication_DispenseUnit")]
            public MedDispenseUnit DispenseUnit { get; set; } = new MedDispenseUnit();
            [XmlElement(ElementName = "Patient_PrescribedMedication_DosageQuantity")]
            public MedDosageQuantity DosageQuantity { get; set; } = new MedDosageQuantity();
            [XmlElement(ElementName = "Patient_PrescribedMedication_DosageUnit")]
            public MedDosageUnit DosageUnit { get; set; } = new MedDosageUnit();
            [XmlElement(ElementName = "Patient_PrescribedMedication_Administrationinstructions")]
            public MedAdministrationinstructions Administrationinstructions { get; set; } = new MedAdministrationinstructions();
            [XmlElement(ElementName = "Patient_PrescribedMedication_LastPrescribedDate")]
            public MedLastPrescribedDate LastPrescribedDate { get; set; } = new MedLastPrescribedDate();
            [XmlAttribute(AttributeName = "order")]
            public string Order { get; set; }
            [XmlAttribute(AttributeName = "minDateTime")]
            public string MinDateTime { get; set; }
            [XmlAttribute(AttributeName = "maxDateTime")]
            public string MaxDateTime { get; set; }
            [XmlAttribute(AttributeName = "conceptID")]
            public string ConceptID { get; set; }
            [XmlAttribute(AttributeName = "referenceID")]
            public string ReferenceId { get; set; }
        }
        public class PrescribedMedications
        {
            [XmlElement(ElementName = "Patient_PrescribedMedication")]
            public List<PrescribedMedication> Medication { get; set; } = new List<PrescribedMedication>();
            [XmlAttribute(AttributeName = "conceptType")]
            public string ConceptType { get; set; } = "List";
        }
        public class ConsultDate
        {
            [XmlAttribute(AttributeName = "conceptID")]
            public string ConceptID { get; set; }
            [XmlText]
            public string Text { get; set; }
        }
        public class ConsultExam
        {
            [XmlAttribute(AttributeName = "conceptID")]
            public string ConceptID { get; set; }
            [XmlAttribute(AttributeName = "IsBase64")]
            public string IsBase64 { get; set; }
            [XmlText]
            public string Text { get; set; }
        }
        public class ConsultHistory
        {
            [XmlAttribute(AttributeName = "conceptID")]
            public string ConceptID { get; set; }
            [XmlAttribute(AttributeName = "IsBase64")]
            public string IsBase64 { get; set; }
            [XmlText]
            public string Text { get; set; }
        }
        public class ConsultAssess
        {
            [XmlAttribute(AttributeName = "conceptID")]
            public string ConceptID { get; set; }
            [XmlAttribute(AttributeName = "IsBase64")]
            public string IsBase64 { get; set; }
            [XmlText]
            public string Text { get; set; }
        }
        public class ConsultPlan
        {
            [XmlAttribute(AttributeName = "conceptID")]
            public string ConceptID { get; set; }
            [XmlAttribute(AttributeName = "IsBase64")]
            public string IsBase64 { get; set; }
            [XmlText]
            public string Text { get; set; }
        }

        [XmlRoot(ElementName = "Patient_Consult")]
        public class Consult
        {
            [XmlElement(ElementName = "Patient_Consult_Date")]
            public ConsultDate ConsultDate { get; set; } = new ConsultDate();
            [XmlElement(ElementName = "Patient_Consult_Exam")]
            public ConsultExam ConsultExam { get; set; } = new ConsultExam();
            [XmlElement(ElementName = "Patient_Consult_History")]
            public ConsultHistory ConsultHistory { get; set; } = new ConsultHistory();
            [XmlElement(ElementName = "Patient_Consult_Assessment")]
            public ConsultAssess ConsultAssess { get; set; } = new ConsultAssess();
            [XmlElement(ElementName = "Patient_Consult_Plan")]
            public ConsultPlan ConsultPlan { get; set; } = new ConsultPlan();
            [XmlAttribute(AttributeName = "order")]
            public string Order { get; set; }
            [XmlAttribute(AttributeName = "maxDateTime")]
            public string MaxDateTime { get; set; }
            [XmlAttribute(AttributeName = "minDateTime")]
            public string MinDateTime { get; set; }
            [XmlAttribute(AttributeName = "conceptID")]
            public string ConceptID { get; set; }
            [XmlAttribute(AttributeName = "referenceID")]
            public string ReferenceId { get; set; }
        }
        [XmlRoot(ElementName = "ConsultNotes")]
        public class ConsultNotes
        {
            [XmlElement(ElementName = "Patient_Consult")]
            public List<Consult> Consult { get; set; } = new List<Consult>();
            [XmlAttribute(AttributeName = "conceptType")]
            public string ConceptType { get; set; } = "List";
        }
        public class AdditionalLine
        {
            [XmlAttribute(AttributeName = "conceptID")]
            public string ConceptID { get; set; }
            [XmlText]
            public string Text { get; set; }
        }
        public class City
        {
            [XmlAttribute(AttributeName = "conceptID")]
            public string ConceptID { get; set; }
            [XmlText]
            public string Text { get; set; }
        }
        public class Postcode
        {
            [XmlAttribute(AttributeName = "conceptID")]
            public string ConceptID { get; set; }
            [XmlText]
            public string Text { get; set; }
        }
        public class StreetName
        {
            [XmlAttribute(AttributeName = "conceptID")]
            public string ConceptID { get; set; }
            [XmlText]
            public string Text { get; set; }
        }
        public class StreetNumber
        {
            [XmlAttribute(AttributeName = "conceptID")]
            public string ConceptID { get; set; }
            [XmlText]
            public string Text { get; set; }
        }
        public class UnitNumber
        {
            [XmlAttribute(AttributeName = "conceptID")]
            public string ConceptID { get; set; }
            [XmlText]
            public string Text { get; set; }
        }
        public class Suburb
        {
            [XmlAttribute(AttributeName = "conceptID")]
            public string ConceptID { get; set; }
            [XmlText]
            public string Text { get; set; }
        }
        public class Firstname
        {
            [XmlAttribute(AttributeName = "conceptID")]
            public string ConceptID { get; set; }
            [XmlText]
            public string Text { get; set; }
        }
        public class Middlename
        {
            [XmlAttribute(AttributeName = "conceptID")]
            public string ConceptID { get; set; }
            [XmlText]
            public string Text { get; set; }
        }
        public class Surname
        {
            [XmlAttribute(AttributeName = "conceptID")]
            public string ConceptID { get; set; }
            [XmlText]
            public string Text { get; set; }
        }
        public class Mobile
        {
            [XmlAttribute(AttributeName = "conceptID")]
            public string ConceptID { get; set; }
            [XmlText]
            public string Text { get; set; }
        }
        public class PreferredNumber
        {
            [XmlAttribute(AttributeName = "conceptID")]
            public string ConceptID { get; set; }
            [XmlText]
            public string Text { get; set; }
        }
        public class Relationship
        {
            [XmlAttribute(AttributeName = "conceptID")]
            public string ConceptID { get; set; }
            [XmlText]
            public string Text { get; set; }
        }
        public class ResidentialPhone
        {
            [XmlAttribute(AttributeName = "conceptID")]
            public string ConceptID { get; set; }
            [XmlText]
            public string Text { get; set; }
        }
        public class WorkPhone
        {
            [XmlAttribute(AttributeName = "conceptID")]
            public string ConceptID { get; set; }
            [XmlText]
            public string Text { get; set; }
        }
        public class IsDefault
        {
            [XmlAttribute(AttributeName = "conceptID")]
            public string ConceptID { get; set; }
            [XmlText]
            public string Text { get; set; }
        }
        public class PatientNOK
        {
            [XmlElement(ElementName = "PatientNOK_Address_AdditionalLine")]
            public AdditionalLine AdditionalLine { get; set; } = new AdditionalLine();
            [XmlElement(ElementName = "PatientNOK_Address_City")]
            public City City { get; set; } = new City();
            [XmlElement(ElementName = "PatientNOK_Address_Postcode")]
            public Postcode Postcode { get; set; } = new Postcode();
            [XmlElement(ElementName = "PatientNOK_Address_StreetName")]
            public StreetName StreetName { get; set; } = new StreetName();
            [XmlElement(ElementName = "PatientNOK_Address_StreetNumber")]
            public StreetNumber StreetNumber { get; set; } = new StreetNumber();
            [XmlElement(ElementName = "PatientNOK_Address_Suburb")]
            public Suburb Suburb { get; set; } = new Suburb();
            [XmlElement(ElementName = "PatientNOK_Firstname")]
            public Firstname Firstname { get; set; } = new Firstname();
            [XmlElement(ElementName = "PatientNOK_Middlename")]
            public Middlename Middlename { get; set; } = new Middlename();
            [XmlElement(ElementName = "PatientNOK_Surname")]
            public Surname Surname { get; set; } = new Surname();
            [XmlElement(ElementName = "PatientNOK_Mobile")]
            public Mobile Mobile { get; set; } = new Mobile();
            [XmlElement(ElementName = "PatientNOK_PreferredNumber")]
            public PreferredNumber PreferredNumber { get; set; } = new PreferredNumber();
            [XmlElement(ElementName = "PatientNOK_Relationship")]
            public Relationship Relationship { get; set; } = new Relationship();
            [XmlElement(ElementName = "PatientNOK_ResidentialPhone")]
            public ResidentialPhone ResidentialPhone { get; set; } = new ResidentialPhone();
            [XmlElement(ElementName = "PatientNOK_WorkPhone")]
            public WorkPhone WorkPhone { get; set; } = new WorkPhone();
            [XmlElement(ElementName = "PatientNOK_IsDefault")]
            public IsDefault IsDefault { get; set; } = new IsDefault();
            [XmlAttribute(AttributeName = "conceptID")]
            public string ConceptID { get; set; }
            [XmlAttribute(AttributeName = "referenceID")]
            public string ReferenceId { get; set; }
        }
        [XmlRoot(ElementName = "Next_Of_Kin")]
        public class NextOfKin
        {
            [XmlElement(ElementName = "PatientNOK")]
            public List<PatientNOK> PatientNOK { get; set; } = new List<PatientNOK>();
            [XmlAttribute(AttributeName = "conceptType")]
            public string ConceptType { get; set; } = "List";
        }
        public class Comments
        {
            [XmlAttribute(AttributeName = "conceptID")]
            public string ConceptID { get; set; }
            [XmlText]
            public string Text { get; set; }
        }
        public class Date
        {
            [XmlAttribute(AttributeName = "conceptID")]
            public string ConceptID { get; set; }
            [XmlText]
            public string Text { get; set; }
        }
        public class Description
        {
            [XmlAttribute(AttributeName = "conceptID")]
            public string ConceptID { get; set; }
            [XmlText]
            public string Text { get; set; }
        }
        public class RecordedByID
        {
            [XmlAttribute(AttributeName = "conceptID")]
            public string ConceptID { get; set; }
            [XmlText]
            public string Text { get; set; }
        }
        public class Category
        {
            [XmlAttribute(AttributeName = "conceptID")]
            public string ConceptID { get; set; }
            [XmlText]
            public string Text { get; set; }
        }
        public class Reaction
        {
            [XmlAttribute(AttributeName = "conceptID")]
            public string ConceptID { get; set; }
            [XmlText]
            public string Text { get; set; }
        }
        public class Severity
        {
            [XmlAttribute(AttributeName = "conceptID")]
            public string ConceptID { get; set; }
            [XmlText]
            public string Text { get; set; }
        }
        public class MedicalWarning
        {
            [XmlElement(ElementName = "Patient_MedicalWarning_Comments")]
            public Comments Comments { get; set; } = new Comments();
            [XmlElement(ElementName = "Patient_MedicalWarning_Date")]
            public Date Date { get; set; } = new Date();
            [XmlElement(ElementName = "Patient_MedicalWarning_Description")]
            public Description Description { get; set; } = new Description();
            [XmlElement(ElementName = "Patient_MedicalWarning_RecordedByID")]
            public RecordedByID RecordedByID { get; set; } = new RecordedByID();
           
            [XmlAttribute(AttributeName = "order")]
            public string Order { get; set; }
            [XmlAttribute(AttributeName = "minDateTime")]
            public string MinDateTime { get; set; }
            [XmlAttribute(AttributeName = "maxDateTime")]
            public string MaxDateTime { get; set; }
            [XmlAttribute(AttributeName = "conceptID")]
            public string ConceptID { get; set; }
            [XmlAttribute(AttributeName = "referenceID")]
            public string ReferenceId { get; set; }
        }
        [XmlRoot(ElementName = "MedicalWarnings")]
        public class MedicalWarnings
        {
            [XmlElement(ElementName = "Patient_MedicalWarning")]
            public List<MedicalWarning> MedicalWarning { get; set; } = new List<MedicalWarning>();
            [XmlAttribute(AttributeName = "conceptType")]
            public string ConceptType { get; set; } = "List";
        }
        public class Title
        {
            [XmlAttribute(AttributeName = "conceptID")]
            public string ConceptID { get; set; }
            [XmlText]
            public string Text { get; set; }
        }
        public class FirstName
        {
            [XmlAttribute(AttributeName = "conceptID")]
            public string ConceptID { get; set; }
            [XmlText]
            public string Text { get; set; }
        }
        public class FullName
        {
            [XmlAttribute(AttributeName = "conceptID")]
            public string ConceptID { get; set; }
            [XmlText]
            public string Text { get; set; }
        }
        public class Phone
        {
            [XmlAttribute(AttributeName = "conceptID")]
            public string ConceptID { get; set; }
            [XmlText]
            public string Text { get; set; }
        }
        public class Fax
        {
            [XmlAttribute(AttributeName = "conceptID")]
            public string ConceptID { get; set; }
            [XmlText]
            public string Text { get; set; }
        }
        public class HealthLinkEDI
        {
            [XmlAttribute(AttributeName = "conceptID")]
            public string ConceptID { get; set; }
            [XmlText]
            public string Text { get; set; }
        }
        public class PMSID
        {
            [XmlAttribute(AttributeName = "conceptID")]
            public string ConceptID { get; set; }
            [XmlText]
            public string Text { get; set; }
        }
        public class Email
        {
            [XmlAttribute(AttributeName = "conceptID")]
            public string ConceptID { get; set; }
            [XmlText]
            public string Text { get; set; }
        }
        public class RegisteredPractitioner
        {
            [XmlElement(ElementName = "RegisteredPractitioner_Title")]
            public Title Title { get; set; } = new Title();
            [XmlElement(ElementName = "RegisteredPractitioner_FirstName")]
            public FirstName FirstName { get; set; } = new FirstName();
            [XmlElement(ElementName = "RegisteredPractitioner_Surname")]
            public Surname Surname { get; set; } = new Surname();
            [XmlElement(ElementName = "RegisteredPractitioner_FullName")]
            public FullName FullName { get; set; } = new FullName();
            [XmlElement(ElementName = "RegisteredPractitioner_RegistrationNumber")]
            public RegistrationNumber RegistrationNumber { get; set; } = new RegistrationNumber();
            [XmlElement(ElementName = "RegisteredPractitioner_RegisteringBody")]
            public RegisteringBody RegisteringBody { get; set; } = new RegisteringBody();
            [XmlElement(ElementName = "RegisteredPractitionerOrganisation_PhysicalAddress_StreetNumber")]
            public StreetNumber StreetNumber { get; set; } = new StreetNumber();
            [XmlElement(ElementName = "RegisteredPractitionerOrganisation_PhysicalAddress_UnitNumber")]
            public UnitNumber UnitNumber { get; set; } = new UnitNumber();
            [XmlElement(ElementName = "RegisteredPractitionerOrganisation_PhysicalAddress_StreetName")]
            public StreetName StreetName { get; set; } = new StreetName();
            [XmlElement(ElementName = "RegisteredPractitionerOrganisation_PhysicalAddress_Suburb")]
            public Suburb Suburb { get; set; } = new Suburb();
            [XmlElement(ElementName = "RegisteredPractitionerOrganisation_PhysicalAddress_City")]
            public City City { get; set; } = new City();
            [XmlElement(ElementName = "RegisteredPractitionerOrganisation_PhysicalAddress_Postcode")]
            public Postcode Postcode { get; set; } = new Postcode();
            [XmlElement(ElementName = "RegisteredPractitionerOrganisation_Phone")]
            public Phone Phone { get; set; } = new Phone();
            [XmlElement(ElementName = "RegisteredPractitionerOrganisation_Fax")]
            public Fax Fax { get; set; } = new Fax();
            [XmlElement(ElementName = "RegisteredPractitionerOrganisation_FacilityHPI")]
            public FacilityHPI FacilityHPI { get; set; } = new FacilityHPI();
            [XmlElement(ElementName = "RegisteredPractitionerOrganisation_HealthLinkEDI")]
            public HealthLinkEDI HealthLinkEDI { get; set; } = new HealthLinkEDI();
            [XmlElement(ElementName = "RegisteredPractitioner_PMSID")]
            public PMSID PMSID { get; set; } = new PMSID();
            [XmlElement(ElementName = "RegisteredPractitioner_Email")]
            public Email Email { get; set; } = new Email();
            [XmlElement(ElementName = "RegisteredPractitioner_PersonalHPI")]
            public PersonalHPI PersonalHPI { get; set; } = new PersonalHPI();
            [XmlAttribute(AttributeName = "name")]
            public string Name { get; set; }
            [XmlAttribute(AttributeName = "conceptID")]
            public string ConceptID { get; set; }
            [XmlAttribute(AttributeName = "referenceID")]
            public string ReferenceId { get; set; }
        }
        [XmlRoot(ElementName = "RegisteredPractitioners")]
        public class RegisteredPractitioners
        {
            [XmlElement(ElementName = "RegisteredPractitioner")]
            public List<RegisteredPractitioner> RegisteredPractitioner { get; set; } = new List<RegisteredPractitioner>();
            [XmlAttribute(AttributeName = "conceptType")]
            public string ConceptType { get; set; } = "List";
        }
        public class ConsumptionDescription
        {
            [XmlAttribute(AttributeName = "conceptID")]
            public string ConceptID { get; set; }
            [XmlText]
            public string Text { get; set; }
        }
        public class Code
        {
            [XmlAttribute(AttributeName = "conceptID")]
            public string ConceptID { get; set; }
            [XmlText]
            public string Text { get; set; }
        }
        public class CodingSystem
        {
            [XmlAttribute(AttributeName = "conceptID")]
            public string ConceptID { get; set; }
            [XmlText]
            public string Text { get; set; }
        }
        public class Smoking
        {
            [XmlElement(ElementName = "Patient_Smoking_ConsumptionDescription")]
            public ConsumptionDescription ConsumptionDescription { get; set; } = new ConsumptionDescription();
            [XmlElement(ElementName = "Patient_Smoking_Code")]
            public Code Code { get; set; } = new Code();
            [XmlElement(ElementName = "Patient_Smoking_CodingSystem")]
            public CodingSystem CodingSystem { get; set; } = new CodingSystem();
            [XmlElement(ElementName = "Patient_Smoking_Date")]
            public Date Date { get; set; } = new Date();
            [XmlAttribute(AttributeName = "numRows")]
            public string NumRows { get; set; }
            [XmlAttribute(AttributeName = "conceptID")]
            public string ConceptID { get; set; }
            [XmlAttribute(AttributeName = "referenceID")]
            public string ReferenceId { get; set; }
        }
        [XmlRoot(ElementName = "SmokingStatus")]
        public class SmokingStatus
        {
            [XmlElement(ElementName = "Patient_Smoking")]
            public List<Smoking> Smoking { get; set; } = new List<Smoking>();
            [XmlAttribute(AttributeName = "conceptType")]
            public string ConceptType { get; set; } = "List";
        }
        [XmlRoot(ElementName = "Patient_Accident_DiagnosisDescription")]
        public class DiagnosisDescription
        {
            [XmlAttribute(AttributeName = "conceptID")]
            public string ConceptID { get; set; }
            [XmlText]
            public string Text { get; set; }
        }
        [XmlRoot(ElementName = "Patient_Accident_IsWorkRelated")]
        public class IsWorkRelated
        {
            [XmlAttribute(AttributeName = "conceptID")]
            public string ConceptID { get; set; }
            [XmlText]
            public string Text { get; set; }
        }
        [XmlRoot(ElementName = "Patient_Accident_Location_Description")]
        public class LocationDescription
        {
            [XmlAttribute(AttributeName = "conceptID")]
            public string ConceptID { get; set; }
            [XmlText]
            public string Text { get; set; }
        }
        public class Accident
        {
            [XmlElement(ElementName = "Patient_Accident_RegistrationNumber")]
            public RegistrationNumber RegistrationNumber { get; set; } = new RegistrationNumber();
            [XmlElement(ElementName = "Patient_Accident_Date")]
            public Date Date { get; set; } = new Date();
            [XmlElement(ElementName = "Patient_Accident_DiagnosisDescription")]
            public DiagnosisDescription DiagnosisDescription { get; set; } = new DiagnosisDescription();
            [XmlElement(ElementName = "Patient_Accident_IsWorkRelated")]
            public IsWorkRelated IsWorkRelated { get; set; } = new IsWorkRelated();
            [XmlElement(ElementName = "Patient_Accident_Location_Description")]
            public LocationDescription LocationDescription { get; set; } = new LocationDescription();
            [XmlAttribute(AttributeName = "order")]
            public string Order { get; set; }
            [XmlAttribute(AttributeName = "minDateTime")]
            public string MinDateTime { get; set; }
            [XmlAttribute(AttributeName = "maxDateTime")]
            public string MaxDateTime { get; set; }
            [XmlAttribute(AttributeName = "conceptID")]
            public string ConceptID { get; set; }
            [XmlAttribute(AttributeName = "referenceID")]
            public string ReferenceId { get; set; }
        }
        [XmlRoot(ElementName = "Accidents")]
        public class Accidents
        {
            [XmlElement(ElementName = "Patient_Accident")]
            public List<Accident> Accident { get; set; } = new List<Accident>();
            [XmlAttribute(AttributeName = "conceptType")]
            public string ConceptType { get; set; } = "List";
        }
        public class BPSYS
        {
            [XmlAttribute(AttributeName = "qualifierName")]
            public string QualifierName { get; set; }
            [XmlAttribute(AttributeName = "qualifierID")]
            public string QualifierID { get; set; }
            [XmlAttribute(AttributeName = "conceptName")]
            public string ConceptName { get; set; }
            [XmlAttribute(AttributeName = "conceptID")]
            public string ConceptID { get; set; }
            [XmlAttribute(AttributeName = "name")]
            public string Name { get; set; }
            [XmlAttribute(AttributeName = "dateTaken")]
            public string DateTaken { get; set; }
            [XmlText]
            public string Text { get; set; }
        }
        public class BPDIA
        {
            [XmlAttribute(AttributeName = "qualifierName")]
            public string QualifierName { get; set; }
            [XmlAttribute(AttributeName = "qualifierID")]
            public string QualifierID { get; set; }
            [XmlAttribute(AttributeName = "conceptName")]
            public string ConceptName { get; set; }
            [XmlAttribute(AttributeName = "conceptID")]
            public string ConceptID { get; set; }
            [XmlAttribute(AttributeName = "name")]
            public string Name { get; set; }
            [XmlAttribute(AttributeName = "dateTaken")]
            public string DateTaken { get; set; }
            [XmlText]
            public string Text { get; set; }
        }
        public class Weight
        {
            [XmlAttribute(AttributeName = "qualifierName")]
            public string QualifierName { get; set; }
            [XmlAttribute(AttributeName = "qualifierID")]
            public string QualifierID { get; set; }
            [XmlAttribute(AttributeName = "conceptName")]
            public string ConceptName { get; set; }
            [XmlAttribute(AttributeName = "conceptID")]
            public string ConceptID { get; set; }
            [XmlAttribute(AttributeName = "name")]
            public string Name { get; set; }
            [XmlAttribute(AttributeName = "dateTaken")]
            public string DateTaken { get; set; }
            [XmlText]
            public string Text { get; set; }
        }
        public class Height
        {
            [XmlAttribute(AttributeName = "qualifierName")]
            public string QualifierName { get; set; }
            [XmlAttribute(AttributeName = "qualifierID")]
            public string QualifierID { get; set; }
            [XmlAttribute(AttributeName = "conceptName")]
            public string ConceptName { get; set; }
            [XmlAttribute(AttributeName = "conceptID")]
            public string ConceptID { get; set; }
            [XmlAttribute(AttributeName = "name")]
            public string Name { get; set; }
            [XmlAttribute(AttributeName = "dateTaken")]
            public string DateTaken { get; set; }
            [XmlText]
            public string Text { get; set; }
        }
        public class BMI
        {
            [XmlAttribute(AttributeName = "qualifierName")]
            public string QualifierName { get; set; }
            [XmlAttribute(AttributeName = "qualifierID")]
            public string QualifierID { get; set; }
            [XmlAttribute(AttributeName = "conceptName")]
            public string ConceptName { get; set; }
            [XmlAttribute(AttributeName = "conceptID")]
            public string ConceptID { get; set; }
            [XmlAttribute(AttributeName = "name")]
            public string Name { get; set; }
            [XmlAttribute(AttributeName = "dateTaken")]
            public string DateTaken { get; set; }
            [XmlText]
            public string Text { get; set; }
        }
        [XmlRoot(ElementName = "Patient_Measurement")]
        public class Measurement
        {
            [XmlElement(ElementName = "Measurement_BP_SYS")]
            public BPSYS BPSYS { get; set; } = new BPSYS();
            [XmlElement(ElementName = "Measurement_BP_DIA")]
            public BPDIA BPDIA { get; set; } = new BPDIA();
            [XmlElement(ElementName = "Measurement_Weight")]
            public Weight Weight { get; set; } = new Weight();
            [XmlElement(ElementName = "Measurement_Height")]
            public Height Height { get; set; } = new Height();
            [XmlElement(ElementName = "Measurement_BMI")]
            public BMI BMI { get; set; } = new BMI();
            [XmlAttribute(AttributeName = "name")]
            public string Name { get; set; } = "measurements";
            [XmlText]
            public string Text { get; set; }
        }
        public class SendingFacility
        {
            [XmlAttribute(AttributeName = "conceptID")]
            public string ConceptID { get; set; }
            [XmlText]
            public string Text { get; set; }
        }
        public class Subject
        {
            [XmlAttribute(AttributeName = "conceptID")]
            public string ConceptID { get; set; }
            [XmlText]
            public string Text { get; set; }
        }
        public class Name
        {
            [XmlAttribute(AttributeName = "conceptID")]
            public string ConceptID { get; set; }
            [XmlText]
            public string Text { get; set; }
        }
        public class DateReceived
        {
            [XmlAttribute(AttributeName = "conceptID")]
            public string ConceptID { get; set; }
            [XmlText]
            public string Text { get; set; }
        }
        public class DataType
        {
            [XmlAttribute(AttributeName = "conceptID")]
            public string ConceptID { get; set; }
            [XmlText]
            public string Text { get; set; }
        }
        public class LaboratoryReport
        {
            [XmlElement(ElementName = "Patient_LaboratoryReport_SendingFacility")]
            public SendingFacility SendingFacility { get; set; } = new SendingFacility();
            [XmlElement(ElementName = "Patient_LaboratoryReport_Subject")]
            public Subject Subject { get; set; } = new Subject();
            [XmlElement(ElementName = "Patient_LaboratoryReport_Name")]
            public Name Name { get; set; } = new Name();
            [XmlElement(ElementName = "Patient_LaboratoryReport_Date_Received")]
            public DateReceived DateReceived { get; set; } = new DateReceived();
            [XmlElement(ElementName = "Patient_LaboratoryReport_DataType")]
            public DataType DataType { get; set; } = new DataType();
            [XmlElement(ElementName = "Patient_LaboratoryReport_Comments")]
            public Comments Comments { get; set; } = new Comments();
            public string Order { get; set; }
            [XmlAttribute(AttributeName = "maxDateTime")]
            public string MaxDateTime { get; set; }
            [XmlAttribute(AttributeName = "minDateTime")]
            public string MinDateTime { get; set; }
            [XmlAttribute(AttributeName = "conceptID")]
            public string ConceptID { get; set; }
            [XmlAttribute(AttributeName = "referenceID")]
            public string ReferenceId { get; set; }
        }
        [XmlRoot(ElementName = "LaboratoryReports")]
        public class LaboratoryReports
        {
            [XmlElement(ElementName = "Patient_LaboratoryReport")]
            public List<LaboratoryReport> LaboratoryReport { get; set; } = new List<LaboratoryReport>();
            [XmlAttribute(AttributeName = "conceptType")]
            public string ConceptType { get; set; } = "List";
        }
        public class Content
        {
            [XmlAttribute(AttributeName = "conceptID")]
            public string ConceptID { get; set; }
            [XmlText]
            public string Text { get; set; }
        }
        public class LaboratoryReportContent
        {
            [XmlElement(ElementName = "Patient_LaboratoryReport_Content")]
            public Content Content { get; set; } = new Content();
            [XmlAttribute(AttributeName = "conceptID")]
            public string ConceptID { get; set; }
            [XmlAttribute(AttributeName = "referenceID")]
            public string ReferenceId { get; set; }
        }
        [XmlRoot(ElementName = "LaboratoryReportsContent")]
        public class LaboratoryReportsContent
        {
            [XmlElement(ElementName = "Patient_LaboratoryReport")]
            public List<LaboratoryReportContent> LaboratoryReportContent { get; set; } = new List<LaboratoryReportContent>();
            [XmlAttribute(AttributeName = "name")]
            public string Name { get; set; } = "clinical.diagnosticReports";
        }
        public class Group
        {
            [XmlElement(ElementName = "DischargeReport_SendingFacility")]
            public SendingFacility SendingFacility { get; set; } = new SendingFacility();
            [XmlElement(ElementName = "DischargeReport_Subject")]
            public Subject Subject { get; set; } = new Subject();
            [XmlElement(ElementName = "DischargeReport_Name")]
            public Name Name { get; set; } = new Name();
            [XmlElement(ElementName = "DischargeReport_DateReceived")]
            public DateReceived DateReceived { get; set; } = new DateReceived();
            [XmlElement(ElementName = "DischargeReport_DataType")]
            public DataType DataType { get; set; } = new DataType();
            [XmlElement(ElementName = "DischargeReport_Comments")]
            public Comments Comments { get; set; } = new Comments();
            [XmlAttribute(AttributeName = "order")]
            public string Order { get; set; }
            [XmlAttribute(AttributeName = "maxDateTime")]
            public string MaxDateTime { get; set; }
            [XmlAttribute(AttributeName = "minDateTime")]
            public string MinDateTime { get; set; }
            [XmlAttribute(AttributeName = "conceptName")]
            public string ConceptName { get; set; }
            [XmlAttribute(AttributeName = "conceptID")]
            public string ConceptID { get; set; }
            [XmlAttribute(AttributeName = "referenceID")]
            public string ReferenceId { get; set; }
        }
        [XmlRoot(ElementName = "DischargeReports")]
        public class DischargeReports
        {
            [XmlElement(ElementName = "group")]
            public List<Group> Group { get; set; } = new List<Group>();
            [XmlAttribute(AttributeName = "name")]
            public string Name { get; set; } = "clinical.DischargeReports";
            [XmlAttribute(AttributeName = "conceptType")]
            public string ConceptType { get; set; } = "List";
        }
        public class RadiologyGroup
        {
            [XmlElement(ElementName = "RadiologyReport_SendingFacility")]
            public SendingFacility SendingFacility { get; set; } = new SendingFacility();
            [XmlElement(ElementName = "RadiologyReport_Subject")]
            public Subject Subject { get; set; } = new Subject();
            [XmlElement(ElementName = "RadiologyReport_Name")]
            public Name Name { get; set; } = new Name();
            [XmlElement(ElementName = "RadiologyReport_DateReceived")]
            public DateReceived DateReceived { get; set; } = new DateReceived();
            [XmlElement(ElementName = "RadiologyReport_DataType")]
            public DataType DataType { get; set; } = new DataType();
            [XmlElement(ElementName = "RadiologyReport_Comments")]
            public Comments Comments { get; set; } = new Comments();
            [XmlAttribute(AttributeName = "order")]
            public string Order { get; set; }
            [XmlAttribute(AttributeName = "maxDateTime")]
            public string MaxDateTime { get; set; }
            [XmlAttribute(AttributeName = "minDateTime")]
            public string MinDateTime { get; set; }
            [XmlAttribute(AttributeName = "conceptName")]
            public string ConceptName { get; set; }
            [XmlAttribute(AttributeName = "conceptID")]
            public string ConceptID { get; set; }
            [XmlAttribute(AttributeName = "referenceID")]
            public string ReferenceId { get; set; }
        }
        [XmlRoot(ElementName = "RadiologyReports")]
        public class RadiologyReports
        {
            [XmlElement(ElementName = "group")]
            public List<RadiologyGroup> RadiologyGroup { get; set; } = new List<RadiologyGroup>();
            [XmlAttribute(AttributeName = "name")]
            public string Name { get; set; } = "clinical.RadiologyReport";
            [XmlAttribute(AttributeName = "conceptType")]
            public string ConceptType { get; set; } = "List";
        }
        public class RadiologyReportContent
        {
            [XmlElement(ElementName = "Patient_RadiologyReport_Content")]
            public Content Content { get; set; } = new Content();
            [XmlAttribute(AttributeName = "conceptID")]
            public string ConceptID { get; set; }
            [XmlAttribute(AttributeName = "referenceID")]
            public string ReferenceId { get; set; }
        }
        [XmlRoot(ElementName = "RadiologyReportsContent")]
        public class RadiologyReportContents
        {
            [XmlElement(ElementName = "Patient_RadiologyReport")]
            public List<RadiologyReportContent> RadiologyReportContent { get; set; } = new List<RadiologyReportContent>();
            [XmlAttribute(AttributeName = "name")]
            public string Name { get; set; } = "clinical.RadiologyReport";
        }
        public class DischargeSummaryContent
        {
            [XmlElement(ElementName = "Patient_DischargeSummary_Content")]
            public Content Content { get; set; } = new Content();
            [XmlAttribute(AttributeName = "conceptID")]
            public string ConceptID { get; set; }
            [XmlAttribute(AttributeName = "referenceID")]
            public string ReferenceId { get; set; }
        }
        [XmlRoot(ElementName = "DischargeSummaryContents")]
        public class DischargeSummaryContents
        {
            [XmlElement(ElementName = "Patient_DischargeSummary")]
            public List<DischargeSummaryContent> DischargeSummaryContent { get; set; } = new List<DischargeSummaryContent>();
            [XmlAttribute(AttributeName = "name")]
            public string Name { get; set; } = "clinical.DischargeReport";
        }
        public class ScannedGroup
        {
            [XmlElement(ElementName = "ScandocumentReport_SendingFacility")]
            public SendingFacility SendingFacility { get; set; } = new SendingFacility();
            [XmlElement(ElementName = "ScandocumentReport_Subject")]
            public Subject Subject { get; set; } = new Subject();
            [XmlElement(ElementName = "ScandocumentReport_Name")]
            public Name Name { get; set; } = new Name();
            [XmlElement(ElementName = "ScandocumentReport_DateReceived")]
            public DateReceived DateReceived { get; set; } = new DateReceived();
            [XmlElement(ElementName = "ScandocumentReport_DataType")]
            public DataType DataType { get; set; } = new DataType();
            [XmlElement(ElementName = "ScandocumentReport_Comments")]
            public Comments Comments { get; set; } = new Comments();
            [XmlElement(ElementName = "ScanContent")]
            public Content Content { get; set; } = new Content();
            [XmlAttribute(AttributeName = "order")]
            public string Order { get; set; }
            [XmlAttribute(AttributeName = "maxDateTime")]
            public string MaxDateTime { get; set; }
            [XmlAttribute(AttributeName = "minDateTime")]
            public string MinDateTime { get; set; }
            [XmlAttribute(AttributeName = "conceptName")]
            public string ConceptName { get; set; }
            [XmlAttribute(AttributeName = "conceptID")]
            public string ConceptID { get; set; }
            [XmlAttribute(AttributeName = "referenceID")]
            public string ReferenceId { get; set; }
        }
        [XmlRoot(ElementName = "ScanDocumentReports")]
        public class ScanDocumentReports
        {
            [XmlElement(ElementName = "group")]
            public List<ScannedGroup> ScannedGroup { get; set; } = new List<ScannedGroup>();
            [XmlAttribute(AttributeName = "name")]
            public string Name { get; set; } = "clinical.ScanDocumentReports";
            [XmlAttribute(AttributeName = "conceptType")]
            public string ConceptType { get; set; } = "List";
        }
        [XmlRoot(ElementName = "ScanReportContent")]
        public class ScanReportContent
        {
            [XmlElement(ElementName = "group")]
            public List<ScannedGroup> ScannedGroup { get; set; } = new List<ScannedGroup>();
            [XmlAttribute(AttributeName = "name")]
            public string Name { get; set; } = "clinical.ScanContent";
        }
        public class ReferralDocument
        {
            [XmlElement(ElementName = "ReferralDocument_Referral_ID")]
            public string ID { get; set; }
            [XmlElement(ElementName = "ReferralDocument_Document_ID")]
            public string DocumentID { get; set; }
            [XmlElement(ElementName = "ReferralDocument_Patient_PMS_ID")]
            public string PatiendID { get; set; }
            [XmlElement(ElementName = "ReferralDocument_Encounter_ID")]
            public string EncounterID { get; set; }
            [XmlElement(ElementName = "ReferralDocument_Referral_Type")]
            public string Type { get; set; }
            [XmlElement(ElementName = "ReferralDocument_Item_Type")]
            public string ItemType { get; set; }
            [XmlElement(ElementName = "ReferralDocument_Referral_Status")]
            public string Status { get; set; }
            [XmlElement(ElementName = "ReferralDocument_Created_Date")]
            public string CreatedDate { get; set; }
            [XmlElement(ElementName = "ReferralDocument_Referrer_Fullname")]
            public string Fullname { get; set; }
            [XmlElement(ElementName = "ReferralDocument_Referrer_PMS_ID")]
            public string ProviderID { get; set; }
            [XmlElement(ElementName = "ReferralDocument_Document_Source")]
            public string Source { get; set; }
            [XmlElement(ElementName = "ReferralDocument_Content_Type")]
            public string ContentType { get; set; }
            [XmlElement(ElementName = "ReferralDocument_Description_Type")]
            public string DescriptionType { get; set; }
            [XmlElement(ElementName = "ReferralDocument_Description")]
            public string Description { get; set; }
            [XmlElement(ElementName = "ReferralDocument_Encoding")]
            public string Encoding { get; set; }
            [XmlElement(ElementName = "ReferralDocument_Content")]
            public string Content { get; set; }
            [XmlElement(ElementName = "ReferralDocument_Error_Text")]
            public string ErrorText { get; set; }
        }
    }
}