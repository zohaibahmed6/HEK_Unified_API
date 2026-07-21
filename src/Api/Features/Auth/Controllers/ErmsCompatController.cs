using System.Xml;
using System.Xml.Serialization;
using HekCoreApi.Adapters.Erms.Auth;
using HekCoreApi.Adapters.Erms.Hiso;
using HekCoreApi.Application.Common.Interfaces;
using HekCoreApi.Application.Features.Erms.Commands;
using HekCoreApi.Application.Features.Erms.Queries;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace HekCoreApi.Api.Features.Auth.Controllers;

/// <summary>
/// ERMS's real "Indici ERMS Web API" (`legacy-reference/ermsapi/DevLocal/ERMSWebAPI/ERMSWebAPI/Controllers/APIController.cs`,
/// confirmed real source). Reproduces legacy exactly - same XML envelope, always HTTP 200, same real
/// encounterId/patientId decode+decrypt quirks. Deliberately kept fully separate from HISO/KARO - no
/// shared middleware, token validation, or DB routing.
/// </summary>
[ApiController]
[Route("erms")]
public sealed class ErmsCompatController : ControllerBase
{
    private readonly IMediator _mediator;
    private readonly ISecretProvider _secretProvider;

    public ErmsCompatController(IMediator mediator, ISecretProvider secretProvider)
    {
        _mediator = mediator;
        _secretProvider = secretProvider;
    }

    /// <summary>Legacy: `GET /api/Ping` - real reply `&lt;Ping&gt;Success!&lt;/Ping&gt;`.</summary>
    [HttpGet("ping")]
    public IActionResult Ping() =>
        Content("<?xml version=\"1.0\" encoding=\"utf-16\"?><Ping>Success!</Ping>", "application/xml");

    /// <summary>Legacy: `POST Authenticate()` (`APIController.cs:35`) - real `GetBase64Value`+`GetDcrptValue`+`[HSS].[uspInsertAndValidateToken]` pipeline.</summary>
    [HttpPost("authenticate")]
    [Consumes("application/xml", "text/xml", "text/plain")]
    public async Task<IActionResult> Authenticate([FromBody] string rawXml, CancellationToken ct)
    {
        ErmsCredential? credential;
        try
        {
            var serializer = new XmlSerializer(typeof(ErmsCredential));
            using var reader = new StringReader(rawXml);
            credential = (ErmsCredential?)serializer.Deserialize(reader);
        }
        catch (InvalidOperationException)
        {
            credential = null;
        }

        if (credential is null)
        {
            return XmlResult(new ErmsErrorResponse());
        }

        var result = await _mediator.Send(new ErmsAuthenticateQuery(credential.Username, credential.Password, credential.PatientId, credential.EncounterId), ct);

        return result.Succeeded
            ? XmlResult(new ErmsAuthenticationResponse
            {
                Token = result.Token ?? string.Empty,
                Expiry = result.Expiry!.Value.ToString("yyyy-MM-ddTHH:mm:ssz"),
                PracticeId = result.PracticeId ?? string.Empty
            })
            : XmlResult(new ErmsErrorResponse { Message = result.ErrorMessage ?? "Authentication failed!" });
    }

    /// <summary>Legacy: `GET GetPatientData(pmsPatientId, pmsEncounterId)` (`APIController.cs:856`) - bearer-token-validated `[HSS].[uspGetDemographics]` read, `ERMSDataTableToListHiso&lt;PatientData&gt;` mapping, first row (or empty `PatientData`) serialized.</summary>
    [HttpGet("GetPatientData")]
    public async Task<IActionResult> GetPatientData(string? pmsPatientId, string? pmsEncounterId, CancellationToken ct)
    {
        var result = string.Empty;
        var error = string.Empty;

        var queryResult = await _mediator.Send(new ErmsGetPatientDataQuery(pmsPatientId, pmsEncounterId, GetAuthorizationToken()), ct);
        if (queryResult.Succeeded)
        {
            try
            {
                var listPatientData = ErmsDataTableMapper.ToListHiso<PatientData>(queryResult.Table!);

                // Legacy quirk preserved: no null check - a null mapper result NREs here and the
                // exception message becomes the <Error><Message> text, exactly as legacy behaves.
                result = PrepareXml(listPatientData!.Count > 0 ? listPatientData[0] : new PatientData());
            }
            catch (Exception ex)
            {
                error = ex.Message;
            }
        }
        else
        {
            error = queryResult.ErrorMessage ?? string.Empty;
        }

        return SetToXml(result, error);
    }

