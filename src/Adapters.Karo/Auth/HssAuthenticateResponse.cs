namespace HekCoreApi.Adapters.Karo.Auth;

/// <summary>
/// HSS Portal's exact real Authenticate response shape (`APIController.cs`'s hand-built JSON):
/// success -> {"status":"success","token":...,"expiry":...,"practiceId":...}
/// fail -> {"status":"fail","message":"Invalid credentials!"|"Authentication failed!"|&lt;exception message&gt;}
/// </summary>
public sealed record HssAuthenticateResponse(string Status, string? Token, string? Expiry, string? PracticeId, string? Message)
{
    public static HssAuthenticateResponse Success(string? token, DateTime expiry, string? practiceId) =>
        new("success", token, expiry.ToString("s"), practiceId, null);

    public static HssAuthenticateResponse Fail(string message) =>
        new("fail", null, null, null, message);
}
