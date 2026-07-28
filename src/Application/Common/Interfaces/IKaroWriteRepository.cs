using HekCoreApi.Application.Common.Models;

namespace HekCoreApi.Application.Common.Interfaces;

/// <summary>Ported from `HSSDA.cs`'s remaining real save/insert methods used by KARO's write operations.</summary>
public interface IKaroWriteRepository
{
    /// <summary>Legacy: `HSSDA.InsertUpdateConsultNotes` -> `[HSS].[uspInsertUpdateConsultNotes]`. Returns the real output param (&gt;0 on success).</summary>
    Task<long> SaveClinicalNotesAsync(string practiceSuffix, RoutingContext routingContext, string? patientId, string? appointmentId, string? userId, string? subjective, string? objective, string? assessment, string? plans, CancellationToken ct = default);

    /// <summary>Legacy: `HSSDA.InsertUpdateDiagnosis` -> `[HSS].[uspInsertUpdateDiagnosis]`. -5 is a real, specific "already exists" sentinel.</summary>
    Task<int> SaveConditionAsync(string practiceSuffix, RoutingContext routingContext, string? patientId, string? appointmentId, string? userId, string? diagnosisType, DateTime? onsetDate, string? summary, bool isLongTerm, string? conceptId, string? diseaseName, string? fsn, CancellationToken ct = default);

    /// <summary>Legacy: `HSSDA.HSSInsertUpdateService` -> `[HSS].[uspInsertUpdateService]`. -3 is a real, specific "invoice already exists" sentinel. `locationId` is always the literal "167" in legacy.</summary>
    Task<int> SaveInvoiceAsync(string practiceSuffix, RoutingContext routingContext, string? patientId, string? encounterId, string? name, string? code, string? fee, string? userId, string? payee, CancellationToken ct = default);

    /// <summary>Legacy: `HSSDA.InsertUpdateObservation` -> `[HSS].[uspInsertUpdateObservation]`.</summary>
    Task<int> SaveObservationsAsync(string practiceSuffix, RoutingContext routingContext, string? patientId, string? appointmentId, string? userId, string? temperature, string? waist, string? height, string? weight, string? bpSys, string? bpDia, string? heartRate, string? notes, string? risk, string? framingham, CancellationToken ct = default);

    /// <summary>Legacy: `HSSDA.InsertUpdateRecall` -> `[HSS].[uspInsertUpdateRecall]`.</summary>
    Task<int> SaveRecallAsync(string practiceSuffix, RoutingContext routingContext, string? patientId, string? appointmentId, string? userId, string? priority, string? group, DateTime? dueDate, string? notes, string? categoryId, CancellationToken ct = default);

    /// <summary>
    /// Ported from `APIController.cs`'s `SaveToDMS` + `HSSDA.DocumentSave` (`[dbo].[uspDocumentSave]`,
    /// real DMS connection) followed by `HSSDA.InsertDocument` (`[HSS].[uspInsertDocument]`, real
    /// Indici connection). `categoryId` is `17` for `itemType=="in"`, else `18` (legacy real constants);
    /// `itemTypeId` for `InsertDocument` is `1` for "in", else `2`.
    /// </summary>
    Task<KaroSaveDocumentResult> SaveDocumentAsync(string practiceSuffix, string practiceSuffixNumeric, RoutingContext routingContext, string? patientId, string? encounterId, byte[]? messageData, string? contentType, string? messageSubject, string? itemType, CancellationToken ct = default);

    /// <summary>
    /// Ported from `APIController.cs`'s `GetTemplateSchema` + `FillSummaryData` + `BuildJsonTimeLineData`
    /// + `InsertSummary` (`SaveSummary`, `:978`) - the dynamic, schema-driven encounter-summary save
    /// pipeline (diabetes/diabetic-foot-exam/retinopathy screening forms). Real sentinels reproduced
    /// exactly: `-4` = invalid identifier (no schema found), `-5` = invalid outcome, other `&lt;=0` =
    /// generic insertion failure.
    /// </summary>
    Task<int> SaveSummaryAsync(string practiceSuffix, RoutingContext routingContext, string? patientId, string? encounterId, string? providerId, string? identifier, string? dateTimeRecorded, string entriesJson, CancellationToken ct = default);
}

public sealed record KaroSaveDocumentResult(bool Succeeded, string? DmsGuidKey);

/// <summary>
/// Non-DB sentinel values `SaveSummaryAsync` can return, kept distinct from the real DB-returned
/// `-4`/`-5` sentinels (`InsertSummary`'s own "invalid identifier"/"invalid outcome") so callers can
/// tell apart legacy's two genuinely different failure messages that both happen before any DB write.
/// </summary>
public static class KaroSaveSummarySentinels
{
    public const int SchemaNotFound = -1000;
    public const int ParseFailed = -1001;
}
