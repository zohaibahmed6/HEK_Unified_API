using System.Data;
using HekCoreApi.Application.Common.Interfaces;
using MediatR;

namespace HekCoreApi.Application.Features.Erms.Queries;

public sealed record ErmsGetPatientDataQuery(string? PatientId, string? EncounterId, string? BearerToken)
    : IRequest<ErmsGetPatientDataResult>;

public sealed record ErmsGetPatientDataResult(bool Succeeded, DataTable? Table, string? ErrorMessage);

/// <summary>
/// Ported from ERMS's `GetPatientData` (`APIController.cs:856`) - base64-decode both params, real
/// `"__"/"_"` encounter split (4th segment overwrites practiceid), decrypt patientId, bearer-token
/// validation via `[HSS].[uspInsertAndValidateToken]`, then `[HSS].[uspGetDemographics]`. Returns the
/// raw pipe-delimited DataTable; the `ERMSDataTableToListHiso` mapping + XML envelope live in the
/// controller/adapter layer. Legacy: invalid token with blank error =&gt; "Invalid token value!".
/// </summary>
public sealed class ErmsGetPatientDataQueryHandler : IRequestHandler<ErmsGetPatientDataQuery, ErmsGetPatientDataResult>
{
    private readonly IErmsRequestParser _parser;
    private readonly IErmsTokenValidator _tokenValidator;
    private readonly IErmsDemographicsRepository _repository;

    public ErmsGetPatientDataQueryHandler(IErmsRequestParser parser, IErmsTokenValidator tokenValidator, IErmsDemographicsRepository repository)
    {
        _parser = parser;
        _tokenValidator = tokenValidator;
        _repository = repository;
    }

    public async Task<ErmsGetPatientDataResult> Handle(ErmsGetPatientDataQuery request, CancellationToken cancellationToken)
    {
        try
        {
            var (encounterId, practiceSuffix, _, _) = _parser.ParseEncounterId(request.EncounterId);
            var patientId = _parser.Decrypt(_parser.DecodeBase64(request.PatientId));

            var validation = await _tokenValidator.ValidateAsync(practiceSuffix, patientId, encounterId, request.BearerToken, cancellationToken);
            if (!validation.Valid)
            {
                return new ErmsGetPatientDataResult(false, null, validation.ErrorMessage ?? "Invalid token value!");
            }

            var table = await _repository.GetDemographicsAsync(practiceSuffix, patientId, cancellationToken);
            return new ErmsGetPatientDataResult(true, table, null);
        }
        catch (Exception ex)
        {
            // Legacy: the raw exception message becomes the <Error><Message> text.
            return new ErmsGetPatientDataResult(false, null, ex.Message);
        }
    }
}
