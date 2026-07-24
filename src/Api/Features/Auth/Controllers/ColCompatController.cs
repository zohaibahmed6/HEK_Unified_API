using System.Runtime.CompilerServices;
using System.Text.Json;
using HekCoreApi.Adapters.Erms.Col;
using HekCoreApi.Api.Telemetry;
using HekCoreApi.Application.Features.Col;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace HekCoreApi.Api.Features.Auth.Controllers;

/// <summary>
/// COL/Pegasus's real Claim Online endpoints, ported from the real `COLController.cs`
/// (`legacy-reference/ermsapi/DevLocal/ERMSWebAPI/ERMSWebAPI/Controllers/COLController.cs` - the
/// previously "undocumented" module, now confirmed from source). All responses are HTTP 200
/// `application/json`; on any error the body is the RAW error text (not a JSON envelope) - a real
/// legacy quirk. Lives in-process with ERMS exactly as legacy does, but shares no state with HISO/KARO.
/// </summary>
[ApiController]
[Route("erms/col")]
public sealed class ColCompatController : ControllerBase
{
    private static readonly JsonSerializerOptions LegacyJson = new(); // Newtonsoft defaults: exact property names, nulls included.

    private readonly IMediator _mediator;
    private readonly ILogger<ColCompatController> _logger;
    private readonly LegacyOperationObserver _observer;

    public ColCompatController(IMediator mediator, ILogger<ColCompatController> logger, LegacyOperationObserver observer)
    {
        _mediator = mediator;
        _logger = logger;
        _observer = observer;
    }

    /// <summary>Legacy: `POST Authenticate()` (`COLController.cs:24`) - JSON Credential body (confirmed real shape), `UserAuthenticate` JSON reply with an `error` field, always HTTP 200. No base64 step (unlike ERMS).</summary>
    [HttpPost("authenticate")]
    public async Task<IActionResult> Authenticate([FromBody] ColCredential credential, CancellationToken ct)
    {
        var result = await _mediator.Send(new ColAuthenticateQuery(credential.Username, credential.Password, credential.PatientId, credential.EncounterId), ct);
        if (!string.IsNullOrEmpty(result.Error))
        {
            _observer.RecordExpectedFailure(_logger, "col", nameof(Authenticate), result.Error, new Dictionary<string, object?> { ["PatientId"] = credential.PatientId, ["EncounterId"] = credential.EncounterId });
        }

        var reply = new ColUserAuthenticate { Token = result.Token, Expiry = result.Expiry, PracticeId = result.PracticeId, error = result.Error };
        return Json(JsonSerializer.Serialize(reply, LegacyJson));
    }

    /// <summary>Legacy: `GET GetCurrentPatientData` (`COLController.cs:95`) - `[OnlineClaim].[uspGetPatientData]`, list JSON (or one empty object).</summary>
    [HttpGet("GetCurrentPatientData")]
    public async Task<IActionResult> GetCurrentPatientData(string? pmsPatientId, string? pmsEncounterId, CancellationToken ct)
    {
        var result = await _mediator.Send(new ColGetCurrentPatientDataQuery(pmsPatientId, pmsEncounterId, GetAuthorizationToken()), ct);
        return await RenderList<ColPatientData>(result);
    }

    /// <summary>Legacy: `GET GetSessionData` (`COLController.cs:147`) - real legacy BUG preserved: `PHCO.GetSessionData` executes an EMPTY stored-procedure name, so this always returns the SQL error text.</summary>
    [HttpGet("GetSessionData")]
    public async Task<IActionResult> GetSessionData(string? pmsPatientId, string? pmsEncounterId, CancellationToken ct)
    {
        var result = await _mediator.Send(new ColGetSessionDataQuery(pmsPatientId, pmsEncounterId, GetAuthorizationToken()), ct);
        return await RenderList<ColSessionData>(result);
    }

    /// <summary>Legacy: `GET GetProviderData` (`COLController.cs:200`) - `[OnlineClaim].[uspGetProvider]`.</summary>
    [HttpGet("GetProviderData")]
    public async Task<IActionResult> GetProviderData(string? pmsPatientId, string? pmsEncounterId, CancellationToken ct)
    {
        var result = await _mediator.Send(new ColGetProviderDataQuery(pmsPatientId, pmsEncounterId, GetAuthorizationToken()), ct);
        return await RenderList<ColProviderData>(result);
    }

