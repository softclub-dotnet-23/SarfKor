using Application.Abstractions;
using Application.Common;
using Domain.Auditing;
using Domain.Catalog;

namespace Application.Catalog.Commands.CreateTaxRate;

public sealed class CreateTaxRateCommandHandler(
    ITaxRateRepository taxRateRepository,
    ICategoryRepository categoryRepository,
    IAuditLogRepository auditLogRepository,
    IUnitOfWork unitOfWork) : ICommandHandler<CreateTaxRateCommand, CreateTaxRateResult>
{
    public async Task<CreateTaxRateResult> Handle(CreateTaxRateCommand command, CancellationToken cancellationToken)
    {
        if (command.CategoryId.HasValue && !await categoryRepository.ExistsAsync(command.CategoryId.Value, cancellationToken))
            return new CreateTaxRateResult(CreateTaxRateOutcome.CategoryNotFound, null);

        var taxRate = new TaxRate
        {
            Name = command.Name,
            Percentage = command.Percentage,
            CategoryId = command.CategoryId,
            EffectiveFrom = command.EffectiveFrom,
            EffectiveTo = command.EffectiveTo
        };
        taxRateRepository.Add(taxRate);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        auditLogRepository.Add(new AuditLog
        {
            PerformedByUserId = command.PerformedByUserId,
            Action = "TaxRate.Created",
            EntityType = nameof(TaxRate),
            EntityId = taxRate.Id,
            Details = $"{command.Name}, {command.Percentage}%",
            IpAddress = command.PerformedByIpAddress,
            OccurredAt = DateTimeOffset.UtcNow
        });
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return new CreateTaxRateResult(CreateTaxRateOutcome.Created, taxRate.Id);
    }
}
