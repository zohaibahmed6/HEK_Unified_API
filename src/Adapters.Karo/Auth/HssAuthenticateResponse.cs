namespace HekCoreApi.Adapters.Karo.Auth;

/// <summary>
/// HSS Portal's exact existing Authenticate response shape (KARO_HSS_doc.md):
/// success -> {"status":"success","token":...,"expiry":...,"practiceId":...}
/// fail -> {"status":"fail","message":"Authentication failed!"}
/// </summary>
public sealed record HssAuthenticateResponse(string Status, string? Token, string? Expiry, string? PracticeId, string? Message)
{
    public static HssAuthenticateResponse Success(string token, DateTimeOffset expiry, string practiceId) =>
        new("success", token, expiry.UtcDateTime.ToString("O"), practiceId, null);

    public static HssAuthenticateResponse Fail() =>
        new("fail", null, null, null, "Authentication failed!");
}
