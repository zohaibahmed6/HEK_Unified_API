using System.Xml.Serialization;

namespace HekCoreApi.Adapters.Erms.Auth;

/// <summary>ERMS's exact existing XML Authenticate request shape (ERMS_doc.md "Authenticate" section).</summary>
[XmlRoot("Credential")]
public sealed class ErmsCredential
{
    [XmlElement("Username")]
    public string Username { get; set; } = string.Empty;

    [XmlElement("Password")]
    public string Password { get; set; } = string.Empty;

    [XmlElement("PatientId")]
    public string? PatientId { get; set; }

    [XmlElement("EncounterId")]
    public string? EncounterId { get; set; }
}
