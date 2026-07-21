namespace HekCoreApi.Adapters.Hiso.GetData;

/// <summary>
/// HISO's real `getData` SOAP response shape: `{ "GetDataResponseReturn": { "dataContainer": {...} } }`.
/// `formMetaData` is echoed back from the request unchanged (legacy behavior); `submittedDataXml` is
/// the filled-in XML document root - null when the real dynamic-mode/new-form gate doesn't pass
/// (legacy returns a null `@return` in those cases; this compat layer returns a null `dataContainer`
/// for the same trigger conditions instead, since a wrapper object with a null inner field is a
/// closer JSON-envelope equivalent to a WCF response whose `@return` was never set).
/// </summary>
public sealed record GetDataResponse(GetDataResponseReturn GetDataResponseReturn)
{
    public static GetDataResponse Empty() => new(new GetDataResponseReturn(null));

    public static GetDataResponse From(GetDataFormMetaData formMetaData, string? submittedDataXml) =>
        new(new GetDataResponseReturn(new GetDataFormData(formMetaData, submittedDataXml)));
}

public sealed record GetDataResponseReturn(GetDataFormData? DataContainer);