    /// <summary>Legacy: `GET GetPatientMeasurement(pmsPatientId, pmsEncounterId)` (`APIController.cs:914`) - `[HSS].[uspGetMeasurement]`, first row (or empty `Measurement`) serialized.</summary>
    [HttpGet("GetPatientMeasurement")]
    public async Task<IActionResult> GetPatientMeasurement(string? pmsPatientId, string? pmsEncounterId, CancellationToken ct)
    {
        var queryResult = await _mediator.Send(new ErmsGetPatientMeasurementQuery(pmsPatientId, pmsEncounterId, GetAuthorizationToken()), ct);
        return RenderSingle<Measurement>(queryResult);
    }

    /// <summary>Legacy: `GET GetSmokingStatus(pmsPatientId, pmsEncounterId)` (`APIController.cs:1476`) - `[HSS].[uspGetSmokingStatus]`; AddRange without a null check (legacy NRE quirk on mapper failure).</summary>
    [HttpGet("GetSmokingStatus")]
    public async Task<IActionResult> GetSmokingStatus(string? pmsPatientId, string? pmsEncounterId, CancellationToken ct)
    {
        var queryResult = await _mediator.Send(new ErmsGetSmokingStatusQuery(pmsPatientId, pmsEncounterId, GetAuthorizationToken()), ct);
        return Render(queryResult, table =>
        {
            var envelope = new SmokingStatus();
            envelope.Smoking.AddRange(ErmsDataTableMapper.ToListHiso<Smoking>(table)!);
            return PrepareXml(envelope);
        });
    }

    /// <summary>Legacy: `GET GetCurrentUser(pmsPatientId, pmsEncounterId, LocationId, pmsUserId)` (`APIController.cs:368`) - `[HSS].[uspGetProvider]`, first row (or empty `CurrentUser`) serialized; LocationId/pmsUserId decrypted.</summary>
    [HttpGet("GetCurrentUser")]
    public async Task<IActionResult> GetCurrentUser(string? pmsPatientId, string? pmsEncounterId, string LocationId = "", string pmsUserId = "", CancellationToken ct = default)
    {
        var queryResult = await _mediator.Send(new ErmsGetCurrentUserQuery(pmsPatientId, pmsEncounterId, LocationId, pmsUserId, GetAuthorizationToken()), ct);
        return RenderSingle<CurrentUser>(queryResult);
    }

    /// <summary>Legacy: `GET GetNextOfKin(pmsPatientId, pmsEncounterId)` (`APIController.cs:793`) - `[HSS].[uspGetNextOfKin]`, list envelope with null/count guard.</summary>
    [HttpGet("GetNextOfKin")]
    public async Task<IActionResult> GetNextOfKin(string? pmsPatientId, string? pmsEncounterId, CancellationToken ct)
    {
        var queryResult = await _mediator.Send(new ErmsGetNextOfKinQuery(pmsPatientId, pmsEncounterId, GetAuthorizationToken()), ct);
        return Render(queryResult, table =>
        {
            var envelope = new NextOfKin();
            var list = ErmsDataTableMapper.ToListHiso<PatientNOK>(table);
            if (list is not null && list.Count > 0)
            {
                envelope.PatientNOK.AddRange(list);
            }

            return PrepareXml(envelope);
        });
    }

    /// <summary>Legacy: `GET GetRegisteredPractitioners(pmsPatientId, pmsEncounterId, pmsLocationId)` (`APIController.cs:1270`) - `[HSS].[uspGetRegisteredPractitioners]`; AddRange without a null check (legacy NRE quirk).</summary>
    [HttpGet("GetRegisteredPractitioners")]
    public async Task<IActionResult> GetRegisteredPractitioners(string? pmsPatientId, string? pmsEncounterId, string pmsLocationId = "", CancellationToken ct = default)
    {
        var queryResult = await _mediator.Send(new ErmsGetRegisteredPractitionersQuery(pmsPatientId, pmsEncounterId, pmsLocationId, GetAuthorizationToken()), ct);
        return Render(queryResult, table =>
        {
            var envelope = new RegisteredPractitioners();
            envelope.RegisteredPractitioner.AddRange(ErmsDataTableMapper.ToListHiso<RegisteredPractitioner>(table)!);
            return PrepareXml(envelope);
        });
    }

