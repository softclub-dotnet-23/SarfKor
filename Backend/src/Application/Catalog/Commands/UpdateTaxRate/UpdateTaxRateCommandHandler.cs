using Application.Abstractions;
using Application.Common;

namespace Application.Catalog.Commands.UpdateTaxRate;

public sealed class UpdateTaxRateCommandHandler(
    ITaxRateRepository taxRateRepository,
    ICategoryRepository categoryRepository,
    IUnitOfWork unitOfWork) : ICommandHandler<UpdateTaxRateCommand, UpdateTaxRateResult>
{
    public async Task<UpdateTaxRateResult> Handle(UpdateTaxRateCommand command, CancellationToken cancellationToken)
    {
        var taxRate = await taxRateRepository.GetByIdAsync(command.TaxRateId, cancellationToken);
        if (taxRate is null)
            return new UpdateTaxRateResult(UpdateTaxRateOutcome.NotFound);

        if (command.CategoryId.HasValue && !await categoryRepository.ExistsAsync(command.CategoryId.Value, cancellationToken))
            return new UpdateTaxRateResult(UpdateTaxRateOutcome.CategoryNotFound);

        taxRate.Name = command.Name;
        taxRate.Percentage = command.Percentage;
        taxRate.CategoryId = command.CategoryId;

        await unitOfWork.SaveChangesAsync(cancellationToken);

        return new UpdateTaxRateResult(UpdateTaxRateOutcome.Updated);
    }
}
