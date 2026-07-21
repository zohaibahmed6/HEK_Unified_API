namespace HekCoreApi.Adapters.Hiso.GetVersion;

/// <summary>
/// Legacy `getVersion` (`FormSessionService.svc.cs`): entirely hardcoded, no DB/config. `hisoversion`
/// is a real integer literal (`1`); `dictionaryVersion` is likewise hardcoded to `1` but legacy sets
/// `dictionaryVersionSpecified=false`, so it's omitted from the response entirely here rather than
/// serialized as a placeholder.
/// </summary>
public sealed record GetVersionResponse(GetVersionResponseReturn GetVersionResponseReturn)
{
    public static GetVersionResponse Real() => new(new GetVersionResponseReturn("PMS", "1.0", 1));
}

public sealed record GetVersionResponseReturn(string Application, string ApplicationVersion, int Hisoversion);
