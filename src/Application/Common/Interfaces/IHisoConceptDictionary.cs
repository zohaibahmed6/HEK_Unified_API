using HekCoreApi.Application.Common.Models;

namespace HekCoreApi.Application.Common.Interfaces;

/// <summary>
/// HISO's real concept dictionary (`[Hiso].[UspGetHisoConcepts]`, confirmed from
/// legacy-reference/Hiso/DAL/DBMessages.cs's `GetHisoConceptDetail` and
/// legacy-reference/Hiso/FormSessionService.svc.cs's `getData`, which caches the result under key
/// `"ConceptList"` for 10 minutes). Replaces the earlier `Hiso.uspGetPatient_{ConceptName}`
/// name-guessing dispatch with the real, DB-driven lookup.
/// </summary>
public interface IHisoConceptDictionary
{
    Task<IReadOnlyList<HisoConceptDetail>> GetConceptsAsync(string practiceId, CancellationToken ct = default);
}
