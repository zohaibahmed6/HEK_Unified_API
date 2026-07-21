namespace HekCoreApi.Adapters.Hiso.ProcessAction;

/// <summary>
/// Legacy `processAction` (`FormSessionService.svc.cs`): `actionId` is one of `"save"`/`"addTask"`/
/// `"addInvoice"`/`"launchForm"` (case-sensitive dispatch, confirmed from real source).
/// `actionContainer` was a raw `System.Xml.XmlElement` in legacy - carried here two ways: a flexible
/// dictionary for `"addTask"`'s flat fields (`code`/`taskDescription`/`dueDate`/`complete`), and a raw
/// XML string (<see cref="ActionContainerXml"/>) for `"save"`'s real section/group/field structure,
/// matching the same JSON-over-XML convention used for `getData`'s `dataContainer`.
/// </summary>
public sealed record ProcessActionRequest(Guid SessionKey, string ActionId, Dictionary<string, object?>? ActionContainer, string? ActionContainerXml = null);
