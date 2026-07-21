namespace HekCoreApi.Adapters.Karo;

/// <summary>Legacy: `SetToJson`'s generic fail envelope (`APIController.cs:2010`) - `{"status":"fail","message":"..."}`, used by every real operation on any error path.</summary>
public sealed record KaroFailResponse(string Status, string Message)
{
    public static KaroFailResponse Of(string message) => new("fail", message);
}
