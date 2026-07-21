using HekCoreApi.Adapters.Hiso.GetData;

namespace HekCoreApi.Adapters.Hiso.SaveContainer;

public sealed record SaveContainerRequest(
    Guid SessionKey, GetDataFormMetaData FormMetaData, string? ResumePath,
    string? View, string? ViewType, string? ViewSignature, bool Completed, string? SubmittedDataXml);
