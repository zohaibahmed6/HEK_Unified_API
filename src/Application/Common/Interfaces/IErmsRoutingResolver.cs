using HekCoreApi.Application.Common.Models;

namespace HekCoreApi.Application.Common.Interfaces;

/// <summary>
/// Builds a <see cref="RoutingContext"/> from ERMS's real <c>EncounterId</c> format
/// (<c>{encId}__{practiceId}__{practiceCode}__{environment}</c>). New code for the canonical
/// routing path only - separate from <c>IErmsRequestParser</c>, which keeps its legacy "4th segment
/// overwrites the routing key" quirk untouched for <c>/erms/*</c>.
/// </summary>
public interface IErmsRoutingResolver
{
    RoutingContext Resolve(string encounterId);
}
