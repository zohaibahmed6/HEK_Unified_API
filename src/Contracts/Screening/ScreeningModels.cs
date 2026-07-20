namespace HekCoreApi.Contracts.Screening;

public sealed record ScreeningCode(string Code, string Description);

public sealed record ScreeningCodeInput(string Code, string? Value);

public sealed record ScreeningCodeResult(string Code, string? Value, bool Saved);
