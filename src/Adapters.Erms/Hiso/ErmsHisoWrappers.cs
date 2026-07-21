using System.Xml.Serialization;

namespace HekCoreApi.Adapters.Erms.Hiso;

// Exact ports of ERMS's HISO wrapper element classes (`ERMSWebAPI/Models/APIModels.cs`) - each is a
// `conceptID` attribute plus text content. Class names must match legacy exactly: `ErmsDataTableMapper`
// (the `ERMSDataTableToListHiso<T>` port) sets `ConceptID`/`Text` on them by reflection, keyed off the
// parent model's property names, so structure and naming are load-bearing, not cosmetic.

public class PatientSurname
{
    [XmlAttribute(AttributeName = "conceptID")]
    public string? ConceptID { get; set; }
    [XmlText]
    public string? Text { get; set; }
}
public class PatientFirstName
{
    [XmlAttribute(AttributeName = "conceptID")]
    public string? ConceptID { get; set; }
    [XmlText]
    public string? Text { get; set; }
}
public class PatientMiddleName
{
    [XmlAttribute(AttributeName = "conceptID")]
    public string? ConceptID { get; set; }
    [XmlText]
    public string? Text { get; set; }
}
public class PatientNHI
{
    [XmlAttribute(AttributeName = "conceptID")]
    public string? ConceptID { get; set; }
    [XmlText]
    public string? Text { get; set; }
}
public class PatientDateOfBirth
{
    [XmlAttribute(AttributeName = "conceptID")]
    public string? ConceptID { get; set; }
    [XmlText]
    public string? Text { get; set; }
}
public class PatientGenderCode
{
    [XmlAttribute(AttributeName = "conceptID")]
    public string? ConceptID { get; set; }
    [XmlText]
    public string? Text { get; set; }
}
public class RAStreetNumber
{
    [XmlAttribute(AttributeName = "conceptID")]
    public string? ConceptID { get; set; }
    [XmlText]
    public string? Text { get; set; }
}
public class RAStreetName
{
    [XmlAttribute(AttributeName = "conceptID")]
    public string? ConceptID { get; set; }
    [XmlText]
    public string? Text { get; set; }
}
public class RASuburb
{
    [XmlAttribute(AttributeName = "conceptID")]
    public string? ConceptID { get; set; }
    [XmlText]
    public string? Text { get; set; }
}
public class RACity
{
    [XmlAttribute(AttributeName = "conceptID")]
    public string? ConceptID { get; set; }
    [XmlText]
    public string? Text { get; set; }
}
public class RAPostcode
{
    [XmlAttribute(AttributeName = "conceptID")]
    public string? ConceptID { get; set; }
    [XmlText]
    public string? Text { get; set; }
}
public class RAAdditionalLine
{
    [XmlAttribute(AttributeName = "conceptID")]
    public string? ConceptID { get; set; }
    [XmlText]
    public string? Text { get; set; }
}
public class PAStreetNumber
{
    [XmlAttribute(AttributeName = "conceptID")]
    public string? ConceptID { get; set; }
    [XmlText]
    public string? Text { get; set; }
}
public class PAStreetName
{
    [XmlAttribute(AttributeName = "conceptID")]
    public string? ConceptID { get; set; }
    [XmlText]
    public string? Text { get; set; }
}
public class PASuburb
{
    [XmlAttribute(AttributeName = "conceptID")]
    public string? ConceptID { get; set; }
    [XmlText]
    public string? Text { get; set; }
}
public class PACity
{
    [XmlAttribute(AttributeName = "conceptID")]
    public string? ConceptID { get; set; }
    [XmlText]
    public string? Text { get; set; }
}
public class PAPostcode
{
    [XmlAttribute(AttributeName = "conceptID")]
    public string? ConceptID { get; set; }
    [XmlText]
    public string? Text { get; set; }
}
public class PAAdditionalLine
{
    [XmlAttribute(AttributeName = "conceptID")]
    public string? ConceptID { get; set; }
    [XmlText]
    public string? Text { get; set; }
}
public class Ethnicity1CodeLevel2
{
    [XmlAttribute(AttributeName = "conceptID")]
    public string? ConceptID { get; set; }
    [XmlText]
    public string? Text { get; set; }
}
public class Ethnicity2CodeLevel2
{
    [XmlAttribute(AttributeName = "conceptID")]
    public string? ConceptID { get; set; }
    [XmlText]
    public string? Text { get; set; }
}
public class Ethnicity3CodeLevel2
{
    [XmlAttribute(AttributeName = "conceptID")]
    public string? ConceptID { get; set; }
    [XmlText]
    public string? Text { get; set; }
}
public class PatientEmailAddress
{
    [XmlAttribute(AttributeName = "conceptID")]
    public string? ConceptID { get; set; }
    [XmlText]
    public string? Text { get; set; }
}
public class PatientMobile
{
    [XmlAttribute(AttributeName = "conceptID")]
    public string? ConceptID { get; set; }
    [XmlText]
    public string? Text { get; set; }
}
public class PatientResidentialPhone
{
    [XmlAttribute(AttributeName = "conceptID")]
    public string? ConceptID { get; set; }
    [XmlText]
    public string? Text { get; set; }
}
public class WorkPhone
{
    [XmlAttribute(AttributeName = "conceptID")]
    public string? ConceptID { get; set; }
    [XmlText]
    public string? Text { get; set; }
}
public class IsEligiblePublicFunds
{
    [XmlAttribute(AttributeName = "conceptID")]
    public string? ConceptID { get; set; }
    [XmlText]
    public string? Text { get; set; }
}
public class PatientHUC
{
    [XmlAttribute(AttributeName = "conceptID")]
    public string? ConceptID { get; set; }
    [XmlText]
    public string? Text { get; set; }
}
public class PatientHUCStartDate
{
    [XmlAttribute(AttributeName = "conceptID")]
    public string? ConceptID { get; set; }
    [XmlText]
    public string? Text { get; set; }
}
public class PatientHUCEndDate
{
    [XmlAttribute(AttributeName = "conceptID")]
    public string? ConceptID { get; set; }
    [XmlText]
    public string? Text { get; set; }
}
public class PatientCSC
{
    [XmlAttribute(AttributeName = "conceptID")]
    public string? ConceptID { get; set; }
    [XmlText]
    public string? Text { get; set; }
}
public class PatientCSCStartDate
{
    [XmlAttribute(AttributeName = "conceptID")]
    public string? ConceptID { get; set; }
    [XmlText]
    public string? Text { get; set; }
}
public class PatientCSCEndDate
{
    [XmlAttribute(AttributeName = "conceptID")]
    public string? ConceptID { get; set; }
    [XmlText]
    public string? Text { get; set; }
}
public class InternalPMSID
{
    [XmlAttribute(AttributeName = "conceptID")]
    public string? ConceptID { get; set; }
    [XmlText]
    public string? Text { get; set; }
}

