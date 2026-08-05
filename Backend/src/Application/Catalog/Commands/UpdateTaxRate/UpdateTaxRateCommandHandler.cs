using System.Text.Json;
using Application.Abstractions;
using Application.Common;
using Domain.Auditing;
using Domain.Catalog;

namespace Application.Catalog.Commands.UpdateTaxRate;

public sealed class UpdateTaxRateCommandHandler(
    ITaxRateRepository taxRateRepository,
    ICategoryRepository categoryRepository,
    IAuditLogRepository auditLogRepository,
    IUnitOfWork unitOfWork) : ICommandHandler<UpdateTaxRateCommand, UpdateTaxRateResult>
{
    public async Task<UpdateTaxRateResult> Handle(UpdateTaxRateCommand command, CancellationToken cancellationToken)
    {
        var taxRate = await taxRateRepository.GetByIdAsync(command.TaxRateId, cancellationToken);
        if (taxRate is null)
            return new UpdateTaxRateResult(UpdateTaxRateOutcome.NotFound);

        if (command.CategoryId.HasValue && !await categoryRepository.ExistsAsync(command.CategoryId.Value, cancellationToken))
            return new UpdateTaxRateResult(UpdateTaxRateOutcome.CategoryNotFound);

        var before = JsonSerializer.Serialize(new { name = taxRate.Name, percentage = taxRate.Percentage, categoryId = taxRate.CategoryId });

        taxRate.Name = command.Name;
        taxRate.Percentage = command.Percentage;
        taxRate.CategoryId = command.CategoryId;
        taxRate.EffectiveFrom = command.EffectiveFrom;
        taxRate.EffectiveTo = command.EffectiveTo;

        var after = JsonSerializer.Serialize(new { name = taxRate.Name, percentage = taxRate.Percentage, categoryId = taxRate.CategoryId });

        auditLogRepository.Add(new AuditLog
        {
            PerformedByUserId = command.PerformedByUserId,
            Action = "TaxRate.Updated",
            EntityType = nameof(TaxRate),
            EntityId = taxRate.Id,
            IpAddress = command.PerformedByIpAddress,
            BeforeStateJson = before,
            AfterStateJson = after,
            OccurredAt = DateTimeOffset.UtcNow
        });

        await unitOfWork.SaveChangesAsync(cancellationToken);

        return new UpdateTaxRateResult(UpdateTaxRateOutcome.Updated);
    }
}
