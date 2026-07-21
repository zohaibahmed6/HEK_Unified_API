namespace HekCoreApi.Adapters.Hiso.GetData;

/// <summary>
/// HISO's real `getData` SOAP request shape (`FormSessionService.svc.cs`, `FormSessionPortType.cs`):
/// `{ sessionKey, dataContainer: FormData }`. `FormData.submittedData` is an untyped `object` at the
/// WCF layer, an `XmlNode[]` at runtime, with the real request XML at index [1] - carried here as a
/// plain XML string (`SubmittedDataXml`) rather than reinventing the legacy form/concept/section XML
/// structure as JSON, since that structure is exactly what the concept-mapping engine parses. Only
/// the envelope (JSON vs. SOAP/XML) differs from legacy - the actual form XML content is unchanged.
/// </summary>
public sealed record GetDataRequest(Guid SessionKey, GetDataFormData DataContainer);

/// <summary>Mirrors legacy's `FormData`/`FormMetaData` - only the fields `getData` actually reads/echoes.</summary>
public sealed record GetDataFormData(GetDataFormMetaData FormMetaData, string? SubmittedDataXml);

/// <summary>
/// Legacy `FormMetaData`'s value-wrapper fields, flattened to plain strings for the JSON envelope.
/// `FormInstanceOperationMode` drives the real dynamic-mode gate ("N" = new form, the only path with
/// real logic; anything else is a genuine empty stub in legacy too, per `FormSessionService.svc.cs`).
/// </summary>
public sealed record GetDataFormMetaData(
    string? FormInstanceId,
    string? FormInstanceVersion,
    string? FormEngineId,
    string? FormInstanceOperationMode,
    string? FormDefinitionId,
    string? FormDefinitionVersion,
    string? FormDefinitionTitle);
