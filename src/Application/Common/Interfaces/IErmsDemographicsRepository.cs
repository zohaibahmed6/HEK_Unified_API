using System.Data;
using HekCoreApi.Application.Common.Models;

namespace HekCoreApi.Application.Common.Interfaces;

/// <summary>
/// Ported from ERMS's `HSSDA.GetDemographics(patientId, practiceid, out error)` (`DAL/South/HSSDA.cs:239`) -
/// real `[HSS].[uspGetDemographics]`, single `@pPatientId` param. Returns the raw pipe-delimited
/// (`|&amp;|` / `|?|`) DataTable exactly as legacy does; the `ERMSDataTableToListHiso` mapping happens in
/// the adapter layer (`ErmsDataTableMapper`) so its reflection quirks stay with the XML models.
/// </summary>
public interface IErmsDemographicsRepository
{
    Task<DataTable> GetDemographicsAsync(string practiceSuffix, RoutingContext routingContext, string? patientId, CancellationToken ct = default);
}