// --- CurrentUser wrappers (`APIModels.cs:11-80`) ---
public class CurrentUserFirstName
{
    [XmlAttribute(AttributeName = "conceptID")]
    public string? ConceptID { get; set; }
    [XmlText]
    public string? Text { get; set; }
}
public class CurrentUserSurname
{
    [XmlAttribute(AttributeName = "conceptID")]
    public string? ConceptID { get; set; }
    [XmlText]
    public string? Text { get; set; }
}
public class CurrentUserMiddlename
{
    [XmlAttribute(AttributeName = "conceptID")]
    public string? ConceptID { get; set; }
    [XmlText]
    public string? Text { get; set; }
}
public class CurrentUserFullName
{
    [XmlAttribute(AttributeName = "conceptID")]
    public string? ConceptID { get; set; }
    [XmlText]
    public string? Text { get; set; }
}
public class RegisteringBody
{
    [XmlAttribute(AttributeName = "conceptID")]
    public string? ConceptID { get; set; }
    [XmlText]
    public string? Text { get; set; }
}
public class RegistrationNumber
{
    [XmlAttribute(AttributeName = "conceptID")]
    public string? ConceptID { get; set; }
    [XmlText]
    public string? Text { get; set; }
}
public class PersonalHPI
{
    [XmlAttribute(AttributeName = "conceptID")]
    public string? ConceptID { get; set; }
    [XmlText]
    public string? Text { get; set; }
}
public class ApplicationUserID
{
    [XmlAttribute(AttributeName = "conceptID")]
    public string? ConceptID { get; set; }
    [XmlText]
    public string? Text { get; set; }
}
public class FacilityHPI
{
    [XmlAttribute(AttributeName = "conceptID")]
    public string? ConceptID { get; set; }
    [XmlText]
    public string? Text { get; set; }
}
public class HealthlinkEDI
{
    [XmlAttribute(AttributeName = "conceptID")]
    public string? ConceptID { get; set; }
    [XmlText]
    public string? Text { get; set; }
}
public class HealthLinkEDI
{
    [XmlAttribute(AttributeName = "conceptID")]
    public string? ConceptID { get; set; }
    [XmlText]
    public string? Text { get; set; }
}
public class PMSID
{
    [XmlAttribute(AttributeName = "conceptID")]
    public string? ConceptID { get; set; }
    [XmlText]
    public string? Text { get; set; }
}

