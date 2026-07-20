namespace HekCoreApi.Contracts.Acc45;

public sealed record FormData(string FormInstanceId, string? ViewType, string? View, IDictionary<string, object?> DataContainer, string? ResumePath);
