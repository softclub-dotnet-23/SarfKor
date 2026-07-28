using Application.Abstractions;
using Application.Common;

namespace Application.Catalog.Commands.UpdateBrand;

public sealed class UpdateBrandCommandHandler(IBrandRepository brandRepository, IUnitOfWork unitOfWork)
    : ICommandHandler<UpdateBrandCommand, UpdateBrandResult>
{
    public async Task<UpdateBrandResult> Handle(UpdateBrandCommand command, CancellationToken cancellationToken)
    {
        var brand = await brandRepository.GetByIdAsync(command.BrandId, cancellationToken);
        if (brand is null)
            return new UpdateBrandResult(UpdateBrandOutcome.NotFound);

        brand.Name = command.Name;
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return new UpdateBrandResult(UpdateBrandOutcome.Updated);
    }
}
