using MediatR;

namespace HekCoreApi.Application.Features.Auth.Hiso;

/// <summary>Resolves a HISO SessionGUID (called at <paramref name="ServerAddress"/>) into full context (HISO-BR-01/02, ADR-007).</summary>
public sealed record ResolveHisoSessionQuery(Guid SessionGuid, string ServerAddress) : IRequest<HisoSessionLookupResult>;
