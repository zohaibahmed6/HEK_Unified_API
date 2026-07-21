using System.Xml.Serialization;

namespace HekCoreApi.Adapters.Erms.Hiso;

/// <summary>
/// Exact port of ERMS's `PatientData` (`ERMSWebAPI/Models/APIModels.cs:339`) - property names are the
/// legacy DataTable column names `ErmsDataTableMapper` matches on; element names/order are the wire shape.
/// </summary>
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
