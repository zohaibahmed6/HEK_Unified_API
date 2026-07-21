namespace HekCoreApi.Adapters.Hiso.ProcessAction;

public sealed record ProcessActionResponse(ProcessActionResponseReturn ProcessActionResponseReturn)
{
    public static ProcessActionResponse From(bool processed) => new(new ProcessActionResponseReturn(processed));
}

public sealed record ProcessActionResponseReturn(bool Processed);