    /// <summary>Legacy: `GET GetAccidents(...)` (`APIController.cs:126`) - `[HSS].[uspGetACC45]`, date-range list; DiagnosisDescription HTML-strip + order/min/max attribute stamping.</summary>
    [HttpGet("GetAccidents")]
    public async Task<IActionResult> GetAccidents(string? pmsPatientId, string? pmsEncounterId, string pmsOrder = "desc",
        string pmsMinDateTime = "", string pmsMaxDateTime = "", CancellationToken ct = default)
    {
        var queryResult = await _mediator.Send(new ErmsGetAccidentsQuery(pmsPatientId, pmsEncounterId, pmsOrder, pmsMinDateTime, pmsMaxDateTime, GetAuthorizationToken()), ct);
        var dateFormat = await GetDateFormatAsync(ct);
        return Render(queryResult, table =>
        {
            var envelope = new Accidents();
            var list = ErmsDataTableMapper.ToListHiso<Accident>(table);
            if (list is not null && list.Count > 0)
            {
                // Legacy (`APIController.cs:178-179`): strips HTML tags and &nbsp; from the diagnosis description.
                list.ForEach(x => x.DiagnosisDescription.Text = x.DiagnosisDescription.Text is not null
                    ? System.Text.RegularExpressions.Regex.Replace(x.DiagnosisDescription.Text, @"<(.|\n)*?>", "").Replace("&nbsp;", " ")
                    : x.DiagnosisDescription.Text);
                StampDated(list, x => { x.Order = OrderStamp(pmsOrder); }, (x, v) => x.MinDateTime = v, (x, v) => x.MaxDateTime = v, queryResult, dateFormat);
                envelope.Accident.AddRange(list);
            }

            return PrepareXml(envelope);
        });
    }

    /// <summary>Legacy: `GET GetClassifications(...)` (`APIController.cs:205`) - `[HSS].[uspGetConditions]`, `Problems` envelope.</summary>
    [HttpGet("GetClassifications")]
    public async Task<IActionResult> GetClassifications(string? pmsPatientId, string? pmsEncounterId, string pmsOrder = "desc",
        string pmsMinDateTime = "", string pmsMaxDateTime = "", CancellationToken ct = default)
    {
        var queryResult = await _mediator.Send(new ErmsGetClassificationsQuery(pmsPatientId, pmsEncounterId, pmsOrder, pmsMinDateTime, pmsMaxDateTime, GetAuthorizationToken()), ct);
        var dateFormat = await GetDateFormatAsync(ct);
        return Render(queryResult, table =>
        {
            var envelope = new Problems();
            var list = ErmsDataTableMapper.ToListHiso<PatientProblem>(table);
            if (list is not null && list.Count > 0)
            {
                StampDated(list, x => { x.Order = OrderStamp(pmsOrder); }, (x, v) => x.MinDateTime = v, (x, v) => x.MaxDateTime = v, queryResult, dateFormat);
                envelope.Problem.AddRange(list);
            }

            return PrepareXml(envelope);
        });
    }

    /// <summary>Legacy: `GET GetConsultNotes(...)` (`APIController.cs:281`) - `[HSS].[uspGetConsultNotes]`, 24-month default lookback; only the order attribute is stamped (min/max stamping is commented out in legacy).</summary>
    [HttpGet("GetConsultNotes")]
    public async Task<IActionResult> GetConsultNotes(string? pmsPatientId, string? pmsEncounterId, string pmsOrder = "desc",
        string pmsMinDateTime = "", string pmsMaxDateTime = "", CancellationToken ct = default)
    {
        var queryResult = await _mediator.Send(new ErmsGetConsultNotesQuery(pmsPatientId, pmsEncounterId, pmsOrder, pmsMinDateTime, pmsMaxDateTime, GetAuthorizationToken()), ct);
        return Render(queryResult, table =>
        {
            var envelope = new ConsultNotes();
            var list = ErmsDataTableMapper.ToListHiso<Consult>(table);
            if (list is not null && list.Count > 0)
            {
                list.ForEach(x => x.Order = OrderStamp(pmsOrder));
                envelope.Consult.AddRange(list);
            }

            return PrepareXml(envelope);
        });
    }

