namespace HekCoreApi.Adapters.Hiso.GetFormView;

/// <summary>Legacy: `formInstanceId` is a real request field but never used in `getFormView`'s body - form lookup is entirely session-driven. Kept for wire-shape fidelity only.</summary>
public sealed record GetFormViewRequest(Guid SessionKey, string? FormInstanceId);
