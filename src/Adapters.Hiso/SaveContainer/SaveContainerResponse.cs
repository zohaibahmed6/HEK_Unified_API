namespace HekCoreApi.Adapters.Hiso.SaveContainer;

public sealed record SaveContainerResponse(SaveContainerResponseReturn SaveContainerResponseReturn)
{
    public static SaveContainerResponse From(bool response) => new(new SaveContainerResponseReturn(response));
}

public sealed record SaveContainerResponseReturn(bool Response);
