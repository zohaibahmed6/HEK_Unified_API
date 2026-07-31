using HekCoreApi.Application.Common.Models;

namespace HekCoreApi.Application.Common.Interfaces;

/// <summary>Ported from `HSSDA.GetHSSDemographics` (`HSSDA.cs:1164`) - real proc `[HSS].[uspGetDemographics]`.</summary>
public interface IKaroDemographicsRepository
{
    Task<KaroDemographicsResult> GetAsync(string practiceSuffix, RoutingContext routingContext, string? patientId, CancellationToken ct = default);
}

public sealed record KaroDemographicsResult(KaroDemographicInfo? Demographic, IReadOnlyList<KaroCardInfo> Cards);

/// <summary>Legacy: `Models.APIModels.DemographicInfo` - field names/casing preserved exactly.
/// `EndEnrolmentDate` (real legacy's last field, `APIModels.cs:42`) was confirmed missing from this
/// port entirely (2026-07-31) - a genuine dropped field, not a data issue, since `uspGetDemographics`
/// returns the column and legacy's own reflection mapper (`Utility.DataTableToList&lt;T&gt;`) picks it
/// up automatically by name; this port's equivalent (`KaroDemographicsRepository.Str`) just never had
/// a property to bind it to.</summary>
public sealed record KaroDemographicInfo(
    string? Nhi, string? BirthDate, string? Type, string? TitleCode, string? Given, string? Family, string? Gender,
    int Ethnicity1, int Ethnicity2, int Ethnicity3, string? Quintile, string? Meshblock, string? CellNumber,
    string? DayPhone, string? Email, string? FullAddress, string? EnrolmentStatus, string? SmokingStatus,
    string? Street, string? Suburb, string? City, string? PostCode, string? IsNZResident, string? DateOfEnrolment,
    string? EndEnrolmentDate);

/// <summary>Legacy: `Models.APIModels.CardInfo` - lowercase property names preserved exactly (real wire shape).</summary>
public sealed record KaroCardInfo(string? cardtype, string? startdate, string? expirydate, string? cardnumber);
