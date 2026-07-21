using System.Xml.Serialization;

namespace HekCoreApi.Adapters.Erms.Hiso;

// Exact ports of the medication/report/scanned models (`ERMSWebAPI/Models/APIModels.cs:496-1519`)
// used by the remaining ERMS Get* operations.

// --- Medication wrappers (`APIModels.cs:496-565`) ---
public class MedStartedDate
{
    [XmlAttribute(AttributeName = "conceptID")]
    public string? ConceptID { get; set; }
    [XmlText]
    public string? Text { get; set; }
}
public class MedName
{
    [XmlAttribute(AttributeName = "conceptID")]
    public string? ConceptID { get; set; }
    [XmlText]
    public string? Text { get; set; }
}
public class MedCode
{
    [XmlAttribute(AttributeName = "conceptID")]
    public string? ConceptID { get; set; }
    [XmlText]
    public string? Text { get; set; }
}
public class MedCodingSystem
{
    [XmlAttribute(AttributeName = "conceptID")]
    public string? ConceptID { get; set; }
    [XmlText]
    public string? Text { get; set; }
}
public class MedDispenseQuantity
{
    [XmlAttribute(AttributeName = "conceptID")]
    public string? ConceptID { get; set; }
    [XmlText]
    public string? Text { get; set; }
}
public class MedDispenseUnit
{
    [XmlAttribute(AttributeName = "conceptID")]
    public string? ConceptID { get; set; }
    [XmlText]
    public string? Text { get; set; }
}
public class MedDosageQuantity
{
    [XmlAttribute(AttributeName = "conceptID")]
    public string? ConceptID { get; set; }
    [XmlText]
    public string? Text { get; set; }
}
public class MedDosageUnit
{
    [XmlAttribute(AttributeName = "conceptID")]
    public string? ConceptID { get; set; }
    [XmlText]
    public string? Text { get; set; }
}
public class MedAdministrationinstructions
{
    [XmlAttribute(AttributeName = "conceptID")]
    public string? ConceptID { get; set; }
    [XmlText]
    public string? Text { get; set; }
}
public class MedLastPrescribedDate
{
    [XmlAttribute(AttributeName = "conceptID")]
    public string? ConceptID { get; set; }
    [XmlText]
    public string? Text { get; set; }
}

/// <summary>Legacy `RegularMedication` (`APIModels.cs:566`).</summary>
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

/// <summary>Legacy `RegularMedications` envelope (`APIModels.cs:599`).</summary>
[XmlRoot(ElementName = "RegularMedications")]
public class RegularMedications
{
    [XmlElement(ElementName = "Patient_RegularMedication")]
    public List<RegularMedication> RegularMedication { get; set; } = new List<RegularMedication>();
    [XmlAttribute(AttributeName = "conceptType")]
    public string ConceptType { get; set; } = "List";
}

/// <summary>Legacy `PrescribedMedication` (`APIModels.cs:607`).</summary>
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

/// <summary>Legacy `PrescribedMedications` envelope (`APIModels.cs:640`) - no XmlRoot in legacy; serializer uses the class name.</summary>
public class PrescribedMedications
{
    [XmlElement(ElementName = "Patient_PrescribedMedication")]
    public List<PrescribedMedication> Medication { get; set; } = new List<PrescribedMedication>();
    [XmlAttribute(AttributeName = "conceptType")]
    public string ConceptType { get; set; } = "List";
}

// --- Report wrappers (`APIModels.cs:1274-1347`) ---
public class SendingFacility
{
    [XmlAttribute(AttributeName = "conceptID")]
    public string? ConceptID { get; set; }
    [XmlText]
    public string? Text { get; set; }
}
public class Subject
{
    [XmlAttribute(AttributeName = "conceptID")]
    public string? ConceptID { get; set; }
    [XmlText]
    public string? Text { get; set; }
}
public class Name
{
    [XmlAttribute(AttributeName = "conceptID")]
    public string? ConceptID { get; set; }
    [XmlText]
    public string? Text { get; set; }
}
public class DateReceived
{
    [XmlAttribute(AttributeName = "conceptID")]
    public string? ConceptID { get; set; }
    [XmlText]
    public string? Text { get; set; }
}
public class DataType
{
    [XmlAttribute(AttributeName = "conceptID")]
    public string? ConceptID { get; set; }
    [XmlText]
    public string? Text { get; set; }
}
public class Content
{
    [XmlAttribute(AttributeName = "conceptID")]
    public string? ConceptID { get; set; }
    [XmlText]
    public string? Text { get; set; }
}

/// <summary>Legacy `LaboratoryReport` (`APIModels.cs:1309`). Quirk preserved: `Order` has NO XmlAttribute in legacy, so it serializes as an `&lt;Order&gt;` element, unlike every other list model.</summary>
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

/// <summary>Legacy `LaboratoryReports` envelope (`APIModels.cs:1333`).</summary>
[XmlRoot(ElementName = "LaboratoryReports")]
public class LaboratoryReports
{
    [XmlElement(ElementName = "Patient_LaboratoryReport")]
    public List<LaboratoryReport> LaboratoryReport { get; set; } = new List<LaboratoryReport>();
    [XmlAttribute(AttributeName = "conceptType")]
    public string ConceptType { get; set; } = "List";
}

/// <summary>Legacy `LaboratoryReportContent` (`APIModels.cs:1348`).</summary>
public class LaboratoryReportContent
{
    [XmlElement(ElementName = "Patient_LaboratoryReport_Content")]
    public Content Content { get; set; } = new Content();
    [XmlAttribute(AttributeName = "conceptID")]
    public string? ConceptID { get; set; }
    [XmlAttribute(AttributeName = "referenceID")]
    public string? ReferenceId { get; set; }
}