// --- Problem wrappers (`APIModels.cs:408-456`) ---
public class ProblemComments
{
    [XmlAttribute(AttributeName = "conceptID")]
    public string? ConceptID { get; set; }
    [XmlText]
    public string? Text { get; set; }
}
public class ProblemDescription
{
    [XmlAttribute(AttributeName = "conceptID")]
    public string? ConceptID { get; set; }
    [XmlText]
    public string? Text { get; set; }
}
public class ProblemDateOfOnset
{
    [XmlAttribute(AttributeName = "conceptID")]
    public string? ConceptID { get; set; }
    [XmlText]
    public string? Text { get; set; }
}
public class ProblemCode
{
    [XmlAttribute(AttributeName = "conceptID")]
    public string? ConceptID { get; set; }
    [XmlText]
    public string? Text { get; set; }
}
public class ProblemCodingSystem
{
    [XmlAttribute(AttributeName = "conceptID")]
    public string? ConceptID { get; set; }
    [XmlText]
    public string? Text { get; set; }
}
public class ProblemDateRecorded
{
    [XmlAttribute(AttributeName = "conceptID")]
    public string? ConceptID { get; set; }
    [XmlText]
    public string? Text { get; set; }
}
public class ProblemIsLongTerm
{
    [XmlAttribute(AttributeName = "conceptID")]
    public string? ConceptID { get; set; }
    [XmlText]
    public string? Text { get; set; }
}

// --- Consult wrappers (`APIModels.cs:647-689`) - `IsBase64` is a real legacy attribute the mapper never populates ---
public class ConsultDate
{
    [XmlAttribute(AttributeName = "conceptID")]
    public string? ConceptID { get; set; }
    [XmlText]
    public string? Text { get; set; }
}
public class ConsultExam
{
    [XmlAttribute(AttributeName = "conceptID")]
    public string? ConceptID { get; set; }
    [XmlAttribute(AttributeName = "IsBase64")]
    public string? IsBase64 { get; set; }
    [XmlText]
    public string? Text { get; set; }
}
public class ConsultHistory
{
    [XmlAttribute(AttributeName = "conceptID")]
    public string? ConceptID { get; set; }
    [XmlAttribute(AttributeName = "IsBase64")]
    public string? IsBase64 { get; set; }
    [XmlText]
    public string? Text { get; set; }
}
public class ConsultAssess
{
    [XmlAttribute(AttributeName = "conceptID")]
    public string? ConceptID { get; set; }
    [XmlAttribute(AttributeName = "IsBase64")]
    public string? IsBase64 { get; set; }
    [XmlText]
    public string? Text { get; set; }
}
public class ConsultPlan
{
    [XmlAttribute(AttributeName = "conceptID")]
    public string? ConceptID { get; set; }
    [XmlAttribute(AttributeName = "IsBase64")]
    public string? IsBase64 { get; set; }
    [XmlText]
    public string? Text { get; set; }
}

