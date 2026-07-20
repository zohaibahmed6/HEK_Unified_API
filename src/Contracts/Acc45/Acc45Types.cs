namespace HekCoreApi.Contracts.Acc45;

public sealed record VersionInfo(string Application, string ApplicationVersion, string DictionaryVersion, int HisoVersion);

public sealed record DeliveryOptions(string? Url, string? PracticeEdi);

public sealed record FormActionInput(string ActionId, IDictionary<string, object?>? ActionContainer);

public sealed record FormActionResult(bool Processed);