    /// <summary>Legacy: `GET GetMedicalAllergies(...)` (`APIController.cs:716`) - real proc `[HSS].[uspGetAllergies]`, `MedicalWarnings` envelope.</summary>
    [HttpGet("GetMedicalAllergies")]
    public async Task<IActionResult> GetMedicalAllergies(string? pmsPatientId, string? pmsEncounterId, string pmsOrder = "desc",
        string pmsMinDateTime = "", string pmsMaxDateTime = "", CancellationToken ct = default)
    {
        var queryResult = await _mediator.Send(new ErmsGetMedicalAllergiesQuery(pmsPatientId, pmsEncounterId, pmsOrder, pmsMinDateTime, pmsMaxDateTime, GetAuthorizationToken()), ct);
        var dateFormat = await GetDateFormatAsync(ct);
        return Render(queryResult, table =>
        {
            var envelope = new MedicalWarnings();
            var list = ErmsDataTableMapper.ToListHiso<MedicalWarning>(table);
            if (list is not null && list.Count > 0)
            {
                StampDated(list, x => { x.Order = OrderStamp(pmsOrder); }, (x, v) => x.MinDateTime = v, (x, v) => x.MaxDateTime = v, queryResult, dateFormat);
                envelope.MedicalWarning.AddRange(list);
            }

            return PrepareXml(envelope);
        });
    }

    /// <summary>Legacy: `GET GetPrescribedMedications(...)` (`APIController.cs:972`) - `[HSS].[uspGetMedications]` with `@pIsLongTerm=false`.</summary>
    [HttpGet("GetPrescribedMedications")]
    public async Task<IActionResult> GetPrescribedMedications(string? pmsPatientId, string? pmsEncounterId, string pmsOrder = "desc",
        string pmsMinDateTime = "", string pmsMaxDateTime = "", CancellationToken ct = default)
    {
        var queryResult = await _mediator.Send(new ErmsGetPrescribedMedicationsQuery(pmsPatientId, pmsEncounterId, pmsOrder, pmsMinDateTime, pmsMaxDateTime, GetAuthorizationToken()), ct);
        var dateFormat = await GetDateFormatAsync(ct);
        return Render(queryResult, table =>
        {
            var envelope = new PrescribedMedications();
            var list = ErmsDataTableMapper.ToListHiso<PrescribedMedication>(table);
            if (list is not null && list.Count > 0)
            {
                StampDated(list, x => { x.Order = OrderStamp(pmsOrder); }, (x, v) => x.MinDateTime = v, (x, v) => x.MaxDateTime = v, queryResult, dateFormat);
                envelope.Medication.AddRange(list);
            }

            return PrepareXml(envelope);
        });
    }

    /// <summary>Legacy: `GET GetRegularMedications(...)` (`APIController.cs:1049`) - `[HSS].[uspGetMedications]` with `@pIsLongTerm=true`.</summary>
    [HttpGet("GetRegularMedications")]
    public async Task<IActionResult> GetRegularMedications(string? pmsPatientId, string? pmsEncounterId, string pmsOrder = "desc",
        string pmsMinDateTime = "", string pmsMaxDateTime = "", CancellationToken ct = default)
    {
        var queryResult = await _mediator.Send(new ErmsGetRegularMedicationsQuery(pmsPatientId, pmsEncounterId, pmsOrder, pmsMinDateTime, pmsMaxDateTime, GetAuthorizationToken()), ct);
        var dateFormat = await GetDateFormatAsync(ct);
        return Render(queryResult, table =>
        {
            var envelope = new RegularMedications();
            var list = ErmsDataTableMapper.ToListHiso<RegularMedication>(table);
            if (list is not null && list.Count > 0)
            {
                StampDated(list, x => { x.Order = OrderStamp(pmsOrder); }, (x, v) => x.MinDateTime = v, (x, v) => x.MaxDateTime = v, queryResult, dateFormat);
                envelope.RegularMedication.AddRange(list);
            }

            return PrepareXml(envelope);
        });
    }

