using Application.Abstractions;
using Application.Common;

namespace Application.Catalog.Commands.DeleteBrand;

public sealed class DeleteBrandCommandHandler(IBrandRepository brandRepository, IUnitOfWork unitOfWork)
    : ICommandHandler<DeleteBrandCommand, DeleteBrandResult>
{
    public async Task<DeleteBrandResult> Handle(DeleteBrandCommand command, CancellationToken cancellationToken)
    {
        var brand = await brandRepository.GetByIdAsync(command.BrandId, cancellationToken);
        if (brand is null)
            return new DeleteBrandResult(DeleteBrandOutcome.NotFound);

        if (await brandRepository.IsInUseAsync(command.BrandId, cancellationToken))
            return new DeleteBrandResult(DeleteBrandOutcome.InUse);

        brandRepository.Remove(brand);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return new DeleteBrandResult(DeleteBrandOutcome.Deleted);
    }
}
