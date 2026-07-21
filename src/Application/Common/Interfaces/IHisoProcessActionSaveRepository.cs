using HekCoreApi.Application.Common.Models;

namespace HekCoreApi.Application.Common.Interfaces;

/// <summary>
/// Ported from legacy-reference/Hiso/PatientBuilder.cs, PatientConsultBuilder.cs,
/// PatientEmployerOrganisationBuilder.cs, PatientProblemBuilder.cs, RegisteredPractitionerBuilder.cs -
/// the 5 builders `processAction`'s `"save"` branch runs in sequence. Each builder's individual save
/// result is discarded in real legacy - the outer method always returns `true` once all 5 have run
/// (a legacy quirk, reproduced faithfully by this repository always returning normally rather than
/// surfacing partial failure, except where legacy itself throws - see method docs).
/// </summary>
public interface IHisoProcessActionSaveRepository
{
    /// <summary>Legacy: PatientBuilder - `[Hiso].[uspPatient_Update]`. Throws on failure (legacy does not swallow).</summary>
    Task SavePatientAsync(string submittedDataXml, HealthLinkSession session, CancellationToken ct = default);

    /// <summary>Legacy: PatientConsultBuilder - `[Hiso].[uspPatientConsult_Update]`. Throws on failure (legacy does not swallow).</summary>
    Task SavePatientConsultAsync(string submittedDataXml, HealthLinkSession session, CancellationToken ct = default);

    /// <summary>Legacy: PatientEmployerOrganisationBuilder - Country/City/Suburb/Designation lookups against
    /// a separate connection (`ConectionStringIndici_Master`), then `[Hiso].[uspPatientEmployerOrganisation_AddUpdate]`.
    /// Legacy swallows all exceptions here (log-only) - reproduced by never throwing.</summary>
    Task SavePatientEmployerOrganisationAsync(string submittedDataXml, HealthLinkSession session, CancellationToken ct = default);

    /// <summary>Legacy: PatientProblemBuilder - `[Hiso].[uspPatientProblem_Add]`. Throws on failure (legacy does not swallow).</summary>
    Task SavePatientProblemAsync(string submittedDataXml, HealthLinkSession session, CancellationToken ct = default);

    /// <summary>Legacy: RegisteredPractitionerBuilder - `[Hiso].[uspRegisteredPractitioner_Update]`. Throws on failure (legacy does not swallow).</summary>
    Task SaveRegisteredPractitionerAsync(string submittedDataXml, HealthLinkSession session, CancellationToken ct = default);
}
