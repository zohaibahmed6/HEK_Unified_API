using System.Xml.Serialization;

namespace HekCoreApi.Adapters.Erms.Auth;

/// <summary>ERMS's exact existing XML Authenticate success response shape (ERMS_doc.md).</summary>
[XmlRoot("Authentication")]
public sealed class ErmsAuthenticationResponse
{
    [XmlElement("Token")]
    public string Token { get; set; } = string.Empty;

    [XmlElement("Expiry")]
    public string Expiry { get; set; } = string.Empty;

    [XmlElement("PracticeId")]
    public string PracticeId { get; set; } = string.Empty;
}

/// <summary>ERMS's exact existing XML Authenticate failure response shape (ERMS_doc.md).</summary>
[XmlRoot("Error")]
public sealed class ErmsErrorResponse
{
    [XmlElement("Message")]
    public string Message { get; set; } = "Authentication failed!";
}