// --- NOK / shared address+name wrappers (`APIModels.cs:723-834`) ---
public class AdditionalLine
{
    [XmlAttribute(AttributeName = "conceptID")]
    public string? ConceptID { get; set; }
    [XmlText]
    public string? Text { get; set; }
}
public class City
{
    [XmlAttribute(AttributeName = "conceptID")]
    public string? ConceptID { get; set; }
    [XmlText]
    public string? Text { get; set; }
}
public class Postcode
{
    [XmlAttribute(AttributeName = "conceptID")]
    public string? ConceptID { get; set; }
    [XmlText]
    public string? Text { get; set; }
}
public class StreetName
{
    [XmlAttribute(AttributeName = "conceptID")]
    public string? ConceptID { get; set; }
    [XmlText]
    public string? Text { get; set; }
}
public class StreetNumber
{
    [XmlAttribute(AttributeName = "conceptID")]
    public string? ConceptID { get; set; }
    [XmlText]
    public string? Text { get; set; }
}
public class UnitNumber
{
    [XmlAttribute(AttributeName = "conceptID")]
    public string? ConceptID { get; set; }
    [XmlText]
    public string? Text { get; set; }
}
public class Suburb
{
    [XmlAttribute(AttributeName = "conceptID")]
    public string? ConceptID { get; set; }
    [XmlText]
    public string? Text { get; set; }
}
public class Firstname
{
    [XmlAttribute(AttributeName = "conceptID")]
    public string? ConceptID { get; set; }
    [XmlText]
    public string? Text { get; set; }
}
public class Middlename
{
    [XmlAttribute(AttributeName = "conceptID")]
    public string? ConceptID { get; set; }
    [XmlText]
    public string? Text { get; set; }
}
public class Surname
{
    [XmlAttribute(AttributeName = "conceptID")]
    public string? ConceptID { get; set; }
    [XmlText]
    public string? Text { get; set; }
}
public class Mobile
{
    [XmlAttribute(AttributeName = "conceptID")]
    public string? ConceptID { get; set; }
    [XmlText]
    public string? Text { get; set; }
}
public class PreferredNumber
{
    [XmlAttribute(AttributeName = "conceptID")]
    public string? ConceptID { get; set; }
    [XmlText]
    public string? Text { get; set; }
}
public class Relationship
{
    [XmlAttribute(AttributeName = "conceptID")]
    public string? ConceptID { get; set; }
    [XmlText]
    public string? Text { get; set; }
}
public class ResidentialPhone
{
    [XmlAttribute(AttributeName = "conceptID")]
    public string? ConceptID { get; set; }
    [XmlText]
    public string? Text { get; set; }
}
public class IsDefault
{
    [XmlAttribute(AttributeName = "conceptID")]
    public string? ConceptID { get; set; }
    [XmlText]
    public string? Text { get; set; }
}

// --- MedicalWarning / shared generic wrappers (`APIModels.cs:880-928`) ---
public class Comments
{
    [XmlAttribute(AttributeName = "conceptID")]
    public string? ConceptID { get; set; }
    [XmlText]
    public string? Text { get; set; }
}
public class Date
{
    [XmlAttribute(AttributeName = "conceptID")]
    public string? ConceptID { get; set; }
    [XmlText]
    public string? Text { get; set; }
}
public class Description
{
    [XmlAttribute(AttributeName = "conceptID")]
    public string? ConceptID { get; set; }
    [XmlText]
    public string? Text { get; set; }
}
public class RecordedByID
{
    [XmlAttribute(AttributeName = "conceptID")]
    public string? ConceptID { get; set; }
    [XmlText]
    public string? Text { get; set; }
}

// --- RegisteredPractitioner wrappers (`APIModels.cs:959-1014`) ---
public class Title
{
    [XmlAttribute(AttributeName = "conceptID")]
    public string? ConceptID { get; set; }
    [XmlText]
    public string? Text { get; set; }
}
public class FirstName
{
    [XmlAttribute(AttributeName = "conceptID")]
    public string? ConceptID { get; set; }
    [XmlText]
    public string? Text { get; set; }
}
public class FullName
{
    [XmlAttribute(AttributeName = "conceptID")]
    public string? ConceptID { get; set; }
    [XmlText]
    public string? Text { get; set; }
}
public class Phone
{
    [XmlAttribute(AttributeName = "conceptID")]
    public string? ConceptID { get; set; }
    [XmlText]
    public string? Text { get; set; }
}
public class Fax
{
    [XmlAttribute(AttributeName = "conceptID")]
    public string? ConceptID { get; set; }
    [XmlText]
    public string? Text { get; set; }
}
public class Email
{
    [XmlAttribute(AttributeName = "conceptID")]
    public string? ConceptID { get; set; }
    [XmlText]
    public string? Text { get; set; }
}

// --- Smoking wrappers (`APIModels.cs:1070-1090`) ---
public class ConsumptionDescription
{
    [XmlAttribute(AttributeName = "conceptID")]
    public string? ConceptID { get; set; }
    [XmlText]
    public string? Text { get; set; }
}
public class Code
{
    [XmlAttribute(AttributeName = "conceptID")]
    public string? ConceptID { get; set; }
    [XmlText]
    public string? Text { get; set; }
}
public class CodingSystem
{
    [XmlAttribute(AttributeName = "conceptID")]
    public string? ConceptID { get; set; }
    [XmlText]
    public string? Text { get; set; }
}

