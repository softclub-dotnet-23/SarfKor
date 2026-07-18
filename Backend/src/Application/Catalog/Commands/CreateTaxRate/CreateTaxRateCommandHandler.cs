using Application.Abstractions;
using Application.Common;
using Domain.Catalog;

namespace Application.Catalog.Commands.CreateTaxRate;

public sealed class CreateTaxRateCommandHandler(
    ITaxRateRepository taxRateRepository,
    IUnitOfWork unitOfWork) : ICommandHandler<CreateTaxRateCommand, CreateTaxRateResult>
{
    public async Task<CreateTaxRateResult> Handle(CreateTaxRateCommand command, CancellationToken cancellationToken)
    {
        var taxRate = new TaxRate { Name = command.Name, Percentage = command.Percentage, CategoryId = command.CategoryId };
        taxRateRepository.Add(taxRate);
        await unitOfWork.SaveChangesAsync(cancellationToken);
        return new CreateTaxRateResult(taxRate.Id);
    }
}
