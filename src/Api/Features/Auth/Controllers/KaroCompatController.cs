using System.Text.Json;
using HekCoreApi.Adapters.Karo;
using HekCoreApi.Adapters.Karo.Auth;
using HekCoreApi.Adapters.Karo.Demographics;
using HekCoreApi.Application.Features.Karo.Commands;
using HekCoreApi.Application.Features.Karo.Queries;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace HekCoreApi.Api.Features.Auth.Controllers;

/// <summary>
/// KARO/HSS's real "Indici HSS Web API" (`legacy-reference/hsswebapi/DevLocal/HSSWebAPI/Controllers/APIController.cs`,
/// confirmed real source, not documentation). Every operation here reproduces legacy exactly - same
/// response envelope (always HTTP 200, `status` field in the body), same success/failure JSON shapes,
/// same real failure message strings, same `encounterId`/`patientId` decryption and practice-suffix
/// routing quirks. Deliberately kept fully separate from `HisoCompatController` - no shared middleware,
/// token validation, or DB routing between HISO and KARO/HSS, per Zohaib's explicit requirement.
/// </summary>
[ApiController]
[Route("karo")]
public sealed class KaroCompatController : ControllerBase
{
    private readonly IMediator _mediator;

    public KaroCompatController(IMediator mediator)
    {
        _mediator = mediator;
    }

    /// <summary>Legacy: `GET /api/Ping` - no params, no auth, always returns exactly `{"status":"success"}`.</summary>
    [HttpGet("ping")]
    public IActionResult Ping() => Ok(new { status = "success" });

    /// <summary>Legacy: `GET Authenticate(...)` - binds only from the query string (`APIController.cs:31`). A separate real overload from the POST one below, never combined.</summary>
    [HttpGet("authenticate")]
    [ProducesResponseType(typeof(HssAuthenticateResponse), StatusCodes.Status200OK)]
    public async Task<IActionResult> AuthenticateGet([FromQuery] HssAuthenticateRequest query, CancellationToken ct) =>
        await AuthenticateAsync(query, ct);

    /// <summary>Legacy: `POST Authenticate()` - binds only from the JSON body (`APIController.cs:93`). A separate real overload from the GET one above, never combined.</summary>
    [HttpPost("authenticate")]
    [ProducesResponseType(typeof(HssAuthenticateResponse), StatusCodes.Status200OK)]
    public async Task<IActionResult> AuthenticatePost([FromBody] HssAuthenticateRequest body, CancellationToken ct) =>
        await AuthenticateAsync(body, ct);

    private async Task<IActionResult> AuthenticateAsync(HssAuthenticateRequest request, CancellationToken ct)
    {
        var result = await _mediator.Send(
            new KaroAuthenticateQuery(request.Username, request.Password, request.PatientId, request.EncounterId, request.System, request.Pho), ct);

        return Ok(result.Succeeded
            ? HssAuthenticateResponse.Success(result.Token, result.Expiry!.Value, result.PracticeId)
            : HssAuthenticateResponse.Fail(result.ErrorMessage ?? "Authentication failed!"));
    }

    /// <summary>Legacy: `GET GetDemographics(...)` (`APIController.cs:274`) - bearer-token-validated, real `[HSS].[uspGetDemographics]`.</summary>
    [HttpGet("demographics")]
    public async Task<IActionResult> GetDemographics(string? system, string? pho, string? patientId, string? encounterId, CancellationToken ct)
    {
        var token = GetAuthorizationToken();
        var result = await _mediator.Send(new KaroDemographicsQuery(system, pho, patientId, encounterId, token), ct);

        if (!result.Succeeded)
        {
            return Ok(KaroFailResponse.Of(result.ErrorMessage ?? "Invalid token value!"));
        }

        var demographic = new KaroDemographic(
            result.Demographic is null ? [] : [result.Demographic],
            [.. result.Cards ?? []]);

        return Ok(new KaroRootResponse<KaroDemographic>(result.PatientId, "Patient", "hss", [demographic]));
    }

    /// <summary>Legacy: `GET GetClinicalNotes(...)` (`APIController.cs:164`) - real `[HSS].[uspGetConsultNotes]`.</summary>
    [HttpGet("clinicalnotes")]
    public async Task<IActionResult> GetClinicalNotes(string? system, string? pho, string? patientId, string? encounterId, CancellationToken ct) =>
        RootOrFail(await _mediator.Send(new KaroClinicalNotesQuery(system, pho, patientId, encounterId, GetAuthorizationToken()), ct), "Notes");