// --- Accident wrappers (`APIModels.cs:1116-1139`) ---
[XmlRoot(ElementName = "Patient_Accident_DiagnosisDescription")]
public class DiagnosisDescription
{
    [XmlAttribute(AttributeName = "conceptID")]
    public string? ConceptID { get; set; }
    [XmlText]
    public string? Text { get; set; }
}
[XmlRoot(ElementName = "Patient_Accident_IsWorkRelated")]
public class IsWorkRelated
{
    [XmlAttribute(AttributeName = "conceptID")]
    public string? ConceptID { get; set; }
    [XmlText]
    public string? Text { get; set; }
}
[XmlRoot(ElementName = "Patient_Accident_Location_Description")]
public class LocationDescription
{
    [XmlAttribute(AttributeName = "conceptID")]
    public string? ConceptID { get; set; }
    [XmlText]
    public string? Text { get; set; }
}

// --- Measurement wrappers (`APIModels.cs:1171-1255`) - full qualifier attribute set;
// the mapper populates Name/QualifierID/QualifierName/DateTaken from the "|?|" inner split,
// ConceptName is a real legacy attribute that is never populated ---
public class BPSYS
{
    [XmlAttribute(AttributeName = "qualifierName")]
    public string? QualifierName { get; set; }
    [XmlAttribute(AttributeName = "qualifierID")]
    public string? QualifierID { get; set; }
    [XmlAttribute(AttributeName = "conceptName")]
    public string? ConceptName { get; set; }
    [XmlAttribute(AttributeName = "conceptID")]
    public string? ConceptID { get; set; }
    [XmlAttribute(AttributeName = "name")]
    public string? Name { get; set; }
    [XmlAttribute(AttributeName = "dateTaken")]
    public string? DateTaken { get; set; }
    [XmlText]
    public string? Text { get; set; }
}
public class BPDIA
{
    [XmlAttribute(AttributeName = "qualifierName")]
    public string? QualifierName { get; set; }
    [XmlAttribute(AttributeName = "qualifierID")]
    public string? QualifierID { get; set; }
    [XmlAttribute(AttributeName = "conceptName")]
    public string? ConceptName { get; set; }
    [XmlAttribute(AttributeName = "conceptID")]
    public string? ConceptID { get; set; }
    [XmlAttribute(AttributeName = "name")]
    public string? Name { get; set; }
    [XmlAttribute(AttributeName = "dateTaken")]
    public string? DateTaken { get; set; }
    [XmlText]
    public string? Text { get; set; }
}
public class Weight
{
    [XmlAttribute(AttributeName = "qualifierName")]
    public string? QualifierName { get; set; }
    [XmlAttribute(AttributeName = "qualifierID")]
    public string? QualifierID { get; set; }
    [XmlAttribute(AttributeName = "conceptName")]
    public string? ConceptName { get; set; }
    [XmlAttribute(AttributeName = "conceptID")]
    public string? ConceptID { get; set; }
    [XmlAttribute(AttributeName = "name")]
    public string? Name { get; set; }
    [XmlAttribute(AttributeName = "dateTaken")]
    public string? DateTaken { get; set; }
    [XmlText]
    public string? Text { get; set; }
}
public class Height
{
    [XmlAttribute(AttributeName = "qualifierName")]
    public string? QualifierName { get; set; }
    [XmlAttribute(AttributeName = "qualifierID")]
    public string? QualifierID { get; set; }
    [XmlAttribute(AttributeName = "conceptName")]
    public string? ConceptName { get; set; }
    [XmlAttribute(AttributeName = "conceptID")]
    public string? ConceptID { get; set; }
    [XmlAttribute(AttributeName = "name")]
    public string? Name { get; set; }
    [XmlAttribute(AttributeName = "dateTaken")]
    public string? DateTaken { get; set; }
    [XmlText]
    public string? Text { get; set; }
}
public class BMI
{
    [XmlAttribute(AttributeName = "qualifierName")]
    public string? QualifierName { get; set; }
    [XmlAttribute(AttributeName = "qualifierID")]
    public string? QualifierID { get; set; }
    [XmlAttribute(AttributeName = "conceptName")]
    public string? ConceptName { get; set; }
    [XmlAttribute(AttributeName = "conceptID")]
    public string? ConceptID { get; set; }
    [XmlAttribute(AttributeName = "name")]
    public string? Name { get; set; }
    [XmlAttribute(AttributeName = "dateTaken")]
    public string? DateTaken { get; set; }
    [XmlText]
    public string? Text { get; set; }
}

