using System.Text.Json.Serialization;

namespace HekCoreApi.Adapters.Karo.Auth;

/// <summary>
/// HSS Portal's exact existing Authenticate payload shape (KARO_HSS_doc.md "Authenticate" section,
/// confirmed field list: Username, Password, PatientId, EncounterId, system, pho). Accepted via
/// both GET query string and POST JSON body (KARO-BR-07) - this DTO covers the POST JSON body;
/// the GET query-string binding is handled by ASP.NET Core's default model binding on the same
/// property names.
/// </summary>
public sealed record HssAuthenticateRequest(
    string Username,
    string Password,
    string? PatientId,
    string? EncounterId,
    [property: JsonPropertyName("system")] string? System,
    [property: JsonPropertyName("pho")] string? Pho);