    /// <summary>Legacy: `GET GetConditions(...)` (`APIController.cs:219`) - real `[HSS].[uspGetConditions]`.</summary>
    [HttpGet("conditions")]
    public async Task<IActionResult> GetConditions(string? system, string? pho, string? patientId, string? encounterId, CancellationToken ct) =>
        RootOrFail(await _mediator.Send(new KaroConditionsQuery(system, pho, patientId, encounterId, GetAuthorizationToken()), ct), "Conditions");

    /// <summary>Legacy: `GET GetDocuments(...)` (`APIController.cs:350`) - real `[HSS].[uspGetDocuments]` (non-AWS path; AWS branch deferred, see repository).</summary>
    [HttpGet("documents")]
    public async Task<IActionResult> GetDocuments(string? system, string? pho, string? patientId, string? encounterId, string? identifier, CancellationToken ct) =>
        RootOrFail(await _mediator.Send(new KaroDocumentsQuery(system, pho, patientId, encounterId, identifier, GetAuthorizationToken()), ct), "Documents");

    /// <summary>Legacy: `GET GetLabResults(...)` (`APIController.cs:452`) - real `[HSS].[uspGetLabResults]`.</summary>
    [HttpGet("labresults")]
    public async Task<IActionResult> GetLabResults(string? system, string? pho, string? patientId, string? encounterId, CancellationToken ct) =>
        RootOrFail(await _mediator.Send(new KaroLabResultsQuery(system, pho, patientId, encounterId, GetAuthorizationToken()), ct), "Labs");

    /// <summary>Legacy: `GET GetMedications(...)` (`APIController.cs:507`) - real `[HSS].[uspGetMedications]`.</summary>
    [HttpGet("medications")]
    public async Task<IActionResult> GetMedications(string? system, string? pho, string? patientId, string? encounterId, CancellationToken ct) =>
        RootOrFail(await _mediator.Send(new KaroMedicationsQuery(system, pho, patientId, encounterId, GetAuthorizationToken()), ct), "Medications");

    /// <summary>Legacy: `GET GetObservations(...)` (`APIController.cs:562`) - real `[HSS].[uspGetObservations]`.</summary>
    [HttpGet("observations")]
    public async Task<IActionResult> GetObservations(string? system, string? pho, string? patientId, string? encounterId, string? conceptId, CancellationToken ct) =>
        RootOrFail(await _mediator.Send(new KaroObservationsQuery(system, pho, patientId, encounterId, conceptId, GetAuthorizationToken()), ct), "Screening");

    /// <summary>Legacy: `GET GetProvider(...)` (`APIController.cs:617`) - real `[HSS].[uspGetProvider]`, only the first row is ever returned.</summary>
    [HttpGet("provider")]
    public async Task<IActionResult> GetProvider(string? system, string? pho, string? patientId, string? encounterId, string? userId, CancellationToken ct) =>
        RootOrFail(await _mediator.Send(new KaroProviderQuery(system, pho, patientId, encounterId, userId, GetAuthorizationToken()), ct), "Provider");

    /// <summary>Legacy: `GET GetRecallCategories(...)` (`APIController.cs:675`) - real `[HSS].[uspGetRecallCategories]`.</summary>
    [HttpGet("recallcategories")]
    public async Task<IActionResult> GetRecallCategories(string? system, string? pho, string? patientId, string? encounterId, string group, CancellationToken ct) =>
        RootOrFail(await _mediator.Send(new KaroRecallCategoriesQuery(system, pho, patientId, encounterId, group, GetAuthorizationToken()), ct), "RecallCategories");

    /// <summary>Legacy: `GET GetEncounterSummary(...)` (`APIController.cs:407`) - real, genuinely hardcoded stub responses, reproduced verbatim.</summary>
    [HttpGet("encountersummary")]
    public async Task<IActionResult> GetEncounterSummary(string? system, string? pho, string? patientId, string? encounterId, string? identifier, CancellationToken ct)
    {
        var result = await _mediator.Send(new KaroEncounterSummaryQuery(system, pho, patientId, encounterId, identifier, GetAuthorizationToken()), ct);
        return Content(result.Json, "application/json");
    }

