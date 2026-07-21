using HekCoreApi.Application.Common.Models;

namespace HekCoreApi.Application.Common.Interfaces;

/// <summary>
/// Ported from legacy-reference/Hiso/Acc45Builder.cs, Acc45DiagnosisBuilder.cs, Acc45ReferralBuilder.cs,
/// and Mapper.cs's `SaveAccidentInformation` (`Appointment.usptblACC45Detail_InsertUpdate_New`).
/// Column resolution for each `&lt;field conceptName="X"&gt;` uses the real concept-mapping table
/// (`Appointment.uspGetHL7ConceptMapping`, `Mapper.GetConceptMappingTable`) - matched by
/// ConceptName-or-Description, taking the first row's ColumnName.
/// </summary>
public interface IAcc45DetailRepository
{
    /// <summary>Legacy: always runs after the definition save (`saveDataContainer`), regardless of `completed`. Returns the affected-ACC45-detail save result (`SaveAccidentInformation` always returns true on success in legacy, throws on failure).</summary>
    Task SaveAccidentInformationAsync(string submittedDataXml, HealthLinkSession session, string formEngineId, CancellationToken ct = default);
}