    /// <summary>Legacy: `GET GetLaboratoryReportList(...)` (`APIController.cs:572`) - `[HSS].[uspGetLabs]`; `Order` serializes as an element (legacy model quirk - no XmlAttribute).</summary>
    [HttpGet("GetLaboratoryReportList")]
    public async Task<IActionResult> GetLaboratoryReportList(string? pmsPatientId, string? pmsEncounterId, string pmsOrder = "desc",
        string pmsMinDateTime = "", string pmsMaxDateTime = "", CancellationToken ct = default)
    {
        var queryResult = await _mediator.Send(new ErmsGetLaboratoryReportListQuery(pmsPatientId, pmsEncounterId, pmsOrder, pmsMinDateTime, pmsMaxDateTime, GetAuthorizationToken()), ct);
        var dateFormat = await GetDateFormatAsync(ct);
        return Render(queryResult, table =>
        {
            var envelope = new LaboratoryReports();
            var list = ErmsDataTableMapper.ToListHiso<LaboratoryReport>(table);
            if (list is not null && list.Count > 0)
            {
                StampDated(list, x => { x.Order = OrderStamp(pmsOrder); }, (x, v) => x.MinDateTime = v, (x, v) => x.MaxDateTime = v, queryResult, dateFormat);
                envelope.LaboratoryReport.AddRange(list);
            }

            return PrepareXml(envelope);
        });
    }

    /// <summary>Legacy: `GET GetRadiologyReportList(...)` (`APIController.cs:1126`) - `[HSS].[uspGetRads]`.</summary>
    [HttpGet("GetRadiologyReportList")]
    public async Task<IActionResult> GetRadiologyReportList(string? pmsPatientId, string? pmsEncounterId, string pmsOrder = "desc",
        string pmsMinDateTime = "", string pmsMaxDateTime = "", CancellationToken ct = default)
    {
        var queryResult = await _mediator.Send(new ErmsGetRadiologyReportListQuery(pmsPatientId, pmsEncounterId, pmsOrder, pmsMinDateTime, pmsMaxDateTime, GetAuthorizationToken()), ct);
        var dateFormat = await GetDateFormatAsync(ct);
        return Render(queryResult, table =>
        {
            var envelope = new RadiologyReports();
            var list = ErmsDataTableMapper.ToListHiso<RadiologyGroup>(table);
            if (list is not null && list.Count > 0)
            {
                StampDated(list, x => { x.Order = OrderStamp(pmsOrder); }, (x, v) => x.MinDateTime = v, (x, v) => x.MaxDateTime = v, queryResult, dateFormat);
                envelope.RadiologyGroup.AddRange(list);
            }

            return PrepareXml(envelope);
        });
    }

    /// <summary>Legacy: `GET GetDischargeSummaryReportList(...)` (`APIController.cs:428`) - `[HSS].[uspGetOtherDocs]` with `@pType="Discharge Summary"` (non-AWS path).</summary>
    [HttpGet("GetDischargeSummaryReportList")]
    public async Task<IActionResult> GetDischargeSummaryReportList(string? pmsPatientId, string? pmsEncounterId, string pmsOrder = "desc",
        string pmsMinDateTime = "", string pmsMaxDateTime = "", CancellationToken ct = default)
    {
        var queryResult = await _mediator.Send(new ErmsGetDischargeSummaryReportListQuery(pmsPatientId, pmsEncounterId, pmsOrder, pmsMinDateTime, pmsMaxDateTime, GetAuthorizationToken()), ct);
        var dateFormat = await GetDateFormatAsync(ct);
        return Render(queryResult, table =>
        {
            var envelope = new DischargeReports();
            var list = ErmsDataTableMapper.ToListHiso<Group>(table);
            if (list is not null && list.Count > 0)
            {
                StampDated(list, x => { x.Order = OrderStamp(pmsOrder); }, (x, v) => x.MinDateTime = v, (x, v) => x.MaxDateTime = v, queryResult, dateFormat);
                envelope.Group.AddRange(list);
            }

            return PrepareXml(envelope);
        });
    }

