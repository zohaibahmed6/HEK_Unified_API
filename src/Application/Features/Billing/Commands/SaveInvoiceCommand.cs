using HekCoreApi.Application.Common.Idempotency;
using HekCoreApi.Application.Common.Interfaces;
using HekCoreApi.Contracts.Billing;
using MediatR;

namespace HekCoreApi.Application.Features.Billing.Commands;

public sealed record SaveInvoiceCommand(int PatientId, string? EncounterId, string PracticeId, InvoiceInput Input, string? IdempotencyKey) : IRequest<IdempotencyOutcome<Invoice>>;

public sealed class SaveInvoiceCommandHandler : IRequestHandler<SaveInvoiceCommand, IdempotencyOutcome<Invoice>>
{
    private readonly IInvoicesRepository _repository;
    private readonly IIdempotencyStore _idempotencyStore;

    public SaveInvoiceCommandHandler(IInvoicesRepository repository, IIdempotencyStore idempotencyStore)
    {
        _repository = repository;
        _idempotencyStore = idempotencyStore;
    }

    public async Task<IdempotencyOutcome<Invoice>> Handle(SaveInvoiceCommand request, CancellationToken cancellationToken)
    {
        if (!string.IsNullOrEmpty(request.IdempotencyKey))
        {
            var key = IdempotencyKeyBuilder.Build("invoices", request.PracticeId, request.PatientId.ToString(), null, request.IdempotencyKey);
            var cached = await _idempotencyStore.TryGetAsync<Invoice>(key, cancellationToken);
            if (cached is not null)
            {
                return new IdempotencyOutcome<Invoice>(true, cached);
            }
        }

        var existing = await _repository.FindByNaturalKeyAsync(request.PatientId, request.PracticeId, request.Input.ServiceCode, request.Input.ServiceDate, cancellationToken);
        if (existing is not null)
        {
            return new IdempotencyOutcome<Invoice>(true, existing);
        }

        var (created, wasDuplicate) = await _repository.SaveAsync(request.PatientId, request.EncounterId, request.PracticeId, request.Input, cancellationToken);

        if (!wasDuplicate && !string.IsNullOrEmpty(request.IdempotencyKey))
        {
            var key = IdempotencyKeyBuilder.Build("invoices", request.PracticeId, request.PatientId.ToString(), null, request.IdempotencyKey);
            await _idempotencyStore.SetAsync(key, created, cancellationToken);
        }

        return new IdempotencyOutcome<Invoice>(wasDuplicate, created);
    }
}
