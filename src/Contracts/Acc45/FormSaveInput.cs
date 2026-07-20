namespace HekCoreApi.Contracts.Acc45;

public sealed record FormSaveInput(IDictionary<string, object?> DataContainer, string? View, string? ViewType, bool Completed, bool? ContinueSession);