    /// <summary>Legacy: `GET GetScannedList(...)` (`APIController.cs:1329`) - `[HSS].[uspGetOtherDocs]` without the type filter (non-AWS path).</summary>
    [HttpGet("GetScannedList")]
    public async Task<IActionResult> GetScannedList(string? pmsPatientId, string? pmsEncounterId, string pmsOrder = "desc",
        string pmsMinDateTime = "", string pmsMaxDateTime = "", CancellationToken ct = default)
    {
        var queryResult = await _mediator.Send(new ErmsGetScannedListQuery(pmsPatientId, pmsEncounterId, pmsOrder, pmsMinDateTime, pmsMaxDateTime, GetAuthorizationToken()), ct);
        var dateFormat = await GetDateFormatAsync(ct);
        return Render(queryResult, table =>
        {
            var envelope = new ScanDocumentReports();
            var list = ErmsDataTableMapper.ToListHiso<ScannedGroup>(table);
            if (list is not null && list.Count > 0)
            {
                StampDated(list, x => { x.Order = OrderStamp(pmsOrder); }, (x, v) => x.MinDateTime = v, (x, v) => x.MaxDateTime = v, queryResult, dateFormat);
                envelope.ScannedGroup.AddRange(list);
            }

            return PrepareXml(envelope);
        });
    }

    /// <summary>Legacy: `GET GetLaboratoryReportDetails(...)` (`APIController.cs:649`) - `[HSS].[uspGetLabResults]`; content RTF-escaped + Base64'd (`ConvertString2RTF(_, true)`).</summary>
    [HttpGet("GetLaboratoryReportDetails")]
    public async Task<IActionResult> GetLaboratoryReportDetails(string? pmsPatientId, string? pmsEncounterId, string? pmsReferenceId, CancellationToken ct)
    {
        var queryResult = await _mediator.Send(new ErmsGetLaboratoryReportDetailsQuery(pmsPatientId, pmsEncounterId, pmsReferenceId, GetAuthorizationToken()), ct);
        return Render(queryResult, table =>
        {
            var envelope = new LaboratoryReportsContent();
            var list = ErmsDataTableMapper.ToListHiso<LaboratoryReportContent>(table);
            if (list!.Count > 0 && !string.IsNullOrWhiteSpace(list[0].Content.Text))
            {
                list[0].Content.Text = ErmsRtfConverter.ConvertString2Rtf(list[0].Content.Text!, true);
                envelope.LaboratoryReportContent.Add(list[0]);
            }
            else
            {
                envelope.LaboratoryReportContent.Add(new LaboratoryReportContent());
            }

            return PrepareXml(envelope);
        });
    }

    /// <summary>Legacy: `GET GetRadiologyReportDetails(...)` (`APIController.cs:1203`) - `[HSS].[uspGetRadResults]`; content RTF-escaped + Base64'd.</summary>
    [HttpGet("GetRadiologyReportDetails")]
    public async Task<IActionResult> GetRadiologyReportDetails(string? pmsPatientId, string? pmsEncounterId, string? pmsReferenceId, CancellationToken ct)
    {
        var queryResult = await _mediator.Send(new ErmsGetRadiologyReportDetailsQuery(pmsPatientId, pmsEncounterId, pmsReferenceId, GetAuthorizationToken()), ct);
        return Render(queryResult, table =>
        {
            var envelope = new RadiologyReportContents();
            var list = ErmsDataTableMapper.ToListHiso<RadiologyReportContent>(table);
            if (list!.Count > 0 && !string.IsNullOrWhiteSpace(list[0].Content.Text))
            {
                list[0].Content.Text = ErmsRtfConverter.ConvertString2Rtf(list[0].Content.Text!, true);
                envelope.RadiologyReportContent.Add(list[0]);
            }
            else
            {
                envelope.RadiologyReportContent.Add(new RadiologyReportContent());
            }

            return PrepareXml(envelope);
        });
    }

