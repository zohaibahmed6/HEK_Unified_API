using System.Xml.Serialization;

namespace HekCoreApi.Adapters.Erms.Hiso;

/// <summary>
/// Exact port of legacy `ReferralDocument` (`APIModels.cs:1520`) - both the `SaveDocument` request body
/// and (with the error/content fields scrubbed in the legacy `finally`) its success response. Legacy has
/// no XmlRoot, so the root element is the class name. Property name `PatiendID` is a real legacy typo.
/// </summary>
public class ReferralDocument
{
    [XmlElement(ElementName = "ReferralDocument_Referral_ID")]
    public string? ID { get; set; }
    [XmlElement(ElementName = "ReferralDocument_Document_ID")]
    public string? DocumentID { get; set; }
    [XmlElement(ElementName = "ReferralDocument_Patient_PMS_ID")]
    public string? PatiendID { get; set; }
    [XmlElement(ElementName = "ReferralDocument_Encounter_ID")]
    public string? EncounterID { get; set; }
    [XmlElement(ElementName = "ReferralDocument_Referral_Type")]
    public string? Type { get; set; }
    [XmlElement(ElementName = "ReferralDocument_Item_Type")]
    public string? ItemType { get; set; }
    [XmlElement(ElementName = "ReferralDocument_Referral_Status")]
    public string? Status { get; set; }
    [XmlElement(ElementName = "ReferralDocument_Created_Date")]
    public string? CreatedDate { get; set; }
    [XmlElement(ElementName = "ReferralDocument_Referrer_Fullname")]
    public string? Fullname { get; set; }
    [XmlElement(ElementName = "ReferralDocument_Referrer_PMS_ID")]
    public string? ProviderID { get; set; }
    [XmlElement(ElementName = "ReferralDocument_Document_Source")]
    public string? Source { get; set; }
    [XmlElement(ElementName = "ReferralDocument_Content_Type")]
    public string? ContentType { get; set; }
    [XmlElement(ElementName = "ReferralDocument_Description_Type")]
    public string? DescriptionType { get; set; }
    [XmlElement(ElementName = "ReferralDocument_Description")]
    public string? Description { get; set; }
    [XmlElement(ElementName = "ReferralDocument_Encoding")]
    public string? Encoding { get; set; }
    [XmlElement(ElementName = "ReferralDocument_Content")]
    public string? Content { get; set; }
    [XmlElement(ElementName = "ReferralDocument_Error_Text")]
    public string? ErrorText { get; set; }
}
