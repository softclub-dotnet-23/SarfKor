using Application.Abstractions;
using Application.Common;
using Domain.Catalog;

namespace Application.Catalog.Commands.CreateBrand;

public sealed class CreateBrandCommandHandler(
    IBrandRepository brandRepository,
    IUnitOfWork unitOfWork) : ICommandHandler<CreateBrandCommand, CreateBrandResult>
{
    public async Task<CreateBrandResult> Handle(CreateBrandCommand command, CancellationToken cancellationToken)
    {
        // Any StorePartner can self-service-create a brand — this is the fast-path guard against
        // two of them creating "Coca-Cola" within the same request window; the unique index on
        // Brand.Name is the actual backstop against the remaining race.
        if (await brandRepository.ExistsByNameAsync(command.Name, cancellationToken))
            return new CreateBrandResult(CreateBrandOutcome.AlreadyExists, null);

        var brand = new Brand { Name = command.Name };
        brandRepository.Add(brand);
        await unitOfWork.SaveChangesAsync(cancellationToken);
        return new CreateBrandResult(CreateBrandOutcome.Created, brand.Id);
    }
}
