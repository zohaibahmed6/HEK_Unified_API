using HekCoreApi.Contracts.Security;

namespace HekCoreApi.Contracts.Screening;

/// <summary>
/// KARO-only canonical resource - confirmed no matching HISO concept exists and ERMS has no
/// screening-codes operation (both genuine gaps). Legacy KARO `practiceId` param for this operation
/// is always the hardcoded literal "6", not derived from the request - reproduced as-is here too,
/// same as the legacy compat endpoint.
/// </summary>
public sealed record ScreeningCodeCanonical(
    string? ConceptId,
    string? ScreeningShortName,
    string? ScreeningName,
    OriginScope Source);