    /// <summary>Legacy: `GET GetRecalls(...)` (`APIController.cs:731`) - real `[HSS].[uspGetRecalls]`.</summary>
    [HttpGet("recalls")]
    public async Task<IActionResult> GetRecalls(string? system, string? pho, string? patientId, string? encounterId, CancellationToken ct) =>
        RootOrFail(await _mediator.Send(new KaroRecallsQuery(system, pho, patientId, encounterId, GetAuthorizationToken()), ct), "Recalls");

    /// <summary>Legacy: `GET GetScreeningCodes(...)` (`APIController.cs:786`) - real `[HSS].[uspGetScreeningCodes]` (practiceId literal "6", real legacy constant).</summary>
    [HttpGet("screeningcodes")]
    public async Task<IActionResult> GetScreeningCodes(string? system, string? pho, string? patientId, string? encounterId, CancellationToken ct) =>
        RootOrFail(await _mediator.Send(new KaroScreeningCodesQuery(system, pho, patientId, encounterId, GetAuthorizationToken()), ct), "ScreeningCodes");

    /// <summary>Legacy: `GET GetPatientAttachment(...)` (`APIController.cs:1193`) - real `[HSS].[uspGetPatientDMS]`.</summary>
    [HttpGet("patientattachment")]
    public async Task<IActionResult> GetPatientAttachment(string? system, string? pho, string? patientId, string? encounterId, string? referenceID, string? sortOrder, string? subject, DateTime? dateFrom, DateTime? dateTo, CancellationToken ct) =>
        RootOrFail(await _mediator.Send(new KaroPatientAttachmentQuery(system, pho, patientId, encounterId, referenceID, sortOrder, subject, dateFrom, dateTo, GetAuthorizationToken()), ct), "PatientDMS");

    /// <summary>Legacy: `POST SaveClinicalNotes()` (`APIController.cs:841`) - real `[HSS].[uspInsertUpdateConsultNotes]`.</summary>
    [HttpPost("clinicalnotes")]
    public async Task<IActionResult> SaveClinicalNotes([FromBody] KaroConsultNoteRequest body, CancellationToken ct) =>
        WriteResult(await _mediator.Send(new KaroSaveClinicalNotesCommand(body.PatientId, body.EncounterId, body.UserId, body.SubjectiveNotes, body.ObjectiveNotes, body.Assessment, body.Plans, GetAuthorizationToken()), ct));

    /// <summary>Legacy: `POST SaveCondition()` (`APIController.cs:906`) - real `[HSS].[uspInsertUpdateDiagnosis]`.</summary>
    [HttpPost("conditions")]
    public async Task<IActionResult> SaveCondition([FromBody] KaroConditionRequest body, CancellationToken ct) =>
        WriteResult(await _mediator.Send(new KaroSaveConditionCommand(body.PatientId, body.EncounterId, body.UserId, body.Type, body.OnSetDate, body.Summary, body.IsLongTerm, body.ConceptId, body.Name, body.FSN, GetAuthorizationToken()), ct));

    /// <summary>Legacy: `POST SaveInvoice()` (`APIController.cs:1695`) - real `[HSS].[uspInsertUpdateService]`.</summary>
    [HttpPost("invoice")]
    public async Task<IActionResult> SaveInvoice([FromBody] KaroInvoiceRequest body, CancellationToken ct) =>
        WriteResult(await _mediator.Send(new KaroSaveInvoiceCommand(body.PatientId, body.EncounterId, body.UserId, body.Name, body.Code, body.Fee, body.payee, GetAuthorizationToken()), ct));

    /// <summary>Legacy: `POST SaveObservations()` (`APIController.cs:1771`) - real `[HSS].[uspInsertUpdateObservation]`.</summary>
    [HttpPost("observations")]
    public async Task<IActionResult> SaveObservations([FromBody] KaroObservationsRequest body, CancellationToken ct) =>
        WriteResult(await _mediator.Send(new KaroSaveObservationsCommand(body.PatientId, body.EncounterId, body.UserId, body.Temperature, body.WaistCircumference, body.Height, body.Weight, body.BPSys, body.BPDia, body.HeartRate, body.Notes, body.Risk, body.Framingham, GetAuthorizationToken()), ct));

