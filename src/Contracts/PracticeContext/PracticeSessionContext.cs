namespace HekCoreApi.Contracts.PracticeContext;

public sealed record PracticeSessionContext(string PracticeId, IDictionary<string, object?> Surgery, IDictionary<string, object?> Session);
