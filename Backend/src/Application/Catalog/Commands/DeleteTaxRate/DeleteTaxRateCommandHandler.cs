using Application.Abstractions;
using Application.Common;

namespace Application.Catalog.Commands.DeleteTaxRate;

public sealed class DeleteTaxRateCommandHandler(ITaxRateRepository taxRateRepository, IUnitOfWork unitOfWork)
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
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return new DeleteTaxRateResult(DeleteTaxRateOutcome.Deleted);
    }
}
