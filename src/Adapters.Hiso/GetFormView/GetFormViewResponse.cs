using HekCoreApi.Adapters.Hiso.GetData;

namespace HekCoreApi.Adapters.Hiso.GetFormView;

/// <summary>Legacy: `dataContainer` is always an empty `FormData` on read (never populated, despite planned-but-unwired code in the real source) - reproduced exactly, not "improved".</summary>
public sealed record GetFormViewResponse(GetFormViewResponseReturn GetFormViewResponseReturn)
{
    public static GetFormViewResponse From(string? resumePath, string? viewType, string? view) =>
        new(new GetFormViewResponseReturn(resumePath, viewType, view, null));
}

public sealed record GetFormViewResponseReturn(string? ResumePath, string? ViewType, string? View, GetDataFormData? DataContainer);
