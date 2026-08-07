using Application.Abstractions;
using Application.Common;
using Domain.Auditing;
using Domain.Catalog;

namespace Application.Catalog.Commands.DeleteTaxRate;

public sealed class DeleteTaxRateCommandHandler(ITaxRateRepository taxRateRepository, IAuditLogRepository auditLogRepository, IUnitOfWork unitOfWork)
    : ICommandHandler<DeleteTaxRateCommand, DeleteTaxRateResult>
{
    public async Task<DeleteTaxRateResult> Handle(DeleteTaxRateCommand command, CancellationToken cancellationToken)
    {
        var taxRate = await taxRateRepository.GetByIdAsync(command.TaxRateId, cancellationToken);
        if (taxRate is null)
            return new DeleteTaxRateResult(DeleteTaxRateOutcome.NotFound);

        if (await taxRateRepository.IsInUseAsync(command.TaxRateId, cancellationToken))
            return new DeleteTaxRateResult(DeleteTaxRateOutcome.InUse);

        taxRateRepository.Remove(taxRate);

        auditLogRepository.Add(new AuditLog
        {
            PerformedByUserId = command.PerformedByUserId,
            Action = "TaxRate.Deleted",
            EntityType = nameof(TaxRate),
            EntityId = taxRate.Id,
            Details = taxRate.Name,
            IpAddress = command.PerformedByIpAddress,
            OccurredAt = DateTimeOffset.UtcNow
        });

        await unitOfWork.SaveChangesAsync(cancellationToken);

        return new DeleteTaxRateResult(DeleteTaxRateOutcome.Deleted);
    }
}