/// <summary>Legacy `LaboratoryReportsContent` envelope (`APIModels.cs:1357`).</summary>
[XmlRoot(ElementName = "LaboratoryReportsContent")]
public class LaboratoryReportsContent
{
    [XmlElement(ElementName = "Patient_LaboratoryReport")]
    public List<LaboratoryReportContent> LaboratoryReportContent { get; set; } = new List<LaboratoryReportContent>();
    [XmlAttribute(AttributeName = "name")]
    public string Name { get; set; } = "clinical.diagnosticReports";
}

/// <summary>Legacy `Group` - discharge report row (`APIModels.cs:1365`).</summary>
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
    public string? Order { get; set; }
    [XmlAttribute(AttributeName = "maxDateTime")]
    public string? MaxDateTime { get; set; }
    [XmlAttribute(AttributeName = "minDateTime")]
    public string? MinDateTime { get; set; }
    [XmlAttribute(AttributeName = "conceptName")]
    public string? ConceptName { get; set; }
    [XmlAttribute(AttributeName = "conceptID")]
    public string? ConceptID { get; set; }
    [XmlAttribute(AttributeName = "referenceID")]
    public string? ReferenceId { get; set; }
}

/// <summary>Legacy `DischargeReports` envelope (`APIModels.cs:1392`) - lowercase `group` element.</summary>
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

/// <summary>Legacy `DischargeSummaryContent` (`APIModels.cs:1456`).</summary>
public class DischargeSummaryContent
{
    [XmlElement(ElementName = "Patient_DischargeSummary_Content")]
    public Content Content { get; set; } = new Content();
    [XmlAttribute(AttributeName = "conceptID")]
    public string? ConceptID { get; set; }
    [XmlAttribute(AttributeName = "referenceID")]
    public string? ReferenceId { get; set; }
}

/// <summary>Legacy `DischargeSummaryContents` envelope (`APIModels.cs:1465`).</summary>
[XmlRoot(ElementName = "DischargeSummaryContents")]
public class DischargeSummaryContents
{
    [XmlElement(ElementName = "Patient_DischargeSummary")]
    public List<DischargeSummaryContent> DischargeSummaryContent { get; set; } = new List<DischargeSummaryContent>();
    [XmlAttribute(AttributeName = "name")]
    public string Name { get; set; } = "clinical.DischargeReport";
}

/// <summary>Legacy `RadiologyGroup` (`APIModels.cs:1402`).</summary>
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
    public string? Order { get; set; }
    [XmlAttribute(AttributeName = "maxDateTime")]
    public string? MaxDateTime { get; set; }
    [XmlAttribute(AttributeName = "minDateTime")]
    public string? MinDateTime { get; set; }
    [XmlAttribute(AttributeName = "conceptName")]
    public string? ConceptName { get; set; }
    [XmlAttribute(AttributeName = "conceptID")]
    public string? ConceptID { get; set; }
    [XmlAttribute(AttributeName = "referenceID")]
    public string? ReferenceId { get; set; }
}

/// <summary>Legacy `RadiologyReports` envelope (`APIModels.cs:1429`) - lowercase `group` element.</summary>
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

/// <summary>Legacy `RadiologyReportContent` (`APIModels.cs:1439`).</summary>
public class RadiologyReportContent
{
    [XmlElement(ElementName = "Patient_RadiologyReport_Content")]
    public Content Content { get; set; } = new Content();
    [XmlAttribute(AttributeName = "conceptID")]
    public string? ConceptID { get; set; }
    [XmlAttribute(AttributeName = "referenceID")]
    public string? ReferenceId { get; set; }
}

/// <summary>Legacy `RadiologyReportContents` envelope (`APIModels.cs:1448`) - root element `RadiologyReportsContent`.</summary>
[XmlRoot(ElementName = "RadiologyReportsContent")]
public class RadiologyReportContents
{
    [XmlElement(ElementName = "Patient_RadiologyReport")]
    public List<RadiologyReportContent> RadiologyReportContent { get; set; } = new List<RadiologyReportContent>();
    [XmlAttribute(AttributeName = "name")]
    public string Name { get; set; } = "clinical.RadiologyReport";
}

/// <summary>Legacy `ScannedGroup` (`APIModels.cs:1473`) - carries both report metadata and a `ScanContent` element (used by both list and details).</summary>
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
    public string? Order { get; set; }
    [XmlAttribute(AttributeName = "maxDateTime")]
    public string? MaxDateTime { get; set; }
    [XmlAttribute(AttributeName = "minDateTime")]
    public string? MinDateTime { get; set; }
    [XmlAttribute(AttributeName = "conceptName")]
    public string? ConceptName { get; set; }
    [XmlAttribute(AttributeName = "conceptID")]
    public string? ConceptID { get; set; }
    [XmlAttribute(AttributeName = "referenceID")]
    public string? ReferenceId { get; set; }
}

/// <summary>Legacy `ScanDocumentReports` envelope (`APIModels.cs:1502`).</summary>
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

/// <summary>Legacy `ScanReportContent` envelope (`APIModels.cs:1512`).</summary>
[XmlRoot(ElementName = "ScanReportContent")]
public class ScanReportContent
{
    [XmlElement(ElementName = "group")]
    public List<ScannedGroup> ScannedGroup { get; set; } = new List<ScannedGroup>();
    [XmlAttribute(AttributeName = "name")]
    public string Name { get; set; } = "clinical.ScanContent";
}
