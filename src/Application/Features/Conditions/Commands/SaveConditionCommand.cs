using HekCoreApi.Application.Common.Idempotency;
using HekCoreApi.Application.Common.Interfaces;
using HekCoreApi.Contracts.Conditions;
using MediatR;

namespace HekCoreApi.Application.Features.Conditions.Commands;

public sealed record SaveConditionCommand(int PatientId, int EncounterId, string PracticeId, ConditionInput Input, string? IdempotencyKey)
    : IRequest<IdempotencyOutcome<Condition>>;

/// <summary>
/// Implements the shared idempotency contract (Contract Design doc Section 12): an Idempotency-Key
/// header match, OR the natural key (same diagnosis code + appointment, KARO-BR-12), returns the
/// existing resource instead of creating a duplicate.
/// </summary>
public sealed class SaveConditionCommandHandler : IRequestHandler<SaveConditionCommand, IdempotencyOutcome<Condition>>
{
    private readonly IConditionsRepository _repository;
    private readonly IIdempotencyStore _idempotencyStore;

    public SaveConditionCommandHandler(IConditionsRepository repository, IIdempotencyStore idempotencyStore)
    {
        _repository = repository;
        _idempotencyStore = idempotencyStore;
    }

    public async Task<IdempotencyOutcome<Condition>> Handle(SaveConditionCommand request, CancellationToken cancellationToken)
    {
        if (!string.IsNullOrEmpty(request.IdempotencyKey))
        {
            var key = IdempotencyKeyBuilder.Build("conditions", request.PracticeId, request.PatientId.ToString(), request.EncounterId.ToString(), request.IdempotencyKey);
            var cached = await _idempotencyStore.TryGetAsync<Condition>(key, cancellationToken);
            if (cached is not null)
            {
                return new IdempotencyOutcome<Condition>(true, cached);
            }
        }

        var existing = await _repository.FindByNaturalKeyAsync(request.EncounterId, request.PracticeId, request.Input.DiagnosisCode, cancellationToken);
        if (existing is not null)
        {
            return new IdempotencyOutcome<Condition>(true, existing);
        }

        var created = await _repository.SaveAsync(request.PatientId, request.EncounterId, request.PracticeId, request.Input, cancellationToken);

        if (!string.IsNullOrEmpty(request.IdempotencyKey))
        {
            var key = IdempotencyKeyBuilder.Build("conditions", request.PracticeId, request.PatientId.ToString(), request.EncounterId.ToString(), request.IdempotencyKey);
            await _idempotencyStore.SetAsync(key, created, cancellationToken);
        }

        return new IdempotencyOutcome<Condition>(false, created);
    }
}