    /// <summary>Legacy: `GET GetDischargeSummaryDetails(...)` (`APIController.cs:505`) - `[HSS].[uspGetDocResults]` with `@pIsDischarge=true` (non-AWS path); no RTF conversion.</summary>
    [HttpGet("GetDischargeSummaryDetails")]
    public async Task<IActionResult> GetDischargeSummaryDetails(string? pmsPatientId, string? pmsEncounterId, string? pmsReferenceId, CancellationToken ct)
    {
        var queryResult = await _mediator.Send(new ErmsGetDischargeSummaryDetailsQuery(pmsPatientId, pmsEncounterId, pmsReferenceId, GetAuthorizationToken()), ct);
        return Render(queryResult, table =>
        {
            var envelope = new DischargeSummaryContents();
            var list = ErmsDataTableMapper.ToListHiso<DischargeSummaryContent>(table);
            if (list!.Count > 0 && !string.IsNullOrWhiteSpace(list[0].Content.Text))
            {
                envelope.DischargeSummaryContent.Add(list[0]);
            }
            else
            {
                envelope.DischargeSummaryContent.Add(new DischargeSummaryContent());
            }

            return PrepareXml(envelope);
        });
    }

    /// <summary>Legacy: `GET GetScannedDetails(...)` (`APIController.cs:1408`) - `[HSS].[uspGetDocResults]` with `@pIsDischarge=false` (non-AWS path); mapped into `ScannedGroup`, no RTF conversion.</summary>
    [HttpGet("GetScannedDetails")]
    public async Task<IActionResult> GetScannedDetails(string? pmsPatientId, string? pmsEncounterId, string? pmsReferenceId, CancellationToken ct)
    {
        var queryResult = await _mediator.Send(new ErmsGetScannedDetailsQuery(pmsPatientId, pmsEncounterId, pmsReferenceId, GetAuthorizationToken()), ct);
        return Render(queryResult, table =>
        {
            var envelope = new ScanReportContent();
            var list = ErmsDataTableMapper.ToListHiso<ScannedGroup>(table);
            if (list!.Count > 0 && !string.IsNullOrWhiteSpace(list[0].Content.Text))
            {
                envelope.ScannedGroup.Add(list[0]);
            }
            else
            {
                envelope.ScannedGroup.Add(new ScannedGroup());
            }

            return PrepareXml(envelope);
        });
    }

    /// <summary>
    /// Legacy: `POST SaveDocument()` (`APIController.cs:1535`) - raw `ReferralDocument` XML body.
    /// Different response contract from the Get* ops: any error =&gt; HTTP <b>400</b> with literal body
    /// "BadRequest"; success =&gt; the scrubbed `ReferralDocument` (error/content fields blanked in the
    /// legacy `finally`) serialized back at HTTP 200.
    /// </summary>
    [HttpPost("SaveDocument")]
    [Consumes("application/xml", "text/xml", "text/plain")]
    public async Task<IActionResult> SaveDocument([FromBody] string rawXml, CancellationToken ct)
    {
        var error = string.Empty;
        var objDocument = new ReferralDocument();

        try
        {
            var serializer = new XmlSerializer(typeof(ReferralDocument));
            using var reader = new StringReader(rawXml);
            objDocument = (ReferralDocument)serializer.Deserialize(reader)!;

            var result = await _mediator.Send(new ErmsSaveDocumentCommand(
                objDocument.ID, objDocument.DocumentID, objDocument.PatiendID, objDocument.EncounterID,
                objDocument.ProviderID, objDocument.Type, objDocument.ItemType, objDocument.CreatedDate,
                objDocument.ContentType, objDocument.Content, GetAuthorizationToken()), ct);
            error = result.Error;
        }
        catch (Exception ex)
        {
            error = ex.Message;
        }
        finally
        {
            // Legacy `finally`: scrub the echo payload regardless of outcome.
            objDocument.ErrorText = error;
            objDocument.ContentType = string.Empty;
            objDocument.DescriptionType = string.Empty;
            objDocument.Description = string.Empty;
            objDocument.Encoding = string.Empty;
            objDocument.Source = string.Empty;
            objDocument.Content = string.Empty;
        }

        if (!string.IsNullOrEmpty(error))
        {
            // Legacy: HttpStatusCode.BadRequest.ToString() == "BadRequest", claimed as application/xml.
            return new ContentResult { Content = "BadRequest", ContentType = "application/xml; charset=utf-8", StatusCode = StatusCodes.Status400BadRequest };
        }

        var responseSerializer = new XmlSerializer(typeof(ReferralDocument));
        using var writer = new StringWriter();
        responseSerializer.Serialize(writer, objDocument);
        return SetToXml(writer.ToString(), string.Empty);
    }

