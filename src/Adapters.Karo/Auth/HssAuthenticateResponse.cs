using System.Text.Json.Serialization;

namespace HekCoreApi.Adapters.Karo.Auth;

/// <summary>
/// HSS Portal's exact real Authenticate response shape (`APIController.cs`'s hand-built JSON):
/// success -> {"status":"success","token":...,"expiry":...,"practiceId":...} (no "message" key at all)
/// fail -> {"status":"fail","message":"Invalid credentials!"|"Authentication failed!"|&lt;exception message&gt;}
/// (no "token"/"expiry"/"practiceId" keys at all). Legacy's hand-built JSON only ever writes the keys
/// relevant to that branch; this record models both branches with one shape, so every field not used
/// by the current branch is null - `JsonIgnore(WhenWritingNull)` reproduces the same "field just isn't
/// there" wire behavior instead of serializing it as an explicit `null` (a cosmetic-only mismatch
/// fixed 2026-07-31 - the extra `"message":null` on success was the only value actually being flagged,
/// but the same fix also correctly drops token/expiry/practiceId on the fail branch).
/// </summary>
public sealed record HssAuthenticateResponse(
    string Status,
    [property: JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)] string? Token,
    [property: JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)] string? Expiry,
    [property: JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)] string? PracticeId,
    [property: JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)] string? Message)
{
    public static HssAuthenticateResponse Success(string? token, DateTime expiry, string? practiceId) =>
        new("success", token, expiry.ToString("s"), practiceId, null);

    public static HssAuthenticateResponse Fail(string message) =>
        new("fail", null, null, null, message);
}