    /// <summary>Legacy: `GET GetSurgeryData` (`COLController.cs:251`) - `[OnlineClaim].[uspGetSurgeryData]`, optional raw `LocationId`.</summary>
    [HttpGet("GetSurgeryData")]
    public async Task<IActionResult> GetSurgeryData(string? pmsPatientId, string? pmsEncounterId, string LocationId = "", CancellationToken ct = default)
    {
        var result = await _mediator.Send(new ColGetSurgeryDataQuery(pmsPatientId, pmsEncounterId, LocationId, GetAuthorizationToken()), ct);
        return await RenderList<ColSurgeryData>(result);
    }

    /// <summary>Legacy: `GET GetDiagnosisData` (`COLController.cs:303`) - `[OnlineClaim].[uspGetConditions]`; `pmsOrder` passed raw (no ToUpper - real COL quirk).</summary>
    [HttpGet("GetDiagnosisData")]
    public async Task<IActionResult> GetDiagnosisData(string? pmsPatientId, string? pmsEncounterId, string pmsOrder = "desc",
        string pmsMinDateTime = "", string pmsMaxDateTime = "", CancellationToken ct = default)
    {
        var result = await _mediator.Send(new ColGetDiagnosisDataQuery(pmsPatientId, pmsEncounterId, pmsOrder, pmsMinDateTime, pmsMaxDateTime, GetAuthorizationToken()), ct);
        return await RenderList<ColDiagnosisData>(result);
    }

    /// <summary>
    /// Legacy: `POST SaveInvoice()` (`COLController.cs:360`) - `[OnlineClaim].[uspInsertUpdateService]`,
    /// masterServiceName "COL". Sentinels preserved exactly, including the real BROKEN JSON emitted for a
    /// duplicate invoice (`-3`): `{"status":"success","message":" Invoice Already exist.}` (missing quote).
    /// </summary>
    [HttpPost("SaveInvoice")]
    public async Task<IActionResult> SaveInvoice([FromBody] ColSaveInvoiceRequest invoice, CancellationToken ct)
    {
        var result = await _mediator.Send(new ColSaveInvoiceCommand(invoice.PatientID, invoice.AccountHolderID, invoice.EncounterID,
            invoice.ServiceName, invoice.ServiceCode, invoice.AmountInclGST, invoice.Description, invoice.Payee,
            invoice.ServiceProvider, invoice.ServiceProviderType, invoice.ServiceDate, invoice.PegasusReference,
            invoice.ClaimShortCode, GetAuthorizationToken()), ct);

        if (!string.IsNullOrEmpty(result.Error))
        {
            _observer.RecordExpectedFailure(_logger, "col", nameof(SaveInvoice), result.Error, new Dictionary<string, object?> { ["PatientId"] = invoice.PatientID, ["EncounterId"] = invoice.EncounterID });
            return Json(result.Error);
        }

        return Json(result.ServiceMappingId == -3
            ? "{\"status\":\"success\",\"message\":\" Invoice Already exist.}"
            : "{\"status\":\"success\",\"message\":\"" + result.ServiceMappingId + "\"}");
    }

    /// <summary>Legacy list shape: serialize the whole list when it has rows, else ONE empty object; the mapper's null return NREs on `.Count` exactly like legacy (message becomes the body).</summary>
    private async Task<IActionResult> RenderList<T>(ColReadResult result, [CallerMemberName] string endpoint = "") where T : class, new()
    {
        var body = string.Empty;
        var error = result.Error;

        if (result.Succeeded)
        {
            var (mapped, mapError) = await _observer.ObserveSwallowedAsync(_logger, "col", endpoint, new Dictionary<string, object?>(), () =>
            {
                var list = ColDataTableMapper.ToList<T>(result.Table!);
                var serialized = list!.Count > 0
                    ? JsonSerializer.Serialize(list, LegacyJson)
                    : JsonSerializer.Serialize(new T(), LegacyJson);
                return Task.FromResult(serialized);
            });
            body = mapped ?? string.Empty;
            error = mapError ?? error;
        }
        else if (!string.IsNullOrEmpty(error))
        {
            _observer.RecordExpectedFailure(_logger, "col", endpoint, error, new Dictionary<string, object?>());
        }

        return Json(string.IsNullOrEmpty(error) ? body : error);
    }

    /// <summary>Legacy: always HTTP 200, `application/json` content type - even when the body is a raw error string.</summary>
    private ContentResult Json(string body) =>
        new() { Content = body, ContentType = "application/json; charset=utf-8", StatusCode = StatusCodes.Status200OK };

    /// <summary>Legacy: Authorization header with "Bearer" stripped.</summary>
    private string GetAuthorizationToken() =>
        (Request.Headers.Authorization.ToString() ?? string.Empty).Replace("Bearer", string.Empty).Trim();
}
