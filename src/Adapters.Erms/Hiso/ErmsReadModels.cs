using System.Xml.Serialization;

namespace HekCoreApi.Adapters.Erms.Hiso;

// Exact ports of the root/list-envelope models used by the ERMS Get* operations
// (`ERMSWebAPI/Models/APIModels.cs`). Property names are the legacy DataTable column names the
// `ErmsDataTableMapper` matches on; element/attribute names and defaults are the wire shape.

/// <summary>Legacy `CurrentUser` (`APIModels.cs:81`).</summary>
[XmlRoot(ElementName = "CurrentUser")]
public class CurrentUser
{
    [XmlElement(ElementName = "CurrentUser_FirstName")]
    public CurrentUserFirstName FirstName { get; set; } = new CurrentUserFirstName();
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

/// <summary>Legacy `PatientProblem` (`APIModels.cs:457`).</summary>
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
    public string? Order { get; set; }
    [XmlAttribute(AttributeName = "minDateTime")]
    public string? MinDateTime { get; set; }
    [XmlAttribute(AttributeName = "maxDateTime")]
    public string? MaxDateTime { get; set; }
    [XmlAttribute(AttributeName = "conceptName")]
    public string? ConceptName { get; set; } = "Problems";
    [XmlAttribute(AttributeName = "conceptID")]
    public string? ConceptID { get; set; }
    [XmlAttribute(AttributeName = "referenceID")]
    public string? ReferenceId { get; set; }
}

/// <summary>Legacy `Problems` envelope (`APIModels.cs:486`).</summary>
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

/// <summary>Legacy `Consult` (`APIModels.cs:692`).</summary>
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
    public string? Order { get; set; }
    [XmlAttribute(AttributeName = "maxDateTime")]
    public string? MaxDateTime { get; set; }
    [XmlAttribute(AttributeName = "minDateTime")]
    public string? MinDateTime { get; set; }
    [XmlAttribute(AttributeName = "conceptID")]
    public string? ConceptID { get; set; }
    [XmlAttribute(AttributeName = "referenceID")]
    public string? ReferenceId { get; set; }
}

/// <summary>Legacy `ConsultNotes` envelope (`APIModels.cs:715`).</summary>
[XmlRoot(ElementName = "ConsultNotes")]
public class ConsultNotes
{
    [XmlElement(ElementName = "Patient_Consult")]
    public List<Consult> Consult { get; set; } = new List<Consult>();
    [XmlAttribute(AttributeName = "conceptType")]
    public string ConceptType { get; set; } = "List";
}

/// <summary>Legacy `PatientNOK` (`APIModels.cs:835`).</summary>
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
    public string? ConceptID { get; set; }
    [XmlAttribute(AttributeName = "referenceID")]
    public string? ReferenceId { get; set; }
}

/// <summary>Legacy `NextOfKin` envelope (`APIModels.cs:872`) - root element is `Next_Of_Kin`.</summary>
[XmlRoot(ElementName = "Next_Of_Kin")]
public class NextOfKin
{
    [XmlElement(ElementName = "PatientNOK")]
    public List<PatientNOK> PatientNOK { get; set; } = new List<PatientNOK>();
    [XmlAttribute(AttributeName = "conceptType")]
    public string ConceptType { get; set; } = "List";
}

/// <summary>Legacy `MedicalWarning` (`APIModels.cs:929`).</summary>
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
    public string? Order { get; set; }
    [XmlAttribute(AttributeName = "minDateTime")]
    public string? MinDateTime { get; set; }
    [XmlAttribute(AttributeName = "maxDateTime")]
    public string? MaxDateTime { get; set; }
    [XmlAttribute(AttributeName = "conceptID")]
    public string? ConceptID { get; set; }
    [XmlAttribute(AttributeName = "referenceID")]
    public string? ReferenceId { get; set; }
}

/// <summary>Legacy `MedicalWarnings` envelope (`APIModels.cs:951`).</summary>
[XmlRoot(ElementName = "MedicalWarnings")]
public class MedicalWarnings
{
    [XmlElement(ElementName = "Patient_MedicalWarning")]
    public List<MedicalWarning> MedicalWarning { get; set; } = new List<MedicalWarning>();
    [XmlAttribute(AttributeName = "conceptType")]
    public string ConceptType { get; set; } = "List";
}

/// <summary>Legacy `RegisteredPractitioner` (`APIModels.cs:1015`).</summary>
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
    public string? Name { get; set; }
    [XmlAttribute(AttributeName = "conceptID")]
    public string? ConceptID { get; set; }
    [XmlAttribute(AttributeName = "referenceID")]
    public string? ReferenceId { get; set; }
}

/// <summary>Legacy `RegisteredPractitioners` envelope (`APIModels.cs:1062`).</summary>
[XmlRoot(ElementName = "RegisteredPractitioners")]
public class RegisteredPractitioners
{
    [XmlElement(ElementName = "RegisteredPractitioner")]
    public List<RegisteredPractitioner> RegisteredPractitioner { get; set; } = new List<RegisteredPractitioner>();
    [XmlAttribute(AttributeName = "conceptType")]
    public string ConceptType { get; set; } = "List";
}

/// <summary>Legacy `Smoking` (`APIModels.cs:1091`) - `numRows` is a real legacy attribute never populated by the mapper.</summary>
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
    public string? NumRows { get; set; }
    [XmlAttribute(AttributeName = "conceptID")]
    public string? ConceptID { get; set; }
    [XmlAttribute(AttributeName = "referenceID")]
    public string? ReferenceId { get; set; }
}

/// <summary>Legacy `SmokingStatus` envelope (`APIModels.cs:1108`).</summary>
[XmlRoot(ElementName = "SmokingStatus")]
public class SmokingStatus
{
    [XmlElement(ElementName = "Patient_Smoking")]
    public List<Smoking> Smoking { get; set; } = new List<Smoking>();
    [XmlAttribute(AttributeName = "conceptType")]
    public string ConceptType { get; set; } = "List";
}

/// <summary>Legacy `Accident` (`APIModels.cs:1140`).</summary>
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
    public string? Order { get; set; }
    [XmlAttribute(AttributeName = "minDateTime")]
    public string? MinDateTime { get; set; }
    [XmlAttribute(AttributeName = "maxDateTime")]
    public string? MaxDateTime { get; set; }
    [XmlAttribute(AttributeName = "conceptID")]
    public string? ConceptID { get; set; }
    [XmlAttribute(AttributeName = "referenceID")]
    public string? ReferenceId { get; set; }
}

/// <summary>Legacy `Accidents` envelope (`APIModels.cs:1163`).</summary>
[XmlRoot(ElementName = "Accidents")]
public class Accidents
{
    [XmlElement(ElementName = "Patient_Accident")]
    public List<Accident> Accident { get; set; } = new List<Accident>();
    [XmlAttribute(AttributeName = "conceptType")]
    public string ConceptType { get; set; } = "List";
}

/// <summary>Legacy `Measurement` (`APIModels.cs:1256`) - root element `Patient_Measurement`, `name="measurements"` default.</summary>
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
    public string? Name { get; set; } = "measurements";
    [XmlText]
    public string? Text { get; set; }
}