    /// <summary>Legacy single-object shape: `list[0]` else `new T()`, with the real no-null-check NRE quirk on mapper failure.</summary>
    private IActionResult RenderSingle<T>(ErmsReadResult queryResult) where T : class, new() =>
        Render(queryResult, table =>
        {
            var list = ErmsDataTableMapper.ToListHiso<T>(table);
            return PrepareXml(list!.Count > 0 ? list[0] : new T());
        });

    /// <summary>Shared tail of every legacy Get* operation: serialize on success, exception message into the error envelope, always HTTP 200 via `SetToXml`.</summary>
    private static IActionResult Render(ErmsReadResult queryResult, Func<System.Data.DataTable, string> serialize)
    {
        var result = string.Empty;
        var error = string.Empty;

        if (queryResult.Succeeded)
        {
            try
            {
                result = serialize(queryResult.Table!);
            }
            catch (Exception ex)
            {
                error = ex.Message;
            }
        }
        else
        {
            error = queryResult.ErrorMessage ?? string.Empty;
        }

        return SetToXml(result, error);
    }

    /// <summary>Legacy: `pmsOrder.Equals("desc")` - deliberately case-sensitive.</summary>
    private static string OrderStamp(string pmsOrder) => pmsOrder.Equals("desc") ? "dateDescend" : "dateAscend";

    /// <summary>Legacy (`APIController.cs:182-185`): min/max stamped only when parsed to a real date, formatted with `AppSettings["DateFormat"]`.</summary>
    private static void StampDated<T>(List<T> list, Action<T> stampOrder, Action<T, string> setMin, Action<T, string> setMax, ErmsReadResult queryResult, string dateFormat)
    {
        list.ForEach(stampOrder);
        if (!queryResult.MinDate.Equals(DateTime.MinValue))
        {
            list.ForEach(x => setMin(x, queryResult.MinDate.ToString(dateFormat)));
        }

        if (!queryResult.MaxDate.Equals(DateTime.MinValue))
        {
            list.ForEach(x => setMax(x, queryResult.MaxDate.ToString(dateFormat)));
        }
    }

    private async Task<string> GetDateFormatAsync(CancellationToken ct) =>
        await _secretProvider.GetSecretAsync("Erms:DateFormat", ct) ?? string.Empty;

    /// <summary>Legacy: `GetAuthorizationToken()` (`APIController.cs:1701`) - Authorization header with "Bearer" stripped.</summary>
    private string GetAuthorizationToken() =>
        (Request.Headers.Authorization.ToString() ?? string.Empty).Replace("Bearer", string.Empty).Trim();

    /// <summary>Legacy: `PrepareXml&lt;T&gt;` (`APIController.cs:1678`) - XmlSerializer via XmlWriter over a StringWriter (utf-16 declaration).</summary>
    private static string PrepareXml<T>(T objRoot)
    {
        var serializer = new XmlSerializer(typeof(T));
        using var sw = new StringWriter();
        using (var xm = XmlWriter.Create(sw))
        {
            serializer.Serialize(xm, objRoot);
        }

        return sw.ToString();
    }

    /// <summary>Legacy: `SetToXml` (`APIController.cs:1667`) - error wins as `&lt;Error&gt;&lt;Message&gt;`, always HTTP 200, UTF-8 `application/xml` content.</summary>
    private static ContentResult SetToXml(string xmlString, string error)
    {
        if (!string.IsNullOrWhiteSpace(error))
        {
            xmlString = "<?xml version=\"1.0\" encoding=\"utf-16\"?>" +
                        "<Error><Message>" + error + "</Message></Error>";
        }

        return new ContentResult
        {
            Content = xmlString,
            ContentType = "application/xml; charset=utf-8",
            StatusCode = StatusCodes.Status200OK
        };
    }

    private ContentResult XmlResult<T>(T value)
    {
        var serializer = new XmlSerializer(typeof(T));
        using var writer = new StringWriter();
        serializer.Serialize(writer, value);
        return new ContentResult
        {
            Content = writer.ToString(),
            ContentType = "application/xml; charset=utf-16",
            StatusCode = StatusCodes.Status200OK
        };
    }
}