    /// <summary>Legacy: `POST SaveRecall()` (`APIController.cs:1856`) - real `[HSS].[uspInsertUpdateRecall]`.</summary>
    [HttpPost("recalls")]
    public async Task<IActionResult> SaveRecall([FromBody] KaroRecallRequest body, CancellationToken ct) =>
        WriteResult(await _mediator.Send(new KaroSaveRecallCommand(body.PatientId, body.EncounterId, body.UserId, body.Priority, body.Group, body.DueDate, body.Notes, body.CategoryId, GetAuthorizationToken()), ct));

    /// <summary>Legacy: `POST SaveScreeningCode()` (`APIController.cs:1925`) - a real, genuine no-op: never validates the token, never touches the DB, always returns success. Reproduced exactly, not "implemented for real".</summary>
    [HttpPost("screeningcodes")]
    public IActionResult SaveScreeningCode() => Ok(new { status = "success", message = "" });

    /// <summary>Legacy: `POST SaveDocument()` (`APIController.cs:1625`) - real DMS write + real Indici document insert. `messageData` is base64 in the JSON body, matching legacy's byte[] binding.</summary>
    [HttpPost("document")]
    public async Task<IActionResult> SaveDocument([FromBody] KaroDocumentRequest body, CancellationToken ct) =>
        WriteResult(await _mediator.Send(new KaroSaveDocumentCommand(body.PatientId, body.EncounterId, body.MessageData, body.ContentType, body.MessageSubject, body.ItemType, GetAuthorizationToken()), ct));

    /// <summary>
    /// Legacy: `POST SaveSummary()` (`APIController.cs:978`) - dynamic JSON body (`patientId`,
    /// `encounterID`, `system`, `identifier`, `providerID`, `dateTimeRecorded`, `entry[]`), matched
    /// case-insensitively exactly as legacy's own manual `JObject` property scan does.
    /// </summary>
    [HttpPost("summary")]
    public async Task<IActionResult> SaveSummary([FromBody] JsonElement body, CancellationToken ct)
    {
        string? patientId = null, encounterId = null, system = null, identifier = null, providerId = null, dateTimeRecorded = null;
        var entriesJson = "[]";

        foreach (var property in body.EnumerateObject())
        {
            var name = property.Name.Trim();
            if (string.Equals(name, "patientId", StringComparison.OrdinalIgnoreCase))
            {
                patientId = property.Value.GetString();
            }
            else if (string.Equals(name, "encounterID", StringComparison.OrdinalIgnoreCase))
            {
                encounterId = property.Value.GetString();
            }
            else if (string.Equals(name, "system", StringComparison.OrdinalIgnoreCase))
            {
                system = property.Value.GetString();
            }
            else if (string.Equals(name, "identifier", StringComparison.OrdinalIgnoreCase))
            {
                identifier = property.Value.GetString();
            }
            else if (string.Equals(name, "providerID", StringComparison.OrdinalIgnoreCase))
            {
                providerId = property.Value.GetString();
            }
            else if (string.Equals(name, "dateTimeRecorded", StringComparison.OrdinalIgnoreCase))
            {
                dateTimeRecorded = property.Value.GetString();
            }
            else if (string.Equals(name, "entry", StringComparison.OrdinalIgnoreCase))
            {
                entriesJson = property.Value.GetRawText();
            }
        }

        return WriteResult(await _mediator.Send(new KaroSaveSummaryCommand(patientId, encounterId, system, identifier, providerId, dateTimeRecorded, entriesJson, GetAuthorizationToken()), ct));
    }

    private IActionResult WriteResult(KaroWriteResult result) =>
        result.Succeeded
            ? Ok(new { status = "success", message = result.SuccessMessage ?? string.Empty })
            : Ok(KaroFailResponse.Of(result.ErrorMessage ?? "Invalid token value!"));

    private IActionResult RootOrFail<T>(Application.Features.Karo.Queries.KaroListResult<T> result, string resourceType) =>
        result.Succeeded
            ? Ok(new KaroRootResponse<T>(result.PatientId, resourceType, "hss", result.Entries ?? []))
            : Ok(KaroFailResponse.Of(result.ErrorMessage ?? "Invalid token value!"));

    /// <summary>Legacy: `GetAuthorizationToken()` (`APIController.cs:1976`) - reads the `Bearer` token from the `Authorization` header.</summary>
    private string? GetAuthorizationToken()
    {
        var value = Request.Headers.Authorization.ToString();
        return string.IsNullOrEmpty(value) ? null : value.Replace("Bearer", string.Empty).Trim();
    }
}
