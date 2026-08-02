using Application.Abstractions;
using Application.Common;
using Domain.Catalog;

namespace Application.Catalog.Commands.CreateTaxRate;

public sealed class CreateTaxRateCommandHandler(
    ITaxRateRepository taxRateRepository,
    ICategoryRepository categoryRepository,
    IUnitOfWork unitOfWork) : ICommandHandler<CreateTaxRateCommand, CreateTaxRateResult>
{
    public async Task<CreateTaxRateResult> Handle(CreateTaxRateCommand command, CancellationToken cancellationToken)
    {
        if (command.CategoryId.HasValue && !await categoryRepository.ExistsAsync(command.CategoryId.Value, cancellationToken))
            return new CreateTaxRateResult(CreateTaxRateOutcome.CategoryNotFound, null);

        var taxRate = new TaxRate { Name = command.Name, Percentage = command.Percentage, CategoryId = command.CategoryId };
        taxRateRepository.Add(taxRate);
        await unitOfWork.SaveChangesAsync(cancellationToken);
        return new CreateTaxRateResult(CreateTaxRateOutcome.Created, taxRate.Id);
    }
}
